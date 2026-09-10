// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Reorders positioned glyph records from logical order to visual order for one
/// resolved line.
/// </summary>
/// <remarks>
/// Implements rule L2 of the
/// <see href="https://www.unicode.org/reports/tr9/#L2">Unicode Bidirectional Algorithm</see>.
/// </remarks>
internal static class BidiReordering
{
    /// <summary>
    /// Supplies level lookup and range reversal for a glyph storage representation.
    /// Static operations let the JIT specialize the shared loop without delegates,
    /// closures, or interface instances on the shaping hot path.
    /// </summary>
    /// <typeparam name="TState">The reordered storage.</typeparam>
    private interface IBidiReorderingOperations<TState>
    {
        /// <summary>
        /// Gets the resolved embedding level of one glyph.
        /// </summary>
        /// <param name="state">The reordered storage.</param>
        /// <param name="index">The glyph index.</param>
        /// <returns>The resolved embedding level.</returns>
        public static abstract int GetLevel(TState state, int index);

        /// <summary>
        /// Reverses one half-open range while keeping each glyph record intact.
        /// </summary>
        /// <param name="state">The reordered storage.</param>
        /// <param name="start">The first glyph index.</param>
        /// <param name="end">The index after the final glyph.</param>
        public static abstract void Reverse(TState state, int start, int end);
    }

    /// <summary>
    /// Reorders layout run fragments for one line after its final source boundary
    /// is known.
    /// </summary>
    /// <param name="glyphs">The logically ordered layout entries.</param>
    /// <param name="layoutMode">The orientation used to finalize each directional run.</param>
    public static void Reorder(List<GlyphLayoutData> glyphs, LayoutMode layoutMode)
    {
        // Browsers slice each already-visual shaped run by source range before
        // line items are reordered. Composition keeps
        // source-codepoint containers logical for line breaking, so first arrange
        // those complete containers in the same visual order as their shaped run.
        // The positioned glyphs inside each container already retain projected
        // visual order and are never reversed here.
        int fragmentStart = 0;
        while (fragmentStart < glyphs.Count)
        {
            int fragmentEnd = FindFragmentEnd(glyphs, fragmentStart, glyphs.Count);
            if ((glyphs[fragmentStart].BidiRun.Level & 1) != 0)
            {
                if (layoutMode.IsVertical())
                {
                    // The public ShapeRun contract is HarfBuzz-verified for upright
                    // bottom-to-top runs: graphemes reverse, while positioned glyph
                    // order inside each grapheme remains unchanged.
                    glyphs.Reverse(fragmentStart, fragmentEnd - fragmentStart);
                    int graphemeStart = fragmentStart;
                    while (graphemeStart < fragmentEnd)
                    {
                        int graphemeIndex = glyphs[graphemeStart].GraphemeIndex;
                        int graphemeEnd = graphemeStart + 1;
                        while (graphemeEnd < fragmentEnd
                            && glyphs[graphemeEnd].GraphemeIndex == graphemeIndex)
                        {
                            graphemeEnd++;
                        }

                        glyphs.Reverse(graphemeStart, graphemeEnd - graphemeStart);
                        graphemeStart = graphemeEnd;
                    }
                }
                else
                {
                    // Horizontal and mixed-vertical backward runs store HarfBuzz's
                    // complete backward glyph stream: shaped source positions appear
                    // in reverse source order while the glyphs inside one source
                    // position already carry the stream's visual order and offsets.
                    // Every entry is one complete source position holding its slice
                    // of that stream, so reversing entry order alone reproduces the
                    // stored stream exactly and no glyph inside an entry ever moves.
                    // This is the browser's fragment discipline: sliced fragments
                    // stay visual and only whole units reorder.
                    glyphs.Reverse(fragmentStart, fragmentEnd - fragmentStart);
                }
            }

            fragmentStart = fragmentEnd;
        }

        int maximumLevel = 0;
        int minimumOddLevel = int.MaxValue;
        for (fragmentStart = 0; fragmentStart < glyphs.Count;)
        {
            int level = glyphs[fragmentStart].BidiRun.Level;
            maximumLevel = Math.Max(maximumLevel, level);
            if ((level & 1) != 0)
            {
                minimumOddLevel = Math.Min(minimumOddLevel, level);
            }

            fragmentStart = FindFragmentEnd(glyphs, fragmentStart, glyphs.Count);
        }

        if (minimumOddLevel == int.MaxValue)
        {
            return;
        }

        // Browsers perform this step only after line breaking and reorder whole
        // runs rather than characters. Apply UAX #9 L2 to the
        // complete fragments, preserving the visual glyph order within each one.
        for (int level = maximumLevel; level >= minimumOddLevel; level--)
        {
            int sequenceStart = 0;
            while (sequenceStart < glyphs.Count)
            {
                int fragmentEnd = FindFragmentEnd(glyphs, sequenceStart, glyphs.Count);
                while (sequenceStart < glyphs.Count
                    && glyphs[sequenceStart].BidiRun.Level < level)
                {
                    sequenceStart = fragmentEnd;
                    fragmentEnd = sequenceStart < glyphs.Count
                        ? FindFragmentEnd(glyphs, sequenceStart, glyphs.Count)
                        : sequenceStart;
                }

                if (sequenceStart == glyphs.Count)
                {
                    break;
                }

                int sequenceEnd = sequenceStart;
                int fragmentCount = 0;
                while (sequenceEnd < glyphs.Count
                    && glyphs[sequenceEnd].BidiRun.Level >= level)
                {
                    sequenceEnd = FindFragmentEnd(glyphs, sequenceEnd, glyphs.Count);
                    fragmentCount++;
                }

                if (fragmentCount > 1)
                {
                    // Reversing the complete storage range reverses both fragment
                    // order and each fragment's contents. Reverse each now-contiguous
                    // fragment once more to retain its already-visual glyph order.
                    // This moves values in place and needs no per-run owner, copied
                    // glyph array, permutation map, or temporary collection.
                    glyphs.Reverse(sequenceStart, sequenceEnd - sequenceStart);
                    int restoredStart = sequenceStart;
                    while (restoredStart < sequenceEnd)
                    {
                        int restoredEnd = FindFragmentEnd(glyphs, restoredStart, sequenceEnd);
                        glyphs.Reverse(restoredStart, restoredEnd - restoredStart);
                        restoredStart = restoredEnd;
                    }
                }

                sequenceStart = sequenceEnd;
            }
        }
    }

    /// <summary>
    /// Reorders positioned shaping records for one line.
    /// </summary>
    /// <param name="glyphs">The logically ordered shaping records.</param>
    /// <param name="bidiRuns">The resolved bidirectional runs covering the source text.</param>
    /// <param name="bidiMap">The source codepoint to bidirectional-run mapping.</param>
    public static void Reorder(ShapingBuffer glyphs, BidiRun[] bidiRuns, int[] bidiMap)
        => Reorder<ShapingGlyphState, ShapingGlyphOperations>(new ShapingGlyphState(glyphs, bidiRuns, bidiMap), 0, glyphs.Count);

    /// <summary>
    /// Reorders a half-open range of positioned shaping records for one line.
    /// </summary>
    /// <param name="glyphs">The logically ordered shaping records.</param>
    /// <param name="bidiRuns">The resolved bidirectional runs covering the source text.</param>
    /// <param name="bidiMap">The source codepoint to bidirectional-run mapping.</param>
    /// <param name="start">The first glyph record in the line.</param>
    /// <param name="end">The glyph record immediately after the line.</param>
    public static void Reorder(ShapingBuffer glyphs, BidiRun[] bidiRuns, int[] bidiMap, int start, int end)
        => Reorder<ShapingGlyphState, ShapingGlyphOperations>(new ShapingGlyphState(glyphs, bidiRuns, bidiMap), start, end);

    /// <summary>
    /// Applies rule L2 of the Unicode Bidirectional Algorithm to one line.
    /// </summary>
    /// <typeparam name="TState">The reordered storage.</typeparam>
    /// <typeparam name="TOperations">The specialized operations for that storage.</typeparam>
    /// <param name="state">The reordered storage and any level-mapping state it needs.</param>
    /// <param name="start">The first glyph record in the line.</param>
    /// <param name="end">The glyph record immediately after the line.</param>
    private static void Reorder<TState, TOperations>(TState state, int start, int end)
        where TOperations : struct, IBidiReorderingOperations<TState>
    {
        int maximumLevel = 0;
        int minimumOddLevel = int.MaxValue;
        for (int i = start; i < end; i++)
        {
            int level = TOperations.GetLevel(state, i);
            maximumLevel = Math.Max(maximumLevel, level);
            if ((level & 1) != 0)
            {
                minimumOddLevel = Math.Min(minimumOddLevel, level);
            }
        }

        if (minimumOddLevel == int.MaxValue)
        {
            return;
        }

        // UAX #9 rule L2 reverses each maximal contiguous sequence whose level is
        // at least the current level, walking down from the highest resolved level
        // to the lowest odd one. Operating directly on the destination storage
        // keeps every glyph's identity, positioning, and source index together and
        // requires no temporary permutation or per-run allocation.
        for (int level = maximumLevel; level >= minimumOddLevel; level--)
        {
            int sequenceStart = start;
            while (sequenceStart < end)
            {
                while (sequenceStart < end && TOperations.GetLevel(state, sequenceStart) < level)
                {
                    sequenceStart++;
                }

                int sequenceEnd = sequenceStart;
                while (sequenceEnd < end && TOperations.GetLevel(state, sequenceEnd) >= level)
                {
                    sequenceEnd++;
                }

                if (sequenceEnd - sequenceStart > 1)
                {
                    TOperations.Reverse(state, sequenceStart, sequenceEnd);
                }

                sequenceStart = sequenceEnd + 1;
            }
        }
    }

    /// <summary>
    /// Finds the exclusive end of the directional run fragment beginning at the
    /// supplied layout index.
    /// </summary>
    /// <param name="glyphs">The line's layout storage.</param>
    /// <param name="start">The first entry in the fragment.</param>
    /// <param name="end">The exclusive search limit.</param>
    /// <returns>The first entry after the fragment.</returns>
    private static int FindFragmentEnd(List<GlyphLayoutData> glyphs, int start, int end)
    {
        BidiRun bidiRun = glyphs[start].BidiRun;
        int index = start + 1;
        while (index < end && glyphs[index].BidiRun.Equals(bidiRun))
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Holds a shaping buffer together with the source mapping needed to recover
    /// each glyph's resolved embedding level.
    /// </summary>
    private readonly struct ShapingGlyphState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShapingGlyphState"/> struct.
        /// </summary>
        /// <param name="glyphs">The positioned shaping records.</param>
        /// <param name="bidiRuns">The resolved bidirectional runs.</param>
        /// <param name="bidiMap">The source codepoint to bidirectional-run mapping.</param>
        public ShapingGlyphState(ShapingBuffer glyphs, BidiRun[] bidiRuns, int[] bidiMap)
        {
            this.Glyphs = glyphs;
            this.BidiRuns = bidiRuns;
            this.BidiMap = bidiMap;
        }

        /// <summary>
        /// Gets the positioned shaping records.
        /// </summary>
        public ShapingBuffer Glyphs { get; }

        /// <summary>
        /// Gets the resolved bidirectional runs.
        /// </summary>
        public BidiRun[] BidiRuns { get; }

        /// <summary>
        /// Gets the source codepoint to bidirectional-run mapping.
        /// </summary>
        public int[] BidiMap { get; }
    }

    /// <summary>
    /// Adapts positioned shaping records to the shared reordering loop.
    /// </summary>
    private readonly struct ShapingGlyphOperations : IBidiReorderingOperations<ShapingGlyphState>
    {
        /// <inheritdoc/>
        public static int GetLevel(ShapingGlyphState state, int index)
        {
            int codePointIndex = state.Glyphs[index].CodePointIndex;
            return state.BidiRuns[state.BidiMap[codePointIndex]].Level;
        }

        /// <inheritdoc/>
        public static void Reverse(ShapingGlyphState state, int start, int end) => state.Glyphs.ReverseRange(start, end);
    }
}

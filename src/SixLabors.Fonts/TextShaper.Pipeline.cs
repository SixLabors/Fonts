// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// The shaping pipeline: font-run itemization, bidi analysis, glyph population, GSUB
/// substitution, and GPOS positioning. <see cref="TextLayout"/> and the public shaping
/// API both consume this single pipeline; layout composes and positions lines from its
/// output but performs no shaping of its own.
/// </summary>
public static partial class TextShaper
{
    /// <summary>
    /// The pool of reusable pipeline state. A scratch is exclusively owned between
    /// <see cref="ObjectPool{T}.Get"/> and <see cref="ObjectPool{T}.Return"/>, and the
    /// shaped result is copied out by value before the scratch is returned, so nothing
    /// pooled escapes a call. Retained scratch storage stays at its high-water mark.
    /// </summary>
    private static readonly ObjectPool<ShapingScratch> ScratchPool = new(new ShapingScratchPooledObjectPolicy());

    /// <summary>
    /// Resolves the ordered sequence of <see cref="TextRun"/> instances that cover <paramref name="text"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="TextOptions.TextRuns"/> is <see langword="null"/> or empty, a single run covering the entire
    /// grapheme range of <paramref name="text"/> using <see cref="TextOptions.Font"/> is returned. Otherwise the
    /// supplied runs are ordered, gaps are filled with default-font runs, and overlapping ranges are trimmed.
    /// </remarks>
    /// <param name="text">The text to partition into runs.</param>
    /// <param name="options">The text options supplying the default font and optional user-defined runs.</param>
    /// <returns>The resolved runs that together cover the entire grapheme range of <paramref name="text"/>.</returns>
    internal static IReadOnlyList<TextRun> BuildTextRuns(ReadOnlySpan<char> text, TextOptions options)
    {
        int start = 0;
        int end = text.GetGraphemeCount();
        if (end == 0)
        {
            return [];
        }

        if (options.TextRuns is null || options.TextRuns.Count == 0)
        {
            TextRun textRun = new()
            {
                Start = 0,
                End = end,
                Font = options.Font
            };

            textRun.ResolveFontWeight(options.FontWeight);
            return [textRun];
        }

        List<TextRun> textRuns = [];
        foreach (TextRun textRun in options.TextRuns.OrderBy(x => x.Start))
        {
            // Fill gaps within runs.
            if (textRun.Start > start)
            {
                textRuns.Add(new TextRun
                {
                    Start = start,
                    End = textRun.Start,
                    Font = options.Font
                });
            }

            // Add the current run, ensuring the font is not null.
            textRun.Font ??= options.Font;

            if (textRun.Placeholder.HasValue && textRun.End != textRun.Start)
            {
                throw new ArgumentException("Placeholder text runs must be zero-length insertion runs.", nameof(options));
            }

            // Ensure that the previous run does not overlap the current.
            if (textRuns.Count > 0)
            {
                int prevIndex = textRuns.Count - 1;
                TextRun previous = textRuns[prevIndex];
                previous.End = Math.Min(previous.End, textRun.Start);
            }

            textRuns.Add(textRun);
            start = textRun.End;
        }

        // Add a final run if required.
        if (start < end)
        {
            textRuns.Add(new TextRun
            {
                Start = start,
                End = end,
                Font = options.Font
            });
        }

        foreach (TextRun textRun in textRuns)
        {
            textRun.ResolveFontWeight(options.FontWeight);
        }

        return textRuns;
    }

    /// <summary>
    /// Materializes the fallback fonts for the pass. Lives apart from the pipeline
    /// body so shaping without fallbacks never pays for the construction.
    /// </summary>
    /// <param name="options">The text options carrying the fallback families.</param>
    /// <returns>The fallback fonts.</returns>
    private static Font[] BuildFallbackFonts(TextOptions options)
    {
        IReadOnlyList<FontFamily> families = options.FallbackFontFamilies;
        Font[] fonts = new Font[families.Count];
        for (int i = 0; i < fonts.Length; i++)
        {
            fonts[i] = new Font(families[i], options.Font.Size, options.Font.RequestedStyle);
        }

        return fonts;
    }

    /// <summary>
    /// Shapes <paramref name="text"/> into a scope that owns the pooled pipeline
    /// state backing the shaped views.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The text options used while shaping.</param>
    /// <param name="prebuiltRuns">
    /// The resolved text runs when the caller retains run references beyond the
    /// scope, or <see langword="null"/> to let the pass reuse pooled run state.
    /// </param>
    /// <returns>The scoped shaping result.</returns>
    internal static ShapedTextScope ShapeText(
        ReadOnlySpan<char> text,
        TextOptions options,
        IReadOnlyList<TextRun>? prebuiltRuns)
    {
        ShapingScratch scratch = ScratchPool.Get();
        try
        {
            ShapingBuffer shaped = ShapeCore(text, options, scratch, prebuiltRuns, false);
            FinalizeDirectionalRuns(shaped, options.LayoutMode, scratch);
            return new ShapedTextScope(ProjectShapedText(shaped, options.LayoutMode, scratch), scratch);
        }
        catch
        {
            ScratchPool.Return(scratch);
            throw;
        }
    }

    /// <summary>
    /// Runs the shaping pipeline through positioning and the post-positioning
    /// passes, leaving the positioned glyph records in the returned buffer and the
    /// bidi state in the scratch. The projections that copy results out for their
    /// consumers sit on top.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The text options used while shaping.</param>
    /// <param name="scratch">The rented pipeline state.</param>
    /// <param name="prebuiltRuns">
    /// The resolved text runs when the caller retains run references beyond the
    /// scratch scope, or <see langword="null"/> to let the pass reuse scratch-owned
    /// run state.
    /// </param>
    /// <param name="useShapingVerticalOrigin">
    /// Whether synthesized vertical origins follow the public shaping contract
    /// instead of the browser layout contract.
    /// </param>
    /// <returns>The positioned buffer.</returns>
    private static ShapingBuffer ShapeCore(ReadOnlySpan<char> text, TextOptions options, ShapingScratch scratch, IReadOnlyList<TextRun>? prebuiltRuns, bool useShapingVerticalOrigin)
    {
        (ShapingBuffer substitutions, ShapingBuffer positionings) = scratch.Prepare(options);

        // Public shaping and browser-compatible layout use different synthesized
        // vertical Y origins. Select that policy once per pass so mark attachment
        // materializes the right origin without another pass or per-glyph state.
        substitutions.UseShapingVerticalOrigin = useShapingVerticalOrigin;
        positionings.UseShapingVerticalOrigin = useShapingVerticalOrigin;

        // Gather the font and fallbacks.
        Font[] fallbackFonts = (options.FallbackFontFamilies?.Count > 0)
            ? BuildFallbackFonts(options)
            : [];

        // Analyse the text for bidi directional runs.
        BidiAlgorithm bidi = scratch.BidiAlgorithm;
        BidiData bidiData = scratch.BidiData;
        int codePointCount;
        if (options.TextBidiMode == TextBidiMode.Override && options.TextDirection != TextDirection.Auto)
        {
            // An explicitly directed run cannot split into internal bidi runs. Only
            // its extent is needed; character classes, brackets, and paragraph
            // boundaries would be populated and then discarded.
            codePointCount = CodePoint.GetCodePointCount(text);
        }
        else
        {
            bidiData.Init(text, (sbyte)options.TextDirection);
            codePointCount = bidiData.Types.Length;
        }

        scratch.ClearBidiRuns();
        if (options.TextBidiMode == TextBidiMode.Override)
        {
            sbyte runLevel = options.TextDirection == TextDirection.Auto
                ? bidi.ResolveEmbeddingLevel(bidiData.Types)
                : (sbyte)options.TextDirection;
            BidiCharacterType runDirection = runLevel == 1 ? BidiCharacterType.RightToLeft : BidiCharacterType.LeftToRight;

            // A directional-run request is already one higher-level protocol unit.
            // Its contents cannot create internal bidi runs, including separators
            // and explicit controls, so only Auto direction requires inspection.
            scratch.AddBidiRun(new BidiRun(runDirection, runLevel, 0, codePointCount));
        }
        else
        {
            // UAX #9 applies level resolution independently to each paragraph.
            // Keeping a run boundary at every newline also prevents font features
            // and joining behaviour from crossing that protocol boundary.
            ReadOnlySpan<int> paragraphEnds = bidiData.ParagraphEnds;
            int paragraphStart = 0;
            for (int paragraph = 0; paragraph <= paragraphEnds.Length; paragraph++)
            {
                int paragraphEnd = paragraph < paragraphEnds.Length ? paragraphEnds[paragraph] : bidiData.Types.Length;
                int paragraphLength = paragraphEnd - paragraphStart;
                if (paragraphLength == 0)
                {
                    paragraphStart = paragraphEnd;
                    continue;
                }

                // Purely left-to-right text resolves to an even-level run without
                // paying for the full bidirectional algorithm.
                if (options.TextDirection != TextDirection.RightToLeft && bidiData.IsUniformLeftToRight)
                {
                    scratch.AddBidiRun(new BidiRun(BidiCharacterType.LeftToRight, 0, paragraphStart, paragraphLength));
                }
                else
                {
                    ArraySlice<sbyte> paragraphLevels = bidiData.GetTempLevelBuffer(paragraphLength);
                    ArraySlice<BidiCharacterType> paragraphTypes = bidiData.Types.Slice(paragraphStart, paragraphLength);
                    ArraySlice<BidiPairedBracketType> paragraphBracketTypes = bidiData.PairedBracketTypes.Slice(paragraphStart, paragraphLength);
                    ArraySlice<int> paragraphBracketValues = bidiData.PairedBracketValues.Slice(paragraphStart, paragraphLength);
                    bidi.Process(paragraphTypes, paragraphBracketTypes, paragraphBracketValues, (sbyte)options.TextDirection, bidiData.HasBrackets, bidiData.HasEmbeddings, bidiData.HasIsolates, paragraphLevels);

                    AppendBidiRuns(scratch, paragraphLevels, paragraphStart);
                }

                paragraphStart = paragraphEnd;
            }
        }

        BidiRun[] bidiRuns = scratch.BidiRuns;
        int[] bidiMap = scratch.GetBidiMap(codePointCount);

        // Incrementally build out buffer of glyphs. Both buffers share the run list so
        // per-glyph run indices agree when records are seeded across them. Callers
        // retaining run references beyond the scratch scope supply their own runs;
        // otherwise the synthesized whole-text run reuses scratch state.
        bool usesDefaultTextRun = prebuiltRuns is null && !(options.TextRuns?.Count > 0);
        IReadOnlyList<TextRun> textRuns = prebuiltRuns ?? ((options.TextRuns?.Count > 0)
            ? BuildTextRuns(text, options)
            : scratch.GetDefaultTextRuns(options));
        substitutions.SetTextRuns(textRuns);
        positionings.SetTextRuns(textRuns);

        // First do multiple font runs using the individual text runs.
        bool complete = true;
        int textRunIndex = 0;
        int codePointIndex = 0;
        int stringIndex = 0;
        int bidiRunIndex = 0;

        // Single-run fast path: shape and seed one buffer in place and flip it to the
        // positioning role, so no record is copied between buffers. When fallback
        // glyphs remain and fallback fonts exist, fall through to the general
        // cross-buffer seed so the fallback passes can merge into the accumulator.
        ShapingBuffer shaped = positionings;
        if (textRuns.Count == 1 && !textRuns[0].Placeholder.HasValue)
        {
            TextRun onlyRun = textRuns[0];
            int graphemeEnd = PopulateAndSubstitute(text, onlyRun.Start, textRuns, ref textRunIndex, ref codePointIndex, ref stringIndex, ref bidiRunIndex, onlyRun.ResolvedFont, bidiRuns, bidiMap, substitutions);

            if (usesDefaultTextRun)
            {
                // Population has already visited every grapheme, so its final index
                // is the exact exclusive end without a separate counting pass.
                onlyRun.End = graphemeEnd;
            }

            complete = substitutions.SeedMetricsInPlace(onlyRun.ResolvedFont);

            if (complete || (fallbackFonts.Length == 0 && options.FontFallbackResolver is null))
            {
                substitutions.SetRole(ShapingBufferRole.Positioning);
                shaped = substitutions;
                complete = true;
            }
            else
            {
                complete = positionings.TryAdd(onlyRun.ResolvedFont, substitutions);
            }

            goto FallbackPasses;
        }

        for (int runIndex = 0; runIndex < textRuns.Count; runIndex++)
        {
            TextRun textRun = textRuns[runIndex];
            if (textRun.Placeholder.HasValue)
            {
                substitutions.Clear();

                while (bidiRunIndex < bidiRuns.Length && codePointIndex == bidiRuns[bidiRunIndex].End)
                {
                    bidiRunIndex++;
                }

                // Placeholder direction comes from the bidi region at the insertion
                // point. If the insertion point is after all source text, use the
                // default even/LTR embedding level.
                BidiRun placeholderBidiRun = bidiRunIndex < bidiRuns.Length
                    ? bidiRuns[bidiRunIndex]
                    : new BidiRun(BidiCharacterType.LeftToRight, 2, codePointIndex, 0);

                // Placeholder runs are inserted into the layout stream and do not consume
                // source graphemes, source codepoints, or bidi runs. The loop position
                // is the placeholder's own run index; the populate tracker below may
                // lag it between runs.
                substitutions.AddPlaceholder(
                    CodePoint.ObjectReplacementChar,
                    placeholderBidiRun,
                    (ushort)runIndex,
                    codePointIndex);

                complete &= positionings.TryAdd(textRun.ResolvedFont, substitutions);
                textRunIndex++;
                continue;
            }

            if (!DoFontRun(
                textRun.Slice(text),
                textRun.Start,
                textRuns,
                ref textRunIndex,
                ref codePointIndex,
                ref stringIndex,
                ref bidiRunIndex,
                false,
                textRun.ResolvedFont,
                bidiRuns,
                bidiMap,
                substitutions,
                positionings))
            {
                complete = false;
            }
        }

        FallbackPasses:
        if (!complete)
        {
            // Finally try our fallback fonts.
            // We do a complete run here across the whole buffer.
            foreach (Font font in fallbackFonts)
            {
                textRunIndex = 0;
                codePointIndex = 0;
                stringIndex = 0;
                bidiRunIndex = 0;
                if (DoFontRun(
                    text,
                    0,
                    textRuns,
                    ref textRunIndex,
                    ref codePointIndex,
                    ref stringIndex,
                    ref bidiRunIndex,
                    true,
                    font,
                    bidiRuns,
                    bidiMap,
                    substitutions,
                    positionings))
                {
                    complete = true;
                    break;
                }
            }
        }

        // Last-resort resolver passes: one whole-buffer pass per newly resolved family.
        // The path only runs when unresolved code points remain, so its collections are
        // transient.
        List<Font>? resolverFonts = null;
        if (!complete && options.FontFallbackResolver is IFontFallbackResolver resolver)
        {
            List<CodePoint> unresolved = [];
            HashSet<int> queriedCodePoints = [];
            HashSet<string> attemptedFamilies = [];

            while (!complete && TryGetNextResolverFont(positionings, resolver, options, unresolved, queriedCodePoints, attemptedFamilies, out Font? next))
            {
                (resolverFonts ??= []).Add(next);

                textRunIndex = 0;
                codePointIndex = 0;
                stringIndex = 0;
                bidiRunIndex = 0;
                complete = DoFontRun(
                    text,
                    0,
                    textRuns,
                    ref textRunIndex,
                    ref codePointIndex,
                    ref stringIndex,
                    ref bidiRunIndex,
                    true,
                    next,
                    bidiRuns,
                    bidiMap,
                    substitutions,
                    positionings);
            }
        }

        // Update the positions of the glyphs in the completed buffer.
        // Each set of metrics is associated with single font and will only be updated
        // by that font so it's safe to use a single buffer.
        Font? lastFont = null;
        for (int i = 0; i < textRuns.Count; i++)
        {
            TextRun textRun = textRuns[i];

            Font font = textRun.ResolvedFont;
            if (font == lastFont)
            {
                continue;
            }

            font.FontMetrics.UpdatePositions(shaped);
            lastFont = font;
        }

        foreach (Font font in fallbackFonts)
        {
            font.FontMetrics.UpdatePositions(shaped);
        }

        if (resolverFonts is not null)
        {
            foreach (Font font in resolverFonts)
            {
                font.FontMetrics.UpdatePositions(shaped);
            }
        }

        // Script-specific expansion runs only after every font has finished
        // positioning. Process segments from the end so an expansion cannot move
        // the not-yet-processed range of an earlier segment.
        List<(int Index, int Count, ScriptClass Script, ShapePlan Plan)> segments = shaped.SegmentPlans;
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            (int index, int count, ScriptClass _, ShapePlan plan) = segments[i];
            plan.Shaper.PostprocessGlyphs(shaped, index, count);
        }

        HideDefaultIgnorables(shaped);

        return shaped;
    }

    /// <summary>
    /// Finalizes each resolved directional run into HarfBuzz visual glyph order and
    /// records its contiguous range without copying or taking ownership of glyphs.
    /// </summary>
    /// <param name="shaped">The positioned glyph buffer in logical run order.</param>
    /// <param name="layoutMode">The shaping orientation used for the glyphs.</param>
    /// <param name="scratch">The pooled state receiving run-range metadata.</param>
    private static void FinalizeDirectionalRuns(ShapingBuffer shaped, LayoutMode layoutMode, ShapingScratch scratch)
    {
        int glyphIndex = 0;
        BidiRun[] bidiRuns = scratch.BidiRuns;
        int[] bidiMap = scratch.BidiMap;
        for (int runIndex = 0; runIndex < scratch.BidiRunCount; runIndex++)
        {
            BidiRun bidiRun = bidiRuns[runIndex];
            int glyphStart = glyphIndex;
            while (glyphIndex < shaped.Count)
            {
                ref GlyphShapingData glyph = ref shaped[glyphIndex];
                bool belongsToRun = glyph.IsPlaceholder
                    ? shaped.GetPlaceholderBidiRun(glyph.CodePointIndex).Equals(bidiRun)
                    : bidiMap[glyph.CodePointIndex] == runIndex;

                if (!belongsToRun)
                {
                    break;
                }

                glyphIndex++;
            }

            ShapedGlyphRange glyphRange = new(glyphStart, glyphIndex - glyphStart);
            scratch.SetBidiGlyphRange(runIndex, in glyphRange);

            if ((bidiRun.Level & 1) == 0)
            {
                continue;
            }

            // Browsers copy the shaper's output sequentially because it is already
            // in visual order for the directional run. Fonts shapes in
            // logical storage, so apply the same proven finalization used by ShapeRun
            // once per resolved run before projecting the contiguous arrays.
            if (layoutMode.IsVertical())
            {
                // Bottom-to-top HarfBuzz shaping reverses graphemes before shaping.
                // Reverse their order while retaining the shaped glyph order inside
                // each grapheme, exactly as the public directional-run contract does.
                shaped.ReverseGraphemeRange(glyphStart, glyphIndex);
            }
            else
            {
                // Horizontal and mixed-vertical directional runs use HarfBuzz's
                // complete backward stream order, including every positioned glyph
                // emitted for one source position.
                shaped.ReverseRange(glyphStart, glyphIndex);
            }
        }
    }

    /// <summary>
    /// Coalesces adjacent equal embedding levels from one paragraph into shaping runs.
    /// </summary>
    /// <param name="scratch">The shaping state receiving the runs.</param>
    /// <param name="levels">The resolved levels relative to the paragraph.</param>
    /// <param name="textStart">The paragraph's code point offset in the complete text.</param>
    private static void AppendBidiRuns(ShapingScratch scratch, ArraySlice<sbyte> levels, int textStart)
    {
        if (levels.Length == 0)
        {
            return;
        }

        int startRun = 0;
        sbyte runLevel = levels[0];
        for (int i = 1; i < levels.Length; i++)
        {
            if (levels[i] == runLevel)
            {
                continue;
            }

            BidiCharacterType direction = (runLevel & 0x01) == 0 ? BidiCharacterType.LeftToRight : BidiCharacterType.RightToLeft;
            scratch.AddBidiRun(new BidiRun(direction, runLevel, textStart + startRun, i - startRun));
            startRun = i;
            runLevel = levels[i];
        }

        BidiCharacterType finalDirection = (runLevel & 0x01) == 0 ? BidiCharacterType.LeftToRight : BidiCharacterType.RightToLeft;
        scratch.AddBidiRun(new BidiRun(finalDirection, runLevel, textStart + startRun, levels.Length - startRun));
    }

    /// <summary>
    /// Copies the shaped result out of the pooled collections: run-constant state
    /// deduplicates into a run table and per-glyph state splits into parallel
    /// identity and geometry arrays of pure values held by the scratch, so the
    /// views stay valid exactly as long as the caller holds the scratch.
    /// </summary>
    /// <param name="shaped">The positioned buffer.</param>
    /// <param name="layoutMode">The layout mode used while shaping.</param>
    /// <param name="scratch">The rented pipeline state holding the bidi results and projection storage.</param>
    /// <returns>The wrapping-independent shaping state, valid within the scratch scope.</returns>
    private static ShapedText ProjectShapedText(ShapingBuffer shaped, LayoutMode layoutMode, ShapingScratch scratch)
    {
        uint verticalMask = ShapePlanFeatures.VerticalFeatureMask;
        int count = shaped.Count;
        (ShapedGlyphInfo[] infos, ShapedGlyphPosition[] positions) = scratch.GetProjection(count);
        scratch.ClearRuns();

        Font? runFont = null;
        int runTextRunIndex = -1;
        BidiRun runBidiRun = default;
        for (int i = 0; i < count; i++)
        {
            ref GlyphShapingData shaping = ref shaped[i];
            ref ShapingBuffer.GlyphMetricsEntry entry = ref shaped.MetricsAt(i);
            ref GlyphShapingPosition shapingPosition = ref shaped.PositionAt(i);

            // Placeholders carry a bidi run of their own, so they always cut a run.
            BidiRun shapingBidiRun = shaping.IsPlaceholder
                ? shaped.GetPlaceholderBidiRun(shaping.CodePointIndex)
                : default;
            if (entry.Font != runFont
                || shaping.TextRunIndex != runTextRunIndex
                || (shaping.IsPlaceholder && !shapingBidiRun.Equals(runBidiRun)))
            {
                runFont = entry.Font;
                runTextRunIndex = shaping.TextRunIndex;
                runBidiRun = shapingBidiRun;
                scratch.AddRun(new ShapedTextRun(entry.Font, entry.PointSize, shaped.TextRuns[shaping.TextRunIndex], shapingBidiRun));
            }

            ShapedGlyphFlags flags = ShapedGlyphFlags.None;
            if (shaping.IsPlaceholder)
            {
                flags |= ShapedGlyphFlags.Placeholder;
            }

            if (shaping.IsSubstituted)
            {
                flags |= ShapedGlyphFlags.Substituted;
            }

            if (shaping.IsDecomposed)
            {
                flags |= ShapedGlyphFlags.Decomposed;
            }

            if ((shaping.AppliedFeatureMask & verticalMask) != 0)
            {
                flags |= ShapedGlyphFlags.VerticalSubstituted;
            }

            if (shaping.IsCursiveScript)
            {
                // Layout needs the post-GSUB script classification only when it
                // applies tracking. Carry one bit through projection instead of
                // retaining the heavier itemization data.
                flags |= ShapedGlyphFlags.CursiveScript;
            }

            infos[i] = new ShapedGlyphInfo(
                shaping.CodePointIndex,
                shaping.CodePoint,
                shaping.CodePointCount,
                entry.Metrics.GlyphId,
                (ushort)(scratch.RunCount - 1),
                flags);

            positions[i] = new ShapedGlyphPosition(
                entry.GetAdvanceWidth(in shapingPosition),
                entry.GetAdvanceHeight(in shapingPosition),
                new Vector2(shapingPosition.Bounds.X, shapingPosition.Bounds.Y),
                entry.Metrics.Offset);
        }

        return new ShapedText(
            scratch.Runs,
            infos,
            positions,
            count,
            scratch.BidiRuns,
            scratch.BidiGlyphRanges,
            scratch.BidiRunCount,
            scratch.BidiMap,
            layoutMode);
    }

    /// <summary>
    /// Renders default ignorable records invisibly after positioning: both advances
    /// and the offset on the axis of movement zero out, and the glyph swaps to its
    /// font's invisible glyph. Records whose font offers no invisible glyph are
    /// deleted instead. Records a lookup substituted keep their glyphs: the
    /// substitution is a deliberate rendering the font produced from the ignorable.
    /// </summary>
    /// <param name="shaped">The positioned buffer.</param>
    private static void HideDefaultIgnorables(ShapingBuffer shaped)
    {
        if (!shaped.HasDefaultIgnorables)
        {
            return;
        }

        CodePoint space = new(0x0020);
        LayoutMode layoutMode = shaped.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = shaped.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = shaped.TextOptions.FontPalette;
        bool isVertical = layoutMode.IsVertical();
        Font? invisibleFont = null;
        ushort invisible = 0;
        bool hasInvisible = false;
        bool hasUnreplaceable = false;
        for (int i = 0; i < shaped.Count; i++)
        {
            ref GlyphShapingData data = ref shaped[i];
            if (!data.IsDefaultIgnorable || data.IsSubstituted)
            {
                continue;
            }

            // Writing through the bounds setters marks them dirty, which is what
            // the projection's advance reads honor. The cross-axis offset stays,
            // so adjustments from positioning survive.
            ref GlyphShapingPosition position = ref shaped.PositionAt(i);
            position.Bounds.Width = 0;
            position.Bounds.Height = 0;
            if (isVertical)
            {
                position.Bounds.Y = 0;
            }
            else
            {
                position.Bounds.X = 0;
            }

            ref ShapingBuffer.GlyphMetricsEntry entry = ref shaped.MetricsAt(i);
            Font font = entry.Font;
            if (!ReferenceEquals(font, invisibleFont))
            {
                invisibleFont = font;
                hasInvisible = font is not null && font.FontMetrics.TryGetGlyphId(space, out invisible);
            }

            if (font is null || !hasInvisible)
            {
                hasUnreplaceable = true;
                continue;
            }

            // The projection reads the glyph id and default advance from the
            // metrics entry, so the invisible glyph's metrics replace it.
            TextRun textRun = shaped.TextRuns[data.TextRunIndex];
            entry.Metrics = font.FontMetrics.GetGlyphMetrics(space, invisible, textRun.TextAttributes, textRun.TextDecorations, layoutMode, textRun.ColorFontSupport ?? colorFontSupport, textRun.FontPalette ?? fontPalette);
            shaped.SetGlyphId(i, invisible);
            data.IsHidden = true;
        }

        if (hasUnreplaceable)
        {
            shaped.DeleteGlyphsInPlace(static data => data.IsDefaultIgnorable && !data.IsSubstituted && !data.IsHidden);
        }
    }

    /// <summary>
    /// Finds the next font for a resolver fallback pass: re-collects the still-unresolved
    /// code points, then queries the resolver for each code point not queried before until
    /// one yields a family not shaped with before.
    /// Termination is structural: a successful return consumes at least one code point from
    /// <paramref name="queriedCodePoints"/>' complement, both sets only grow, and the
    /// candidates come from the text's finite code points — so repeated calls must
    /// eventually return <see langword="false"/> and the caller's loop is bounded by the
    /// number of distinct unresolved code points.
    /// </summary>
    /// <param name="positionings">The accumulator buffer holding the shaped records.</param>
    /// <param name="resolver">The configured fallback resolver.</param>
    /// <param name="options">The text options supplying the requested family, size, style, and culture.</param>
    /// <param name="unresolved">The reusable scratch list receiving the unresolved code points.</param>
    /// <param name="queriedCodePoints">The code points already sent to the resolver, matched or not.</param>
    /// <param name="attemptedFamilies">The family names already shaped with.</param>
    /// <param name="font">When this method returns <see langword="true"/>, the font for the next pass.</param>
    /// <returns><see langword="true"/> if a new family was resolved; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetNextResolverFont(
        ShapingBuffer positionings,
        IFontFallbackResolver resolver,
        TextOptions options,
        List<CodePoint> unresolved,
        HashSet<int> queriedCodePoints,
        HashSet<string> attemptedFamilies,
        [NotNullWhen(true)] out Font? font)
    {
        unresolved.Clear();
        positionings.CollectUnresolvedCodePoints(unresolved);

        foreach (CodePoint codePoint in unresolved)
        {
            if (!queriedCodePoints.Add(codePoint.Value))
            {
                continue;
            }

            if (resolver.TryResolve(codePoint, options.Font.Family, options.Font.RequestedStyle, options.Culture, out FontFamily family) && attemptedFamilies.Add(family.Name))
            {
                font = new Font(family, options.Font.Size, options.Font.RequestedStyle);
                return true;
            }
        }

        font = null;
        return false;
    }

    /// <summary>
    /// Shapes a single font run — maps codepoints in <paramref name="text"/> to glyph ids using
    /// <paramref name="font"/>, then runs GSUB substitution and GPOS positioning. Codepoints that
    /// the font cannot map are recorded for a later fallback pass.
    /// </summary>
    /// <param name="text">The run-relative text slice to shape.</param>
    /// <param name="start">The starting grapheme index (absolute within the original input).</param>
    /// <param name="textRuns">The ordered list of resolved text runs.</param>
    /// <param name="textRunIndex">The index of the current text run; advanced as the enumerator crosses run boundaries.</param>
    /// <param name="codePointIndex">The running codepoint index (absolute within the original input).</param>
    /// <param name="stringIndex">The running char index (absolute within the original input).</param>
    /// <param name="bidiRunIndex">The running bidi run index.</param>
    /// <param name="isFallbackRun">
    /// <see langword="true"/> if this call is the fallback-font pass (in which case unmapped codepoints
    /// may still emit <c>.notdef</c> glyphs).
    /// </param>
    /// <param name="font">The font to shape with.</param>
    /// <param name="bidiRuns">The resolved bidi runs covering the whole input.</param>
    /// <param name="bidiMap">A codepoint → bidi-run mapping accumulated across shaping passes.</param>
    /// <param name="substitutions">The GSUB substitution buffer to write into.</param>
    /// <param name="positionings">The GPOS positioning buffer to write into.</param>
    /// <returns>
    /// <see langword="true"/> if every codepoint mapped successfully; <see langword="false"/> if any
    /// codepoint remains unmapped (so a fallback-font pass is needed).
    /// </returns>
    private static bool DoFontRun(
        ReadOnlySpan<char> text,
        int start,
        IReadOnlyList<TextRun> textRuns,
        ref int textRunIndex,
        ref int codePointIndex,
        ref int stringIndex,
        ref int bidiRunIndex,
        bool isFallbackRun,
        Font font,
        BidiRun[] bidiRuns,
        int[] bidiMap,
        ShapingBuffer substitutions,
        ShapingBuffer positionings)
    {
        _ = PopulateAndSubstitute(text, start, textRuns, ref textRunIndex, ref codePointIndex, ref stringIndex, ref bidiRunIndex, font, bidiRuns, bidiMap, substitutions);

        bool result = !isFallbackRun
            ? positionings.TryAdd(font, substitutions)
            : positionings.TryUpdate(font, substitutions);
        return result;
    }

    /// <summary>
    /// Populates the substitution buffer from <paramref name="text"/> and runs bidi
    /// mirroring and GSUB substitution over it, leaving the shaped records in the
    /// buffer for either in-place metrics seeding or a cross-buffer seed.
    /// </summary>
    /// <param name="text">The run-relative text slice to shape.</param>
    /// <param name="start">The starting grapheme index (absolute within the original input).</param>
    /// <param name="textRuns">The ordered list of resolved text runs.</param>
    /// <param name="textRunIndex">The index of the current text run; advanced as the enumerator crosses run boundaries.</param>
    /// <param name="codePointIndex">The running codepoint index (absolute within the original input).</param>
    /// <param name="stringIndex">The running char index (absolute within the original input).</param>
    /// <param name="bidiRunIndex">The running bidi run index.</param>
    /// <param name="font">The font to shape with.</param>
    /// <param name="bidiRuns">The resolved bidi runs covering the whole input.</param>
    /// <param name="bidiMap">A codepoint → bidi-run mapping accumulated across shaping passes.</param>
    /// <param name="substitutions">The GSUB substitution buffer to write into.</param>
    /// <returns>The exclusive grapheme index reached after consuming the text.</returns>
    private static int PopulateAndSubstitute(ReadOnlySpan<char> text, int start, IReadOnlyList<TextRun> textRuns, ref int textRunIndex, ref int codePointIndex, ref int stringIndex, ref int bidiRunIndex, Font font, BidiRun[] bidiRuns, int[] bidiMap, ShapingBuffer substitutions)
    {
        // For each run we start with a fresh substitution buffer to avoid
        // overwriting the glyph ids.
        substitutions.Clear();

        // A font without variation sequences never consumes the following codepoint
        // during glyph lookup, so the per-codepoint lookahead decode is skipped.
        bool hasVariationSequences = font.FontMetrics.HasUnicodeVariationSequences;

        // Shaping needs each grapheme's boundary and source slice, but not terminal
        // width, emoji, or display flags. The boundary-only mode avoids deriving
        // metadata that no shaping operation consumes.
        int graphemeIndex = start;
        int inputGroupStart = substitutions.Count;
        bool previousWasContinuation = false;
        bool previousWasRegionalIndicator = false;
        bool previousWasZeroWidthJoiner = false;
        SpanGraphemeEnumerator graphemeEnumerator = new(text, true);
        while (graphemeEnumerator.MoveNext())
        {
            ReadOnlySpan<char> grapheme = graphemeEnumerator.CurrentSpan;
            int graphemeMax = grapheme.Length - 1;
            int graphemeCodePointIndex = 0;
            int charIndex = 0;

            while (textRunIndex < textRuns.Count - 1 && graphemeIndex == textRuns[textRunIndex].End)
            {
                textRunIndex++;
            }

            // Now enumerate through each codepoint in the grapheme.
            bool skipNextCodePoint = false;
            SpanCodePointEnumerator codePointEnumerator = new(grapheme);
            while (codePointEnumerator.MoveNext())
            {
                CodePoint current = codePointEnumerator.Current;
                int currentStringIndex = stringIndex;
                stringIndex += current.Utf16SequenceLength;

                uint value = (uint)current.Value;
                GraphemeClusterClass graphemeClass = CodePoint.GetGraphemeClusterClass(current);

                // HarfBuzz deliberately uses a smaller continuation rule set than
                // the complete Unicode grapheme algorithm. In particular, ZWNJ and
                // adjacent Hangul letters keep distinct input starts, while marks,
                // emoji modifiers, paired regional indicators, ZWJ emoji sequences,
                // half-width voiced marks, and emoji tag characters continue the
                // preceding input group.
                bool isRegionalIndicator = graphemeClass == GraphemeClusterClass.RegionalIndicator;
                bool isZeroWidthJoiner = CodePoint.IsZeroWidthJoiner(current);
                bool isContinuation =

                    // Combining, spacing-combining, and enclosing marks belong to
                    // the preceding base character.
                    CodePoint.IsMark(current)

                    // U+1F3FB..U+1F3FF are the five emoji skin-tone modifiers.
                    || value is >= 0x1F3FB and <= 0x1F3FF

                    // Regional indicators form flag pairs. The second indicator
                    // continues the first; the third starts the next pair.
                    || (isRegionalIndicator && previousWasRegionalIndicator && !previousWasContinuation)

                    // U+200D ZERO WIDTH JOINER connects the characters on either side.
                    || isZeroWidthJoiner

                    // An extended pictographic character following U+200D continues
                    // the emoji sequence selected by that joiner.
                    || (previousWasZeroWidthJoiner && graphemeClass == GraphemeClusterClass.ExtendedPictographic)

                    // U+FF9E and U+FF9F are the half-width Katakana voiced and
                    // semi-voiced sound marks.
                    || value is >= 0xFF9E and <= 0xFF9F

                    // U+E0020 TAG SPACE through U+E007F CANCEL TAG encode the
                    // invisible tag sequences used by emoji subregion flags.
                    || value is >= 0xE0020 and <= 0xE007F;

                if (!isContinuation)
                {
                    // The preceding group is now complete. Combine its exact input
                    // starts once, before GSUB can move or expand any of its records.
                    if (substitutions.Count - inputGroupStart > 1)
                    {
                        substitutions.CombineInputStarts(inputGroupStart, substitutions.Count);
                    }

                    inputGroupStart = substitutions.Count;
                }

                previousWasContinuation = isContinuation;
                previousWasRegionalIndicator = isRegionalIndicator;
                previousWasZeroWidthJoiner = isZeroWidthJoiner;

                if (codePointIndex == bidiRuns[bidiRunIndex].End)
                {
                    bidiRunIndex++;
                }

                if (skipNextCodePoint)
                {
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                bidiMap[codePointIndex] = bidiRunIndex;

                int charsConsumed = 0;
                charIndex += current.Utf16SequenceLength;
                CodePoint? next = hasVariationSequences && graphemeCodePointIndex < graphemeMax
                    ? CodePoint.DecodeFromUtf16At(grapheme, charIndex, out charsConsumed)
                    : null;

                charIndex += charsConsumed;

                // Get the glyph id for the codepoint and add to the buffer. Every
                // codepoint enters the buffer, including unmapped default
                // ignorables as the missing glyph: sequence matching treats them
                // as transparent and the hide stage replaces them at the end.
                _ = substitutions.TryGetGlyphId(font.FontMetrics, current, next, out ushort glyphId, out skipNextCodePoint);

                // Capture all three source coordinates while the input enumerators
                // provide them. Later substitutions move, duplicate, or combine the
                // complete record without reconstructing source positions.
                substitutions.AddGlyph(glyphId, current, (TextDirection)bidiRuns[bidiRunIndex].Direction, (ushort)textRunIndex, codePointIndex, currentStringIndex, graphemeIndex);

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            graphemeIndex++;
        }

        if (substitutions.Count - inputGroupStart > 1)
        {
            // Complete the final input group because no following codepoint exists
            // to close it inside the loop.
            substitutions.CombineInputStarts(inputGroupStart, substitutions.Count);
        }

        // Apply the simple and complex substitutions.
        SubstituteBidiMirrors(font.FontMetrics, substitutions);

        font.FontMetrics.ApplySubstitution(substitutions);
        return graphemeIndex;
    }

    /// <summary>
    /// Substitutes mirrored bracket glyphs (for example <c>(</c> ↔ <c>)</c>) inside right-to-left
    /// bidi runs, per Unicode Bidirectional Algorithm rule L4. Relies on the font's <c>rtlm</c>
    /// feature when available and falls back to the Unicode mirror table otherwise.
    /// </summary>
    /// <param name="fontMetrics">The font metrics used to look up mirrored glyph ids.</param>
    /// <param name="buffer">The substitution buffer whose glyphs will be rewritten in place.</param>
    private static void SubstituteBidiMirrors(FontMetrics fontMetrics, ShapingBuffer buffer)
    {
        for (int i = 0; i < buffer.Count; i++)
        {
            ref GlyphShapingData data = ref buffer[i];

            if (data.Direction != TextDirection.RightToLeft)
            {
                continue;
            }

            if (!CodePoint.TryGetBidiMirror(data.CodePoint, out CodePoint mirror))
            {
                continue;
            }

            if (fontMetrics.TryGetGlyphId(mirror, out ushort glyphId))
            {
                buffer.Replace(i, glyphId, KnownFeatureTags.RightToLeftMirroredForms);
            }
        }

        // TODO: This only replaces certain glyphs. We should investigate the specification further.
        // https://www.unicode.org/reports/tr50/#vertical_alternates
        if (buffer.TextOptions.LayoutMode.IsHorizontal())
        {
            return;
        }

        for (int i = 0; i < buffer.Count; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (CodePoint.GetVerticalOrientationType(data.CodePoint) is VerticalOrientationType.Upright or VerticalOrientationType.TransformUpright)
            {
                continue;
            }

            if (!CodePoint.TryGetVerticalMirror(data.CodePoint, out CodePoint mirror))
            {
                continue;
            }

            if (fontMetrics.TryGetGlyphId(mirror, out ushort glyphId))
            {
                buffer.Replace(i, glyphId, KnownFeatureTags.VerticalAlternates);
            }
        }
    }

    /// <summary>
    /// A shaping result scoped to the pooled pipeline state backing its views.
    /// Disposal returns the state to the pool and ends the views' validity, so
    /// consumers copy what they retain before the scope closes.
    /// </summary>
    internal readonly ref struct ShapedTextScope
    {
        /// <summary>
        /// The pooled pipeline state owned by the scope.
        /// </summary>
        private readonly ShapingScratch scratch;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapedTextScope"/> struct.
        /// </summary>
        /// <param name="shaped">The shaped views.</param>
        /// <param name="scratch">The pooled pipeline state backing the views.</param>
        public ShapedTextScope(ShapedText shaped, ShapingScratch scratch)
        {
            this.Shaped = shaped;
            this.scratch = scratch;
        }

        /// <summary>
        /// Gets the wrapping-independent shaping state, valid until disposal.
        /// </summary>
        public ShapedText Shaped { get; }

        /// <summary>
        /// Returns the pooled pipeline state, ending the views' validity.
        /// </summary>
        public void Dispose() => ScratchPool.Return(this.scratch);
    }

    /// <summary>
    /// The pooling policy for <see cref="ShapingScratch"/> instances: scratch state is
    /// reset on acquisition by <see cref="ShapingScratch.Prepare"/>, so returned
    /// instances are always accepted.
    /// </summary>
    private sealed class ShapingScratchPooledObjectPolicy : IPooledObjectPolicy<ShapingScratch>
    {
        /// <inheritdoc/>
        public ShapingScratch Create() => new();

        /// <inheritdoc/>
        public bool Return(ShapingScratch obj) => true;
    }
}

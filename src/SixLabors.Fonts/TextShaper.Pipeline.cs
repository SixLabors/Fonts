// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts;

/// <summary>
/// Maps characters to glyphs and groups them into shaping runs. It applies substitution and
/// positioning with the selected Apple Advanced Typography or OpenType tables.
/// <see cref="TextLayout"/> and the public shaping API use this pipeline.
/// Layout uses its output to compose and position lines.
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
                textRuns.Add(new()
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
            textRuns.Add(new()
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
            return new(ProjectShapedText(shaped, options.LayoutMode, scratch), scratch);
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
        ShapingBuffer shaped = scratch.Prepare(options);
        shaped.UseShapingVerticalOrigin = useShapingVerticalOrigin;

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

        bool usesDefaultTextRun = prebuiltRuns is null && !(options.TextRuns?.Count > 0);
        IReadOnlyList<TextRun> textRuns = prebuiltRuns ?? ((options.TextRuns?.Count > 0)
            ? BuildTextRuns(text, options)
            : scratch.GetDefaultTextRuns(options));

        shaped.SetTextRuns(textRuns);
        shaped.SetShapingLimits(codePointCount);
        (ScriptHorizontalDirection? nativeDirection, OrderedDictionary<FontFamily, Font>? fallbackFonts, int graphemeCount) =
            ReadShapingInput(text, options, textRuns, bidiRuns, bidiMap, shaped);

        shaped.HorizontalScriptDirection = nativeDirection;

        if (usesDefaultTextRun)
        {
            textRuns[0].End = graphemeCount;
        }

        int runIndex = 0;
        int inputIndex = 0;
        ReadOnlySpan<(int End, ScriptItemizer.ShapingRun Run)> inputRuns = shaped.InputRuns;
        bool hasFontOrRunBoundaries = fallbackFonts is not null || textRuns.Count > 1;
        int glyphStart = 0;
        while (glyphStart < shaped.Count)
        {
            if (shaped[glyphStart].IsPlaceholder)
            {
                // Placeholders occupy a slot outside the source run counts.
                shaped.SeedPlaceholderMetrics(glyphStart);
                glyphStart++;
                continue;
            }

            if (inputIndex == inputRuns[runIndex].End)
            {
                runIndex++;
            }

            (int inputEnd, ScriptItemizer.ShapingRun run) = inputRuns[runIndex];
            ushort textRunIndex = shaped[glyphStart].TextRunIndex;
            int fontIndex = shaped[glyphStart].FallbackFontIndex;
            Font font = fontIndex == 0
                ? textRuns[textRunIndex].ResolvedFont
                : fallbackFonts!.GetAt(fontIndex - 1).Value;

            // The run end counts original mapped glyphs. Add its remaining
            // length to the current index, which includes prior substitutions.
            int glyphEnd = glyphStart + (inputEnd - inputIndex);
            if (hasFontOrRunBoundaries)
            {
                // Declared runs and fallback faces can split a shaping run.
                // Stop at the first such boundary within the recorded range.
                int runEnd = glyphEnd;
                glyphEnd = glyphStart + 1;

                while (glyphEnd < runEnd)
                {
                    ref GlyphShapingData glyph = ref shaped[glyphEnd];
                    if (glyph.IsPlaceholder || glyph.TextRunIndex != textRunIndex || glyph.FallbackFontIndex != fontIndex)
                    {
                        break;
                    }

                    glyphEnd++;
                }
            }

            inputIndex += glyphEnd - glyphStart;
            shaped.SetSpan(glyphStart, glyphEnd);
            shaped.SetRole(ShapingBufferRole.Substitution);
            FontMetrics metrics = font.FontMetrics;
            SubstituteBidiMirrors(metrics, shaped, glyphStart);
            ShapePlan plan = metrics.ApplySubstitution(shaped, run);

            int count = shaped.SpanEnd - glyphStart;
            if (count > 0)
            {
                shaped.SeedMetricsInPlace(font, glyphStart);
                shaped.SetRole(ShapingBufferRole.Positioning);
                plan.FontMetrics.UpdatePositions(shaped, plan, run.IsVertical);
                if (shaped.HasOutOfRangeAdvances)
                {
                    // Resolve attachments with the original pen steps before
                    // converting the advances for the unsigned layout contract.
                    AdvancedTypographicUtils.NormalizeAdvances(shaped);
                }

                plan.Shaper.PostprocessGlyphs(shaped, glyphStart, count);
            }

            // Substitution can change the span length. Its current end is
            // the first glyph of the next span. The source run counts mapped glyphs.
            glyphStart = shaped.SpanEnd;
        }

        shaped.SetRole(ShapingBufferRole.Positioning);
        shaped.SetSpan(0, shaped.Count);
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
            if (layoutMode.IsVertical() || shaped.HorizontalScriptDirection == ScriptHorizontalDirection.LeftToRight)
            {
                // Bottom-to-top and forced right-to-left runs with a
                // left-to-right script keep glyph order inside each grapheme.
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
            FontGlyphMetrics glyphMetrics = entry.Metrics;

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

                // The selected face supplies glyph metrics. The text run sets
                // the size at which layout uses those metrics.
                TextRun textRun = shaped.TextRuns[runTextRunIndex];
                scratch.AddRun(new(entry.Font, textRun.ResolvedFont.Size, textRun, shapingBidiRun));
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

            infos[i] = new(
                shaping.CodePointIndex,
                shaping.CodePoint,
                shaping.CodePointCount,
                glyphMetrics.GlyphId,
                (ushort)(scratch.RunCount - 1),
                flags);

            positions[i] = new(
                entry.GetAdvanceWidth(in shapingPosition),
                entry.GetAdvanceHeight(in shapingPosition),
                new Vector2(shapingPosition.Bounds.X, shapingPosition.Bounds.Y),
                glyphMetrics.Offset);
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
    /// Selects a font for the current character before the input loop advances.
    /// </summary>
    /// <param name="buffer">The preshaped glyph buffer.</param>
    /// <param name="index">The entry for the current character.</param>
    /// <param name="next">The next character in the grapheme, if present.</param>
    /// <param name="fallbackFonts">The font list in priority order. The method creates the list on first use.</param>
    /// <param name="precedingFontIndex">The fallback font index of the preceding character in the grapheme, or zero.</param>
    /// <param name="queried">Code points already sent to the resolver.</param>
    /// <param name="consumed">Receives whether the selected glyph includes the following character.</param>
    private static void ResolveFallbackGlyph(
        ShapingBuffer buffer,
        int index,
        CodePoint? next,
        ref OrderedDictionary<FontFamily, Font>? fallbackFonts,
        int precedingFontIndex,
        ref HashSet<int>? queried,
        ref bool consumed)
    {
        TextOptions options = buffer.TextOptions;
        if (fallbackFonts is null)
        {
            // Allocate the list only when a character needs a font search.
            // Keep configured fonts first. The resolver adds new fonts at the end.
            IReadOnlyList<FontFamily> families = options.FallbackFontFamilies ?? [];
            fallbackFonts = new(families.Count);
            for (int i = 0; i < families.Count; i++)
            {
                FontFamily configuredFamily = families[i];
                fallbackFonts.TryAdd(configuredFamily, new Font(configuredFamily, options.Font.Size, options.Font.RequestedStyle));
            }
        }

        ref GlyphShapingData glyph = ref buffer[index];
        int preferredFontIndex = glyph.IsDefaultIgnorable ? precedingFontIndex : 0;
        (int FontIndex, ushort GlyphId) baseMapping = default;

        // An invisible character can affect the glyph before it. Try that
        // glyph's font first so both characters can stay in one shaping span.
        if (preferredFontIndex != 0 && TryMapFallbackGlyph(buffer, index, next, fallbackFonts, preferredFontIndex, precedingFontIndex, ref baseMapping, ref consumed))
        {
            return;
        }

        for (int i = 1; i <= fallbackFonts.Count; i++)
        {
            if (i != preferredFontIndex && TryMapFallbackGlyph(buffer, index, next, fallbackFonts, i, precedingFontIndex, ref baseMapping, ref consumed))
            {
                return;
            }
        }

        // Ask the resolver only after trying the fonts already on the list.
        // A visible base glyph here can still lack the requested presentation.
        if ((!glyph.IsDefaultIgnorable || glyph.GlyphId == 0)
            && options.FontFallbackResolver is IFontFallbackResolver resolver
            && (queried ??= []).Add(glyph.CodePoint.Value)
            && resolver.TryResolve(glyph.CodePoint, options.Font.Family, options.Font.RequestedStyle, options.Culture, out FontFamily family)
            && !fallbackFonts.ContainsKey(family))
        {
            // Add new families at the end so later glyphs try them in order.
            Font font = new(family, options.Font.Size, options.Font.RequestedStyle);
            fallbackFonts.Add(family, font);
            if (TryMapFallbackGlyph(buffer, index, next, fallbackFonts, fallbackFonts.Count, precedingFontIndex, ref baseMapping, ref consumed))
            {
                return;
            }
        }

        // A text or emoji style can be unavailable in every font. Keep the
        // grapheme together if its preceding font maps the base. Otherwise
        // keep the original base glyph or use the first fallback that has one.
        if (baseMapping.FontIndex != 0 && (baseMapping.FontIndex == precedingFontIndex || glyph.GlyphId == 0))
        {
            buffer.SetGlyphId(index, baseMapping.GlyphId);
            glyph.FallbackFontIndex = baseMapping.FontIndex;
            consumed = false;
        }
    }

    /// <summary>
    /// Assigns a font if it maps the character and meets its presentation request.
    /// </summary>
    /// <param name="buffer">The preshaped glyph buffer.</param>
    /// <param name="index">The missing entry.</param>
    /// <param name="next">The next character in the grapheme, if present.</param>
    /// <param name="fallbackFonts">The fallback font list in priority order.</param>
    /// <param name="fontIndex">The one-based index of the candidate font.</param>
    /// <param name="precedingFontIndex">The fallback font index of the preceding character in the grapheme, or zero.</param>
    /// <param name="baseMapping">The base glyph to use if no font matches the presentation request.</param>
    /// <param name="consumed">Receives whether a successful mapping includes the following character.</param>
    /// <returns>Whether the font was assigned.</returns>
    private static bool TryMapFallbackGlyph(
        ShapingBuffer buffer,
        int index,
        CodePoint? next,
        OrderedDictionary<FontFamily, Font> fallbackFonts,
        int fontIndex,
        int precedingFontIndex,
        ref (int FontIndex, ushort GlyphId) baseMapping,
        ref bool consumed)
    {
        ref GlyphShapingData glyph = ref buffer[index];
        FontMetrics metrics = fallbackFonts.GetAt(fontIndex - 1).Value.FontMetrics;
        CodePoint? selector = GetVariationSelector(next, metrics);

        // Use the font's cmap cache here. The buffer's cache is useful when
        // mapping one face at a time and clears when its font changes.
        if (!metrics.TryGetGlyphId(glyph.CodePoint, selector, out ushort glyphId, out bool consumesNext))
        {
            return false;
        }

        if (!consumesNext && !MatchesPresentation(buffer, glyph.TextRunIndex, next, metrics))
        {
            // Save the base glyph from the same grapheme's font when possible.
            // Keep the first mapping otherwise to avoid repeating the lookup.
            if (baseMapping.FontIndex == 0 || fontIndex == precedingFontIndex)
            {
                baseMapping = (fontIndex, glyphId);
            }

            return false;
        }

        buffer.SetGlyphId(index, glyphId);
        glyph.FallbackFontIndex = fontIndex;
        consumed = consumesNext;
        return true;
    }

    /// <summary>
    /// Selects the following code point when it can affect cmap lookup.
    /// </summary>
    /// <param name="next">The next character in the grapheme, if present.</param>
    /// <param name="font">The font to map with.</param>
    /// <returns>The code point to pass to cmap.</returns>
    private static CodePoint? GetVariationSelector(CodePoint? next, FontMetrics font)
        => IsPresentationSelector(next) || (next.HasValue && font.HasUnicodeVariationSequences) ? next : null;

    /// <summary>
    /// Checks whether a code point requests text or emoji presentation.
    /// </summary>
    /// <param name="codePoint">The next code point, if present.</param>
    /// <returns>Whether it is a text or emoji presentation selector.</returns>
    private static bool IsPresentationSelector(CodePoint? codePoint)
        => codePoint?.Value == ShapingBuffer.TextPresentationSelector || codePoint?.Value == ShapingBuffer.EmojiPresentationSelector;

    /// <summary>
    /// Checks the font's color support when cmap did not consume a presentation selector.
    /// </summary>
    /// <param name="buffer">The buffer supplying color font options.</param>
    /// <param name="textRunIndex">The declared run supplying color font options.</param>
    /// <param name="next">The next character in the grapheme, if present.</param>
    /// <param name="font">The font that maps the base character.</param>
    /// <returns>Whether the font supports the requested presentation.</returns>
    private static bool MatchesPresentation(ShapingBuffer buffer, ushort textRunIndex, CodePoint? next, FontMetrics font)
    {
        if (!IsPresentationSelector(next))
        {
            return true;
        }

        ColorFontSupport color = buffer.TextRuns[textRunIndex].ColorFontSupport ?? buffer.TextOptions.ColorFontSupport;
        return font.HasColorTable(color) == (next?.Value == ShapingBuffer.EmojiPresentationSelector);
    }

    /// <summary>
    /// Checks whether an unmapped source character needs fallback.
    /// </summary>
    /// <param name="codePoint">The unmapped source character.</param>
    /// <returns>Whether the character needs a glyph.</returns>
    private static bool RequiresGlyph(CodePoint codePoint)
        => !CodePoint.IsControl(codePoint)
            && (!UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) || UnicodeUtility.ShouldRenderWhiteSpaceOnly(codePoint));

    /// <summary>
    /// Maps the text to glyphs, selects fallback fonts, and resolves the source runs.
    /// </summary>
    /// <param name="text">The original UTF-16 text.</param>
    /// <param name="options">The shaping options.</param>
    /// <param name="textRuns">The declared or default text runs.</param>
    /// <param name="bidiRuns">The resolved bidi runs.</param>
    /// <param name="bidiMap">The destination for the bidi run index of each character.</param>
    /// <param name="buffer">The buffer receiving the selected fonts, glyph IDs, and source runs.</param>
    /// <returns>The native script direction, fallback fonts, and grapheme count.</returns>
    private static (ScriptHorizontalDirection? Direction, OrderedDictionary<FontFamily, Font>? FallbackFonts, int GraphemeCount) ReadShapingInput(
        ReadOnlySpan<char> text,
        TextOptions options,
        IReadOnlyList<TextRun> textRuns,
        BidiRun[] bidiRuns,
        int[] bidiMap,
        ShapingBuffer buffer)
    {
        int codePointIndex = 0;
        int inputGlyphCount = 0;
        int runStart = 0;
        int stringIndex = 0;
        int textRunIndex = 0;
        int bidiRunIndex = 0;
        int clusterStart = 0;
        int mappedClusterStart = -1;
        int clusterStringIndex = 0;
        int clusterGraphemeIndex = 0;
        int graphemeCount = 0;

        bool previousWasContinuation = false;
        bool previousWasRegionalIndicator = false;
        bool previousWasZeroWidthJoiner = false;
        bool mixedVertical = options.LayoutMode.IsVerticalMixed();
        bool vertical = options.LayoutMode.IsVertical();
        bool hasFallback = options.FallbackFontFamilies?.Count > 0 || options.FontFallbackResolver is not null;
        OrderedDictionary<FontFamily, Font>? fallbackFonts = null;
        HashSet<int>? queried = null;
        bool needsNativeDirection = options.TextBidiMode == TextBidiMode.Override
            && options.TextDirection == TextDirection.RightToLeft
            && options.LayoutMode.IsHorizontal();

        ScriptHorizontalDirection? nativeDirection = null;
        ScriptItemizer.ShapingRun run = default;
        ScriptClass? scriptOverride = options.Script;
        FontMetrics metrics = options.Font.FontMetrics;
        bool hasVariationSequences = metrics.HasUnicodeVariationSequences;
        CultureInfo culture = options.Culture ?? CultureInfo.CurrentCulture;
        IReadOnlyList<Tag> featureTags = options.FeatureTags;

        // Only grapheme boundaries are needed here. The enumerator can skip
        // computing width and display flags, which layout resolves separately.
        SpanGraphemeEnumerator graphemes = new(text, true);
        while (graphemes.MoveNext())
        {
            int previousTextRun = textRunIndex;
            bool followsPlaceholder = false;
            while (textRunIndex < textRuns.Count - 1 && graphemeCount >= textRuns[textRunIndex].End)
            {
                if (textRuns[textRunIndex].Placeholder.HasValue)
                {
                    // Use the bidi run of the following source character.
                    // The placeholder occupies a slot and consumes no text.
                    int followingRun = codePointIndex == bidiRuns[bidiRunIndex].End ? bidiRunIndex + 1 : bidiRunIndex;
                    buffer.AddPlaceholder(CodePoint.ObjectReplacementChar, bidiRuns[followingRun], (ushort)textRunIndex, codePointIndex);
                    followsPlaceholder = true;
                }

                textRunIndex++;
            }

            if (codePointIndex == 0 || previousTextRun != textRunIndex)
            {
                TextRun textRun = textRuns[textRunIndex];
                metrics = textRun.ResolvedFont.FontMetrics;
                hasVariationSequences = metrics.HasUnicodeVariationSequences;
                scriptOverride = textRun.Script ?? options.Script;
                culture = textRun.Culture ?? options.Culture ?? CultureInfo.CurrentCulture;
                featureTags = textRun.FeatureTags ?? options.FeatureTags;
            }

            bool firstInGrapheme = true;
            bool consumed = false;
            ReadOnlySpan<char> grapheme = graphemes.CurrentSpan;
            int graphemeOffset = 0;
            SpanCodePointEnumerator characters = new(grapheme);
            while (characters.MoveNext())
            {
                CodePoint codePoint = characters.Current;
                uint value = (uint)codePoint.Value;
                GraphemeClusterClass graphemeClass = CodePoint.GetGraphemeClusterClass(codePoint);

                // Input clusters follow the shaping continuation rules. Marks,
                // emoji modifiers, regional indicator pairs, ZWJ emoji sequences,
                // half-width voiced marks, and emoji tags share an input start.
                bool isRegionalIndicator = graphemeClass == GraphemeClusterClass.RegionalIndicator;
                bool isZeroWidthJoiner = CodePoint.IsZeroWidthJoiner(codePoint);
                bool continuation = CodePoint.IsMark(codePoint)
                    || value is >= 0x1F3FB and <= 0x1F3FF
                    || (isRegionalIndicator && previousWasRegionalIndicator && !previousWasContinuation)
                    || isZeroWidthJoiner
                    || (previousWasZeroWidthJoiner && graphemeClass == GraphemeClusterClass.ExtendedPictographic)
                    || value is >= 0xFF9E and <= 0xFF9F
                    || value is >= 0xE0020 and <= 0xE007F;

                if (!continuation || (firstInGrapheme && (previousTextRun != textRunIndex || followsPlaceholder)))
                {
                    clusterStart = codePointIndex;
                }

                previousWasContinuation = continuation;
                previousWasRegionalIndicator = isRegionalIndicator;
                previousWasZeroWidthJoiner = isZeroWidthJoiner;

                // UAX #9 limits shaping to one bidi level run. Two levels
                // with the same reading direction still have separate runs.
                bool bidiBoundary = codePointIndex == bidiRuns[bidiRunIndex].End;
                if (bidiBoundary)
                {
                    bidiRunIndex++;
                }

                // Record a bidi index even for a selector that cmap consumes.
                // The index describes the source character.
                bidiMap[codePointIndex] = bidiRunIndex;

                if (mixedVertical && firstInGrapheme)
                {
                    // A transform-and-rotate character needs vertical shaping
                    // so the font can supply a vertical form. The rest of the
                    // grapheme, including combining marks, uses the same axis.
                    VerticalOrientationType orientation = CodePoint.GetVerticalOrientationType(codePoint);
                    vertical = AdvancedTypographicUtils.IsVerticalGlyph(orientation) || orientation == VerticalOrientationType.TransformRotate;
                }

                // Resolve the source script here. A font can substitute a
                // glyph from another character, but the input run still owns it.
                ScriptClass script = scriptOverride ?? CodePoint.GetScriptClass(codePoint);
                if (codePointIndex == 0
                    || bidiBoundary
                    || (firstInGrapheme && followsPlaceholder)
                    || (firstInGrapheme && previousTextRun != textRunIndex && !run.HasSameSettings(culture, featureTags))
                    || !run.TryInclude(codePoint, script, vertical))
                {
                    // Use counts of emitted glyphs. Consumed selectors keep
                    // their source indices but do not occupy buffer slots.
                    if (inputGlyphCount > runStart)
                    {
                        buffer.AddInputRun(inputGlyphCount, run);
                        runStart = inputGlyphCount;
                    }

                    run = new(script, culture, featureTags, vertical);
                }

                if (needsNativeDirection && (options.Script is not null || run.Script is not (ScriptClass.Common or ScriptClass.Inherited or ScriptClass.Unknown)))
                {
                    nativeDirection = ScriptDirectionData.GetDirection(options.Script ?? run.Script);
                    needsNativeDirection = false;
                }

                int utf16Length = codePoint.Utf16SequenceLength;
                graphemeOffset += utf16Length;
                int selectorFontIndex = 0;
                ushort glyphId = 0;
                bool skipSelector = consumed;
                bool selectorUsesBaseFont = hasFallback && !firstInGrapheme && IsPresentationSelector(codePoint);
                if (selectorUsesBaseFont && !skipSelector)
                {
                    selectorFontIndex = buffer[buffer.Count - 1].FallbackFontIndex;
                    if (selectorFontIndex != 0)
                    {
                        // The base glyph's fallback font supplies the selector if it
                        // maps one. Otherwise the base glyph already sets the presentation.
                        FontMetrics precedingMetrics = fallbackFonts!.GetAt(selectorFontIndex - 1).Value.FontMetrics;
                        skipSelector = !precedingMetrics.TryGetGlyphId(codePoint, out glyphId);
                    }
                }

                if (skipSelector)
                {
                    // The preceding glyph already accounts for this selector.
                    // Advance through the source without adding a glyph.
                    consumed = false;
                }
                else
                {
                    // Only cmap variation sequences and presentation selectors
                    // need the next character. Limit lookahead to this grapheme.
                    CodePoint? next = graphemeOffset < grapheme.Length
                        && (hasVariationSequences
                            || grapheme[graphemeOffset] == ShapingBuffer.TextPresentationSelector
                            || grapheme[graphemeOffset] == ShapingBuffer.EmojiPresentationSelector)
                        ? CodePoint.DecodeFromUtf16At(grapheme, graphemeOffset)
                        : null;

                    bool mapped = selectorFontIndex != 0 || buffer.TryGetGlyphId(metrics, codePoint, next, out glyphId, out consumed);

                    // Every glyph in a cluster uses the source indices of the
                    // first glyph that was emitted. Selectors consumed by cmap
                    // have no glyph slot and do not set these indices.
                    if (clusterStart != mappedClusterStart)
                    {
                        mappedClusterStart = clusterStart;
                        clusterStringIndex = stringIndex;
                        clusterGraphemeIndex = graphemeCount;
                    }

                    TextDirection direction = (TextDirection)bidiRuns[bidiRunIndex].Direction;
                    buffer.AddGlyph(glyphId, codePoint, direction, (ushort)textRunIndex, clusterStart, clusterStringIndex, clusterGraphemeIndex);
                    inputGlyphCount++;

                    if (hasFallback)
                    {
                        int index = buffer.Count - 1;
                        buffer[index].FallbackFontIndex = selectorFontIndex;
                        int precedingFontIndex = firstInGrapheme ? 0 : buffer[index - 1].FallbackFontIndex;

                        bool needsFallback;
                        if (buffer[index].IsDefaultIgnorable)
                        {
                            // Once fallback has started, an invisible character
                            // can stay with the font chosen for the preceding glyph.
                            // Keep a presentation selector with its base font even
                            // if cmap supplies glyph zero. A font change breaks the sequence.
                            needsFallback = !selectorUsesBaseFont && fallbackFonts is not null
                                && (!mapped || precedingFontIndex != 0);
                            next = null;
                        }
                        else
                        {
                            needsFallback = (!mapped || (!consumed && !MatchesPresentation(buffer, (ushort)textRunIndex, next, metrics)))
                                && RequiresGlyph(codePoint);

                            if (needsFallback && next is null && graphemeOffset < grapheme.Length)
                            {
                                // A fallback font can have a cmap variation even
                                // when the declared font has no variation table.
                                next = CodePoint.DecodeFromUtf16At(grapheme, graphemeOffset);
                            }
                        }

                        if (needsFallback)
                        {
                            // Finish the font search before reading the selector.
                            // Only the selected mapping can consume that character.
                            ResolveFallbackGlyph(buffer, index, next, ref fallbackFonts, precedingFontIndex, ref queried, ref consumed);
                        }
                    }
                }

                codePointIndex++;
                stringIndex += utf16Length;
                firstInGrapheme = false;
            }

            graphemeCount++;
        }

        if (inputGlyphCount > runStart)
        {
            buffer.AddInputRun(inputGlyphCount, run);
        }

        // Placeholders at the end have no following bidi run.
        for (; textRunIndex < textRuns.Count; textRunIndex++)
        {
            if (textRuns[textRunIndex].Placeholder.HasValue)
            {
                BidiRun endRun = new(BidiCharacterType.LeftToRight, 2, codePointIndex, 0);
                buffer.AddPlaceholder(CodePoint.ObjectReplacementChar, endRun, (ushort)textRunIndex, codePointIndex);
            }
        }

        return (nativeDirection, fallbackFonts, graphemeCount);
    }

    /// <summary>
    /// Maps Unicode mirrors before the substitution table runs.
    /// In right-to-left runs, the map applies Unicode Bidirectional Algorithm rule L4.
    /// In vertical layouts, it also uses the vertical mirror map for glyphs that rotate by default.
    /// </summary>
    /// <param name="fontMetrics">The font metrics used to look up mirrored glyph ids.</param>
    /// <param name="buffer">The substitution buffer whose glyphs will be rewritten in place.</param>
    /// <param name="start">The first glyph in the new font span.</param>
    private static void SubstituteBidiMirrors(FontMetrics fontMetrics, ShapingBuffer buffer, int start)
    {
        int end = buffer.SpanEnd;

        // The mapped run ends at each bidi boundary. Its font spans therefore
        // have one direction, which decides whether to look for bidi mirrors.
        if (buffer[start].Direction == TextDirection.RightToLeft)
        {
            for (int i = start; i < end; i++)
            {
                if (CodePoint.TryGetBidiMirror(buffer[i].CodePoint, out CodePoint mirror)
                    && fontMetrics.TryGetGlyphId(mirror, out ushort glyphId))
                {
                    buffer.Replace(i, glyphId, KnownFeatureTags.RightToLeftMirroredForms);
                }
            }
        }

        // Use the vertical mirror map for characters it covers.
        // TODO: Check which other characters need vertical alternates.
        // See Unicode Standard Annex #50, Vertical Alternates.
        // https://www.unicode.org/reports/tr50/#vertical_alternates
        if (buffer.TextOptions.LayoutMode.IsHorizontal())
        {
            return;
        }

        for (int i = start; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (AdvancedTypographicUtils.IsVerticalGlyph(CodePoint.GetVerticalOrientationType(data.CodePoint)))
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

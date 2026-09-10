// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <content>
/// Line break candidate collection and layout-level tailoring.
/// </content>
internal static partial class TextLayout
{
    private const int SoftHyphen = 0x00AD;
    private const int StandardHyphen = 0x2010;
    private const int StandardEllipsis = 0x2026;

    /// <summary>
    /// Shapes the text and composes the logical <see cref="TextLine"/> before
    /// width-dependent line breaking. Shaping runs on pooled pipeline state scoped
    /// entirely to this call; composition copies everything the result retains.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <returns>The logical text line and line break opportunities before line breaking.</returns>
    public static LogicalTextLine ComposeLogicalLine(ReadOnlySpan<char> text, TextOptions options)
    {
        // Composition retains run references in its layout data, so the runs are
        // built per call rather than reused from pooled state.
        IReadOnlyList<TextRun> runs = TextShaper.BuildTextRuns(text, options);
        using TextShaper.ShapedTextScope scope = TextShaper.ShapeText(text, options, runs);
        return ComposeLogicalLine(scope.Shaped, text, options);
    }

    /// <summary>
    /// Composes the logical <see cref="TextLine"/> from shaped glyph data before width-dependent line breaking.
    /// </summary>
    /// <param name="shapedText">The width-independent shaping state.</param>
    /// <param name="text">The original source text.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <returns>The logical text line and line break opportunities before line breaking.</returns>
    private static LogicalTextLine ComposeLogicalLine(
        in ShapedText shapedText,
        ReadOnlySpan<char> text,
        TextOptions options)
    {
        bool isHorizontalLayout = shapedText.LayoutMode.IsHorizontal();
        bool isVerticalLayout = shapedText.LayoutMode.IsVertical();
        bool isVerticalMixedLayout = shapedText.LayoutMode.IsVerticalMixed();
        bool hasTracking = options.Tracking != 0;

        int graphemeIndex = 0;
        int codePointIndex = 0;
        int glyphSearchBidiRunIndex = -1;
        int glyphSearchIndex = 0;
        TextLine textLine = new();
        int stringIndex = 0;
        List<WordSegmentRun> wordSegments = [];
        List<GlyphLayoutData> hyphenationMarkers = [];
        CodePoint? hyphenationMarkerCodePoint = GetHyphenationMarkerCodePoint(options);

        // Browsers hand layout each directional run's glyphs exactly as the shaper
        // finalized them, already in visual order, and consume that storage
        // directly. Materialize the projection once into a single array in that
        // same per-run visual order; every line entry below is a contiguous slice
        // of it, so no later stage can rearrange the glyphs inside an entry.
        //
        // This is composition's one deliberate heap allocation. The composed line
        // is retained and re-laid-out beyond the pooled shaping scope, so the
        // shaped stream must be copied into storage the line owns. It replaces the
        // former per-source-position collections, and renting it instead would
        // require a disposal contract the retained result does not have.
        PositionedGlyphMetrics[] glyphStorage = new PositionedGlyphMetrics[shapedText.GlyphCount];
        for (int i = 0; i < shapedText.GlyphCount; i++)
        {
            ref readonly ShapedGlyphInfo info = ref shapedText.Infos[i];
            if (info.IsPlaceholder)
            {
                // Placeholder records materialize generated metrics when their
                // entries are added below; their storage slots are never sliced.
                continue;
            }

            ShapedTextRun run = shapedText.Runs[info.RunIndex];
            ref readonly ShapedGlyphPosition position = ref shapedText.Positions[i];

            // The shaped result carries numbers only; composition resolves the
            // metrics instance from the owning font's cache by the same arguments
            // shaping used, so the same instance is returned.
            FontGlyphMetrics glyphMetrics = run.Font.FontMetrics.GetGlyphMetrics(
                info.CodePoint,
                info.GlyphId,
                run.TextRun.TextAttributes,
                run.TextRun.TextDecorations,
                shapedText.LayoutMode,
                run.TextRun.ColorFontSupport ?? options.ColorFontSupport,
                run.TextRun.FontPalette ?? options.FontPalette);

            // Full hinting substitutes whole pixel hinted advances on the flow axis only, so
            // the pen accumulates on the pixel grid exactly as the classic rasterizer's does
            // while cross axis inputs, such as the width vertical layout centres upright
            // glyphs with, keep their shaped values. Rotated glyphs in mixed vertical lines
            // flow by their width; upright vertical flow keeps its shaped fractional advances so standard and full hinting stack identically, with the emit snap landing both on the same pixels. Shaping advance
            // adjustments such as pair kerning are dropped with the fractional advance: the
            // classic pipeline never applied them, and folding a sub-pixel kern into a whole
            // pixel pen shifts a glyph a full pixel off the reference. The metrics resolve
            // the effective mode themselves so fonts forced onto full hinting by the
            // compatibility lists receive matching advances under any requested mode.
            ushort advanceWidth = position.AdvanceWidth;
            ushort advanceHeight = position.AdvanceHeight;
            float rawScaledPPEM = options.Dpi * run.Font.Size;
            bool flowsByWidth = isHorizontalLayout
                || (isVerticalMixedLayout && CodePoint.GetVerticalOrientationType(info.CodePoint) is VerticalOrientationType.Rotate or VerticalOrientationType.TransformRotate);

            if (flowsByWidth && glyphMetrics.TryGetHintedAdvanceWidth(run.Font.Size, options.Dpi, options.HintingMode, out float hintedAdvancePx))
            {
                float hintedUnits = hintedAdvancePx * glyphMetrics.UnitsPerEm * 72F / rawScaledPPEM;
                advanceWidth = (ushort)Math.Clamp(MathF.Floor(hintedUnits + 0.5F), 0F, ushort.MaxValue);
            }

            glyphStorage[i] = new PositionedGlyphMetrics(glyphMetrics, advanceWidth, advanceHeight, position.Offset, run.TextRun);
        }

        // Word-boundary segments are prepared with the logical line, while grapheme
        // and codepoint enumeration still own shaping data creation.
        SpanWordEnumerator wordEnumerator = new(text);
        while (wordEnumerator.MoveNext())
        {
            WordSegment wordSegment = wordEnumerator.Current;
            int wordSegmentGraphemeStart = graphemeIndex;

            SpanGraphemeEnumerator graphemeEnumerator = new(wordSegment.Span);
            while (graphemeEnumerator.MoveNext())
            {
                // Now enumerate through each codepoint in the grapheme.
                ReadOnlySpan<char> grapheme = graphemeEnumerator.Current.Span;
                int graphemeCodePointIndex = 0;
                SpanCodePointEnumerator codePointEnumerator = new(grapheme);
                while (codePointEnumerator.MoveNext())
                {
                    if (!shapedText.TryGetGlyphsAtOffset(
                        codePointIndex,
                        ref glyphSearchBidiRunIndex,
                        ref glyphSearchIndex,
                        out int glyphStart,
                        out int glyphCount,
                        out float pointSize,
                        out bool isSubstituted,
                        out bool isVerticalSubstitution,
                        out bool isDecomposed,
                        out int nextShapedCodePointIndex))
                    {
                        // Codepoint was skipped during original enumeration.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    BidiRun bidiRun = shapedText.BidiRuns[shapedText.BidiMap[codePointIndex]];

                    // Locate the entry's contiguous slice of the retained storage
                    // without copying a single glyph. Placeholder records sit at the
                    // edges of a source position's range and become standalone
                    // entries, exactly as browsers keep atomic inlines as their own
                    // line items.
                    int sliceStart = -1;
                    int sliceEnd = -1;
                    bool isCursiveScript = false;
                    float verticalGroupAdvance = 0;
                    for (int i = 0; i < glyphCount; i++)
                    {
                        int shapedGlyphIndex = glyphStart + i;
                        ref readonly ShapedGlyphInfo info = ref shapedText.Infos[shapedGlyphIndex];
                        isCursiveScript |= info.IsCursiveScript;
                        if (info.IsPlaceholder)
                        {
                            ShapedTextRun run = shapedText.Runs[info.RunIndex];
                            textLine.AddPlaceholder(
                                PlaceholderGlyphMetrics.Create(run.Font, run.TextRun, options.Dpi),
                                in run,
                                info.CodePointIndex,
                                graphemeIndex,
                                stringIndex,
                                isHorizontalLayout,
                                isVerticalMixedLayout,
                                options.LineSpacing);

                            continue;
                        }

                        if (sliceStart < 0)
                        {
                            sliceStart = shapedGlyphIndex;
                        }

                        sliceEnd = shapedGlyphIndex;

                        if (!isHorizontalLayout)
                        {
                            // Accumulate while this loop already visits every
                            // positioned glyph, so the upright path needs no second
                            // pass over the slice.
                            ref readonly PositionedGlyphMetrics positioned = ref glyphStorage[shapedGlyphIndex];
                            FontGlyphMetrics positionedMetrics = positioned.Metrics;
                            float scaleAY = shapedText.Runs[info.RunIndex].PointSize / positionedMetrics.ScaleFactor.Y;
                            float positionedAdvance = positioned.AdvanceHeight * scaleAY;
                            VerticalMetrics verticalMetrics = positionedMetrics.FontMetrics.VerticalMetrics;
                            if (verticalMetrics.Synthesized && positioned.AdvanceHeight != 0)
                            {
                                // Browsers provide the device-rounded fallback height to
                                // shaping as the nominal vertical advance. Replace only that
                                // nominal component so positioning deltas survive, while the
                                // zero advance shaping assigned to marks remains untouched.
                                float nominalAdvance = positionedMetrics.AdvanceHeight * scaleAY;

                                // scaleAY converts design units to the DPI-normalized
                                // layout space consumed by TextLayout. Round in target
                                // device pixels, then return to that layout space.
                                float deviceScale = options.Dpi;
                                float browserAdvance = (MathF.Floor((verticalMetrics.Ascender * scaleAY * deviceScale) + .5F)
                                    + MathF.Floor((-verticalMetrics.Descender * scaleAY * deviceScale) + .5F)) / deviceScale;
                                positionedAdvance += browserAdvance - nominalAdvance;
                            }

                            verticalGroupAdvance += positionedAdvance;
                        }
                    }

                    if (sliceStart < 0)
                    {
                        // This source codepoint was skipped during shaping; any placeholder
                        // sharing the same source offset has already been added above.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    ReadOnlyMemory<PositionedGlyphMetrics> metrics = glyphStorage.AsMemory(sliceStart, sliceEnd - sliceStart + 1);
                    ReadOnlySpan<PositionedGlyphMetrics> metricsSpan = metrics.Span;
                    Font entryFont = shapedText.Runs[shapedText.Infos[sliceStart].RunIndex].Font;
                    FontGlyphMetrics glyph = metricsSpan[0].Metrics;

                    // Retrieve the current codepoint from the enumerator.
                    // If the glyph represents a substituted codepoint and the substitution is a single codepoint substitution,
                    // or composite glyph, then the codepoint should be updated to the substitution value so we can read its properties.
                    // Substitutions that are decomposed glyphs will have multiple metrics and any layout should be based on the
                    // original codepoint.
                    //
                    // Note: Not all glyphs in a font will have a codepoint associated with them. e.g. most compositions, ligatures, etc.
                    CodePoint codePoint = codePointEnumerator.Current;
                    if (isSubstituted && metricsSpan.Length == 1)
                    {
                        codePoint = glyph.CodePoint;
                    }

                    // Determine whether the glyph advance should be calculated using vertical or horizontal metrics
                    // For vertical mixed layout we will rotate glyphs with the vertical orientation type R or TR
                    // which do not already have a vertical substitution.
                    bool shouldRotate = isVerticalMixedLayout &&
                         !isVerticalSubstitution &&
                         CodePoint.GetVerticalOrientationType(codePoint) is
                                     VerticalOrientationType.Rotate or
                                     VerticalOrientationType.TransformRotate;

                    // Determine whether the glyph advance should be offset for vertical layout.
                    bool shouldOffset = isVerticalLayout &&
                        !isVerticalSubstitution &&
                         CodePoint.GetVerticalOrientationType(codePoint) is
                                     VerticalOrientationType.Rotate or
                                     VerticalOrientationType.TransformRotate;

                    if (CodePoint.IsVariationSelector(codePoint))
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    // Calculate the advance for the current codepoint.
                    float glyphAdvance;
                    if (isHorizontalLayout || shouldRotate)
                    {
                        glyphAdvance = metricsSpan[0].AdvanceWidth;
                    }
                    else
                    {
                        glyphAdvance = metricsSpan[0].AdvanceHeight;
                    }

                    bool usePositionedVerticalAdvances = false;

                    bool isSoftHyphen = codePoint.Value == SoftHyphen;
                    if (isSoftHyphen)
                    {
                        glyphAdvance = 0;
                    }
                    else if (CodePoint.IsTabulation(codePoint))
                    {
                        if (options.TabWidth > -1F)
                        {
                            // Do not use the default font tab width. Instead find the advance for the space glyph
                            // and multiply that by the options value.
                            CodePoint space = new(0x0020);
                            if (glyph.FontMetrics.TryGetGlyphId(space, out ushort spaceGlyphId))
                            {
                                FontGlyphMetrics spaceMetrics = glyph.FontMetrics.GetGlyphMetrics(
                                      space,
                                      spaceGlyphId,
                                      glyph.TextAttributes,
                                      glyph.TextDecorations,
                                      shapedText.LayoutMode,
                                      metricsSpan[0].TextRun.ColorFontSupport ?? options.ColorFontSupport,
                                      metricsSpan[0].TextRun.FontPalette ?? options.FontPalette);

                                // The tab advance lives only in the positioned snapshot;
                                // the metrics instance is shared and must not be mutated.
                                // Writing through the storage slot keeps the entry's
                                // slice and the retained stream in agreement.
                                if (isHorizontalLayout || shouldRotate)
                                {
                                    glyphAdvance = spaceMetrics.AdvanceWidth * options.TabWidth;
                                    glyphStorage[sliceStart] = new PositionedGlyphMetrics(glyph, (ushort)glyphAdvance, metricsSpan[0].AdvanceHeight, metricsSpan[0].Offset, metricsSpan[0].TextRun);
                                }
                                else
                                {
                                    glyphAdvance = spaceMetrics.AdvanceHeight * options.TabWidth;
                                    glyphStorage[sliceStart] = new PositionedGlyphMetrics(glyph, metricsSpan[0].AdvanceWidth, (ushort)glyphAdvance, metricsSpan[0].Offset, metricsSpan[0].TextRun);
                                }
                            }
                        }
                    }
                    else if (metricsSpan.Length == 1 && (CodePoint.IsZeroWidthJoiner(codePoint) || CodePoint.IsZeroWidthNonJoiner(codePoint)))
                    {
                        // The zero-width joiner characters should be ignored when determining word or
                        // line break boundaries so are safe to skip here. Any existing instances are the result of font error
                        // unless multiple metrics are associated with code point. In this case they are most likely the result
                        // of a substitution and shouldn't be ignored.
                        glyphAdvance = 0;
                    }
                    else if (!CodePoint.IsNewLine(codePoint))
                    {
                        // Standard text. Browser layout retains every shaped glyph
                        // advance; the entry's advance is the sum over its slice while
                        // the per-glyph values remain in the retained storage for the
                        // positioned walk.
                        usePositionedVerticalAdvances = !isHorizontalLayout && !shouldRotate;
                        if (isHorizontalLayout || shouldRotate)
                        {
                            for (int i = 1; i < metricsSpan.Length; i++)
                            {
                                glyphAdvance += metricsSpan[i].AdvanceWidth;
                            }
                        }
                        else
                        {
                            for (int i = 1; i < metricsSpan.Length; i++)
                            {
                                glyphAdvance += metricsSpan[i].AdvanceHeight;
                            }
                        }
                    }

                    // Now scale the advance. We use inches for comparison.
                    if (isHorizontalLayout || shouldRotate)
                    {
                        glyphAdvance *= pointSize / glyph.ScaleFactor.X;
                    }
                    else if (usePositionedVerticalAdvances)
                    {
                        // Ordinary upright text follows the positioned glyph stream.
                        // Special characters above deliberately keep their established
                        // zero or caller-defined advance instead.
                        glyphAdvance = verticalGroupAdvance;
                    }
                    else
                    {
                        // Tabs, soft hyphens, joiners, and hard breaks have already
                        // selected their logical advance. Scale that value normally
                        // rather than replacing it with a font-wide vertical fallback.
                        glyphAdvance *= pointSize / glyph.ScaleFactor.Y;
                    }

                    int graphemeCodePointMax = CodePoint.GetCodePointCount(grapheme) - 1;
                    int graphemeCodePointEnd = codePointIndex - graphemeCodePointIndex + graphemeCodePointMax;

                    // The next distinct source start, not an individual glyph's
                    // coverage count, tells layout whether this is the final shaped
                    // input represented by the current .NET grapheme.
                    bool isLastInGrapheme = nextShapedCodePointIndex > graphemeCodePointEnd;

                    // Browsers attach letter spacing once to the final visual
                    // glyph for a shaped source start, preserving relative
                    // positions between a base and its combining marks. Adding the
                    // spacing to the entry's advance realizes exactly that: the
                    // positioned walk assigns each entry's residual advance to the
                    // final glyph of its slice. Browsers apply the same boundary.
                    // CSS Text §8.2.1 governs the spacing:
                    // https://www.w3.org/TR/css-text-4/#letter-spacing-property
                    if (isLastInGrapheme && hasTracking)
                    {
                        // Tab characters and line terminators never receive tracking.
                        // CSS Text §8.2.1 also requires
                        // cursive joins to remain unspaced while word separators receive it:
                        // https://www.w3.org/TR/css-text-4/#cursive-tracking
                        if ((!isCursiveScript || CodePoint.IsWhiteSpace(codePoint))
                            && !CodePoint.IsTabulation(codePoint)
                            && !CodePoint.IsNewLine(codePoint))
                        {
                            if (isHorizontalLayout || shouldRotate)
                            {
                                glyphAdvance += options.Tracking * glyph.FontMetrics.UnitsPerEm * (pointSize / glyph.ScaleFactor.X);
                            }
                            else
                            {
                                glyphAdvance += options.Tracking * glyph.FontMetrics.UnitsPerEm * (pointSize / glyph.ScaleFactor.Y);
                            }
                        }
                    }

                    // Convert design-space units to pixels based on the target point size.
                    // ScaleFactor.Y represents the vertical UPEM scaling factor for this glyph.
                    float scaleY = pointSize / glyph.ScaleFactor.Y;

                    // Choose which metrics table to use based on layout orientation.
                    // Horizontal is the default; vertical fonts use VMTX if available.
                    IMetricsHeader metricsHeader = isHorizontalLayout || shouldRotate
                        ? glyph.FontMetrics.HorizontalMetrics
                        : glyph.FontMetrics.VerticalMetrics;

                    // Ascender and descender are stored in font design units, so scale them to pixels.
                    float ascender = metricsHeader.Ascender * scaleY;

                    // Match browser line-height calculation logic.
                    // Reference: https://www.w3.org/TR/CSS2/visudet.html#propdef-line-height
                    // The line height in CSS is based on a multiple of the font-size (pointSize),
                    // but fonts may define a custom LineHeight in their metrics that differs from UPEM.
                    float descender = Math.Abs(metricsHeader.Descender * scaleY);
                    float lineHeight = glyph.UnitsPerEm * scaleY;

                    // The delta centers the font's line box within the CSS line box when
                    // LineHeight differs from the nominal font size.
                    float delta = ((metricsHeader.LineHeight * scaleY) - lineHeight) * 0.5F;

                    // Adjust ascender and descender symmetrically by delta to preserve visual balance.
                    ascender -= delta;
                    descender -= delta;

                    GlyphLayoutMode mode = GlyphLayoutMode.Horizontal;
                    if (isVerticalLayout)
                    {
                        mode = GlyphLayoutMode.Vertical;
                    }
                    else if (isVerticalMixedLayout)
                    {
                        mode = shouldRotate ? GlyphLayoutMode.VerticalRotated : GlyphLayoutMode.Vertical;
                    }

                    int hyphenationMarkerIndex = -1;
                    if (isSoftHyphen && hyphenationMarkerCodePoint.HasValue)
                    {
                        // U+00AD is shaped as an invisible source entry, but if this exact
                        // discretionary break is later selected we need a visible marker with
                        // the same run, font attributes, bidi mapping, and source mapping. Build
                        // that marker here while those values are already in hand; BreakLines can
                        // then account for its advance without rescanning or reshaping the line.
                        hyphenationMarkerIndex = hyphenationMarkers.Count;
                        hyphenationMarkers.Add(CreateGeneratedMarker(
                            metricsSpan[0],
                            pointSize,
                            bidiRun,
                            graphemeIndex,
                            isLastInGrapheme,
                            codePointIndex,
                            graphemeCodePointIndex,
                            stringIndex,
                            hyphenationMarkerCodePoint.Value,
                            shapedText.LayoutMode,
                            entryFont,
                            options));
                    }

                    // One entry per shaped source position, holding the whole slice.
                    // Line breaking and reordering move this unit; the glyphs inside
                    // keep the shaper's visual stream untouched.
                    textLine.Add(
                        metrics,
                        entryFont,
                        pointSize,
                        glyphAdvance,
                        lineHeight,
                        ascender,
                        descender,
                        delta,
                        bidiRun,
                        graphemeIndex,
                        isLastInGrapheme,
                        codePointIndex,
                        graphemeCodePointIndex,
                        shouldRotate || shouldOffset,
                        isDecomposed,
                        stringIndex,
                        mode,
                        options.LineSpacing,
                        hyphenationMarkerIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }

                stringIndex += grapheme.Length;
                graphemeIndex++;
            }

            wordSegments.Add(new WordSegmentRun(
                wordSegmentGraphemeStart,
                graphemeIndex,
                wordSegment.Utf16Offset,
                wordSegment.Utf16Offset + wordSegment.Utf16Length));
        }

        // Placeholders do not consume source text. A placeholder inserted at
        // the final source position has no following codepoint to visit in
        // the main loop, so we add those trailing placeholder entries here.
        if (shapedText.TryGetGlyphsAtOffset(
            codePointIndex,
            ref glyphSearchBidiRunIndex,
            ref glyphSearchIndex,
            out int endStart,
            out int endCount,
            out _,
            out _,
            out _,
            out _,
            out _))
        {
            for (int i = 0; i < endCount; i++)
            {
                ref readonly ShapedGlyphInfo info = ref shapedText.Infos[endStart + i];
                if (info.IsPlaceholder)
                {
                    ShapedTextRun run = shapedText.Runs[info.RunIndex];
                    textLine.AddPlaceholder(
                        PlaceholderGlyphMetrics.Create(run.Font, run.TextRun, options.Dpi),
                        in run,
                        info.CodePointIndex,
                        graphemeIndex,
                        stringIndex,
                        isHorizontalLayout,
                        isVerticalMixedLayout,
                        options.LineSpacing);
                }
            }
        }

        // Browsers retain the source text and query break opportunities through a
        // lazy cursor during line filling rather than materializing the paragraph's
        // candidates. Retain the text once so every wrapping length can run the
        // same streaming query.
        return new LogicalTextLine(textLine, text.ToArray(), wordSegments, hyphenationMarkers);
    }

    /// <summary>
    /// Applies line-break opportunities to a shaped <see cref="TextLine"/> using the configured
    /// <see cref="TextOptions.WordBreaking"/> behavior and supplied wrapping length.
    /// Finalizes each line (trimming trailing whitespace and applying bidi reordering) and applies
    /// justification where requested.
    /// </summary>
    /// <param name="logicalLine">The logical text line and line break opportunities to break.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <param name="wrappingLength">The wrapping length in pixels.</param>
    /// <returns>The shaped, line-broken, finalized text box ready for glyph placement.</returns>
    public static TextBox BreakLines(
        in LogicalTextLine logicalLine,
        TextOptions options,
        float wrappingLength)
    {
        int maxLines = options.MaxLines;

        if (maxLines == 0)
        {
            TextDirection emptyTextDirection = options.TextDirection == TextDirection.RightToLeft
                ? TextDirection.RightToLeft
                : TextDirection.LeftToRight;

            return new TextBox([], emptyTextDirection);
        }

        TextDirection textDirection = GetTextDirection(logicalLine, options);

        List<TextLine> textLines = [];
        TextLineBreakEnumerator lineEnumerator = new(logicalLine, options);

        while (lineEnumerator.MoveNext(wrappingLength))
        {
            textLines.Add(lineEnumerator.Current);
        }

        return new TextBox(textLines, textDirection);
    }

    /// <summary>
    /// Gets the block-level text direction for a prepared logical line.
    /// </summary>
    /// <param name="logicalLine">The prepared logical line.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <returns>The block-level text direction.</returns>
    public static TextDirection GetTextDirection(in LogicalTextLine logicalLine, TextOptions options)
        => options.TextDirection == TextDirection.Auto && logicalLine.TextLine.Count > 0
            ? logicalLine.TextLine[0].TextDirection
            : options.TextDirection;

    /// <summary>
    /// Gets the configured hyphenation marker codepoint.
    /// </summary>
    /// <remarks>
    /// Also the switch the line-filling cursor uses to decide whether soft-hyphen
    /// break opportunities participate in wrapping at all.
    /// </remarks>
    /// <param name="options">The text options used for layout.</param>
    /// <returns>The configured hyphenation marker codepoint, or <see langword="null"/> when hyphenation is disabled.</returns>
    public static CodePoint? GetHyphenationMarkerCodePoint(TextOptions options)
        => options.TextHyphenation switch
        {
            TextHyphenation.Standard => new CodePoint(StandardHyphen),
            TextHyphenation.Custom => options.CustomHyphen,
            _ => null
        };

    /// <summary>
    /// Creates a visible generated marker that matches the layout style of the anchor entry.
    /// </summary>
    /// <param name="anchor">The positioned anchor glyph that supplies font, run, attributes, and decorations.</param>
    /// <param name="pointSize">The point size at which the marker is rendered.</param>
    /// <param name="bidiRun">The bidi run that the marker belongs to.</param>
    /// <param name="graphemeIndex">The source grapheme index to map the marker to.</param>
    /// <param name="isLastInGrapheme">Whether the marker maps to the last entry in its grapheme.</param>
    /// <param name="codePointIndex">The source codepoint index to map the marker to.</param>
    /// <param name="graphemeCodePointIndex">The source codepoint-in-grapheme index to map the marker to.</param>
    /// <param name="stringIndex">The UTF-16 source index to map the marker to.</param>
    /// <param name="markerCodePoint">The marker codepoint to create.</param>
    /// <param name="layoutMode">The layout mode used to calculate marker orientation.</param>
    /// <param name="font">The font used to shape and render the marker.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <returns>The generated marker entry.</returns>
    public static GlyphLayoutData CreateGeneratedMarker(
        PositionedGlyphMetrics anchor,
        float pointSize,
        BidiRun bidiRun,
        int graphemeIndex,
        bool isLastInGrapheme,
        int codePointIndex,
        int graphemeCodePointIndex,
        int stringIndex,
        CodePoint markerCodePoint,
        LayoutMode layoutMode,
        Font font,
        TextOptions options)
    {
        FontGlyphMetrics anchorMetric = anchor.Metrics;
        _ = anchorMetric.FontMetrics.TryGetGlyphId(markerCodePoint, out ushort markerGlyphId);

        FontGlyphMetrics markerMetric = anchorMetric.FontMetrics.GetGlyphMetrics(
            markerCodePoint,
            markerGlyphId,
            anchorMetric.TextAttributes,
            anchorMetric.TextDecorations,
            layoutMode,
            anchor.TextRun.ColorFontSupport ?? options.ColorFontSupport,
            anchor.TextRun.FontPalette ?? options.FontPalette);

        bool isHorizontalLayout = layoutMode.IsHorizontal();
        bool isVerticalLayout = layoutMode.IsVertical();
        bool isVerticalMixedLayout = layoutMode.IsVerticalMixed();
        bool shouldRotate = isVerticalMixedLayout &&
            CodePoint.GetVerticalOrientationType(markerCodePoint) is
                        VerticalOrientationType.Rotate or
                        VerticalOrientationType.TransformRotate;

        bool shouldOffset = isVerticalLayout &&
            CodePoint.GetVerticalOrientationType(markerCodePoint) is
                        VerticalOrientationType.Rotate or
                        VerticalOrientationType.TransformRotate;

        GlyphLayoutMode markerMode = GlyphLayoutMode.Horizontal;
        if (isVerticalLayout)
        {
            markerMode = GlyphLayoutMode.Vertical;
        }
        else if (isVerticalMixedLayout)
        {
            markerMode = shouldRotate ? GlyphLayoutMode.VerticalRotated : GlyphLayoutMode.Vertical;
        }

        float markerAdvance = isHorizontalLayout || shouldRotate
            ? markerMetric.AdvanceWidth * (pointSize / markerMetric.ScaleFactor.X)
            : markerMetric.AdvanceHeight * (pointSize / markerMetric.ScaleFactor.Y);

        // Generated markers must reserve the same CSS line box as ordinary glyphs
        // from the same run so truncation and discretionary hyphens do not collapse
        // or expand line spacing.
        float markerScaleY = pointSize / markerMetric.ScaleFactor.Y;
        IMetricsHeader markerMetricsHeader = isHorizontalLayout || shouldRotate
            ? markerMetric.FontMetrics.HorizontalMetrics
            : markerMetric.FontMetrics.VerticalMetrics;

        float markerAscender = markerMetricsHeader.Ascender * markerScaleY;
        float markerDescender = Math.Abs(markerMetricsHeader.Descender * markerScaleY);
        float markerLineHeight = markerMetric.UnitsPerEm * markerScaleY;
        float markerDelta = ((markerMetricsHeader.LineHeight * markerScaleY) - markerLineHeight) * 0.5F;

        markerAscender -= markerDelta;
        markerDescender -= markerDelta;

        FontRectangle markerBox = FontGlyphMetrics.ShouldSkipGlyphRendering(markerMetric.CodePoint)
            ? FontRectangle.Empty
            : markerMetric.GetBoundingBox(markerMode, Vector2.Zero, pointSize, anchor.TextRun, Vector2.Zero, new Vector2(markerMetric.AdvanceWidth, markerMetric.AdvanceHeight));

        // Generated markers are not part of the shaped stream, so the entry owns
        // its single-glyph storage.
        return new GlyphLayoutData(
            new PositionedGlyphMetrics[] { new(markerMetric, markerMetric.AdvanceWidth, markerMetric.AdvanceHeight, Vector2.Zero, anchor.TextRun) },
            font,
            pointSize,
            markerAdvance,
            markerLineHeight * options.LineSpacing,
            markerAscender,
            markerDescender,
            markerDelta,
            MathF.Min(0, markerBox.Y),
            bidiRun,
            graphemeIndex,
            isLastInGrapheme,
            codePointIndex,
            graphemeCodePointIndex,
            shouldRotate || shouldOffset,
            false,
            stringIndex);
    }

    /// <summary>
    /// Gets the configured ellipsis marker codepoint.
    /// </summary>
    /// <param name="options">The text options used for layout.</param>
    /// <returns>The configured ellipsis marker codepoint, or <see langword="null"/> when ellipsis is disabled.</returns>
    public static CodePoint? GetEllipsisMarkerCodePoint(TextOptions options)
        => options.TextEllipsis switch
        {
            TextEllipsis.Standard => new CodePoint(StandardEllipsis),
            TextEllipsis.Custom => options.CustomEllipsis,
            _ => null
        };
}

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
    /// Composes the logical <see cref="TextLine"/> from shaped glyph data before width-dependent line breaking.
    /// </summary>
    /// <param name="shapedText">The width-independent shaping state.</param>
    /// <param name="text">The original source text.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <returns>The logical text line and line break opportunities before line breaking.</returns>
    public static LogicalTextLine ComposeLogicalLine(
        in ShapedText shapedText,
        ReadOnlySpan<char> text,
        TextOptions options)
    {
        bool isHorizontalLayout = shapedText.LayoutMode.IsHorizontal();
        bool isVerticalLayout = shapedText.LayoutMode.IsVertical();
        bool isVerticalMixedLayout = shapedText.LayoutMode.IsVerticalMixed();

        int graphemeIndex = 0;
        int codePointIndex = 0;
        int glyphSearchIndex = 0;
        TextLine textLine = new();
        int stringIndex = 0;
        List<WordSegmentRun> wordSegments = [];
        List<GlyphLayoutData> hyphenationMarkers = [];
        CodePoint? hyphenationMarkerCodePoint = GetHyphenationMarkerCodePoint(options);

        // No glyph should contain more than 64 metrics.
        // We do a sanity check below just in case.
        Span<float> decomposedAdvancesBuffer = stackalloc float[64];

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
                    if (!shapedText.Positionings.TryGetGlyphMetricsAtOffset(
                        codePointIndex,
                        ref glyphSearchIndex,
                        out float pointSize,
                        out bool isSubstituted,
                        out bool isVerticalSubstitution,
                        out bool isDecomposed,
                        out IReadOnlyList<GlyphPositioningCollection.GlyphPositioningData>? glyphData))
                    {
                        // Codepoint was skipped during original enumeration.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    List<GlyphMetrics> metrics = [];
                    for (int i = 0; i < glyphData.Count; i++)
                    {
                        GlyphPositioningCollection.GlyphPositioningData data = glyphData[i];
                        if (data.Data.IsPlaceholder)
                        {
                            textLine.AddPlaceholder(
                                data,
                                graphemeIndex,
                                stringIndex,
                                isHorizontalLayout,
                                isVerticalMixedLayout,
                                options.LineSpacing);

                            continue;
                        }

                        metrics.Add(data.Metrics);
                    }

                    if (metrics.Count == 0)
                    {
                        // This source codepoint was skipped during shaping; any placeholder
                        // sharing the same source offset has already been added above.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    GlyphMetrics glyph = metrics[0];

                    // Retrieve the current codepoint from the enumerator.
                    // If the glyph represents a substituted codepoint and the substitution is a single codepoint substitution,
                    // or composite glyph, then the codepoint should be updated to the substitution value so we can read its properties.
                    // Substitutions that are decomposed glyphs will have multiple metrics and any layout should be based on the
                    // original codepoint.
                    //
                    // Note: Not all glyphs in a font will have a codepoint associated with them. e.g. most compositions, ligatures, etc.
                    CodePoint codePoint = codePointEnumerator.Current;
                    if (isSubstituted && metrics.Count == 1)
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

                    // This should never happen, but we need to ensure that the buffer is large enough
                    // if, for some crazy reason, a glyph does contain more than 64 metrics.
                    Span<float> decomposedAdvances = metrics.Count > decomposedAdvancesBuffer.Length
                        ? new float[metrics.Count]
                        : decomposedAdvancesBuffer[..(isDecomposed ? metrics.Count : 1)];

                    float glyphAdvance;
                    if (isHorizontalLayout || shouldRotate)
                    {
                        glyphAdvance = glyph.AdvanceWidth;
                    }
                    else
                    {
                        glyphAdvance = glyph.AdvanceHeight;
                    }

                    decomposedAdvances[0] = glyphAdvance;

                    bool isSoftHyphen = codePoint.Value == SoftHyphen;
                    if (isSoftHyphen)
                    {
                        glyphAdvance = 0;
                        decomposedAdvances[0] = 0;
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
                                GlyphMetrics spaceMetrics = glyph.FontMetrics.GetGlyphMetrics(
                                      space,
                                      spaceGlyphId,
                                      glyph.TextAttributes,
                                      glyph.TextDecorations,
                                      shapedText.LayoutMode,
                                      options.ColorFontSupport);

                                if (isHorizontalLayout || shouldRotate)
                                {
                                    glyphAdvance = spaceMetrics.AdvanceWidth * options.TabWidth;
                                    glyph.SetAdvanceWidth((ushort)glyphAdvance);
                                }
                                else
                                {
                                    glyphAdvance = spaceMetrics.AdvanceHeight * options.TabWidth;
                                    glyph.SetAdvanceHeight((ushort)glyphAdvance);
                                }
                            }
                        }
                    }
                    else if (metrics.Count == 1 && (CodePoint.IsZeroWidthJoiner(codePoint) || CodePoint.IsZeroWidthNonJoiner(codePoint)))
                    {
                        // The zero-width joiner characters should be ignored when determining word or
                        // line break boundaries so are safe to skip here. Any existing instances are the result of font error
                        // unless multiple metrics are associated with code point. In this case they are most likely the result
                        // of a substitution and shouldn't be ignored.
                        glyphAdvance = 0;
                        decomposedAdvances[0] = 0;
                    }
                    else if (!CodePoint.IsNewLine(codePoint))
                    {
                        // Standard text.
                        // If decomposed we need to add the advance; otherwise, use the largest advance for the metrics.
                        if (isHorizontalLayout || shouldRotate)
                        {
                            for (int i = 1; i < metrics.Count; i++)
                            {
                                float a = metrics[i].AdvanceWidth;
                                if (isDecomposed)
                                {
                                    glyphAdvance += a;
                                    decomposedAdvances[i] = a;
                                }
                                else if (a > glyphAdvance)
                                {
                                    glyphAdvance = a;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 1; i < metrics.Count; i++)
                            {
                                float a = metrics[i].AdvanceHeight;
                                if (isDecomposed)
                                {
                                    glyphAdvance += a;
                                    decomposedAdvances[i] = a;
                                }
                                else if (a > glyphAdvance)
                                {
                                    glyphAdvance = a;
                                }
                            }
                        }
                    }

                    // Now scale the advance. We use inches for comparison.
                    if (isHorizontalLayout || shouldRotate)
                    {
                        float scaleAX = pointSize / glyph.ScaleFactor.X;
                        glyphAdvance *= scaleAX;
                        for (int i = 0; i < decomposedAdvances.Length; i++)
                        {
                            decomposedAdvances[i] *= scaleAX;
                        }
                    }
                    else
                    {
                        float scaleAY = pointSize / glyph.ScaleFactor.Y;
                        glyphAdvance *= scaleAY;
                        for (int i = 0; i < decomposedAdvances.Length; i++)
                        {
                            decomposedAdvances[i] *= scaleAY;
                        }
                    }

                    int graphemeCodePointMax = CodePoint.GetCodePointCount(grapheme) - 1;

                    // For non-decomposed glyphs the length is always 1.
                    for (int i = 0; i < decomposedAdvances.Length; i++)
                    {
                        // Determine if this is the last codepoint in the grapheme.
                        bool isLastInGrapheme = graphemeCodePointIndex == graphemeCodePointMax && i == decomposedAdvances.Length - 1;

                        float decomposedAdvance = decomposedAdvances[i];

                        // Work out the scaled metrics for the glyph.
                        GlyphMetrics metric = metrics[i];

                        // Adjust the advance for the last decomposed glyph to add tracking if applicable.
                        // Tracking should only be added once per grapheme, so only on the last codepoint of the grapheme.
                        if (isLastInGrapheme && options.Tracking != 0 && i == decomposedAdvances.Length - 1)
                        {
                            // Tracking should not be applied to tab characters or non-rendered codepoints.
                            if (!CodePoint.IsTabulation(codePoint) && !UnicodeUtility.ShouldNotBeRendered(codePoint))
                            {
                                if (isHorizontalLayout || shouldRotate)
                                {
                                    float scaleAX = pointSize / glyph.ScaleFactor.X;
                                    decomposedAdvance += options.Tracking * metric.FontMetrics.UnitsPerEm * scaleAX;
                                }
                                else
                                {
                                    float scaleAY = pointSize / glyph.ScaleFactor.Y;
                                    decomposedAdvance += options.Tracking * metric.FontMetrics.UnitsPerEm * scaleAY;
                                }
                            }
                        }

                        // Convert design-space units to pixels based on the target point size.
                        // ScaleFactor.Y represents the vertical UPEM scaling factor for this glyph.
                        float scaleY = pointSize / metric.ScaleFactor.Y;

                        // Choose which metrics table to use based on layout orientation.
                        // Horizontal is the default; vertical fonts use VMTX if available.
                        IMetricsHeader metricsHeader = isHorizontalLayout || shouldRotate
                            ? metric.FontMetrics.HorizontalMetrics
                            : metric.FontMetrics.VerticalMetrics;

                        // Ascender and descender are stored in font design units, so scale them to pixels.
                        float ascender = metricsHeader.Ascender * scaleY;

                        // Match browser line-height calculation logic.
                        // Reference: https://www.w3.org/TR/CSS2/visudet.html#propdef-line-height
                        // The line height in CSS is based on a multiple of the font-size (pointSize),
                        // but fonts may define a custom LineHeight in their metrics that differs from UPEM.
                        float descender = Math.Abs(metricsHeader.Descender * scaleY);
                        float lineHeight = metric.UnitsPerEm * scaleY;

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
                                glyph,
                                pointSize,
                                shapedText.BidiRuns[shapedText.BidiMap[codePointIndex]],
                                graphemeIndex,
                                isLastInGrapheme,
                                codePointIndex,
                                graphemeCodePointIndex,
                                stringIndex,
                                hyphenationMarkerCodePoint.Value,
                                shapedText.LayoutMode,
                                options));
                        }

                        // Add our metrics to the line.
                        textLine.Add(
                            isDecomposed ? new GlyphMetrics[] { metric } : metrics,
                            pointSize,
                            decomposedAdvance,
                            lineHeight,
                            ascender,
                            descender,
                            delta,
                            shapedText.BidiRuns[shapedText.BidiMap[codePointIndex]],
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
                    }

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
        if (shapedText.Positionings.TryGetGlyphMetricsAtOffset(
            codePointIndex,
            ref glyphSearchIndex,
            out _,
            out _,
            out _,
            out _,
            out IReadOnlyList<GlyphPositioningCollection.GlyphPositioningData>? endGlyphData))
        {
            for (int i = 0; i < endGlyphData.Count; i++)
            {
                GlyphPositioningCollection.GlyphPositioningData data = endGlyphData[i];
                if (data.Data.IsPlaceholder)
                {
                    textLine.AddPlaceholder(
                        data,
                        graphemeIndex,
                        stringIndex,
                        isHorizontalLayout,
                        isVerticalMixedLayout,
                        options.LineSpacing);
                }
            }
        }

        // Line break candidates are width-independent and belong with the composed logical line.
        List<LineBreak> lineBreaks = CollectLineBreaks(text, hyphenationMarkerCodePoint.HasValue);

        return new LogicalTextLine(textLine, lineBreaks, wordSegments, hyphenationMarkers);
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
    /// Collects the line break opportunities used by the wrapping loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="LineBreakEnumerator"/> is the Unicode-conforming default line breaker. Its default
    /// constructor remains independent from layout policy so the Unicode conformance tests continue to
    /// describe only the default UAX #14 behavior. This method is the boundary where layout-specific
    /// tailoring is requested.
    /// </para>
    /// <para>
    /// The line breaker itself is streaming and does not allocate. Layout materializes the resulting
    /// break opportunities because the line fitting loop scans the same candidates repeatedly while it
    /// removes finalized lines from the front of the shaped text line.
    /// </para>
    /// <para>
    /// Solidus handling is intentionally conservative. UAX #14 classifies U+002F SOLIDUS as SY, which
    /// gives ordinary text a break opportunity after a slash. That is valid for the default algorithm,
    /// but it produced undesirable layout in issue 448 for ordinary slash-separated text. At the same
    /// time, UAX #14 section 8 explicitly calls out URL tailoring that can allow breaks after slash
    /// separated URL segments even when the next segment starts with a digit. The result here is:
    /// keep default slash behavior for standard enumeration, suppress ordinary slash breaks for layout,
    /// and reintroduce the narrow URL numeric-segment break only for URL-like runs.
    /// </para>
    /// </remarks>
    /// <param name="text">The original source text being laid out.</param>
    /// <param name="includeHyphenationBreaks">Whether soft-hyphen break opportunities should be included.</param>
    /// <returns>The ordered line break opportunities after layout-level tailoring.</returns>
    private static List<LineBreak> CollectLineBreaks(ReadOnlySpan<char> text, bool includeHyphenationBreaks)
    {
        LineBreakEnumerator lineBreakEnumerator = new(text, tailorUrls: true);
        List<LineBreak> lineBreaks = [];
        while (lineBreakEnumerator.MoveNext())
        {
            LineBreak lineBreak = lineBreakEnumerator.Current;
            if (lineBreak.IsHyphenationBreak && !includeHyphenationBreaks)
            {
                continue;
            }

            lineBreaks.Add(lineBreak);
        }

        return lineBreaks;
    }

    private static CodePoint? GetHyphenationMarkerCodePoint(TextOptions options)
        => options.TextHyphenation switch
        {
            TextHyphenation.Standard => new CodePoint(StandardHyphen),
            TextHyphenation.Custom => options.CustomHyphen,
            _ => null
        };

    /// <summary>
    /// Creates a visible generated marker that matches the layout style of the anchor entry.
    /// </summary>
    /// <param name="anchorMetric">The glyph metric that supplies font, run, attributes, and decorations.</param>
    /// <param name="pointSize">The point size at which the marker is rendered.</param>
    /// <param name="bidiRun">The bidi run that the marker belongs to.</param>
    /// <param name="graphemeIndex">The source grapheme index to map the marker to.</param>
    /// <param name="isLastInGrapheme">Whether the marker maps to the last entry in its grapheme.</param>
    /// <param name="codePointIndex">The source codepoint index to map the marker to.</param>
    /// <param name="graphemeCodePointIndex">The source codepoint-in-grapheme index to map the marker to.</param>
    /// <param name="stringIndex">The UTF-16 source index to map the marker to.</param>
    /// <param name="markerCodePoint">The marker codepoint to create.</param>
    /// <param name="layoutMode">The layout mode used to calculate marker orientation.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <returns>The generated marker entry.</returns>
    internal static GlyphLayoutData CreateGeneratedMarker(
        GlyphMetrics anchorMetric,
        float pointSize,
        BidiRun bidiRun,
        int graphemeIndex,
        bool isLastInGrapheme,
        int codePointIndex,
        int graphemeCodePointIndex,
        int stringIndex,
        CodePoint markerCodePoint,
        LayoutMode layoutMode,
        TextOptions options)
    {
        anchorMetric.FontMetrics.TryGetGlyphId(markerCodePoint, out ushort markerGlyphId);

        GlyphMetrics markerMetric = anchorMetric.FontMetrics.GetGlyphMetrics(
            markerCodePoint,
            markerGlyphId,
            anchorMetric.TextAttributes,
            anchorMetric.TextDecorations,
            layoutMode,
            options.ColorFontSupport);

        markerMetric = markerMetric.CloneForRendering(anchorMetric.TextRun);

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

        FontRectangle markerBox = GlyphMetrics.ShouldSkipGlyphRendering(markerMetric.CodePoint)
            ? FontRectangle.Empty
            : markerMetric.GetBoundingBox(markerMode, Vector2.Zero, pointSize);

        return new GlyphLayoutData(
            new GlyphMetrics[] { markerMetric },
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

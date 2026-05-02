// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <content>
/// Line break candidate collection and layout-level tailoring.
/// </content>
internal static partial class TextLayout
{
    /// <summary>
    /// Assembles shaped glyphs into <see cref="TextLine"/> instances, applying line-break
    /// opportunities derived from <see cref="LineBreakEnumerator"/> and the configured
    /// <see cref="TextOptions.WordBreaking"/> / <see cref="TextOptions.WrappingLength"/> settings.
    /// Finalizes each line (trimming trailing whitespace and applying bidi reordering) and applies
    /// justification where requested.
    /// </summary>
    /// <param name="text">The original source text.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <param name="bidiRuns">The resolved bidi runs covering the whole input.</param>
    /// <param name="bidiMap">The code point to bidi-run mapping built during shaping.</param>
    /// <param name="positionings">The GPOS positioning collection containing the shaped entries.</param>
    /// <param name="layoutMode">The active layout mode (drives horizontal vs vertical metrics selection).</param>
    /// <returns>The shaped, line-broken, finalized text box ready for glyph placement.</returns>
    private static TextBox BreakLines(
        ReadOnlySpan<char> text,
        TextOptions options,
        BidiRun[] bidiRuns,
        Dictionary<int, int> bidiMap,
        GlyphPositioningCollection positionings,
        LayoutMode layoutMode)
    {
        bool shouldWrap = options.WrappingLength > 0;

        // Wrapping length is always provided in pixels. Convert to inches for comparison.
        float wrappingLength = shouldWrap ? options.WrappingLength / options.Dpi : float.MaxValue;
        bool breakAll = options.WordBreaking == WordBreaking.BreakAll;
        bool keepAll = options.WordBreaking == WordBreaking.KeepAll;
        bool breakWord = options.WordBreaking == WordBreaking.BreakWord;
        bool isHorizontalLayout = layoutMode.IsHorizontal();
        bool isVerticalLayout = layoutMode.IsVertical();
        bool isVerticalMixedLayout = layoutMode.IsVerticalMixed();

        int graphemeIndex;
        int codePointIndex = 0;
        int glyphSearchIndex = 0;
        List<TextLine> textLines = [];
        TextLine textLine = new();
        int stringIndex = 0;

        // No glyph should contain more than 64 metrics.
        // We do a sanity check below just in case.
        Span<float> decomposedAdvancesBuffer = stackalloc float[64];

        // Enumerate through each grapheme in the text.
        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        for (graphemeIndex = 0; graphemeEnumerator.MoveNext(); graphemeIndex++)
        {
            // Now enumerate through each codepoint in the grapheme.
            ReadOnlySpan<char> grapheme = graphemeEnumerator.Current.Span;
            int graphemeCodePointIndex = 0;
            SpanCodePointEnumerator codePointEnumerator = new(grapheme);
            while (codePointEnumerator.MoveNext())
            {
                if (!positionings.TryGetGlyphMetricsAtOffset(
                    codePointIndex,
                    ref glyphSearchIndex,
                    out float pointSize,
                    out bool isSubstituted,
                    out bool isVerticalSubstitution,
                    out bool isDecomposed,
                    out IReadOnlyList<GlyphMetrics>? metrics))
                {
                    // Codepoint was skipped during original enumeration.
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

                if (CodePoint.IsTabulation(codePoint))
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
                                  layoutMode,
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

                    // Add our metrics to the line.
                    textLine.Add(
                        isDecomposed ? new GlyphMetrics[] { metric } : metrics,
                        pointSize,
                        decomposedAdvance,
                        lineHeight,
                        ascender,
                        descender,
                        delta,
                        bidiRuns[bidiMap[codePointIndex]],
                        graphemeIndex,
                        isLastInGrapheme,
                        codePointIndex,
                        graphemeCodePointIndex,
                        shouldRotate || shouldOffset,
                        isDecomposed,
                        stringIndex,
                        mode,
                        options.LineSpacing);
                }

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            stringIndex += grapheme.Length;
        }

        // Calculate the break opportunities once. The wrapping loop below may scan them
        // repeatedly as each finalized line is split off from the remaining text.
        List<LineBreak> lineBreaks = CollectLineBreaks(text);

        int processed = 0;
        while (textLine.Count > 0)
        {
            LineBreak? bestBreak = null;
            foreach (LineBreak lineBreak in lineBreaks)
            {
                // Skip breaks that are already behind the processed portion
                if (lineBreak.PositionWrap <= processed)
                {
                    continue;
                }

                // Measure the text up to the adjusted break point
                float advance = textLine.MeasureAt(lineBreak.PositionMeasure - processed);
                if (advance >= wrappingLength)
                {
                    bestBreak ??= lineBreak;
                    break;
                }

                // If it's a mandatory break, stop immediately
                if (lineBreak.Required)
                {
                    bestBreak = lineBreak;
                    break;
                }

                // Update the best break
                bestBreak = lineBreak;
            }

            if (bestBreak != null)
            {
                LineBreak breakAt = bestBreak.Value;
                if (breakAll)
                {
                    // Break-all works differently to the other modes.
                    // It will break at any character so we simply toggle the breaking operation depending
                    // on whether the break is required.
                    TextLine? remaining;
                    if (bestBreak.Value.Required)
                    {
                        if (textLine.TrySplitAt(breakAt, keepAll, out remaining))
                        {
                            processed = breakAt.PositionWrap;
                            textLines.Add(textLine.Finalize(true));
                            textLine = remaining;
                        }
                    }
                    else if (textLine.TrySplitAt(wrappingLength, out remaining))
                    {
                        processed += textLine.Count;
                        textLines.Add(textLine.Finalize());
                        textLine = remaining;
                    }
                    else
                    {
                        processed += textLine.Count;
                    }
                }
                else
                {
                    // Split the current line at the adjusted break index
                    if (textLine.TrySplitAt(breakAt, keepAll, out TextLine? remaining))
                    {
                        // If 'keepAll' is true then the break could be later than expected.
                        processed = keepAll
                            ? processed + Math.Max(textLine.Count, breakAt.PositionWrap - processed)
                            : breakAt.PositionWrap;

                        if (breakWord)
                        {
                            // A break was found, but we need to check if the line is too long
                            // and break if required.
                            if (textLine.ScaledLineAdvance > wrappingLength &&
                                textLine.TrySplitAt(wrappingLength, out TextLine? overflow))
                            {
                                // Reinsert the overflow at the beginning of the remaining line
                                processed -= overflow.Count;
                                remaining.InsertAt(0, overflow);
                            }
                        }

                        // Add the split part to the list and continue processing.
                        textLines.Add(textLine.Finalize(breakAt.Required));
                        textLine = remaining;
                    }
                    else
                    {
                        processed += textLine.Count;
                    }
                }
            }
            else
            {
                // We're at the last line break which should be at the end of the
                // text. We can break here and finalize the line.
                if (breakWord || breakAll)
                {
                    while (textLine.ScaledLineAdvance > wrappingLength)
                    {
                        if (!textLine.TrySplitAt(wrappingLength, out TextLine? overflow))
                        {
                            break;
                        }

                        textLines.Add(textLine.Finalize());
                        textLine = overflow;
                    }
                }

                textLines.Add(textLine.Finalize(true));
                break;
            }
        }

        // Finally we justify each line that does not end a paragraph.
        for (int i = 0; i < textLines.Count; i++)
        {
            TextLine line = textLines[i];
            if (!line.SkipJustification)
            {
                line.Justify(options);
            }
        }

        return new TextBox(textLines);
    }

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
    /// <returns>The ordered line break opportunities after layout-level tailoring.</returns>
    private static List<LineBreak> CollectLineBreaks(ReadOnlySpan<char> text)
    {
        LineBreakEnumerator lineBreakEnumerator = new(text, tailorUrls: true);
        List<LineBreak> lineBreaks = [];
        while (lineBreakEnumerator.MoveNext())
        {
            lineBreaks.Add(lineBreakEnumerator.Current);
        }

        return lineBreaks;
    }
}

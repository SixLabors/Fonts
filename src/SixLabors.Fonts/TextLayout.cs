// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulated logic or laying out text.
/// </summary>
internal static class TextLayout
{
    public static IReadOnlyList<GlyphLayout> GenerateLayout(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return Array.Empty<GlyphLayout>();
        }

        TextBox textBox = ProcessText(text, options);
        return LayoutText(textBox, options);
    }

    public static IReadOnlyList<TextRun> BuildTextRuns(ReadOnlySpan<char> text, TextOptions options)
    {
        if (options.TextRuns is null || options.TextRuns.Count == 0)
        {
            return new TextRun[]
            {
                new()
                {
                    Start = 0,
                    End = text.GetGraphemeCount(),
                    Font = options.Font
                }
            };
        }

        int start = 0;
        int end = text.GetGraphemeCount();
        List<TextRun> textRuns = [];
        foreach (TextRun textRun in options.TextRuns!.OrderBy(x => x.Start))
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

        return textRuns;
    }

    private static TextBox ProcessText(ReadOnlySpan<char> text, TextOptions options)
    {
        // Gather the font and fallbacks.
        Font[] fallbackFonts = (options.FallbackFontFamilies?.Count > 0)
            ? [.. options.FallbackFontFamilies.Select(x => new Font(x, options.Font.Size, options.Font.RequestedStyle))]
            : [];

        LayoutMode layoutMode = options.LayoutMode;
        GlyphSubstitutionCollection substitutions = new(options);
        GlyphPositioningCollection positionings = new(options);

        // Analyse the text for bidi directional runs.
        BidiAlgorithm bidi = BidiAlgorithm.Instance.Value!;
        BidiData bidiData = new();
        bidiData.Init(text, (sbyte)options.TextDirection);

        // If we have embedded directional overrides then change those
        // ranges to neutral.
        if (options.TextDirection != TextDirection.Auto)
        {
            bidiData.SaveTypes();
            bidiData.Types.Span.Fill(BidiCharacterType.OtherNeutral);
            bidiData.PairedBracketTypes.Span.Clear();
        }

        bidi.Process(bidiData);

        // Get the list of directional runs
        BidiRun[] bidiRuns = [.. BidiRun.CoalesceLevels(bidi.ResolvedLevels)];
        Dictionary<int, int> bidiMap = [];

        // Incrementally build out collection of glyphs.
        IReadOnlyList<TextRun> textRuns = BuildTextRuns(text, options);

        // First do multiple font runs using the individual text runs.
        bool complete = true;
        int textRunIndex = 0;
        int codePointIndex = 0;
        int bidiRunIndex = 0;
        foreach (TextRun textRun in textRuns)
        {
            if (!DoFontRun(
                textRun.Slice(text),
                textRun.Start,
                textRuns,
                ref textRunIndex,
                ref codePointIndex,
                ref bidiRunIndex,
                false,
                textRun.Font!,
                bidiRuns,
                bidiMap,
                substitutions,
                positionings))
            {
                complete = false;
            }
        }

        if (!complete)
        {
            // Finally try our fallback fonts.
            // We do a complete run here across the whole collection.
            foreach (Font font in fallbackFonts)
            {
                textRunIndex = 0;
                codePointIndex = 0;
                bidiRunIndex = 0;
                if (DoFontRun(
                    text,
                    0,
                    textRuns,
                    ref textRunIndex,
                    ref codePointIndex,
                    ref bidiRunIndex,
                    true,
                    font,
                    bidiRuns,
                    bidiMap,
                    substitutions,
                    positionings))
                {
                    break;
                }
            }
        }

        // Update the positions of the glyphs in the completed collection.
        // Each set of metrics is associated with single font and will only be updated
        // by that font so it's safe to use a single collection.
        foreach (TextRun textRun in textRuns)
        {
            textRun.Font!.FontMetrics.UpdatePositions(positionings);
        }

        foreach (Font font in fallbackFonts)
        {
            font.FontMetrics.UpdatePositions(positionings);
        }

        return BreakLines(text, options, bidiRuns, bidiMap, positionings, layoutMode);
    }

    private static List<GlyphLayout> LayoutText(TextBox textBox, TextOptions options)
    {
        LayoutMode layoutMode = options.LayoutMode;
        List<GlyphLayout> glyphs = [];

        Vector2 boxLocation = options.Origin / options.Dpi;
        Vector2 penLocation = boxLocation;

        // If a wrapping length is specified that should be used to determine the
        // box size to align text within.
        float maxScaledAdvance = textBox.ScaledMaxAdvance();
        if (options.TextAlignment != TextAlignment.Start && options.WrappingLength > 0)
        {
            maxScaledAdvance = Math.Max(options.WrappingLength / options.Dpi, maxScaledAdvance);
        }

        TextDirection direction = textBox.TextDirection();

        if (layoutMode == LayoutMode.HorizontalTopBottom)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                glyphs.AddRange(LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation));
            }
        }
        else if (layoutMode == LayoutMode.HorizontalBottomTop)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                glyphs.AddRange(LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation));
            }
        }
        else if (layoutMode is LayoutMode.VerticalLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                glyphs.AddRange(LayoutLineVertical(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation));
            }
        }
        else if (layoutMode is LayoutMode.VerticalRightLeft)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                glyphs.AddRange(LayoutLineVertical(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation));
            }
        }
        else if (layoutMode is LayoutMode.VerticalMixedLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                glyphs.AddRange(LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation));
            }
        }
        else
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                glyphs.AddRange(LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation));
            }
        }

        return glyphs;
    }

    private static List<GlyphLayout> LayoutLineHorizontal(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation)
    {
        // Offset the location to center the line vertically.
        bool isFirstLine = index == 0;
        float lineHeight = textLine.ScaledMaxLineHeight;
        float advanceY = lineHeight * options.LineSpacing;
        float offsetY = (advanceY - lineHeight) * .5F;
        float yLineAdvance = advanceY - offsetY;

        float originX = penLocation.X;
        float offsetX = 0;

        // Set the Y-Origin for the line.
        if (isFirstLine)
        {
            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetY -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing * .5F;
                    }

                    break;
                case VerticalAlignment.Bottom:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetY -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing;
                    }

                    break;
            }
        }

        penLocation.Y += offsetY;

        // Set the X-Origin for horizontal alignment.
        switch (options.HorizontalAlignment)
        {
            case HorizontalAlignment.Right:
                offsetX = -maxScaledAdvance;
                break;
            case HorizontalAlignment.Center:
                offsetX = -(maxScaledAdvance * .5F);
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        penLocation.X += offsetX;

        List<GlyphLayout> glyphs = [];
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                glyphs.Add(new GlyphLayout(
                    new Glyph(data.Metrics[0], data.PointSize),
                    boxLocation,
                    penLocation,
                    Vector2.Zero,
                    data.ScaledAdvance,
                    yLineAdvance,
                    GlyphLayoutMode.Horizontal,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                penLocation.X = originX;
                penLocation.Y += yLineAdvance;
                boxLocation.X = originX;
                boxLocation.Y += advanceY;
                return glyphs;
            }

            int j = 0;
            foreach (GlyphMetrics metric in data.Metrics)
            {
                glyphs.Add(new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    boxLocation,
                    penLocation + new Vector2(0, textLine.ScaledMaxAscender),
                    Vector2.Zero,
                    data.ScaledAdvance,
                    advanceY,
                    GlyphLayoutMode.Horizontal,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex));

                j++;
            }

            boxLocation.X += data.ScaledAdvance;
            penLocation.X += data.ScaledAdvance;
        }

        boxLocation.X = originX;
        penLocation.X = originX;
        if (glyphs.Count > 0)
        {
            penLocation.Y += yLineAdvance;
            boxLocation.Y += advanceY;
        }

        return glyphs;
    }

    private static List<GlyphLayout> LayoutLineVertical(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation)
    {
        float originY = penLocation.Y;
        float offsetY = 0;

        // Offset the location to center the line horizontally.
        float scaledMaxLineHeight = textLine.ScaledMaxLineHeight;
        float advanceX = scaledMaxLineHeight * options.LineSpacing;
        float offsetX = (advanceX - scaledMaxLineHeight) * .5F;
        float xLineAdvance = advanceX - offsetX;

        // Set the Y-Origin for the line.
        switch (options.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                offsetY = 0;
                break;
            case VerticalAlignment.Center:
                offsetY -= maxScaledAdvance * .5F;
                break;
            case VerticalAlignment.Bottom:
                offsetY -= maxScaledAdvance;
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        penLocation.Y += offsetY;

        bool isFirstLine = index == 0;
        if (isFirstLine)
        {
            // Set the X-Origin for horizontal alignment.
            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing;
                    }

                    break;
                case HorizontalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing * .5F;
                    }

                    break;
            }
        }

        penLocation.X += offsetX;

        List<GlyphLayout> glyphs = [];
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                glyphs.Add(new GlyphLayout(
                    new Glyph(data.Metrics[0], data.PointSize),
                    boxLocation,
                    penLocation,
                    Vector2.Zero,
                    xLineAdvance,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                boxLocation.X += advanceX;
                boxLocation.Y = originY;
                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                return glyphs;
            }

            int j = 0;
            foreach (GlyphMetrics metric in data.Metrics)
            {
                // Align the glyph horizontally and vertically centering vertically around the baseline.
                Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;

                float alignX = 0;
                if (data.IsTransformed)
                {
                    // Calculate the horizontal alignment offset:
                    // - Normalize lsb to zero
                    // - Center the glyph horizontally within the max line height.
                    alignX -= metric.LeftSideBearing * scale.X;
                    alignX += (scaledMaxLineHeight - (metric.Bounds.Size().X * scale.X)) * .5F;
                }

                Vector2 offset = new(alignX, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                glyphs.Add(new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    boxLocation,
                    penLocation + new Vector2((scaledMaxLineHeight - data.ScaledLineHeight) * .5F, 0),
                    offset,
                    advanceX,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex));

                j++;
            }

            penLocation.Y += data.ScaledAdvance;
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (glyphs.Count > 0)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }

        return glyphs;
    }

    private static List<GlyphLayout> LayoutLineVerticalMixed(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation)
    {
        float originY = penLocation.Y;
        float offsetY = 0;

        // Offset the location to center the line horizontally.
        float scaledMaxLineHeight = textLine.ScaledMaxLineHeight;
        float advanceX = scaledMaxLineHeight * options.LineSpacing;
        float offsetX = (advanceX - scaledMaxLineHeight) * .5F;
        float xLineAdvance = advanceX - offsetX;

        // Set the Y-Origin for the line.
        switch (options.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                offsetY = 0;
                break;
            case VerticalAlignment.Center:
                offsetY -= maxScaledAdvance * .5F;
                break;
            case VerticalAlignment.Bottom:
                offsetY -= maxScaledAdvance;
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        penLocation.Y += offsetY;

        bool isFirstLine = index == 0;
        if (isFirstLine)
        {
            // Set the X-Origin for horizontal alignment.
            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing;
                    }

                    break;
                case HorizontalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * options.LineSpacing * .5F;
                    }

                    break;
            }
        }

        penLocation.X += offsetX;

        List<GlyphLayout> glyphs = [];
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                glyphs.Add(new GlyphLayout(
                    new Glyph(data.Metrics[0], data.PointSize),
                    boxLocation,
                    penLocation,
                    Vector2.Zero,
                    xLineAdvance,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                boxLocation.X += advanceX;
                boxLocation.Y = originY;
                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                return glyphs;
            }

            if (data.IsTransformed)
            {
                int j = 0;
                foreach (GlyphMetrics metric in data.Metrics)
                {
                    // Align the glyphs horizontally so the baseline is centered.
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;

                    // Calculate the initial horizontal offset to center the glyph baseline:
                    // - Take half the difference between the max line height (scaledMaxLineHeight)
                    //   and the current glyph's line height (data.ScaledLineHeight).
                    // - The line height includes both ascender and descender metrics.
                    float baselineDelta = (scaledMaxLineHeight - data.ScaledLineHeight) * .5F;

                    // Adjust the horizontal offset further by considering the descender differences:
                    // - Subtract the current glyph's descender (data.ScaledDescender) to align it properly.
                    float descenderAbs = Math.Abs(data.ScaledDescender);
                    float descenderDelta = (Math.Abs(textLine.ScaledMaxDescender) - descenderAbs) * .5F;

                    // Final horizontal center offset combines the baseline and descender adjustments.
                    float centerOffsetX = baselineDelta + descenderAbs + descenderDelta;

                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        boxLocation,
                        penLocation + new Vector2(centerOffsetX, 0),
                        Vector2.Zero,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.VerticalRotated,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex));

                    j++;
                }
            }
            else
            {
                int j = 0;
                foreach (GlyphMetrics metric in data.Metrics)
                {
                    // Align the glyph horizontally and vertically centering vertically around the baseline.
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                    Vector2 offset = new(0, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        boxLocation,
                        penLocation + new Vector2((scaledMaxLineHeight - data.ScaledLineHeight) * .5F, 0),
                        offset,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.Vertical,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex));

                    j++;
                }
            }

            penLocation.Y += data.ScaledAdvance;
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (glyphs.Count > 0)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }

        return glyphs;
    }

    private static bool DoFontRun(
        ReadOnlySpan<char> text,
        int start,
        IReadOnlyList<TextRun> textRuns,
        ref int textRunIndex,
        ref int codePointIndex,
        ref int bidiRunIndex,
        bool isFallbackRun,
        Font font,
        BidiRun[] bidiRuns,
        Dictionary<int, int> bidiMap,
        GlyphSubstitutionCollection substitutions,
        GlyphPositioningCollection positionings)
    {
        // For each run we start with a fresh substitution collection to avoid
        // overwriting the glyph ids.
        substitutions.Clear();

        // Enumerate through each grapheme in the text.
        int graphemeIndex = start;
        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        while (graphemeEnumerator.MoveNext())
        {
            int graphemeMax = graphemeEnumerator.Current.Length - 1;
            int graphemeCodePointIndex = 0;
            int charIndex = 0;

            if (graphemeIndex == textRuns[textRunIndex].End)
            {
                textRunIndex++;
            }

            // Now enumerate through each codepoint in the grapheme.
            bool skipNextCodePoint = false;
            SpanCodePointEnumerator codePointEnumerator = new(graphemeEnumerator.Current);
            while (codePointEnumerator.MoveNext())
            {
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
                CodePoint current = codePointEnumerator.Current;
                charIndex += current.Utf16SequenceLength;
                CodePoint? next = graphemeCodePointIndex < graphemeMax
                    ? CodePoint.DecodeFromUtf16At(graphemeEnumerator.Current, charIndex, out charsConsumed)
                    : null;

                charIndex += charsConsumed;

                // Get the glyph id for the codepoint and add to the collection.
                font.FontMetrics.TryGetGlyphId(current, next, out ushort glyphId, out skipNextCodePoint);
                substitutions.AddGlyph(glyphId, current, (TextDirection)bidiRuns[bidiRunIndex].Direction, textRuns[textRunIndex], codePointIndex);

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            graphemeIndex++;
        }

        // Apply the simple and complex substitutions.
        // TODO: Investigate HarfBuzz normalizer.
        SubstituteBidiMirrors(font.FontMetrics, substitutions);
        font.FontMetrics.ApplySubstitution(substitutions);

        return !isFallbackRun
            ? positionings.TryAdd(font, substitutions)
            : positionings.TryUpdate(font, substitutions);
    }

    private static void SubstituteBidiMirrors(FontMetrics fontMetrics, GlyphSubstitutionCollection collection)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            GlyphShapingData data = collection[i];

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
                collection.Replace(i, glyphId, FeatureTags.RightToLeftMirroredForms);
            }
        }

        // TODO: This only replaces certain glyphs. We should investigate the specification further.
        // https://www.unicode.org/reports/tr50/#vertical_alternates
        if (collection.TextOptions.LayoutMode.IsHorizontal())
        {
            return;
        }

        for (int i = 0; i < collection.Count; i++)
        {
            GlyphShapingData data = collection[i];
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
                collection.Replace(i, glyphId, FeatureTags.VerticalAlternates);
            }
        }
    }

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
            int graphemeCodePointIndex = 0;
            SpanCodePointEnumerator codePointEnumerator = new(graphemeEnumerator.Current);
            while (codePointEnumerator.MoveNext())
            {
                if (!positionings.TryGetGlyphMetricsAtOffset(
                    codePointIndex,
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
                if (isSubstituted &&
                    metrics.Count == 1 &&
                    glyph.FontMetrics.TryGetCodePoint(glyph.GlyphId, out CodePoint substitution))
                {
                    codePoint = substitution;
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

                // This should never happen, but we need to ensure that the buffer is large enough
                // if, for some crazy reason, a glyph does contain more than 64 metrics.
                Span<float> decomposedAdvances = metrics.Count > decomposedAdvancesBuffer.Length
                    ? new float[metrics.Count]
                    : decomposedAdvancesBuffer[..(isDecomposed ? metrics.Count : 1)];

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
                                  options.ColorFontSupport)[0];

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

                // For non-decomposed glyphs the length is always 1.
                for (int i = 0; i < decomposedAdvances.Length; i++)
                {
                    float decomposedAdvance = decomposedAdvances[i];

                    // Work out the scaled metrics for the glyph.
                    GlyphMetrics metric = metrics[i];
                    float scaleY = pointSize / metric.ScaleFactor.Y;
                    IMetricsHeader metricsHeader = isHorizontalLayout || shouldRotate
                        ? metric.FontMetrics.HorizontalMetrics
                        : metric.FontMetrics.VerticalMetrics;
                    float ascender = metricsHeader.Ascender * scaleY;

                    // Match how line height is calculated for browsers.
                    // https://www.w3.org/TR/CSS2/visudet.html#propdef-line-height
                    float descender = Math.Abs(metricsHeader.Descender * scaleY);
                    float lineHeight = metric.UnitsPerEm * scaleY;
                    float delta = ((metricsHeader.LineHeight * scaleY) - lineHeight) * .5F;
                    ascender -= delta;
                    descender -= delta;

                    // Add our metrics to the line.
                    textLine.Add(
                        isDecomposed ? new GlyphMetrics[] { metric } : metrics,
                        pointSize,
                        decomposedAdvance,
                        lineHeight,
                        ascender,
                        descender,
                        bidiRuns[bidiMap[codePointIndex]],
                        graphemeIndex,
                        codePointIndex,
                        graphemeCodePointIndex,
                        shouldRotate || shouldOffset,
                        isDecomposed,
                        stringIndex);
                }

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            stringIndex += graphemeEnumerator.Current.Length;
        }

        // Now we need to loop through our line and split it at any line breaks.
        // First calculate the position of potential line breaks.
        LineBreakEnumerator lineBreakEnumerator = new(text);
        List<LineBreak> lineBreaks = [];
        while (lineBreakEnumerator.MoveNext())
        {
            // URLs are now so common in regular plain text that they need to be taken into account when
            // assigning general-purpose line breaking properties.
            //
            // To handle this we disallow breaks after solidus (U+002F) entirely.
            // Testing seems to indicate Chrome and other browsers do this as well.
            //
            // We do this outside of the line breaker so that the expected results from the Unicode
            // tests are not affected.
            // https://www.unicode.org/reports/tr14/#SY
            LineBreak current = lineBreakEnumerator.Current;
            int i = current.PositionMeasure;
            if (i < textLine.Count)
            {
                CodePoint c = textLine[i].CodePoint;
                CodePoint p = textLine[Math.Max(0, i - 1)].CodePoint;
                if (c.Value == 0x002F || p.Value == 0x002F)
                {
                    continue;
                }
            }

            lineBreaks.Add(current);
        }

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
                            textLines.Add(textLine.Finalize(options));
                            textLine = remaining;
                        }
                    }
                    else if (textLine.TrySplitAt(wrappingLength, out remaining))
                    {
                        processed += textLine.Count;
                        textLines.Add(textLine.Finalize(options));
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
                        textLines.Add(textLine.Finalize(options));
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

                        textLines.Add(textLine.Finalize(options));
                        textLine = overflow;
                    }
                }

                textLines.Add(textLine.Finalize(options));
                break;
            }
        }

        return new TextBox(textLines);
    }

    internal sealed class TextBox
    {
        private float? scaledMaxAdvance;

        public TextBox(IReadOnlyList<TextLine> textLines)
            => this.TextLines = textLines;

        public IReadOnlyList<TextLine> TextLines { get; }

        public float ScaledMaxAdvance()
            => this.scaledMaxAdvance ??= this.TextLines.Max(x => x.ScaledLineAdvance);

        public TextDirection TextDirection() => this.TextLines[0][0].TextDirection;
    }

    internal sealed class TextLine
    {
        private readonly List<GlyphLayoutData> data;
        private readonly Dictionary<int, float> advances = [];

        public TextLine() => this.data = new(16);

        public TextLine(int capacity) => this.data = new(capacity);

        public int Count => this.data.Count;

        public float ScaledLineAdvance { get; private set; }

        public float ScaledMaxLineHeight { get; private set; } = -1;

        public float ScaledMaxAscender { get; private set; } = -1;

        public float ScaledMaxDescender { get; private set; } = -1;

        public GlyphLayoutData this[int index] => this.data[index];

        public void Add(
            IReadOnlyList<GlyphMetrics> metrics,
            float pointSize,
            float scaledAdvance,
            float scaledLineHeight,
            float scaledAscender,
            float scaledDescender,
            BidiRun bidiRun,
            int graphemeIndex,
            int codePointIndex,
            int graphemeCodePointIndex,
            bool isTransformed,
            bool isDecomposed,
            int stringIndex)
        {
            // Reset metrics.
            // We track the maximum metrics for each line to ensure glyphs can be aligned.
            if (graphemeCodePointIndex == 0)
            {
                this.ScaledLineAdvance += scaledAdvance;
            }

            this.ScaledMaxLineHeight = MathF.Max(this.ScaledMaxLineHeight, scaledLineHeight);
            this.ScaledMaxAscender = MathF.Max(this.ScaledMaxAscender, scaledAscender);
            this.ScaledMaxDescender = MathF.Max(this.ScaledMaxDescender, scaledDescender);

            this.data.Add(new(
                metrics,
                pointSize,
                scaledAdvance,
                scaledLineHeight,
                scaledAscender,
                scaledDescender,
                bidiRun,
                graphemeIndex,
                codePointIndex,
                graphemeCodePointIndex,
                isTransformed,
                isDecomposed,
                stringIndex));
        }

        public void InsertAt(int index, TextLine textLine)
        {
            this.data.InsertRange(index, textLine.data);
            RecalculateLineMetrics(this);
        }

        public float MeasureAt(int index)
        {
            if (this.advances.TryGetValue(index, out float advance))
            {
                return advance;
            }

            if (index >= this.data.Count)
            {
                index = this.data.Count - 1;
            }

            while (index >= 0 && CodePoint.IsWhiteSpace(this.data[index].CodePoint))
            {
                // If the index is whitespace, we need to measure at the previous
                // non-whitespace glyph to ensure we don't break too early.
                index--;
            }

            advance = 0;
            for (int i = 0; i <= index; i++)
            {
                advance += this.data[i].ScaledAdvance;
            }

            this.advances[index] = advance;
            return advance;
        }

        public bool TrySplitAt(float length, [NotNullWhen(true)] out TextLine? result)
        {
            float advance = this.data[0].ScaledAdvance;

            // Ensure at least one glyph is in the line.
            // trailing whitespace should be ignored as it is trimmed
            // on finalization.
            for (int i = 1; i < this.data.Count; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                advance += glyph.ScaledAdvance;
                if (CodePoint.IsWhiteSpace(glyph.CodePoint))
                {
                    continue;
                }

                if (advance >= length)
                {
                    int count = this.data.Count - i;
                    result = new(count);
                    result.data.AddRange(this.data.GetRange(i, count));
                    RecalculateLineMetrics(result);

                    this.data.RemoveRange(i, count);
                    RecalculateLineMetrics(this);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TrySplitAt(LineBreak lineBreak, bool keepAll, [NotNullWhen(true)] out TextLine? result)
        {
            int index = this.data.Count;
            GlyphLayoutData glyphData = default;
            while (index > 0)
            {
                glyphData = this.data[--index];
                if (glyphData.CodePointIndex == lineBreak.PositionWrap)
                {
                    break;
                }
            }

            // Word breaks should not be used for Chinese/Japanese/Korean (CJK) text
            // when word-breaking mode is keep-all.
            if (index > 0
                && !lineBreak.Required
                && keepAll
                && UnicodeUtility.IsCJKCodePoint((uint)glyphData.CodePoint.Value))
            {
                // Loop through previous glyphs to see if there is
                // a non CJK codepoint we can break at.
                while (index > 0)
                {
                    glyphData = this.data[--index];
                    if (!UnicodeUtility.IsCJKCodePoint((uint)glyphData.CodePoint.Value))
                    {
                        index++;
                        break;
                    }
                }
            }

            if (index == 0)
            {
                result = null;
                return false;
            }

            // Create a new line ensuring we capture the initial metrics.
            int count = this.data.Count - index;
            result = new(count);
            result.data.AddRange(this.data.GetRange(index, count));
            RecalculateLineMetrics(result);

            // Remove those items from this line.
            this.data.RemoveRange(index, count);
            RecalculateLineMetrics(this);

            return true;
        }

        private void TrimTrailingWhitespace()
        {
            int count = this.data.Count;
            int index = count;
            while (index > 1)
            {
                // Trim trailing breaking whitespace.
                CodePoint point = this.data[index - 1].CodePoint;
                if (!CodePoint.IsWhiteSpace(point) || CodePoint.IsNonBreakingSpace(point))
                {
                    break;
                }

                index--;
            }

            if (index < count)
            {
                this.data.RemoveRange(index, count - index);
            }
        }

        public TextLine Finalize(TextOptions options)
        {
            this.TrimTrailingWhitespace();
            this.BidiReOrder();
            RecalculateLineMetrics(this);

            this.Justify(options);
            RecalculateLineMetrics(this);
            return this;
        }

        public void Justify(TextOptions options)
        {
            if (options.WrappingLength == -1F || options.TextJustification == TextJustification.None)
            {
                return;
            }

            if (this.ScaledLineAdvance == 0)
            {
                return;
            }

            float delta = (options.WrappingLength / options.Dpi) - this.ScaledLineAdvance;
            if (delta <= 0)
            {
                return;
            }

            // Increase the advance for all non zero-width glyphs but the last.
            if (options.TextJustification == TextJustification.InterCharacter)
            {
                int nonZeroCount = 0;
                for (int i = 0; i < this.data.Count - 1; i++)
                {
                    GlyphLayoutData glyph = this.data[i];
                    if (!CodePoint.IsZeroWidthJoiner(glyph.CodePoint) && !CodePoint.IsZeroWidthNonJoiner(glyph.CodePoint))
                    {
                        nonZeroCount++;
                    }
                }

                float padding = delta / nonZeroCount;
                for (int i = 0; i < this.data.Count - 1; i++)
                {
                    GlyphLayoutData glyph = this.data[i];
                    if (!CodePoint.IsZeroWidthJoiner(glyph.CodePoint) && !CodePoint.IsZeroWidthNonJoiner(glyph.CodePoint))
                    {
                        glyph.ScaledAdvance += padding;
                        this.data[i] = glyph;
                    }
                }

                return;
            }

            // Increase the advance for all spaces but the last.
            if (options.TextJustification == TextJustification.InterWord)
            {
                // Count all the whitespace characters.
                int whiteSpaceCount = 0;
                for (int i = 0; i < this.data.Count - 1; i++)
                {
                    GlyphLayoutData glyph = this.data[i];
                    if (CodePoint.IsWhiteSpace(glyph.CodePoint))
                    {
                        whiteSpaceCount++;
                    }
                }

                float padding = delta / whiteSpaceCount;
                for (int i = 0; i < this.data.Count - 1; i++)
                {
                    GlyphLayoutData glyph = this.data[i];
                    if (CodePoint.IsWhiteSpace(glyph.CodePoint))
                    {
                        glyph.ScaledAdvance += padding;
                        this.data[i] = glyph;
                    }
                }
            }
        }

        public void BidiReOrder()
        {
            // Build up the collection of ordered runs.
            BidiRun run = this.data[0].BidiRun;
            OrderedBidiRun orderedRun = new(run.Level);
            OrderedBidiRun? current = orderedRun;
            for (int i = 0; i < this.data.Count; i++)
            {
                GlyphLayoutData g = this.data[i];
                if (run != g.BidiRun)
                {
                    run = g.BidiRun;
                    current.Next = new(run.Level);
                    current = current.Next;
                }

                current.Add(g);
            }

            // Reorder them into visual order.
            orderedRun = LinearReOrder(orderedRun);

            // Now perform a recursive reversal of each run.
            // From the highest level found in the text to the lowest odd level on each line, including intermediate levels
            // not actually present in the text, reverse any contiguous sequence of characters that are at that level or higher.
            // https://unicode.org/reports/tr9/#L2
            int max = 0;
            int min = int.MaxValue;
            for (int i = 0; i < this.data.Count; i++)
            {
                int level = this.data[i].BidiRun.Level;
                if (level > max)
                {
                    max = level;
                }

                if ((level & 1) != 0 && level < min)
                {
                    min = level;
                }
            }

            if (min > max)
            {
                min = max;
            }

            if (max == 0 || (min == max && (max & 1) == 0))
            {
                // Nothing to reverse.
                return;
            }

            // Now apply the reversal and replace the original contents.
            int minLevelToReverse = max;
            while (minLevelToReverse >= min)
            {
                current = orderedRun;
                while (current != null)
                {
                    if (current.Level >= minLevelToReverse)
                    {
                        current.Reverse();
                    }

                    current = current.Next;
                }

                minLevelToReverse--;
            }

            this.data.Clear();
            current = orderedRun;
            while (current != null)
            {
                this.data.AddRange(current.AsSlice());
                current = current.Next;
            }
        }

        private static void RecalculateLineMetrics(TextLine textLine)
        {
            // Lastly recalculate this line metrics.
            float advance = 0;
            float ascender = 0;
            float descender = 0;
            float lineHeight = 0;
            for (int i = 0; i < textLine.Count; i++)
            {
                GlyphLayoutData glyph = textLine[i];
                advance += glyph.ScaledAdvance;
                ascender = MathF.Max(ascender, glyph.ScaledAscender);
                descender = MathF.Max(descender, glyph.ScaledDescender);
                lineHeight = MathF.Max(lineHeight, glyph.ScaledLineHeight);
            }

            textLine.ScaledLineAdvance = advance;
            textLine.ScaledMaxAscender = ascender;
            textLine.ScaledMaxDescender = descender;
            textLine.ScaledMaxLineHeight = lineHeight;
            textLine.advances.Clear();
        }

        /// <summary>
        /// Reorders a series of runs from logical to visual order, returning the left most run.
        /// <see href="https://github.com/fribidi/linear-reorder/blob/f2f872257d4d8b8e137fcf831f254d6d4db79d3c/linear-reorder.c"/>
        /// </summary>
        /// <param name="line">The ordered bidi run.</param>
        /// <returns>The <see cref="OrderedBidiRun"/>.</returns>
        private static OrderedBidiRun LinearReOrder(OrderedBidiRun? line)
        {
            BidiRange? range = null;
            OrderedBidiRun? run = line;

            while (run != null)
            {
                OrderedBidiRun? next = run.Next;

                while (range != null && range.Level > run.Level
                    && range.Previous != null && range.Previous.Level >= run.Level)
                {
                    range = BidiRange.MergeWithPrevious(range);
                }

                if (range != null && range.Level >= run.Level)
                {
                    // Attach run to the range.
                    if ((run.Level & 1) != 0)
                    {
                        // Odd, range goes to the right of run.
                        run.Next = range.Left;
                        range.Left = run;
                    }
                    else
                    {
                        // Even, range goes to the left of run.
                        range.Right!.Next = run;
                        range.Right = run;
                    }

                    range.Level = run.Level;
                }
                else
                {
                    BidiRange r = new();
                    r.Left = r.Right = run;
                    r.Level = run.Level;
                    r.Previous = range;
                    range = r;
                }

                run = next;
            }

            while (range?.Previous != null)
            {
                range = BidiRange.MergeWithPrevious(range);
            }

            // Terminate.
            range!.Right!.Next = null;
            return range!.Left!;
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        internal struct GlyphLayoutData
        {
            public GlyphLayoutData(
                IReadOnlyList<GlyphMetrics> metrics,
                float pointSize,
                float scaledAdvance,
                float scaledLineHeight,
                float scaledAscender,
                float scaledDescender,
                BidiRun bidiRun,
                int graphemeIndex,
                int codePointIndex,
                int graphemeCodePointIndex,
                bool isTransformed,
                bool isDecomposed,
                int stringIndex)
            {
                this.Metrics = metrics;
                this.PointSize = pointSize;
                this.ScaledAdvance = scaledAdvance;
                this.ScaledLineHeight = scaledLineHeight;
                this.ScaledAscender = scaledAscender;
                this.ScaledDescender = scaledDescender;
                this.BidiRun = bidiRun;
                this.GraphemeIndex = graphemeIndex;
                this.CodePointIndex = codePointIndex;
                this.GraphemeCodePointIndex = graphemeCodePointIndex;
                this.IsTransformed = isTransformed;
                this.IsDecomposed = isDecomposed;
                this.StringIndex = stringIndex;
            }

            public readonly CodePoint CodePoint => this.Metrics[0].CodePoint;

            public IReadOnlyList<GlyphMetrics> Metrics { get; }

            public float PointSize { get; }

            public float ScaledAdvance { get; set; }

            public float ScaledLineHeight { get; }

            public float ScaledAscender { get; }

            public float ScaledDescender { get; }

            public BidiRun BidiRun { get; }

            public readonly TextDirection TextDirection => (TextDirection)this.BidiRun.Direction;

            public int GraphemeIndex { get; }

            public int GraphemeCodePointIndex { get; }

            public int CodePointIndex { get; }

            public bool IsTransformed { get; }

            public bool IsDecomposed { get; }

            public int StringIndex { get; }

            public readonly bool IsNewLine => CodePoint.IsNewLine(this.CodePoint);

            private readonly string DebuggerDisplay => FormattableString
                .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {this.TextDirection} : {this.CodePointIndex}, level: {this.BidiRun.Level}");
        }

        private sealed class OrderedBidiRun
        {
            private ArrayBuilder<GlyphLayoutData> info;

            public OrderedBidiRun(int level) => this.Level = level;

            public int Level { get; }

            public OrderedBidiRun? Next { get; set; }

            public void Add(GlyphLayoutData info) => this.info.Add(info);

            public ArraySlice<GlyphLayoutData> AsSlice() => this.info.AsSlice();

            public void Reverse() => this.AsSlice().Span.Reverse();
        }

        private sealed class BidiRange
        {
            public int Level { get; set; }

            public OrderedBidiRun? Left { get; set; }

            public OrderedBidiRun? Right { get; set; }

            public BidiRange? Previous { get; set; }

            public static BidiRange MergeWithPrevious(BidiRange? range)
            {
                BidiRange previous = range!.Previous!;
                BidiRange left;
                BidiRange right;

                if ((previous.Level & 1) != 0)
                {
                    // Odd, previous goes to the right of range.
                    left = range;
                    right = previous;
                }
                else
                {
                    // Even, previous goes to the left of range.
                    left = previous;
                    right = range;
                }

                // Stitch them
                left.Right!.Next = right.Left;
                previous.Left = left.Left;
                previous.Right = right.Right;

                return previous;
            }
        }
    }
}

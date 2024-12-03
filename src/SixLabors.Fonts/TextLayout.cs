// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
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
        List<TextRun> textRuns = new((options.TextRuns!.Count << 1) + 1);
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

    private static unsafe TextBox ProcessText(ReadOnlySpan<char> text, TextOptions options)
    {
        // Gather the font and fallbacks.
        // Font[] fallbackFonts = (options.FallbackFontFamilies?.Count > 0)
        //     ? options.FallbackFontFamilies.Where(x => !x.Equals(options.Font.Family)).Select(x => new Font(x, options.Font.Size, options.Font.RequestedStyle)).ToArray()
        //     : Array.Empty<Font>();
        Font[]? fallbackFonts = null;
        BidiAlgorithm bidi = default;
        BidiData bidiData = default;
        try
        {
            IReadOnlyList<FontFamily> fallbackFontFamilies = options.FallbackFontFamilies;
            if (fallbackFontFamilies != null && fallbackFontFamilies.Count > 0)
            {
                fallbackFonts = ArrayPool<Font>.Shared.Rent(fallbackFontFamilies.Count);
                int i = 0;
                foreach (FontFamily family in fallbackFontFamilies)
                {
                    fallbackFonts[i++] = new Font(family, options.Font.Size, options.Font.RequestedStyle);
                }
            }

            LayoutMode layoutMode = options.LayoutMode;
            GlyphSubstitutionCollection substitutions = new(options);
            GlyphPositioningCollection positionings = new(options);

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
            // IEnumerable<BidiRun> bidiRuns = BidiRun.CoalesceLevels(bidi.ResolvedLevels);
            int count = 0;
            Span<BidiRun> bidiRuns = new(NativeMemory.Alloc((nuint)(bidi.ResolvedLevels.Length * sizeof(BidiRun))), bidi.ResolvedLevels.Length);
            BidiDictionary<Index, Index> bidiMap = default;
            try
            {
                foreach (BidiRun run in BidiRun.CoalesceLevels(bidi.ResolvedLevels))
                {
                    bidiRuns[count++] = run;
                }

                fixed (BidiRun* ptr = bidiRuns)
                {
                    bidiRuns = new Span<BidiRun>(NativeMemory.Realloc(ptr, (nuint)(count * sizeof(BidiRun))), count);
                }

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
                        ref bidiMap,
                        substitutions,
                        positionings))
                    {
                        complete = false;
                    }
                }

                if (!complete && fallbackFonts != null && fallbackFonts.Length > 0)
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
                            ref bidiMap,
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

                if (fallbackFonts != null && fallbackFonts.Length > 0)
                {
                    foreach (Font font in fallbackFonts)
                    {
                        font.FontMetrics.UpdatePositions(positionings);
                    }

                    ArrayPool<Font>.Shared.Return(fallbackFonts);
                }

                TextBox result = BreakLines(text, options, bidiRuns, ref bidiMap, positionings, layoutMode);
                return result;
            }
            finally
            {
                fixed (BidiRun* ptr = bidiRuns)
                {
                    if (ptr != null)
                    {
                        NativeMemory.Free(ptr);
                        bidiRuns = null;
                    }
                }

                bidiMap.Free();
            }
        }
        finally
        {
            if (fallbackFonts != null)
            {
                ArrayPool<Font>.Shared.Return(fallbackFonts);
            }

            bidi.Free();
            bidiData.Free();
        }
    }

    private static IReadOnlyList<GlyphLayout> LayoutText(TextBox textBox, TextOptions options)
    {
        LayoutMode layoutMode = options.LayoutMode;

        Vector2 boxLocation = options.Origin / options.Dpi;
        Vector2 penLocation = boxLocation;

        // If a wrapping length is specified that should be used to determine the
        // box size to align text within.
        float maxScaledAdvance = textBox.ScaledMaxAdvance();
        if (options.TextAlignment != TextAlignment.Start && options.WrappingLength > 0)
        {
            maxScaledAdvance = Math.Max(options.WrappingLength / options.Dpi, maxScaledAdvance);
        }

        int count = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLine textLine = textBox.TextLines[i];
            for (int j = 0; j < textLine.Count; j++)
            {
                TextLine.GlyphLayoutData data = textLine[i];
                if (!data.IsNewLine)
                {
                    count += data.Metrics.Count;
                }
            }
        }

        TextDirection direction = textBox.TextDirection();
        List<GlyphLayout> glyphs = new(count);

        if (layoutMode == LayoutMode.HorizontalTopBottom)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }
        else if (layoutMode == LayoutMode.HorizontalBottomTop)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }
        else if (layoutMode is LayoutMode.VerticalLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineVertical(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }
        else if (layoutMode is LayoutMode.VerticalRightLeft)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }
        else if (layoutMode is LayoutMode.VerticalMixedLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }
        else
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                foreach ((GlyphLayout glyph, Vector2 box, Vector2 pen) in LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    boxLocation,
                    penLocation))
                {
                    glyphs.Add(glyph);
                    boxLocation = box;
                    penLocation = pen;
                }
            }
        }

        return glyphs;
    }

    private static IEnumerable<(GlyphLayout Glyph, Vector2 BoxLocation, Vector2 PenLocation)> LayoutLineHorizontal(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        Vector2 boxLocation,
        Vector2 penLocation)
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
        int count = 0;
        GlyphLayout? current = null;
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                penLocation.X = originX;
                penLocation.Y += yLineAdvance;

                boxLocation.X = originX;
                boxLocation.Y += advanceY;
                continue;
            }

            for (int j = 0; j < data.Metrics.Count; j++)
            {
                if (current != null)
                {
                    yield return (current.Value, boxLocation, penLocation);
                }

                GlyphMetrics metric = data.Metrics[j];
                current = new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    boxLocation,
                    penLocation + new Vector2(0, textLine.ScaledMaxAscender),
                    Vector2.Zero,
                    data.ScaledAdvance,
                    advanceY,
                    GlyphLayoutMode.Horizontal,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex);
                count++;
            }

            boxLocation.X += data.ScaledAdvance;
            penLocation.X += data.ScaledAdvance;
        }

        boxLocation.X = originX;
        penLocation.X = originX;
        if (count > 0)
        {
            penLocation.Y += yLineAdvance;
            boxLocation.Y += advanceY;
        }

        if (current != null)
        {
            yield return (current.Value, boxLocation, penLocation);
        }
    }

    private static IEnumerable<(GlyphLayout Glyph, Vector2 BoxLocation, Vector2 PenLocation)> LayoutLineVertical(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        Vector2 boxLocation,
        Vector2 penLocation)
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

        int count = 0;
        GlyphLayout? current = null;
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                boxLocation.X += advanceX;
                boxLocation.Y = originY;

                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                continue;
            }

            for (int j = 0; j < data.Metrics.Count; j++)
            {
                if (current != null)
                {
                    yield return (current.Value, boxLocation, penLocation);
                }

                GlyphMetrics metric = data.Metrics[j];

                // Align the glyph horizontally and vertically centering horizontally around the baseline.
                Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                float oX = (data.ScaledLineHeight - (metric.Bounds.Size().X * scale.X)) * .5F;
                Vector2 offset = new(oX, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                current = new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    boxLocation,
                    penLocation + new Vector2((scaledMaxLineHeight - data.ScaledLineHeight) * .5F, 0),
                    offset,
                    advanceX,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex);
                count++;
            }

            penLocation.Y += data.ScaledAdvance;
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (count > 0)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }

        if (current != null)
        {
            yield return (current.Value, boxLocation, penLocation);
        }
    }

    private static IEnumerable<(GlyphLayout Glyph, Vector2 BoxLocation, Vector2 PenLocation)> LayoutLineVerticalMixed(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        Vector2 boxLocation,
        Vector2 penLocation)
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

        int count = 0;
        GlyphLayout? current = null;
        for (int i = 0; i < textLine.Count; i++)
        {
            TextLine.GlyphLayoutData data = textLine[i];
            if (data.IsNewLine)
            {
                boxLocation.X += advanceX;
                boxLocation.Y = originY;

                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                continue;
            }

            if (data.IsRotated)
            {
                for (int j = 0; j < data.Metrics.Count; j++)
                {
                    if (current != null)
                    {
                        yield return (current.Value, boxLocation, penLocation);
                    }

                    GlyphMetrics metric = data.Metrics[j];
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                    current = new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        boxLocation,
                        penLocation + new Vector2(((scaledMaxLineHeight - data.ScaledLineHeight) * .5F) + data.ScaledDescender, 0),
                        Vector2.Zero,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.VerticalRotated,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex);
                    count++;
                }
            }
            else
            {
                for (int j = 0; j < data.Metrics.Count; j++)
                {
                    if (current != null)
                    {
                        yield return (current.Value, boxLocation, penLocation);
                    }

                    GlyphMetrics metric = data.Metrics[j];

                    // Align the glyph horizontally and vertically centering horizontally around the baseline.
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                    float oX = (data.ScaledLineHeight - (metric.Bounds.Size().X * scale.X)) * .5F;
                    Vector2 offset = new(oX, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                    current = new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        boxLocation,
                        penLocation + new Vector2((scaledMaxLineHeight - data.ScaledLineHeight) * .5F, 0),
                        offset,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.Vertical,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex);
                    count++;
                }
            }

            penLocation.Y += data.ScaledAdvance;
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (count > 0)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }

        if (current != null)
        {
            yield return (current.Value, boxLocation, penLocation);
        }
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
        ReadOnlySpan<BidiRun> bidiRuns,
        ref BidiDictionary<Index, Index> bidiMap,
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

                bidiMap.Add(codePointIndex, bidiRunIndex);

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
                collection.Replace(i, glyphId);
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
                collection.Replace(i, glyphId);
            }
        }
    }

    private static TextBox BreakLines(
        ReadOnlySpan<char> text,
        TextOptions options,
        ReadOnlySpan<BidiRun> bidiRuns,
        ref BidiDictionary<Index, Index> bidiMap,
        GlyphPositioningCollection positionings,
        LayoutMode layoutMode)
    {
        bool shouldWrap = options.WrappingLength > 0;
        float wrappingLength = shouldWrap ? options.WrappingLength / options.Dpi : float.MaxValue;
        bool breakAll = options.WordBreaking == WordBreaking.BreakAll;
        bool keepAll = options.WordBreaking == WordBreaking.KeepAll;
        bool breakWord = options.WordBreaking == WordBreaking.BreakWord;
        bool isHorizontalLayout = layoutMode.IsHorizontal();
        bool isVerticalMixedLayout = layoutMode.IsVerticalMixed();

        // Calculate the position of potential line breaks.
        LineBreakEnumerator lineBreakEnumerator = new(text);
        List<LineBreak> lineBreaks = new();
        while (lineBreakEnumerator.MoveNext())
        {
            lineBreaks.Add(lineBreakEnumerator.Current);
        }

        int lineBreakIndex = 0;
        LineBreak lastLineBreak = lineBreaks[lineBreakIndex];
        LineBreak currentLineBreak = lineBreaks[lineBreakIndex];
        int graphemeIndex;
        int codePointIndex = 0;
        float lineAdvance = 0;
        List<TextLine> textLines = new(lineBreaks.Count);
        TextLine textLine = new();
        int glyphCount = 0;
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
                if (!positionings.TryGetGlyphMetricsAtOffset(codePointIndex, out float pointSize, out bool isDecomposed, out IReadOnlyList<GlyphMetrics>? metrics))
                {
                    // Codepoint was skipped during original enumeration.
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                // Determine whether the glyph advance should be calculated using vertical or horizontal metrics
                // For vertical mixed layout we will be rotating glyphs with the vertical orientation type R or TR.
                CodePoint codePoint = codePointEnumerator.Current;
                VerticalOrientationType verticalOrientationType = CodePoint.GetVerticalOrientationType(codePoint);
                bool isRotated = isVerticalMixedLayout && verticalOrientationType is VerticalOrientationType.Rotate or VerticalOrientationType.TransformRotate;

                if (CodePoint.IsVariationSelector(codePoint))
                {
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                // Calculate the advance for the current codepoint.
                GlyphMetrics glyph = metrics[0];

                float glyphAdvance;

                // This should never happen, but we need to ensure that the buffer is large enough
                // if, for some crazy reason, a glyph does contain more than 64 metrics.
                Span<float> decomposedAdvances = metrics.Count > decomposedAdvancesBuffer.Length
                    ? new float[metrics.Count]
                    : decomposedAdvancesBuffer[..(isDecomposed ? metrics.Count : 1)];

                if (isHorizontalLayout || isRotated)
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

                            if (isHorizontalLayout || isRotated)
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
                    if (isHorizontalLayout || isRotated)
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

                // Now scale the advance.
                if (isHorizontalLayout || isRotated)
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

                // Should we start a new line?
                bool requiredBreak = false;
                if (graphemeCodePointIndex == 0)
                {
                    // Mandatory wrap at index.
                    if (currentLineBreak.PositionWrap == codePointIndex && currentLineBreak.Required)
                    {
                        textLines.Add(textLine.Finalize());
                        glyphCount += textLine.Count;
                        textLine = new();
                        lineAdvance = 0;
                        requiredBreak = true;
                    }
                    else if (shouldWrap && lineAdvance + glyphAdvance >= wrappingLength)
                    {
                        // Forced wordbreak
                        if (breakAll && textLine.Count > 0)
                        {
                            textLines.Add(textLine.Finalize());
                            glyphCount += textLine.Count;
                            textLine = new();
                            lineAdvance = 0;
                        }
                        else if (currentLineBreak.PositionMeasure == codePointIndex)
                        {
                            // Exact length match. Check for CJK
                            if (keepAll)
                            {
                                TextLine split = textLine.SplitAt(lastLineBreak, keepAll);
                                if (!split.Equals(textLine))
                                {
                                    textLines.Add(textLine.Finalize());
                                    textLine = split;
                                    lineAdvance = split.ScaledLineAdvance;
                                }
                            }
                            else if (textLine.Count > 0)
                            {
                                textLines.Add(textLine.Finalize());
                                glyphCount += textLine.Count;
                                textLine = new();
                                lineAdvance = 0;
                            }
                        }
                        else if (currentLineBreak.PositionWrap == codePointIndex)
                        {
                            // Exact length match. Check for CJK
                            TextLine split = textLine.SplitAt(currentLineBreak, keepAll);
                            if (!split.Equals(textLine))
                            {
                                textLines.Add(textLine.Finalize());
                                textLine = split;
                                lineAdvance = split.ScaledLineAdvance;
                            }
                            else if (textLine.Count > 0)
                            {
                                textLines.Add(textLine.Finalize());
                                textLine = new();
                                lineAdvance = 0;
                            }
                        }
                        else if (lastLineBreak.PositionWrap < codePointIndex && !CodePoint.IsWhiteSpace(codePoint))
                        {
                            // Split the current text line into two at the last wrapping point if the current glyph
                            // does not represent whitespace. Whitespace characters will be correctly trimmed at the
                            // next iteration.
                            TextLine split = textLine.SplitAt(lastLineBreak, keepAll);
                            if (!split.Equals(textLine))
                            {
                                textLines.Add(textLine.Finalize());
                                textLine = split;
                                lineAdvance = split.ScaledLineAdvance;
                            }
                            else if (breakWord && textLine.Count > 0)
                            {
                                textLines.Add(textLine.Finalize());
                                glyphCount += textLine.Count;
                                textLine = new();
                                lineAdvance = 0;
                            }
                        }
                        else if (breakWord && textLine.Count > 0)
                        {
                            textLines.Add(textLine.Finalize());
                            glyphCount += textLine.Count;
                            textLine = new();
                            lineAdvance = 0;
                        }
                    }
                }

                // Find the next line break.
                if (currentLineBreak.PositionWrap == codePointIndex)
                {
                    lastLineBreak = currentLineBreak;
                    currentLineBreak = lineBreaks[++lineBreakIndex];
                }

                // Do not start a line following a break with breaking whitespace
                // unless the break was required.
                if (textLine.Count == 0
                    && textLines.Count > 0
                    && !requiredBreak
                    && CodePoint.IsWhiteSpace(codePoint)
                    && !CodePoint.IsNonBreakingSpace(codePoint)
                    && !CodePoint.IsTabulation(codePoint)
                    && !CodePoint.IsNewLine(codePoint))
                {
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                if (textLine.Count > 0 && CodePoint.IsNewLine(codePoint))
                {
                    // Do not add new lines unless at position zero.
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                // For non-decomposed glyphs the length is always 1.
                for (int i = 0; i < decomposedAdvances.Length; i++)
                {
                    float decomposedAdvance = decomposedAdvances[i];

                    // Work out the scaled metrics for the glyph.
                    GlyphMetrics metric = metrics[i];
                    float scaleY = pointSize / metric.ScaleFactor.Y;
                    IMetricsHeader metricsHeader = isHorizontalLayout || isRotated
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
                    bidiMap.TryGetValue(codePointIndex, out Index index);
                    lineAdvance += decomposedAdvance;
                    textLine.Add(
                        isDecomposed ? new GlyphMetrics[] { metric } : metrics,
                        pointSize,
                        decomposedAdvance,
                        lineHeight,
                        ascender,
                        descender,
                        bidiRuns[index],
                        graphemeIndex,
                        codePointIndex,
                        isRotated,
                        isDecomposed,
                        stringIndex);
                }

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            stringIndex += graphemeEnumerator.Current.Length;
        }

        // Add the final line.
        if (textLine.Count > 0)
        {
            textLines.Add(textLine.Finalize());
        }

        return new TextBox(options, textLines);
    }

    private struct Index : IEqualityComparer<Index>
    {
        public int Value;

        public static implicit operator Index(int value) => new() { Value = value };

        public static implicit operator int(Index value) => value.Value;

        public readonly bool Equals(Index x, Index y) => x.Value == y.Value;

        public readonly int GetHashCode([DisallowNull] Index obj) => obj.Value;
    }

    internal readonly struct TextBox
    {
        public TextBox(TextOptions options, IReadOnlyList<TextLine> textLines)
        {
            this.TextLines = textLines;
            for (int i = 0; i < this.TextLines.Count - 1; i++)
            {
                this.TextLines[i].Justify(options);
            }
        }

        public IReadOnlyList<TextLine> TextLines { get; }

        public float ScaledMaxAdvance()
            => this.TextLines.Max(x => x.ScaledLineAdvance);

        public TextDirection TextDirection() => this.TextLines[0][0].TextDirection;
    }

    internal struct TextLine
    {
        private readonly List<GlyphLayoutData> data = new();

        public TextLine()
        {
        }

        public readonly int Count => this.data.Count;

        public float ScaledLineAdvance { get; private set; } = 0;

        public float ScaledMaxLineHeight { get; private set; } = -1;

        public float ScaledMaxAscender { get; private set; } = -1;

        public float ScaledMaxDescender { get; private set; } = -1;

        public readonly GlyphLayoutData this[int index] => this.data[index];

        public void Add(
            IReadOnlyList<GlyphMetrics> metrics,
            float pointSize,
            float scaledAdvance,
            float scaledLineHeight,
            float scaledAscender,
            float scaledDescender,
            BidiRun bidiRun,
            int graphemeIndex,
            int offset,
            bool isRotated,
            bool isDecomposed,
            int stringIndex)
        {
            // Reset metrics.
            // We track the maximum metrics for each line to ensure glyphs can be aligned.
            this.ScaledLineAdvance += scaledAdvance;
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
                offset,
                isRotated,
                isDecomposed,
                stringIndex));
        }

        public TextLine SplitAt(LineBreak lineBreak, bool keepAll)
        {
            int index = this.data.Count;
            GlyphLayoutData glyphWrap = default;
            while (index > 0)
            {
                glyphWrap = this.data[--index];

                if (glyphWrap.Offset == lineBreak.PositionWrap)
                {
                    break;
                }
            }

            if (index == 0)
            {
                // Now trim trailing whitespace from this line in the case of an exact
                // length line break (non CJK)
                this.TrimTrailingWhitespaceAndRecalculateMetrics();
                return this;
            }

            // Word breaks should not be used for Chinese/Japanese/Korean (CJK) text
            // when word-breaking mode is keep-all.
            if (keepAll && UnicodeUtility.IsCJKCodePoint((uint)glyphWrap.CodePoint.Value))
            {
                // Loop through previous glyphs to see if there is
                // a non CJK codepoint we can break at.
                while (index > 0)
                {
                    glyphWrap = this.data[--index];
                    if (!UnicodeUtility.IsCJKCodePoint((uint)glyphWrap.CodePoint.Value))
                    {
                        index++;
                        break;
                    }
                }

                if (index == 0)
                {
                    // Now trim trailing whitespace from this line in the case of an exact
                    // length line break (non CJK)
                    this.TrimTrailingWhitespaceAndRecalculateMetrics();
                    return this;
                }
            }

            // Create a new line ensuring we capture the initial metrics.
            TextLine result = new();
            result.data.AddRange(this.data.GetRange(index, this.data.Count - index));

            float advance = 0;
            float ascender = 0;
            float descender = 0;
            float lineHeight = 0;
            for (int i = 0; i < result.data.Count; i++)
            {
                GlyphLayoutData glyph = result.data[i];
                advance += glyph.ScaledAdvance;
                ascender = MathF.Max(ascender, glyph.ScaledAscender);
                descender = MathF.Max(descender, glyph.ScaledDescender);
                lineHeight = MathF.Max(lineHeight, glyph.ScaledLineHeight);
            }

            result.ScaledLineAdvance = advance;
            result.ScaledMaxAscender = ascender;
            result.ScaledMaxDescender = descender;
            result.ScaledMaxLineHeight = lineHeight;

            // Remove those items from this line.
            this.data.RemoveRange(index, this.data.Count - index);

            // Now trim trailing whitespace from this line.
            this.TrimTrailingWhitespaceAndRecalculateMetrics();

            return result;
        }

        private void TrimTrailingWhitespaceAndRecalculateMetrics()
        {
            int index = this.data.Count;
            while (index > 0)
            {
                if (!CodePoint.IsWhiteSpace(this.data[index - 1].CodePoint))
                {
                    break;
                }

                index--;
            }

            if (index < this.data.Count && index != 0)
            {
                this.data.RemoveRange(index, this.data.Count - index);
            }

            // Lastly recalculate this line metrics.
            float advance = 0;
            float ascender = 0;
            float descender = 0;
            float lineHeight = 0;
            for (int i = 0; i < this.data.Count; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                advance += glyph.ScaledAdvance;
                ascender = MathF.Max(ascender, glyph.ScaledAscender);
                descender = MathF.Max(descender, glyph.ScaledDescender);
                lineHeight = MathF.Max(lineHeight, glyph.ScaledLineHeight);
            }

            this.ScaledLineAdvance = advance;
            this.ScaledMaxAscender = ascender;
            this.ScaledMaxDescender = descender;
            this.ScaledMaxLineHeight = lineHeight;
        }

        public readonly TextLine Finalize() => this.BidiReOrder();

        public readonly void Justify(TextOptions options)
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

        private readonly TextLine BidiReOrder()
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
                return this;
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

            return this;
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
                int offset,
                bool isRotated,
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
                this.Offset = offset;
                this.IsRotated = isRotated;
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

            public int Offset { get; }

            public bool IsRotated { get; }

            public bool IsDecomposed { get; }

            public int StringIndex { get; }

            public readonly bool IsNewLine => CodePoint.IsNewLine(this.CodePoint);

            private readonly string DebuggerDisplay => FormattableString
                .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {this.TextDirection} : {this.Offset}, level: {this.BidiRun.Level}");
        }

        private sealed class OrderedBidiRun
        {
            private ArrayBuilder<GlyphLayoutData> info;

            public OrderedBidiRun(int level) => this.Level = level;

            ~OrderedBidiRun()
            {
                this.info.Free();
                this.info = default;
            }

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

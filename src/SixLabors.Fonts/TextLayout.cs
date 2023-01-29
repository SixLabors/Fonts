// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
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
            List<TextRun> textRuns = new();
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
                if (textRun.Font is null)
                {
                    textRun.Font = options.Font;
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
                // Offset error by user, last index in input string
                // instead of exclusive index.
                if (start == end - 1)
                {
                    int prevIndex = textRuns.Count - 1;
                    TextRun previous = textRuns[prevIndex];
                    previous.End++;
                }
                else
                {
                    textRuns.Add(new()
                    {
                        Start = start,
                        End = end,
                        Font = options.Font
                    });
                }
            }

            return textRuns;
        }

        private static TextBox ProcessText(ReadOnlySpan<char> text, TextOptions options)
        {
            // Gather the font and fallbacks.
            Font[] fallbackFonts = (options.FallbackFontFamilies?.Count > 0)
                ? options.FallbackFontFamilies.Select(x => new Font(x, options.Font.Size, options.Font.RequestedStyle)).ToArray()
                : Array.Empty<Font>();

            LayoutMode layoutMode = options.LayoutMode;
            GlyphSubstitutionCollection substitutions = new(options);
            GlyphPositioningCollection positionings = new(options);

            // Analyse the text for bidi directional runs.
            BidiAlgorithm bidi = BidiAlgorithm.Instance.Value!;
            var bidiData = new BidiData();
            bidiData.Init(text, (sbyte)options.TextDirection);

            // If we have embedded directional overrides then change those
            // ranges to neutral.
            if (options.TextDirection != TextDirection.Auto)
            {
                bidiData.SaveTypes();
                bidiData.Types.Span.Fill(BidiCharacterType.OtherNeutral);
                bidiData.PairedBracketTypes.Span.Fill(BidiPairedBracketType.None);
            }

            bidi.Process(bidiData);

            // Get the list of directional runs
            BidiRun[] bidiRuns = BidiRun.CoalesceLevels(bidi.ResolvedLevels).ToArray();
            Dictionary<int, int> bidiMap = new();

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

        private static IReadOnlyList<GlyphLayout> LayoutText(TextBox textBox, TextOptions options)
        {
            LayoutMode layoutMode = options.LayoutMode;
            List<GlyphLayout> glyphs = new();
            Vector2 location = options.Origin / options.Dpi;
            float maxScaledAdvance = textBox.ScaledMaxAdvance();
            TextDirection direction = textBox.TextDirection();

            if (layoutMode == LayoutMode.HorizontalTopBottom)
            {
                for (int i = 0; i < textBox.TextLines.Count; i++)
                {
                    glyphs.AddRange(LayoutLineHorizontal(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, i, ref location));
                }
            }
            else if (layoutMode == LayoutMode.HorizontalBottomTop)
            {
                int index = 0;
                for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
                {
                    glyphs.AddRange(LayoutLineHorizontal(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, index++, ref location));
                }
            }
            else if (layoutMode == LayoutMode.VerticalLeftRight)
            {
                for (int i = 0; i < textBox.TextLines.Count; i++)
                {
                    glyphs.AddRange(LayoutLineVertical(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, i, ref location));
                }
            }
            else
            {
                int index = 0;
                for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
                {
                    glyphs.AddRange(LayoutLineVertical(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, index++, ref location));
                }
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineHorizontal(
            TextBox textBox,
            TextLine textLine,
            TextDirection direction,
            float maxScaledAdvance,
            TextOptions options,
            int index,
            ref Vector2 location)
        {
            float scaledMaxLineGap = textBox.ScaledMaxLineGap(textLine.MaxPointSize);
            float scaledMaxAscender = textBox.ScaledMaxAscender(textLine.MaxPointSize);
            float scaledMaxDescender = textBox.ScaledMaxDescender(textLine.MaxPointSize);
            float scaledMaxLineHeight = textBox.ScaledMaxLineHeight(textLine.MaxPointSize);

            bool isFirstLine = index == 0;
            bool isLastLine = index == textBox.TextLines.Count - 1;
            float scaledLineAdvance = scaledMaxLineHeight * options.LineSpacing;

            // Recalculate the advance based upon the next line.
            // If larger, we want to scale it up to ensure it it pushed down far enough.
            // We split the different at 2/3 (heuristically determined value based upon extensive visual testing).
            if (!isFirstLine && !isLastLine)
            {
                TextLine next = textBox.TextLines[index + 1];
                float nextLineAdvance = textBox.ScaledMaxLineHeight(next.MaxPointSize) * options.LineSpacing;
                scaledLineAdvance += (nextLineAdvance - scaledLineAdvance) * .667F;
            }

            float originX = location.X;
            float offsetY = 0;
            float offsetX = 0;

            // Set the Y-Origin for the line.
            if (isFirstLine)
            {
                switch (options.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        offsetY = scaledMaxAscender;
                        break;
                    case VerticalAlignment.Center:
                        offsetY = (scaledMaxAscender - (scaledMaxDescender + scaledMaxLineGap)) * .5F;
                        for (int i = index; i < textBox.TextLines.Count - 1; i++)
                        {
                            float advance = textBox.ScaledMaxLineHeight(textBox.TextLines[i].MaxPointSize);
                            if (i != 0)
                            {
                                TextLine next = textBox.TextLines[index + 1];
                                float nextLineAdvance = textBox.ScaledMaxLineHeight(next.MaxPointSize);
                                advance += (nextLineAdvance - advance) * .667F;
                            }

                            offsetY -= advance * options.LineSpacing * .5F;
                        }

                        break;
                    case VerticalAlignment.Bottom:
                        offsetY = -(scaledMaxDescender + scaledMaxLineGap);
                        for (int i = index; i < textBox.TextLines.Count - 1; i++)
                        {
                            float advance = textBox.ScaledMaxLineHeight(textBox.TextLines[i].MaxPointSize);
                            if (i != 0)
                            {
                                TextLine next = textBox.TextLines[index + 1];
                                float nextLineAdvance = textBox.ScaledMaxLineHeight(next.MaxPointSize);
                                advance += (nextLineAdvance - advance) * .667F;
                            }

                            offsetY -= advance * options.LineSpacing;
                        }

                        break;
                }

                location.Y += offsetY;
            }

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

            location.X += offsetX;

            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphLayoutData data = textLine[i];
                if (data.IsNewLine)
                {
                    location.Y += scaledLineAdvance;
                    continue;
                }

                foreach (GlyphMetrics metric in data.Metrics)
                {
                    // Advance Width & Height can be 0 which is fine for layout but not for measuring.
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                    float advanceX = data.ScaledAdvance;
                    float advanceY = metric.AdvanceHeight * scale.Y;
                    if (advanceX == 0)
                    {
                        advanceX = (metric.LeftSideBearing + metric.Width) * scale.X;
                    }

                    if (advanceY == 0)
                    {
                        advanceY = (metric.TopSideBearing + metric.Height) * scale.Y;
                    }

                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        location,
                        scaledMaxAscender,
                        scaledMaxDescender,
                        scaledMaxLineGap,
                        scaledLineAdvance,
                        advanceX,
                        advanceY,
                        i == 0));
                }

                location.X += data.ScaledAdvance;
            }

            location.X = originX;
            if (glyphs.Count > 0)
            {
                location.Y += scaledLineAdvance;
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineVertical(
            TextBox textBox,
            TextLine textLine,
            TextDirection direction,
            float maxScaledAdvance,
            TextOptions options,
            int index,
            ref Vector2 location)
        {
            float originY = location.Y;
            float offsetY = 0;
            float offsetX = 0;

            // Set the Y-Origin for the line.
            float scaledMaxLineGap = textBox.ScaledMaxLineGap(textLine.MaxPointSize);
            float scaledMaxAscender = textBox.ScaledMaxAscender(textLine.MaxPointSize);
            float scaledMaxDescender = textBox.ScaledMaxDescender(textLine.MaxPointSize);
            float scaledMaxLineHeight = textBox.ScaledMaxLineHeight(textLine.MaxPointSize);

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

            location.Y += offsetY;

            bool isFirstLine = index == 0;
            if (isFirstLine)
            {
                // Set the X-Origin for horizontal alignment.
                switch (options.HorizontalAlignment)
                {
                    case HorizontalAlignment.Right:
                        // The textline methods are memoized so we're safe to call them multiple times.
                        for (int i = 0; i < textBox.TextLines.Count; i++)
                        {
                            offsetX -= textBox.ScaledMaxLineHeight(textBox.TextLines[i].MaxPointSize) * options.LineSpacing;
                        }

                        break;
                    case HorizontalAlignment.Center:
                        for (int i = 0; i < textBox.TextLines.Count; i++)
                        {
                            offsetX -= textBox.ScaledMaxLineHeight(textBox.TextLines[i].MaxPointSize) * options.LineSpacing * .5F;
                        }

                        break;
                }
            }

            location.X += offsetX;

            List<GlyphLayout> glyphs = new();
            float xWidth = scaledMaxLineHeight * (isFirstLine ? 1F : options.LineSpacing);
            float xLineAdvance = scaledMaxLineHeight * options.LineSpacing;

            if (isFirstLine)
            {
                xLineAdvance -= (xLineAdvance - scaledMaxLineHeight) * .5F;
            }

            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphLayoutData data = textLine[i];
                if (data.IsNewLine)
                {
                    location.X += xLineAdvance;
                    location.Y = originY;
                    continue;
                }

                foreach (GlyphMetrics metric in data.Metrics)
                {
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                    float advanceX = xLineAdvance;
                    float advanceY = data.ScaledAdvance;

                    // Advance Width & Height can be 0 which is fine for layout but not for measuring.
                    if (advanceX == 0)
                    {
                        advanceX = (metric.LeftSideBearing + metric.Width) * scale.X;
                    }

                    if (advanceY == 0)
                    {
                        advanceY = (metric.TopSideBearing + metric.Height) * scale.Y;
                    }

                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        location + new Vector2((xWidth - (metric.AdvanceWidth * scale.X)) * .5F, data.ScaledAscender),
                        scaledMaxAscender,
                        scaledMaxDescender,
                        scaledMaxLineGap,
                        scaledMaxLineHeight,
                        advanceX,
                        advanceY,
                        i == 0));
                }

                location.Y += data.ScaledAdvance;
            }

            location.Y = originY;
            if (glyphs.Count > 0)
            {
                location.X += xLineAdvance;
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
            var graphemeEnumerator = new SpanGraphemeEnumerator(text);
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
                var codePointEnumerator = new SpanCodePointEnumerator(graphemeEnumerator.Current);
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
                GlyphShapingData data = collection.GetGlyphShapingData(i);

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
            if (collection.IsVerticalLayoutMode)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(i);
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
            float wrappingLength = shouldWrap ? options.WrappingLength / options.Dpi : float.MaxValue;
            bool breakAll = options.WordBreaking == WordBreaking.BreakAll;
            bool keepAll = options.WordBreaking == WordBreaking.KeepAll;
            bool isHorizontal = !layoutMode.IsVertical();

            // Calculate the position of potential line breaks.
            var lineBreakEnumerator = new LineBreakEnumerator(text);
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
            List<TextLine> textLines = new();
            TextLine textLine = new();
            int glyphCount = 0;

            // Enumerate through each grapheme in the text.
            var graphemeEnumerator = new SpanGraphemeEnumerator(text);
            for (graphemeIndex = 0; graphemeEnumerator.MoveNext(); graphemeIndex++)
            {
                // Now enumerate through each codepoint in the grapheme.
                int graphemeCodePointIndex = 0;
                var codePointEnumerator = new SpanCodePointEnumerator(graphemeEnumerator.Current);
                while (codePointEnumerator.MoveNext())
                {
                    if (!positionings.TryGetGlyphMetricsAtOffset(codePointIndex, out float pointSize, out bool isDecomposed, out IReadOnlyList<GlyphMetrics>? metrics))
                    {
                        // Codepoint was skipped during original enumeration.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    CodePoint codePoint = codePointEnumerator.Current;
                    if (CodePoint.IsVariationSelector(codePoint))
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    // Calculate the advance for the current codepoint.
                    GlyphMetrics glyph = metrics[0];
                    float glyphAdvance = isHorizontal ? glyph.AdvanceWidth : glyph.AdvanceHeight;
                    if (CodePoint.IsTabulation(codePoint))
                    {
                        glyphAdvance *= options.TabWidth;
                    }
                    else if (metrics.Count == 1 && (CodePoint.IsZeroWidthJoiner(codePoint) || CodePoint.IsZeroWidthNonJoiner(codePoint)))
                    {
                        // The zero-width joiner characters should be ignored when determining word or
                        // line break boundaries so are safe to skip here. Any existing instances are the result of font error
                        // unless multiple metrics are associated with code point. In this case they are most likely the result
                        // of a substitution and shouldn't be ignored.
                        glyphAdvance = 0;
                    }
                    else if (!CodePoint.IsNewLine(codePoint))
                    {
                        // Standard text.
                        // If decomposed we need to add the advance; otherwise, use the largest advance for the metrics.
                        if (isHorizontal)
                        {
                            for (int i = 1; i < metrics.Count; i++)
                            {
                                float a = metrics[i].AdvanceWidth;
                                if (isDecomposed)
                                {
                                    glyphAdvance += a;
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
                                }
                                else if (a > glyphAdvance)
                                {
                                    glyphAdvance = a;
                                }
                            }
                        }
                    }

                    glyphAdvance *= pointSize / (isHorizontal ? glyph.ScaleFactor.X : glyph.ScaleFactor.Y);

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
                            if (breakAll)
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
                                    if (split != textLine)
                                    {
                                        textLines.Add(textLine.Finalize());
                                        textLine = split;
                                        lineAdvance = split.ScaledLineAdvance;
                                    }
                                }
                                else
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
                                if (split != textLine)
                                {
                                    textLines.Add(textLine.Finalize());
                                    textLine = split;
                                    lineAdvance = split.ScaledLineAdvance;
                                }
                            }
                            else if (lastLineBreak.PositionWrap < codePointIndex)
                            {
                                // Split the current textline into two at the last wrapping point.
                                TextLine split = textLine.SplitAt(lastLineBreak, keepAll);
                                if (split != textLine)
                                {
                                    textLines.Add(textLine.Finalize());
                                    textLine = split;
                                    lineAdvance = split.ScaledLineAdvance;
                                }
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

                    GlyphMetrics metric = metrics[0];
                    float scaleY = pointSize / metric.ScaleFactor.Y;
                    float ascender = metric.FontMetrics.Ascender * scaleY;

                    // Adjust ascender for glyphs with a negative tsb. e.g. emoji to prevent cutoff.
                    if (!CodePoint.IsWhiteSpace(codePoint))
                    {
                        short tsbOffset = 0;
                        for (int i = 0; i < metrics.Count; i++)
                        {
                            tsbOffset = Math.Min(tsbOffset, metrics[i].TopSideBearing);
                        }

                        if (tsbOffset < 0)
                        {
                            ascender -= tsbOffset * scaleY;
                        }
                    }

                    float descender = Math.Abs(metric.FontMetrics.Descender * scaleY);
                    float lineHeight = metric.FontMetrics.LineHeight * scaleY;
                    float lineGap = lineHeight - (ascender + descender);

                    // Add our metrics to the line.
                    lineAdvance += glyphAdvance;
                    textLine.Add(
                        metrics,
                        pointSize,
                        glyphAdvance,
                        lineHeight,
                        ascender,
                        descender,
                        lineGap,
                        bidiRuns[bidiMap[codePointIndex]],
                        graphemeIndex,
                        codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            // Add the final line.
            if (textLine.Count > 0)
            {
                textLines.Add(textLine.Finalize());
            }

            return new TextBox(options, textLines);
        }

        internal sealed class TextBox
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

            // TODO: It would be very good to cache these.
            public float ScaledMaxAdvance()
                => this.TextLines.Max(x => x.ScaledLineAdvance);

            public float ScaledMaxLineHeight(float pointSize)
                => this.TextLines.Where(x => x.MaxPointSize == pointSize).Max(x => x.ScaledMaxLineHeight);

            public float ScaledMaxAscender(float pointSize)
                => this.TextLines.Where(x => x.MaxPointSize == pointSize).Max(x => x.ScaledMaxAscender);

            public float ScaledMaxDescender(float pointSize)
                => this.TextLines.Where(x => x.MaxPointSize == pointSize).Max(x => x.ScaledMaxDescender);

            public float ScaledMaxLineGap(float pointSize)
                => this.TextLines.Where(x => x.MaxPointSize == pointSize).Max(x => x.ScaledMaxLineGap);

            public TextDirection TextDirection() => this.TextLines[0][0].TextDirection;
        }

        internal sealed class TextLine
        {
            private readonly List<GlyphLayoutData> data = new();

            public int Count => this.data.Count;

            public float MaxPointSize { get; private set; } = -1;

            public float ScaledLineAdvance { get; private set; } = 0;

            public float ScaledMaxLineHeight { get; private set; } = -1;

            public float ScaledMaxAscender { get; private set; } = -1;

            public float ScaledMaxDescender { get; private set; } = -1;

            public float ScaledMaxLineGap { get; private set; } = -1;

            public GlyphLayoutData this[int index] => this.data[index];

            public void Add(
                IReadOnlyList<GlyphMetrics> metrics,
                float pointSize,
                float scaledAdvance,
                float scaledLineHeight,
                float scaledAscender,
                float scaledDescender,
                float scaledLineGap,
                BidiRun bidiRun,
                int graphemeIndex,
                int offset)
            {
                // Reset metrics.
                // We track the maximum metrics for each line to ensure glyphs can be aligned.
                // These will be grouped by the point size for each run within the text to ensure
                // multi-line text maintains an even layout for equal point sizes.
                this.MaxPointSize = MathF.Max(this.MaxPointSize, pointSize);
                this.ScaledLineAdvance += scaledAdvance;
                this.ScaledMaxLineHeight = MathF.Max(this.ScaledMaxLineHeight, scaledLineHeight);
                this.ScaledMaxAscender = MathF.Max(this.ScaledMaxAscender, scaledAscender);
                this.ScaledMaxDescender = MathF.Max(this.ScaledMaxDescender, scaledDescender);
                this.ScaledMaxLineGap = MathF.Max(this.ScaledMaxLineGap, scaledLineGap);

                this.data.Add(new(metrics, pointSize, scaledAdvance, scaledLineHeight, scaledAscender, scaledDescender, scaledLineGap, bidiRun, graphemeIndex, offset));
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
                        return this;
                    }
                }

                // Create a new line ensuring we capture the intitial metrics.
                TextLine result = new();
                result.data.AddRange(this.data.GetRange(index, this.data.Count - index));
                result.ScaledLineAdvance = result.data.Sum(x => x.ScaledAdvance);
                result.MaxPointSize = result.data.Max(x => x.PointSize);
                result.ScaledMaxAscender = result.data.Max(x => x.ScaledAscender);
                result.ScaledMaxDescender = result.data.Max(x => x.ScaledDescender);
                result.ScaledMaxLineHeight = result.data.Max(x => x.ScaledLineHeight);
                result.ScaledMaxLineGap = result.data.Max(x => x.ScaledLineGap);

                // Remove those items from this line.
                this.data.RemoveRange(index, this.data.Count - index);

                // Now trim trailing whitespace from this line.
                index = this.data.Count;
                while (index > 0)
                {
                    if (!CodePoint.IsWhiteSpace(this.data[index - 1].CodePoint))
                    {
                        break;
                    }

                    index--;
                }

                if (index < this.data.Count)
                {
                    this.data.RemoveRange(index, this.data.Count - index);
                }

                // Lastly recalculate this line metrics.
                this.ScaledLineAdvance = this.data.Sum(x => x.ScaledAdvance);
                this.MaxPointSize = this.data.Max(x => x.PointSize);
                this.ScaledMaxAscender = this.data.Max(x => x.ScaledAscender);
                this.ScaledMaxDescender = this.data.Max(x => x.ScaledDescender);
                this.ScaledMaxLineHeight = this.data.Max(x => x.ScaledLineHeight);
                this.ScaledMaxLineGap = this.data.Max(x => x.ScaledLineGap);

                return result;
            }

            public TextLine Finalize() => this.BidiReOrder();

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

            private TextLine BidiReOrder()
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
                    float scaledLineGap,
                    BidiRun bidiRun,
                    int graphemeIndex,
                    int offset)
                {
                    this.Metrics = metrics;
                    this.PointSize = pointSize;
                    this.ScaledAdvance = scaledAdvance;
                    this.ScaledLineHeight = scaledLineHeight;
                    this.ScaledAscender = scaledAscender;
                    this.ScaledDescender = scaledDescender;
                    this.ScaledLineGap = scaledLineGap;
                    this.BidiRun = bidiRun;
                    this.GraphemeIndex = graphemeIndex;
                    this.Offset = offset;
                }

                public CodePoint CodePoint => this.Metrics[0].CodePoint;

                public IReadOnlyList<GlyphMetrics> Metrics { get; }

                public float PointSize { get; }

                public float ScaledAdvance { get; set; }

                public float ScaledLineHeight { get; }

                public float ScaledAscender { get; }

                public float ScaledDescender { get; }

                public float ScaledLineGap { get; }

                public BidiRun BidiRun { get; }

                public TextDirection TextDirection => (TextDirection)this.BidiRun.Direction;

                public int GraphemeIndex { get; }

                public int Offset { get; }

                public bool IsNewLine => CodePoint.IsNewLine(this.CodePoint);

                private string DebuggerDisplay => FormattableString
                    .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {this.TextDirection} : {this.Offset}, level: {this.BidiRun.Level}");
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
}

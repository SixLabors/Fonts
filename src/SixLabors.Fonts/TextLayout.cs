// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            if (options.TextRuns?.Count == 0)
            {
                return new TextRun[]
                {
                    new()
                    {
                        Start = 0,
                        End = CodePoint.GetCodePointCount(text),
                        Font = options.Font
                    }
                };
            }

            int start = 0;
            int end = CodePoint.GetCodePointCount(text);
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
            foreach (TextRun textRun in textRuns)
            {
                if (!DoFontRun(
                    textRun.Slice(text),
                    textRun.Start,
                    textRuns,
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
                    if (DoFontRun(
                        text,
                        0,
                        textRuns,
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
            float maxScaledAdvance = textBox.TextLines.Max(x => x.ScaledLineAdvance());
            TextDirection direction = textBox.TextDirection();
            if (layoutMode == LayoutMode.HorizontalTopBottom)
            {
                for (int i = 0; i < textBox.TextLines.Count; i++)
                {
                    glyphs.AddRange(LayoutLineHorizontal(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                }
            }
            else if (layoutMode == LayoutMode.HorizontalBottomTop)
            {
                for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
                {
                    glyphs.AddRange(LayoutLineHorizontal(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                }
            }
            else if (layoutMode == LayoutMode.VerticalLeftRight)
            {
                for (int i = 0; i < textBox.TextLines.Count; i++)
                {
                    glyphs.AddRange(LayoutLineVertical(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                }
            }
            else
            {
                for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
                {
                    glyphs.AddRange(LayoutLineVertical(textBox, textBox.TextLines[i], direction, maxScaledAdvance, options, glyphs.Count == 0, ref location));
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
            bool first,
            ref Vector2 location)
        {
            float originX = location.X;
            float offsetY = 0;
            float offsetX = 0;
            if (first)
            {
                // Set the Y-Origin for the first line.
                float scaledMaxAscender = textBox.ScaledMaxAscender + textBox.ScaledMaxLineGap;
                float scaledMaxDescender = textBox.ScaledMaxDescender;
                switch (options.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        offsetY = scaledMaxAscender;
                        break;
                    case VerticalAlignment.Center:
                        offsetY = (scaledMaxAscender * .5F) - (scaledMaxDescender * .5F);
                        offsetY -= (textBox.TextLines.Count - 1) * textBox.ScaledMaxLineHeight * options.LineSpacing * .5F;
                        break;
                    case VerticalAlignment.Bottom:
                        offsetY = -scaledMaxDescender;
                        offsetY -= (textBox.TextLines.Count - 1) * textBox.ScaledMaxLineHeight * options.LineSpacing;
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
                        offsetX += maxScaledAdvance - textLine.ScaledLineAdvance();
                        break;
                    case TextAlignment.Center:
                        offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                        break;
                }
            }
            else
            {
                switch (options.TextAlignment)
                {
                    case TextAlignment.Start:
                        offsetX += maxScaledAdvance - textLine.ScaledLineAdvance();
                        break;
                    case TextAlignment.Center:
                        offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                        break;
                }
            }

            location.X += offsetX;

            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphLayoutData info = textLine[i];
                if (info.IsNewLine)
                {
                    location.Y += textBox.ScaledMaxLineHeight * options.LineSpacing;
                    continue;
                }

                foreach (GlyphMetrics metric in info.Metrics)
                {
                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, info.PointSize),
                        location,
                        info.ScaledAdvance,
                        metric.AdvanceHeight * (info.PointSize / metric.ScaleFactor),
                        textBox.ScaledMaxLineHeight * options.LineSpacing,
                        i == 0));
                }

                location.X += info.ScaledAdvance;
            }

            location.X = originX;
            if (glyphs.Count > 0)
            {
                location.Y += textBox.ScaledMaxLineHeight * options.LineSpacing;
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineVertical(
            TextBox textBox,
            TextLine textLine,
            TextDirection direction,
            float maxScaledAdvance,
            TextOptions options,
            bool first,
            ref Vector2 location)
        {
            float originY = location.Y;
            float offsetY = 0;
            float offsetX = 0;

            // Set the Y-Origin for the first line.
            float scaledMaxAscender = textBox.ScaledMaxAscender + textBox.ScaledMaxLineGap;
            float scaledMaxDescender = textBox.ScaledMaxDescender;
            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    offsetY = scaledMaxAscender;
                    break;
                case VerticalAlignment.Center:
                    offsetY = (scaledMaxAscender * .5F) - (scaledMaxDescender * .5F);
                    offsetY -= (maxScaledAdvance - textBox.ScaledMaxLineHeight) * .5F;
                    break;
                case VerticalAlignment.Bottom:
                    offsetY = -scaledMaxDescender;
                    offsetY -= maxScaledAdvance - textBox.ScaledMaxLineHeight;
                    break;
            }

            // Set the alignment of lines within the text.
            if (direction == TextDirection.LeftToRight)
            {
                switch (options.TextAlignment)
                {
                    case TextAlignment.End:
                        offsetY += maxScaledAdvance - textLine.ScaledLineAdvance();
                        break;
                    case TextAlignment.Center:
                        offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                        break;
                }
            }
            else
            {
                switch (options.TextAlignment)
                {
                    case TextAlignment.Start:
                        offsetY += maxScaledAdvance - textLine.ScaledLineAdvance();
                        break;
                    case TextAlignment.Center:
                        offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                        break;
                }
            }

            location.Y += offsetY;

            if (first)
            {
                // Set the X-Origin for horizontal alignment.
                switch (options.HorizontalAlignment)
                {
                    case HorizontalAlignment.Right:
                        offsetX = -(textBox.ScaledMaxLineHeight * options.LineSpacing);
                        offsetX -= (textBox.TextLines.Count - 1) * textBox.ScaledMaxLineHeight * options.LineSpacing;
                        break;
                    case HorizontalAlignment.Center:
                        offsetX = -(textBox.ScaledMaxLineHeight * options.LineSpacing * .5F);
                        offsetX -= (textBox.TextLines.Count - 1) * textBox.ScaledMaxLineHeight * options.LineSpacing * .5F;
                        break;
                }
            }

            location.X += offsetX;

            List<GlyphLayout> glyphs = new();
            float xWidth = textBox.ScaledMaxLineHeight * (first ? 1F : options.LineSpacing);
            float xLineAdvance = textBox.ScaledMaxLineHeight * options.LineSpacing;

            if (first)
            {
                xLineAdvance -= (xLineAdvance - textBox.ScaledMaxLineHeight) * .5F;
            }

            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphLayoutData info = textLine[i];
                if (info.IsNewLine)
                {
                    location.X += xLineAdvance;
                    location.Y = originY;
                    continue;
                }

                foreach (GlyphMetrics metric in info.Metrics)
                {
                    glyphs.Add(new GlyphLayout(
                        new Glyph(metric, info.PointSize),
                        location + new Vector2((xWidth - (metric.AdvanceWidth * (info.PointSize / metric.ScaleFactor))) * .5F, 0),
                        xLineAdvance,
                        info.ScaledAdvance,
                        textBox.ScaledMaxLineHeight,
                        i == 0));
                }

                location.Y += info.ScaledAdvance;
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
            int graphemeIndex;
            int codePointIndex = start;
            int bidiRun = 0;
            int textRun = 0;
            var graphemeEnumerator = new SpanGraphemeEnumerator(text);
            for (graphemeIndex = 0; graphemeEnumerator.MoveNext(); graphemeIndex++)
            {
                int graphemeMax = graphemeEnumerator.Current.Length - 1;
                int graphemeCodePointIndex = 0;
                int charIndex = 0;

                // Now enumerate through each codepoint in the grapheme.
                bool skipNextCodePoint = false;
                var codePointEnumerator = new SpanCodePointEnumerator(graphemeEnumerator.Current);
                while (codePointEnumerator.MoveNext())
                {
                    if (codePointIndex == bidiRuns[bidiRun].End)
                    {
                        bidiRun++;
                    }

                    if (codePointIndex == textRuns[textRun].End)
                    {
                        textRun++;
                    }

                    if (skipNextCodePoint)
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    bidiMap[codePointIndex] = bidiRun;

                    int charsConsumed = 0;
                    CodePoint current = codePointEnumerator.Current;
                    charIndex += current.Utf16SequenceLength;
                    CodePoint? next = graphemeCodePointIndex < graphemeMax
                        ? CodePoint.DecodeFromUtf16At(graphemeEnumerator.Current, charIndex, out charsConsumed)
                        : null;

                    charIndex += charsConsumed;

                    // Get the glyph id for the codepoint and add to the collection.
                    font.FontMetrics.TryGetGlyphId(current, next, out ushort glyphId, out skipNextCodePoint);
                    substitutions.AddGlyph(glyphId, current, (TextDirection)bidiRuns[bidiRun].Direction, textRuns[textRun].TextAttributes, codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            // Apply the simple and complex substitutions.
            // TODO: Investigate HarfBuzz normlizer.
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

            float scaledMaxAscender = 0;
            float scaledMaxDescender = 0;
            float scaledMaxLineHeight = 0;
            float scaledMaxLeftSideBearing = 0;

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
                    if (!positionings.TryGetGlyphMetricsAtOffset(codePointIndex, out float pointSize, out GlyphMetrics[]? metrics))
                    {
                        // Codepoint was skipped during original enumeration.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    // Calculate the advance for the current codepoint.
                    CodePoint codePoint = codePointEnumerator.Current;
                    GlyphMetrics glyph = metrics[0];
                    float glyphAdvance = isHorizontal ? glyph.AdvanceWidth : glyph.AdvanceHeight;
                    if (CodePoint.IsVariationSelector(codePoint))
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    if (CodePoint.IsTabulation(codePoint))
                    {
                        glyphAdvance *= options.TabWidth;
                    }
                    else if (metrics.Length == 1 && (CodePoint.IsZeroWidthJoiner(codePoint) || CodePoint.IsZeroWidthNonJoiner(codePoint)))
                    {
                        // The zero-width joiner characters should be ignored when determining word or
                        // line break boundaries so are safe to skip here. Any existing instances are the result of font error.
                        // It multiple metrics are associated with code point, they are most likely the result of a substitution so we shouldn't ignore it.
                        glyphAdvance = 0;
                    }
                    else if (!CodePoint.IsNewLine(codePoint))
                    {
                        // Standard text. Use the largest advance for the metrics.
                        if (isHorizontal)
                        {
                            for (int i = 1; i < metrics.Length; i++)
                            {
                                float a = metrics[i].AdvanceWidth;
                                if (a > glyphAdvance)
                                {
                                    glyphAdvance = a;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 1; i < metrics.Length; i++)
                            {
                                float a = metrics[i].AdvanceHeight;
                                if (a > glyphAdvance)
                                {
                                    glyphAdvance = a;
                                }
                            }
                        }
                    }

                    glyphAdvance *= pointSize / glyph.ScaleFactor;

                    // Should we start a new line?
                    bool requiredBreak = false;
                    if (graphemeCodePointIndex == 0)
                    {
                        // Mandatory wrap at index.
                        if (currentLineBreak.PositionWrap == codePointIndex && currentLineBreak.Required)
                        {
                            textLines.Add(textLine.BidiReOrder());
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
                                textLines.Add(textLine.BidiReOrder());
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
                                        textLines.Add(textLine.BidiReOrder());
                                        textLine = split;
                                        lineAdvance = split.ScaledLineAdvance();
                                    }
                                }
                                else
                                {
                                    textLines.Add(textLine.BidiReOrder());
                                    glyphCount += textLine.Count;
                                    textLine = new();
                                    lineAdvance = 0;
                                }
                            }
                            else if (lastLineBreak.PositionWrap < codePointIndex)
                            {
                                // Split the current textline into two at the last wrapping point.
                                TextLine split = textLine.SplitAt(lastLineBreak, keepAll);
                                if (split != textLine)
                                {
                                    textLines.Add(textLine.BidiReOrder());
                                    textLine = split;
                                    lineAdvance = split.ScaledLineAdvance();
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
                    float ascender = metric.FontMetrics.Ascender * pointSize / metric.ScaleFactor;
                    float descender = Math.Abs(metric.FontMetrics.Descender * pointSize / metric.ScaleFactor);
                    float lineHeight = metric.FontMetrics.LineHeight * pointSize / metric.ScaleFactor;
                    float leftSideBearing = metric.LeftSideBearing * pointSize / metric.ScaleFactor;

                    if (ascender > scaledMaxAscender)
                    {
                        scaledMaxAscender = ascender;
                    }

                    if (descender > scaledMaxDescender)
                    {
                        scaledMaxDescender = descender;
                    }

                    if (lineHeight > scaledMaxLineHeight)
                    {
                        scaledMaxLineHeight = lineHeight;
                    }

                    if (leftSideBearing > scaledMaxLeftSideBearing)
                    {
                        scaledMaxLeftSideBearing = leftSideBearing;
                    }

                    // Add our metrics to the line.
                    lineAdvance += glyphAdvance;
                    textLine.Add(
                        metrics,
                        pointSize,
                        glyphAdvance,
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
                textLines.Add(textLine.BidiReOrder());
            }

            return new TextBox(textLines, scaledMaxAscender, scaledMaxDescender, scaledMaxLineHeight, scaledMaxLeftSideBearing);
        }

        internal sealed class TextBox
        {
            public TextBox(
                IList<TextLine> textLines,
                float scaledMaxAscender,
                float scaledMaxDescender,
                float scaledMaxLineHeight,
                float scaledMaxLeftSideBearing)
            {
                this.TextLines = new(textLines);
                this.ScaledMaxAscender = scaledMaxAscender;
                this.ScaledMaxDescender = scaledMaxDescender;
                this.ScaledMaxLineHeight = scaledMaxLineHeight;
                this.ScaledMaxLineGap = scaledMaxLineHeight - (scaledMaxAscender + scaledMaxDescender);
                this.ScaledMaxLeftSideBearing = scaledMaxLeftSideBearing;
            }

            public float ScaledMaxAscender { get; }

            public float ScaledMaxDescender { get; }

            public float ScaledMaxLineHeight { get; }

            public float ScaledMaxLineGap { get; }

            public float ScaledMaxLeftSideBearing { get; }

            public ReadOnlyCollection<TextLine> TextLines { get; }

            public TextDirection TextDirection() => this.TextLines[0][0].TextDirection;
        }

        internal sealed class TextLine
        {
            private readonly List<GlyphLayoutData> info = new();

            public int Count => this.info.Count;

            public GlyphLayoutData this[int index] => this.info[index];

            public float ScaledLineAdvance()
                => this.info.Sum(x => x.ScaledAdvance);

            public void Add(
                GlyphMetrics[] metrics,
                float pointSize,
                float scaledAdvance,
                BidiRun bidiRun,
                int graphemeIndex,
                int offset)
                => this.info.Add(new(metrics, pointSize, scaledAdvance, bidiRun, graphemeIndex, offset));

            public TextLine SplitAt(LineBreak lineBreak, bool keepAll)
            {
                int index = this.info.Count;
                GlyphLayoutData glyphWrap = default;
                while (index > 0)
                {
                    glyphWrap = this.info[--index];

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
                        glyphWrap = this.info[--index];
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

                TextLine result = new();
                result.info.AddRange(this.info.GetRange(index, this.info.Count - index));
                this.info.RemoveRange(index, this.info.Count - index);

                // Trim trailing whitespace from previous line.
                index = this.info.Count;
                while (index > 0)
                {
                    if (!CodePoint.IsWhiteSpace(this.info[index - 1].CodePoint))
                    {
                        break;
                    }

                    index--;
                }

                if (index < this.info.Count)
                {
                    this.info.RemoveRange(index, this.info.Count - index);
                }

                return result;
            }

            public TextLine BidiReOrder()
            {
                // Build up the collection of ordered runs.
                BidiRun run = this.info[0].BidiRun;
                OrderedBidiRun orderedRun = new(run.Level);
                OrderedBidiRun? current = orderedRun;
                for (int i = 0; i < this.info.Count; i++)
                {
                    GlyphLayoutData g = this.info[i];
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
                for (int i = 0; i < this.info.Count; i++)
                {
                    int level = this.info[i].BidiRun.Level;
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

                this.info.Clear();
                current = orderedRun;
                while (current != null)
                {
                    this.info.AddRange(current.AsSlice());
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
            internal readonly struct GlyphLayoutData
            {
                public GlyphLayoutData(
                    GlyphMetrics[] metrics,
                    float pointSize,
                    float scaledAdvance,
                    BidiRun bidiRun,
                    int graphemeIndex,
                    int offset)
                {
                    this.Metrics = metrics;
                    this.PointSize = pointSize;
                    this.ScaledAdvance = scaledAdvance;
                    this.BidiRun = bidiRun;
                    this.GraphemeIndex = graphemeIndex;
                    this.Offset = offset;
                }

                public CodePoint CodePoint => this.Metrics[0].CodePoint;

                public GlyphMetrics[] Metrics { get; }

                public float PointSize { get; }

                public float ScaledAdvance { get; }

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

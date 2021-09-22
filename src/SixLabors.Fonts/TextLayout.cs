// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    internal class TextLayout
    {
        internal static TextLayout Default { get; set; } = new();

        public IReadOnlyList<GlyphLayout> GenerateLayout(ReadOnlySpan<char> text, RendererOptions options)
        {
            if (text.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            if (options.WrappingWidth > 0)
            {
                // Trim trailing white spaces from the text
                text = text.TrimEnd(null);
            }

            // Check our string again after trimming.
            if (text.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            TextBox textBox = ProcessText(text, options);
            return LayoutText(textBox, options);
        }

        private static TextBox ProcessText(ReadOnlySpan<char> text, RendererOptions options)
        {
            // Gather the font and fallbacks.
            IFontMetrics mainFont = options.Font.FontMetrics;
            IFontMetrics[] fallbackFonts;
            if (options.FallbackFontFamilies is null)
            {
                fallbackFonts = Array.Empty<IFontMetrics>();
            }
            else
            {
                fallbackFonts = options.FallbackFontFamilies
                    .Select(x => new Font(x, options.Font.Size, options.Font.RequestedStyle).FontMetrics)
                    .ToArray();
            }

            LayoutMode layoutMode = options.LayoutMode;
            var substitutions = new GlyphSubstitutionCollection();
            var positionings = new GlyphPositioningCollection(layoutMode);

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
            ArraySlice<sbyte> resolvedLevels = bidi.ResolvedLevels;

            // Now process the embedded runs
            if (options.TextDirection != TextDirection.Auto)
            {
                // Restore types
                bidiData.RestoreTypes();

                // Get a temp buffer to store the results
                // (We can't use the Bidi's built in buffer because we're about to patch it)
                ArraySlice<sbyte> levels = bidiData.GetTempLevelBuffer(bidiData.Types.Length);

                // Reprocess the data
                bidi.Process(
                    bidiData.Types,
                    bidiData.PairedBracketTypes,
                    bidiData.PairedBracketValues,
                    (sbyte)options.TextDirection,
                    bidiData.HasBrackets,
                    bidiData.HasEmbeddings,
                    bidiData.HasIsolates,
                    levels);

                // Copy result levels back to the full level set
                levels.Span.CopyTo(resolvedLevels.Span);
            }

            // Get the list of directional runs
            BidiRun[] bidiRuns = BidiRun.CoalesceLevels(bidi.ResolvedLevels).ToArray();
            Dictionary<int, int> bidiMap = new();

            // Incrementally build out collection of glyphs.
            // For each run we start with a fresh substitution collection to avoid
            // overwriting the glyph ids.
            if (!DoFontRun(
                text,
                options,
                mainFont,
                bidiRuns,
                bidiMap,
                substitutions,
                positionings))
            {
                foreach (IFontMetrics font in fallbackFonts)
                {
                    substitutions.Clear();
                    if (DoFontRun(
                        text,
                        options,
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

            if (options.ApplyKerning)
            {
                // Update the positions of the glyphs in the completed collection.
                // Each set of metrics is associated with single font and will only be updated
                // by that font so it's safe to use a single collection.
                mainFont.UpdatePositions(positionings);
                foreach (IFontMetrics font in fallbackFonts)
                {
                    font.UpdatePositions(positionings);
                }
            }

            return BreakLines(text, options, bidiRuns, bidiMap, positionings, layoutMode);
        }

        private static IReadOnlyList<GlyphLayout> LayoutText(TextBox textBox, RendererOptions options)
        {
            LayoutMode layoutMode = options.LayoutMode;
            List<GlyphLayout> glyphs = new();

            Vector2 location = options.Origin / new Vector2(options.DpiX, options.DpiY);
            float maxScaledAscender = textBox.ScaledMaxAscender;
            float maxScaledAdvance = textBox.TextLines.Max(x => x.ScaledLineAdvance());
            if (layoutMode == LayoutMode.Horizontal)
            {
                foreach (TextLine textLine in textBox.TextLines)
                {
                    glyphs.AddRange(LayoutLineHorizontal(textBox, textLine, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                }
            }
            else
            {
                foreach (TextLine textLine in textBox.TextLines)
                {
                    glyphs.AddRange(LayoutLineVertical(textBox, textLine, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                }
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineHorizontal(
            TextBox textBox,
            TextLine textLine,
            float maxScaledAdvance,
            RendererOptions options,
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
            switch (options.TextAlignment)
            {
                case TextAlignment.Right:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance();
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                    break;
            }

            location.X += offsetX;

            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphInfo info = textLine[i];
                foreach (GlyphMetrics metric in info.Metrics)
                {
                    float scale = info.PointSize / metric.ScaleFactor;
                    if (info.IsNewLine)
                    {
                        location.Y += textBox.ScaledMaxLineHeight * options.LineSpacing;
                        continue;
                    }

                    glyphs.Add(new GlyphLayout(
                        info.GraphemeIndex,
                        metric.CodePoint,
                        new Glyph(metric, info.PointSize),
                        location,
                        info.ScaledAdvance,
                        metric.AdvanceHeight * scale,
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
            float maxScaledAdvance,
            RendererOptions options,
            bool first,
            ref Vector2 location)
        {
            float originY = location.Y;
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
            switch (options.TextAlignment)
            {
                case TextAlignment.Right:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance();
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance() * .5F);
                    break;
            }

            location.X += offsetX;

            // TODO: This seems massive?
            float maxAdvancedWith = textLine.ScaledMaxAdvanceWidth();
            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphInfo info = textLine[i];
                foreach (GlyphMetrics metric in info.Metrics)
                {
                    float scale = info.PointSize / metric.ScaleFactor;
                    if (info.IsNewLine)
                    {
                        location.X += maxAdvancedWith;
                        location.Y = originY;
                        continue;
                    }

                    glyphs.Add(new GlyphLayout(
                        info.GraphemeIndex,
                        metric.CodePoint,
                        new Glyph(metric, info.PointSize),
                        location,
                        metric.AdvanceWidth * scale,
                        info.ScaledAdvance,
                        textBox.ScaledMaxLineHeight * options.LineSpacing,
                        i == 0));
                }

                location.Y += info.ScaledAdvance;
            }

            location.Y = originY;
            if (glyphs.Count > 0)
            {
                location.X += maxAdvancedWith;
            }

            return glyphs;
        }

        private static bool DoFontRun(
            ReadOnlySpan<char> text,
            RendererOptions options,
            IFontMetrics fontMetrics,
            BidiRun[] bidiRuns,
            Dictionary<int, int> bidiMap,
            GlyphSubstitutionCollection substitutions,
            GlyphPositioningCollection positionings)
        {
            // Enumerate through each grapheme in the text.
            int graphemeIndex;
            int codePointIndex = 0;
            int bidiRun = 0;
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
                    fontMetrics.TryGetGlyphId(current, next, out int glyphId, out skipNextCodePoint);
                    substitutions.AddGlyph(glyphId, current, (TextDirection)bidiRuns[bidiRun].Direction, codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            // We always do this, with or without kerning so that bidi mirrored types
            // are substituted correctly.
            SubstituteBidiMirrors(fontMetrics, substitutions);

            if (options.ApplyKerning)
            {
                AssignShapingFeatures(substitutions);
                fontMetrics.ApplySubstitution(substitutions);
            }

            return positionings.TryAddOrUpdate(fontMetrics, substitutions, options);
        }

        private static void SubstituteBidiMirrors(IFontMetrics fontMetrics, GlyphSubstitutionCollection substitutions)
        {
            // TODO: Vertical bidi mirrors appear to be different.
            // See hb-ot-shape.cc in HarfBuzz. Line 651.
            for (int i = 0; i < substitutions.Count; i++)
            {
                substitutions.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out TextDirection direction, out _, out _);

                if (direction != TextDirection.RightToLeft)
                {
                    continue;
                }

                if (!CodePoint.TryGetBidiMirror(codePoint, out CodePoint mirror))
                {
                    continue;
                }

                if (fontMetrics.TryGetGlyphId(mirror, out int glyphId))
                {
                    substitutions.Replace(i, glyphId);
                }
            }
        }

        private static void AssignShapingFeatures(GlyphSubstitutionCollection substitutions)
        {
            for (int i = 0; i < substitutions.Count; i++)
            {
                substitutions.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out _, out _, out _);
                Script current = CodePoint.GetScript(codePoint);

                // Choose a shaper based on the script.
                // This determines which features to apply to which glyphs.
                BaseShaper shaper = ShaperFactory.Create(current);
                int index = i;
                int count = 1;
                while (i < substitutions.Count - 1)
                {
                    // We want to assign the same shaper to individual sections of the text rather
                    // than the text as a whole to ensure that different language shapers do not interfere
                    // with each other when the text contains multiple languages.
                    substitutions.GetCodePointAndGlyphIds(i + 1, out codePoint, out _, out _, out _);
                    Script next = CodePoint.GetScript(codePoint);
                    if (next is not Script.Common and not Script.Unknown and not Script.Inherited && next != current)
                    {
                        break;
                    }

                    i++;
                    count++;
                }

                // Assign Substitution features to each glyph.
                shaper.AssignFeatures(substitutions, index, count);
            }
        }

        private static TextBox BreakLines(
            ReadOnlySpan<char> text,
            RendererOptions options,
            BidiRun[] bidiRuns,
            Dictionary<int, int> bidiMap,
            GlyphPositioningCollection positionings,
            LayoutMode layoutMode)
        {
            float pointSize = options.Font.Size;
            bool shouldWrap = options.WrappingWidth > 0;
            float wrappingLength = shouldWrap ? options.WrappingWidth / options.DpiX : float.MaxValue;
            bool breakAll = options.WordBreaking == WordBreaking.BreakAll;
            bool keepAll = options.WordBreaking == WordBreaking.KeepAll;
            bool isHorizontal = layoutMode == LayoutMode.Horizontal;

            float scaledMaxAscender = 0;
            float scaledMaxDescender = 0;
            float scaledMaxLineHeight = 0;

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
                    if (!positionings.TryGetGlyphMetricsAtOffset(codePointIndex, out GlyphMetrics[]? metrics))
                    {
                        // Codepoint was skipped during original enumeration.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    CodePoint codePoint = codePointEnumerator.Current;

                    // Do not start a line following a break with breaking whitespace.
                    if (textLine.Count == 0 && textLines.Count > 0)
                    {
                        if (CodePoint.IsWhiteSpace(codePoint)
                            && !CodePoint.IsNonBreakingSpace(codePoint)
                            && !CodePoint.IsTabulation(codePoint))
                        {
                            codePointIndex++;
                            graphemeCodePointIndex++;
                            continue;
                        }
                    }

                    // Calculate the advance for the current codepoint.
                    GlyphMetrics glyph = metrics[0];
                    float glyphAdvance = isHorizontal ? glyph.AdvanceWidth : glyph.AdvanceHeight;
                    if (CodePoint.IsTabulation(codePoint))
                    {
                        glyphAdvance *= options.TabWidth;
                    }
                    else if (CodePoint.IsZeroWidthJoiner(codePoint) || CodePoint.IsZeroWidthJoiner(codePoint))
                    {
                        // The zero-width joiner characters should be ignored when determining word or
                        // line break boundaries so are safe to skip here.
                        // Any existing instances are the result of font error.
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

                    if (glyphAdvance == 0)
                    {
                        // Nothing to render.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    glyphAdvance *= pointSize / glyph.ScaleFactor;

                    // Should we start a new line?
                    if (graphemeCodePointIndex == 0)
                    {
                        // Mandatory wrap at index.
                        if (currentLineBreak.PositionWrap == codePointIndex && currentLineBreak.Required)
                        {
                            textLines.Add(textLine.BidiReOrder());
                            glyphCount += textLine.Count;
                            textLine = new();
                            lineAdvance = 0;
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
                    if (textLine.Count == 0 && textLines.Count > 0
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

            return new TextBox(textLines, scaledMaxAscender, scaledMaxDescender, scaledMaxLineHeight);
        }

        internal sealed class TextBox
        {
            public TextBox(IList<TextLine> textLines, float scaledMaxAscender, float scaledMaxDescender, float scaledMaxLineHeight)
            {
                this.TextLines = new(textLines);
                this.ScaledMaxAscender = scaledMaxAscender;
                this.ScaledMaxDescender = scaledMaxDescender;
                this.ScaledMaxLineHeight = scaledMaxLineHeight;
                this.ScaledMaxLineGap = scaledMaxLineHeight - (scaledMaxAscender + scaledMaxDescender);
            }

            public float ScaledMaxAscender { get; }

            public float ScaledMaxDescender { get; }

            public float ScaledMaxLineHeight { get; }

            public float ScaledMaxLineGap { get; }

            public ReadOnlyCollection<TextLine> TextLines { get; }
        }

        internal sealed class TextLine
        {
            private readonly List<GlyphInfo> info = new();

            public int Count => this.info.Count;

            public GlyphInfo this[int index] => this.info[index];

            public float ScaledLineAdvance()
                => this.info.Sum(x => x.ScaledAdvance);

            public float ScaledMaxAscender()
                => this.info.Max(x => x.Metrics[0].FontMetrics.Ascender * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledMaxDescender()
                => this.info.Max(x => Math.Abs(x.Metrics[0].FontMetrics.Descender) * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledMaxLineHeight()
                => this.info.Max(x => x.Metrics[0].FontMetrics.LineHeight * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledMaxLineGap()
                => this.info.Max(x => x.Metrics[0].FontMetrics.LineGap * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledMaxAdvanceWidth()
                => this.info.Max(x => x.Metrics[0].FontMetrics.AdvanceWidthMax * x.PointSize / x.Metrics[0].ScaleFactor);

            public void Add(
                GlyphMetrics[] metrics,
                float pointSize,
                float advance,
                BidiRun bidiRun,
                int graphemeIndex,
                int offset)
                => this.info.Add(new(metrics, pointSize, advance, bidiRun, graphemeIndex, offset));

            public TextLine SplitAt(LineBreak lineBreak, bool keepAll)
            {
                int index = this.info.Count;
                GlyphInfo glyphWrap = default;
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
                    GlyphInfo g = this.info[i];
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
            internal readonly struct GlyphInfo
            {
                public GlyphInfo(
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

            private class OrderedBidiRun
            {
                private ArrayBuilder<GlyphInfo> info;

                public OrderedBidiRun(int level) => this.Level = level;

                public int Level { get; }

                public OrderedBidiRun? Next { get; set; }

                public void Add(GlyphInfo info) => this.info.Add(info);

                public ArraySlice<GlyphInfo> AsSlice() => this.info.AsSlice();

                public void Reverse() => this.AsSlice().Span.Reverse();
            }

            private class BidiRange
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

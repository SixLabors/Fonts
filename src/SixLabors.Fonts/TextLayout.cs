// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
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
        internal static TextLayout Default { get; set; } = new TextLayout();

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

            IEnumerable<TextLine> textLines = ProcessText(text, options);
            return LayoutText(textLines, options);
        }

        private static IEnumerable<TextLine> ProcessText(ReadOnlySpan<char> text, RendererOptions options)
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

            const LayoutMode layoutMode = LayoutMode.Horizontal; // TODO: Support vertical.
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
            BidiRun[] bidiRuns = BidiRun.CoalescLevels(bidi.ResolvedLevels).ToArray();
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

        private static IReadOnlyList<GlyphLayout> LayoutText(IEnumerable<TextLine> textLines, RendererOptions options)
        {
            const LayoutMode layoutMode = LayoutMode.Horizontal; // TODO: Support vertical.
            TextDirection textDirection = textLines.First().Direction();
            List<GlyphLayout> glyphs = new();

            int lineCount = textLines.Count();
            Vector2 location = options.Origin / new Vector2(options.DpiX, options.DpiY);
            if (layoutMode == LayoutMode.Horizontal)
            {
                float maxScaledAdvance = textLines.Max(x => x.ScaledAdvance());
                if (textDirection == TextDirection.LeftToRight)
                {
                    foreach (TextLine textLine in textLines)
                    {
                        glyphs.AddRange(LayoutLineLeftToRightHorizontal(textLine, lineCount, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                    }
                }
                else if (textDirection == TextDirection.RightToLeft)
                {
                    foreach (TextLine textLine in textLines)
                    {
                        glyphs.AddRange(LayoutLineLeftToRightHorizontal(textLine, lineCount, maxScaledAdvance, options, glyphs.Count == 0, ref location));
                    }
                }
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineLeftToRightHorizontal(
            TextLine textLine,
            int lineCount,
            float maxScaledAdvance,
            RendererOptions options,
            bool first,
            ref Vector2 location)
        {
            float originY = 0;
            float originX = 0;
            if (first)
            {
                // Set the Y-Origin for the first line.
                float lineScaledAscender = textLine.ScaledAscender();
                float lineScaledDescender = textLine.ScaledDescender();
                switch (options.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        originY = lineScaledAscender;
                        break;
                    case VerticalAlignment.Center:
                        originY = (lineScaledAscender * .5F) - (lineScaledDescender * .5F);
                        originY -= (lineCount - 1) * textLine.ScaledLineHeight() * options.LineSpacing * .5F;
                        break;
                    case VerticalAlignment.Bottom:
                        originY = -lineScaledDescender;
                        originY -= (lineCount - 1) * textLine.ScaledLineHeight() * options.LineSpacing;
                        break;
                }

                location.Y += originY;
            }

            // Set the X-Origin for horizontal alignment.
            float wrappingAdvance = options.WrappingWidth > 0 && options.WrappingWidth / options.DpiX < maxScaledAdvance
                ? options.WrappingWidth / options.DpiX
                : 0;

            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    originX = wrappingAdvance - textLine.ScaledAdvance();
                    break;
                case HorizontalAlignment.Center:
                    originX = (wrappingAdvance * .5F) - (textLine.ScaledAdvance() * .5F);
                    break;
            }

            location.X += originX;

            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphInfo info = textLine[i];

                // TODO: Handle embedded RTL values.
                foreach (GlyphMetrics metric in info.Metrics)
                {
                    float scale = info.PointSize / metric.ScaleFactor;
                    glyphs.Add(new GlyphLayout(
                        info.GraphemeIndex,
                        metric.CodePoint,
                        new Glyph(metric, info.PointSize),
                        location,
                        info.ScaledAdvance,
                        metric.AdvanceHeight * scale,
                        metric.FontMetrics.LineHeight * scale * options.LineSpacing,
                        i == 0));
                }

                location.X += info.ScaledAdvance;
            }

            location.X = originX + (options.Origin.X / options.DpiX);
            if (glyphs.Count > 0)
            {
                location.Y += glyphs.Max(x => x.LineHeight);
            }

            return glyphs;
        }

        private static IEnumerable<GlyphLayout> LayoutLineRightToLeftHorizontal(
            TextLine textLine,
            int lineCount,
            float maxScaledAdvance,
            RendererOptions options,
            bool first,
            ref Vector2 location)
        {
            float originY = 0;
            float originX = maxScaledAdvance;
            if (first)
            {
                // Set the Y-Origin for the first line.
                float lineScaledAscender = textLine.ScaledAscender();
                float lineScaledDescender = textLine.ScaledDescender();
                switch (options.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        originY = lineScaledAscender;
                        break;
                    case VerticalAlignment.Center:
                        originY = (lineScaledAscender * .5F) - (lineScaledDescender * .5F);
                        originY -= (lineCount - 1) * textLine.ScaledLineHeight() * options.LineSpacing * .5F;
                        break;
                    case VerticalAlignment.Bottom:
                        originY = -lineScaledDescender;
                        originY -= (lineCount - 1) * textLine.ScaledLineHeight() * options.LineSpacing;
                        break;
                }

                location.Y += originY;
            }

            // Set the X-Origin for horizontal alignment.
            float wrappingAdvance = options.WrappingWidth > 0 && options.WrappingWidth / options.DpiX < maxScaledAdvance
                ? options.WrappingWidth / options.DpiX
                : 0;

            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    //originX = wrappingAdvance - textLine.ScaledAdvance();
                    break;
                case HorizontalAlignment.Center:
                    originX = (wrappingAdvance * .5F) - (textLine.ScaledAdvance() * .5F);
                    break;
            }

            location.X += originX;

            List<GlyphLayout> glyphs = new();
            for (int i = 0; i < textLine.Count; i++)
            {
                TextLine.GlyphInfo info = textLine[i];

                // TODO: Handle embedded LTR values.
                foreach (GlyphMetrics metric in info.Metrics)
                {
                    float scale = info.PointSize / metric.ScaleFactor;
                    glyphs.Add(new GlyphLayout(
                        info.GraphemeIndex,
                        metric.CodePoint,
                        new Glyph(metric, info.PointSize),
                        location,
                        info.ScaledAdvance,
                        metric.AdvanceHeight * scale,
                        metric.FontMetrics.LineHeight * scale * options.LineSpacing,
                        i == 0));
                }

                location.X -= info.ScaledAdvance;
            }

            location.X = originX + (options.Origin.X / options.DpiX);
            if (glyphs.Count > 0)
            {
                location.Y += glyphs.Max(x => x.LineHeight);
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
                    substitutions.AddGlyph(glyphId, current, codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            // We always do this, with or without kerning so that bidi mirrored types
            // are substituted correctly.
            SubstituteBidiMirrors(fontMetrics, bidiRuns, bidiMap, substitutions);

            if (options.ApplyKerning)
            {
                AssignShapingFeatures(substitutions);
                fontMetrics.ApplySubstitution(substitutions);
            }

            return positionings.TryAddOrUpdate(fontMetrics, substitutions, options);
        }

        private static void SubstituteBidiMirrors(
            IFontMetrics fontMetrics,
            BidiRun[] bidiRuns,
            Dictionary<int, int> bidiMap,
            GlyphSubstitutionCollection substitutions)
        {
            // TODO: Vertical bidi mirrors appear to be different.
            // See hb-ot-shape.cc in HarfBuzz. Line 651.
            for (int i = 0; i < substitutions.Count; i++)
            {
                substitutions.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out int offset, out IEnumerable<int> _);
                if (bidiMap.TryGetValue(offset, out int run))
                {
                    BidiRun bidiRun = bidiRuns[run];
                    if (bidiRun.Direction != BidiCharacterType.RightToLeft)
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
        }

        private static void AssignShapingFeatures(GlyphSubstitutionCollection substitutions)
        {
            for (int i = 0; i < substitutions.Count; i++)
            {
                substitutions.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out int _, out IEnumerable<int> _);
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
                    substitutions.GetCodePointAndGlyphIds(i + 1, out codePoint, out _, out _);
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

        private static IEnumerable<TextLine> BreakLines(
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
                    if (!positionings.TryGetGlypMetricsAtOffset(codePointIndex, out GlyphMetrics[]? metrics))
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
                    if (glyphAdvance == 0)
                    {
                        // Nothing to render.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    if (CodePoint.IsTabulation(codePoint))
                    {
                        float tabStop = glyphAdvance * options.TabWidth;
                        float tabAdvance = 0;
                        if (tabStop > 0)
                        {
                            tabAdvance = tabStop - (lineAdvance % tabStop);
                        }

                        if (tabAdvance < glyphAdvance)
                        {
                            // Ensure tab advance is at least a glyph advance.
                            tabAdvance += tabStop;
                        }

                        glyphAdvance = tabAdvance;
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
                    if (graphemeCodePointIndex == 0)
                    {
                        // Mandatory wrap at index.
                        if (currentLineBreak.PositionWrap == codePointIndex && currentLineBreak.Required)
                        {
                            textLines.Add(textLine);
                            glyphCount += textLine.Count;
                            textLine = new TextLine();
                            lineAdvance = 0;
                        }
                        else if (shouldWrap && lineAdvance + glyphAdvance >= wrappingLength)
                        {
                            // Forced wordbreak
                            if (breakAll)
                            {
                                textLines.Add(textLine);
                                glyphCount += textLine.Count;
                                textLine = new TextLine();
                                lineAdvance = 0;
                            }
                            else if (lastLineBreak.PositionWrap < codePointIndex)
                            {
                                // Split the current textline into two at the last wrapping point.
                                TextLine split = textLine.SplitAt(lastLineBreak, keepAll);
                                if (split != textLine)
                                {
                                    textLines.Add(textLine);
                                    textLine = split;
                                    lineAdvance = split.ScaledAdvance();
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
                textLines.Add(textLine);
            }

            return textLines;
        }

        internal class TextLine
        {
            private readonly List<GlyphInfo> info = new();

            public int Count => this.info.Count;

            public GlyphInfo this[int index] => this.info[index];

            public TextDirection Direction() => (TextDirection)this.info[0].BidiRun.Direction;

            public float ScaledAdvance()
                => this.info.Sum(x => x.ScaledAdvance);

            public float ScaledAscender()
                => this.info.Max(x => x.Metrics[0].FontMetrics.Ascender * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledDescender()
                => this.info.Max(x => Math.Abs(x.Metrics[0].FontMetrics.Descender) * x.PointSize / x.Metrics[0].ScaleFactor);

            public float ScaledLineHeight()
                => this.info.Max(x => Math.Abs(x.Metrics[0].FontMetrics.LineHeight) * x.PointSize / x.Metrics[0].ScaleFactor);

            public void Add(
                GlyphMetrics[] metrics,
                float pointSize,
                float advance,
                BidiRun bidiRun,
                int graphemeIndex,
                int offset)
                => this.info.Add(
                    new GlyphInfo(
                        metrics,
                        pointSize,
                        advance,
                        bidiRun,
                        graphemeIndex,
                        offset));

            public TextLine SplitAt(LineBreak lineBreak, bool keepAll)
            {
                int index = this.info.Count;
                GlyphInfo? glyphWrap = null;
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
                if (keepAll && UnicodeUtility.IsCJKCodePoint((uint)glyphWrap!.CodePoint.Value))
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
                index = this.info.Count - 1;
                while (index > 0)
                {
                    if (!CodePoint.IsWhiteSpace(this.info[index].CodePoint))
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

            [DebuggerDisplay("{DebuggerDisplay,nq}")]
            internal class GlyphInfo
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

                private string DebuggerDisplay => FormattableString
                    .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {this.TextDirection} : {this.Offset}");
            }
        }
    }
}

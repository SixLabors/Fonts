// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    internal class TextLayout2
    {
        internal static TextLayout2 Default { get; set; } = new TextLayout2();

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

            IEnumerable<TextLine> textLines = ProcessText(text, options);

            // TODO: Actually layout the glyphs.
            return Array.Empty<GlyphLayout>();
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
                    if (bidiRun.Direction == BidiCharacterType.RightToLeft)
                    {
                        if (CodePoint.TryGetBidiMirror(codePoint, out CodePoint mirror))
                        {
                            if (fontMetrics.TryGetGlyphId(mirror, out int glyphId))
                            {
                                substitutions.Replace(i, glyphId);
                            }
                        }
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

            int lastPositionWrap = CodePoint.GetCodePointCount(text);
            int nextPositionWrap = lastPositionWrap;
            bool nextWrapRequired = false;

            // Calculate the initial position of potential line breaks.
            var lineBreakEnumerator = new LineBreakEnumerator(text);
            if (shouldWrap && lineBreakEnumerator.MoveNext())
            {
                LineBreak b = lineBreakEnumerator.Current;
                lastPositionWrap = b.PositionWrap;
                nextPositionWrap = b.PositionWrap;
                nextWrapRequired = b.Required;
            }

            int graphemeIndex;
            int codePointIndex = 0;
            float lineAdvance = 0;
            List<TextLine> textLines = new();
            TextLine textLine = new();

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

                    // Do not start a line with whitespace.
                    if (textLine.Count == 0)
                    {
                        // Do not start a line with whitespace.
                        if (CodePoint.IsWhiteSpace(codePoint))
                        {
                            codePointIndex++;
                            graphemeCodePointIndex++;
                            continue;
                        }
                    }

                    // Calculate the advance for the current codepoint.
                    GlyphMetrics glyph = metrics[0];
                    float glyphAdvance = isHorizontal ? glyph.AdvanceWidth : glyph.AdvanceHeight;
                    if (glyphAdvance > 0 && !CodePoint.IsNewLine(codePoint) && !CodePoint.IsWhiteSpace(codePoint))
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
                    else if (codePoint.Value == '\t')
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
                    else if (codePoint.Value == '\r')
                    {
                        // Carriage Return resets the coordinates to 0
                        lineAdvance = 0;
                        glyphAdvance = 0;
                    }

                    glyphAdvance *= pointSize / glyph.ScaleFactor;

                    // Should we start a new line?
                    if (shouldWrap && graphemeCodePointIndex == 0)
                    {
                        // Mandatory wrap at index.
                        if (nextPositionWrap == codePointIndex && nextWrapRequired)
                        {
                            textLines.Add(textLine);
                            textLine = new TextLine();
                            lineAdvance = 0;
                        }
                        else if (lineAdvance + glyphAdvance >= wrappingLength)
                        {
                            // Forced wordbreak
                            if (breakAll)
                            {
                                textLines.Add(textLine);
                                textLine = new TextLine();
                                lineAdvance = 0;
                            }
                            else if (lastPositionWrap < codePointIndex)
                            {
                                if (!(keepAll && UnicodeUtility.IsCJKCodePoint((uint)codePoint.Value)))
                                {
                                    // Split the current textline into two at the last wrapping point.
                                    TextLine split = textLine.SplitAt(lastPositionWrap);
                                    textLines.Add(textLine);
                                    textLine = split;
                                    lineAdvance = 0;
                                }
                            }
                        }
                    }

                    // Find the next line break.
                    if (shouldWrap && nextPositionWrap == codePointIndex && lineBreakEnumerator.MoveNext())
                    {
                        LineBreak b = lineBreakEnumerator.Current;
                        lastPositionWrap = nextPositionWrap;
                        nextPositionWrap = b.PositionWrap;
                        nextWrapRequired = b.Required;
                    }

                    // Do not start a line with whitespace.
                    if (textLine.Count == 0 && CodePoint.IsWhiteSpace(codePoint))
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    // Add our metrics to the line.
                    lineAdvance += glyphAdvance;
                    textLine.Add(metrics, bidiRuns[bidiMap[codePointIndex]], codePointIndex);
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
            private readonly List<GlyphInfo> glyphs = new();

            public int Count => this.glyphs.Count;

            public GlyphInfo this[int index] => this.glyphs[index];

            public TextDirection Direction() => (TextDirection)this.glyphs[0].BidiRun.Direction;

            public void Add(GlyphMetrics[] metrics, BidiRun bidiRun, int offset)
                => this.glyphs.Add(new GlyphInfo(metrics, bidiRun, offset));

            public TextLine SplitAt(int offset)
            {
                int index = this.glyphs.IndexOf(this.glyphs.Find(x => x.Offset == offset)!);
                TextLine result = new();
                result.glyphs.AddRange(this.glyphs.GetRange(index, this.glyphs.Count - index));
                this.glyphs.RemoveRange(index, this.glyphs.Count - index);
                return result;
            }

            [DebuggerDisplay("{DebuggerDisplay,nq}")]
            internal class GlyphInfo
            {
                public GlyphInfo(GlyphMetrics[] metrics, BidiRun bidiRun, int offset)
                {
                    this.Metrics = metrics;
                    this.BidiRun = bidiRun;
                    this.Offset = offset;
                }

                public GlyphMetrics[] Metrics { get; }

                public BidiRun BidiRun { get; }

                public int Offset { get; }

                private string DebuggerDisplay => FormattableString
                    .Invariant($"{this.Metrics[0].CodePoint.ToDebuggerDisplay()} : {this.BidiRun.Direction} : {this.Offset}");
            }
        }
    }
}

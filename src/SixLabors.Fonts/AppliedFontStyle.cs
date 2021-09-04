// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    internal struct AppliedFontStyle
    {
        private Dictionary<int, GlyphMetrics[]> glyphMetricsMap;
        public IFontMetrics[] FallbackFonts;
        public IFontMetrics MainFont;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;
        public ColorFontSupport ColorFontSupport;

        public void ProcessText(ReadOnlySpan<char> text)
        {
            var collection = new GlyphSubstitutionCollection();
            this.glyphMetricsMap = new Dictionary<int, GlyphMetrics[]>();

            // TODO: It would be better if we could slice the text and only
            // parse the codepoints within the start-end positions.
            // However we actually have to refactor TextLayout to actually
            // create slices.
            this.DoFontRun(text, this.MainFont, collection, this.glyphMetricsMap);

            foreach (IFontMetrics font in this.FallbackFonts)
            {
                collection.Clear();
                this.DoFontRun(text, font, collection, this.glyphMetricsMap);
            }
        }

        private void DoFontRun(
            ReadOnlySpan<char> text,
            IFontMetrics fontMetrics,
            IGlyphSubstitutionCollection collection,
            Dictionary<int, GlyphMetrics[]> glyphMetricsMap)
        {
            // Enumerate through each grapheme in the text.
            int graphemeIndex;
            int codePointIndex = 0;
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
                    if (skipNextCodePoint)
                    {
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    int charsConsumed = 0;
                    CodePoint current = codePointEnumerator.Current;
                    charIndex += current.Utf16SequenceLength;
                    CodePoint? next = graphemeCodePointIndex < graphemeMax
                        ? CodePoint.DecodeFromUtf16At(graphemeEnumerator.Current, charIndex, out charsConsumed)
                        : null;

                    charIndex += charsConsumed;

                    // Get the glyph id for the codepoint and add to the collection.
                    fontMetrics.TryGetGlyphId(current, next, out int glyphId, out skipNextCodePoint);
                    collection.AddGlyph(glyphId, current, codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            if (this.ApplyKerning)
            {
                // TODO: GPOS?
                fontMetrics.ApplySubstitions(collection);
            }

            // Now loop through and assign metrics to our map.
            for (int idx = 0; idx < codePointIndex; idx++)
            {
                // No glyphs, this codepoint has been skipped.
                if (!collection.TryGetCodePointAndGlyphIdsAtOffset(idx, out CodePoint? codePoint, out IEnumerable<int>? glyphIds))
                {
                    continue;
                }

                // Never allow fallback fonts to replace previously matched glyphs.
                if (glyphMetricsMap.TryGetValue(idx, out GlyphMetrics[]? metrics)
                    && metrics[0].GlyphType != GlyphType.Fallback)
                {
                    continue;
                }

                var m = new List<GlyphMetrics>();
                foreach (int id in glyphIds)
                {
                    m.AddRange(fontMetrics.GetGlyphMetrics(codePoint.Value, id, this.ColorFontSupport));
                }

                glyphMetricsMap[idx] = m.ToArray();
            }
        }

        public bool TryGetGlypMetrics(int offset, out GlyphMetrics[] metrics)
            => this.glyphMetricsMap.TryGetValue(offset - this.Start, out metrics);

        public GlyphMetrics[] GetGlyphLayers(CodePoint codePoint)
        {
            GlyphMetrics glyph = this.MainFont.GetGlyphMetrics(codePoint);
            if (glyph.GlyphType == GlyphType.Fallback)
            {
                foreach (IFontMetrics? f in this.FallbackFonts)
                {
                    GlyphMetrics g = f.GetGlyphMetrics(codePoint);
                    if (g.GlyphType != GlyphType.Fallback)
                    {
                        glyph = g;
                        break;
                    }
                }
            }

            // TODO: This looks never null.
            if (glyph == null)
            {
                return Array.Empty<GlyphMetrics>();
            }

            if (this.ColorFontSupport == ColorFontSupport.MicrosoftColrFormat)
            {
                if (glyph.FontMetrics.TryGetColoredVectors(codePoint, glyph.Index, out GlyphMetrics[]? layers))
                {
                    return layers;
                }
            }

            return new[] { glyph };
        }

        public Vector2 GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
        {
            if (glyph.FontMetrics != previousGlyph?.FontMetrics)
            {
                return Vector2.Zero;
            }

            return ((IFontMetrics)glyph.FontMetrics).GetOffset(glyph, previousGlyph);
        }
    }
}

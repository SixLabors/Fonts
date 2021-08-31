// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    internal struct AppliedFontStyle
    {
        public IFontMetrics[] FallbackFonts;
        public IFontMetrics MainFont;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;

        public void GatherGlyphIds(ReadOnlySpan<char> text)
        {
            IGlyphSubstitutionCollection collection = new GlyphSubstitutionCollection();

            // Enumerate through each grapheme in the text.
            int graphemeIndex;
            int codePointIndex = 0;
            var graphemeEnumerator = new SpanGraphemeEnumerator(text.Slice(this.Start, this.End - this.Start));
            for (graphemeIndex = 0; graphemeEnumerator.MoveNext(); graphemeIndex++)
            {
                int graphemeMax = graphemeEnumerator.Current.Length - 1;
                int graphemCodePointIndex = 0;
                int charIndex = 0;

                // Now enumerate through each codepoint in the grapheme.
                bool skipNextCodePoint = false;
                var codePointEnumerator = new SpanCodePointEnumerator(graphemeEnumerator.Current);
                while (codePointEnumerator.MoveNext())
                {
                    if (skipNextCodePoint)
                    {
                        continue;
                    }

                    int charsConsumed = 0;
                    CodePoint current = codePointEnumerator.Current;
                    charIndex += current.Utf16SequenceLength;
                    CodePoint? next = graphemCodePointIndex < graphemeMax
                        ? CodePoint.DecodeFromUtf16At(graphemeEnumerator.Current, charIndex, out charsConsumed)
                        : null;

                    charIndex += charsConsumed;

                    // Get the glyph index for the collection and add to the collection.
                    if (!this.MainFont.TryGetGlyphId(current, next, out ushort glyphId, out skipNextCodePoint))
                    {
                        foreach (IFontMetrics? f in this.FallbackFonts)
                        {
                            if (f.TryGetGlyphId(current, next, out glyphId, out skipNextCodePoint))
                            {
                                break;
                            }
                        }
                    }

                    collection.AddGlyph(glyphId, current, codePointIndex);

                    codePointIndex++;
                    graphemCodePointIndex++;
                }
            }

            this.MainFont.ApplySubstition(collection);
            foreach (IFontMetrics? f in this.FallbackFonts)
            {
                f.ApplySubstition(collection);
            }

            // TODO:
            // 2: Create a map so we can iterate through the codepoints again matching the correct codepoint.
        }

        public GlyphMetrics[] GetGlyphLayers(CodePoint codePoint, ColorFontSupport colorFontOptions)
        {
            GlyphMetrics glyph = this.MainFont.GetGlyphMetrics(codePoint);
            if (glyph.GlyphType == GlyphType.Fallback)
            {
                foreach (IFontMetrics? f in this.FallbackFonts)
                {
                    GlyphMetrics? g = f.GetGlyphMetrics(codePoint);
                    if (g.GlyphType != GlyphType.Fallback)
                    {
                        glyph = g;
                        break;
                    }
                }
            }

            if (glyph == null)
            {
                return Array.Empty<GlyphMetrics>();
            }

            if (colorFontOptions == ColorFontSupport.MicrosoftColrFormat)
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

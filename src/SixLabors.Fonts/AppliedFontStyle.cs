// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    internal struct AppliedFontStyle
    {
        private GlyphPositioningCollection positioningCollection;

        // TODO: Clean all this assignment up.
        public RendererOptions Options;
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
            this.positioningCollection = new();

            // TODO: It would be better if we could slice the text and only
            // parse the codepoints within the start-end positions.
            // However we actually have to refactor TextLayout to actually
            // create slices.
            this.DoFontRun(text, this.MainFont, collection, this.positioningCollection);

            foreach (IFontMetrics font in this.FallbackFonts)
            {
                collection.Clear();
                this.DoFontRun(text, font, collection, this.positioningCollection);
            }
        }

        private void DoFontRun(
            ReadOnlySpan<char> text,
            IFontMetrics fontMetrics,
            GlyphSubstitutionCollection substitutionCollection,
            GlyphPositioningCollection positioningCollection)
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
                    substitutionCollection.AddGlyph(glyphId, current, codePointIndex);

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            if (this.ApplyKerning)
            {
                // TODO: GPOS
                fontMetrics.ApplySubstitions(substitutionCollection);
            }

            positioningCollection.AddOrUpdate(fontMetrics, substitutionCollection, this.Options);
        }

        public bool TryGetGlypMetrics(int offset, [NotNullWhen(true)] out GlyphMetrics[]? metrics)
            => this.positioningCollection.TryGetGlypMetricsAtOffset(offset - this.Start, out metrics);

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

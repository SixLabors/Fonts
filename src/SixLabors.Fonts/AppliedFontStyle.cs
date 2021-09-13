// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
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
            // TODO: It would be better if we could slice the text and only
            // parse the codepoints within the start-end positions.
            // However we actually have to refactor TextLayout to actually
            // create slices.
            var collection = new GlyphSubstitutionCollection();
            this.positioningCollection = new();

            // Incrementally build out collection of glyphs.
            // For each run we start with a fresh substitution collection to avoid
            // overwriting the glyph ids.
            this.DoFontRun(text, this.MainFont, collection, this.positioningCollection);
            foreach (IFontMetrics font in this.FallbackFonts)
            {
                collection.Clear();
                this.DoFontRun(text, font, collection, this.positioningCollection);
            }

            if (this.ApplyKerning)
            {
                // Update the positions of the glyphs in the completed collection.
                // Each set of metrics is associated with single font and will only be updated
                // by that font so it's safe to use a single collection.
                this.MainFont.UpdatePositions(this.positioningCollection);
                foreach (IFontMetrics font in this.FallbackFonts)
                {
                    font.UpdatePositions(this.positioningCollection);
                }
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
                AssignShapingFeatures(substitutionCollection);
                fontMetrics.ApplySubstitions(substitutionCollection);
            }

            positioningCollection.AddOrUpdate(fontMetrics, substitutionCollection, this.Options);
        }

        public bool TryGetGlyphMetrics(int offset, [NotNullWhen(true)] out GlyphMetrics[]? metrics)
            => this.positioningCollection.TryGetGlypMetricsAtOffset(offset - this.Start, out metrics);

        // TODO: Remove this and update tests.
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

        // TODO: Remove this and call from UpdatePositions when no GPos table exists.
        public Vector2 GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
        {
            if (glyph.FontMetrics != previousGlyph?.FontMetrics)
            {
                return Vector2.Zero;
            }

            return ((IFontMetrics)glyph.FontMetrics).GetOffset(glyph, previousGlyph);
        }

        private static void AssignShapingFeatures(GlyphSubstitutionCollection collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out int _, out IEnumerable<int> _);
                Script script = CodePoint.GetScript(codePoint);

                // Choose a shaper based on the script.
                // This determines which features to apply to which glyphs.
                BaseShaper shaper = ShaperFactory.Create(script);
                int index = i;
                int count = 1;
                while (i < collection.Count - 1)
                {
                    // We want to assign the same shaper to individual sections of the text rather
                    // than the text as a whole to ensure that different language shapers do not interfere
                    // with each other when the text contains multiple languages.
                    collection.GetCodePointAndGlyphIds(i + 1, out codePoint, out _, out _);
                    if (CodePoint.GetScript(codePoint) != script)
                    {
                        break;
                    }

                    i++;
                    count++;
                }

                // Assign Substitution features to each glyph.
                shaper.AssignFeatures(collection, index, count);
            }
        }
    }
}

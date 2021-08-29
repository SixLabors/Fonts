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

        // TODO: This should accept all the glyphs in the range to
        // allow complex substitutions.
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

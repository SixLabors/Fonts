// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    internal struct AppliedFontStyle
    {
        public IFontInstance[] FallbackFonts;
        public IFontInstance MainFont;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;

        public GlyphMetrics[] GetGlyphLayers(CodePoint codePoint, ColorFontSupport colorFontOptions)
        {
            GlyphMetrics glyph = this.MainFont.GetGlyph(codePoint);
            if (glyph.GlyphType == GlyphType.Fallback)
            {
                foreach (IFontInstance? f in this.FallbackFonts)
                {
                    GlyphMetrics? g = f.GetGlyph(codePoint);
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
                if (glyph.Font.TryGetColoredVectors(codePoint, glyph.Index, out GlyphMetrics[]? layers))
                {
                    return layers;
                }
            }

            return new[] { glyph };
        }

        public Vector2 GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
        {
            if (glyph.Font != previousGlyph?.Font)
            {
                return Vector2.Zero;
            }

            return ((IFontInstance)glyph.Font).GetOffset(glyph, previousGlyph);
        }
    }
}

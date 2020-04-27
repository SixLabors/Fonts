// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;

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

        public GlyphInstance[] GetGlyphLayers(int codePoint, ColorFontSupport colorFontOptions)
        {
            GlyphInstance glyph = this.MainFont.GetGlyph(codePoint);
            if (glyph.GlyphType == GlyphType.Fallback)
            {
                foreach (var f in this.FallbackFonts)
                {
                    var g = f.GetGlyph(codePoint);
                    if (g.GlyphType != GlyphType.Fallback)
                    {
                        glyph = g;
                        break;
                    }
                }
            }

            if (glyph == null)
            {
                return Array.Empty<GlyphInstance>();
            }

            if (colorFontOptions == ColorFontSupport.MicrosoftColrFormat)
            {
                if (glyph.Font.TryGetColoredVectors(glyph.Index, out var layers))
                {
                    return layers;
                }
            }

            return new[] { glyph };
        }

        public Vector2 GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph)
        {
            if (glyph.Font != previousGlyph?.Font)
            {
                return Vector2.Zero;
            }

            return ((IFontInstance)glyph.Font).GetOffset(glyph, previousGlyph);
        }
    }
}

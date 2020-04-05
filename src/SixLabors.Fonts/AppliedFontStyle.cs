// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Numerics;

namespace SixLabors.Fonts
{
    internal struct AppliedFontStyle
    {
        public IEnumerable<IFontInstance> FallbackFonts;
        public IFontInstance MainFont;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;

        public GlyphInstance GetGlyph(int codePoint)
        {
            GlyphInstance glyph = this.MainFont.GetGlyph(codePoint);
            if (glyph.Fallback)
            {
                foreach (var f in this.FallbackFonts)
                {
                    var g = f.GetGlyph(codePoint);
                    if (!g.Fallback)
                    {
                        return g;
                    }
                }
            }

            return glyph;
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

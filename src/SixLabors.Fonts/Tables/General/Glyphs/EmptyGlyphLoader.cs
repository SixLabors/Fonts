using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal class EmptyGlyphLoader : GlyphLoader
    {
        public EmptyGlyphLoader(Bounds fallbackEmptyBounds)
        {
            this.fallbackEmptyBounds = fallbackEmptyBounds;
        }

        private bool loop = false;
        private readonly Bounds fallbackEmptyBounds;
        private GlyphVector? glyph;

        public override Glyphs.GlyphVector CreateGlyph(GlyphTable table)
        {
            if (this.loop)
            {
                if(this.glyph == null)
                {
                    this.glyph = new GlyphVector(new Vector2[0], new bool[0], new ushort[0], this.fallbackEmptyBounds);
                }
                return this.glyph.Value;
            }
            this.loop = true;
            return table.GetGlyph(0);
        }
    }
}

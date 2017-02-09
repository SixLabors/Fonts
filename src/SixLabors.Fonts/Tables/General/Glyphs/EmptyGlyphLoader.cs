using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal class EmptyGlyphLoader : GlyphLoader
    {
        public EmptyGlyphLoader()
        {
        }

        private byte counter = 0;

        public override Glyphs.GlyphVector CreateGlyph(GlyphTable table)
        {
            this.counter++;
            if (this.counter > 100)
            {
                throw new Fonts.Exceptions.FontException("loop detected loading glyphs");
            }

            return table.GetGlyph(0);
        }
    }
}

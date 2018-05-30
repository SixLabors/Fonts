using System.Collections.Generic;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeGlyphTable : GlyphTable
    {
        private readonly List<FakeGlyphSource> glyphs;

        public FakeGlyphTable(List<FakeGlyphSource> glyphs)
            : base(new GlyphLoader[glyphs.Count])
        {
            this.glyphs = glyphs;
        }

        internal override GlyphVector GetGlyph(int index)
        {
            foreach (FakeGlyphSource c in this.glyphs)
            {
                if (c.Index == index)
                {
                    return c.Vector;
                }
            }

            return default(GlyphVector);
        }
    }
}

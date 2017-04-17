using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.Tables.General.CMap;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeCmapSubtable : CMapSubTable
    {
        private readonly List<FakeGlyphSource> glyphs;

        public FakeCmapSubtable(List<FakeGlyphSource> glyphs)
        {
            this.glyphs = glyphs;
        }
        public override ushort GetGlyphId(char character)
        {
            foreach (FakeGlyphSource c in this.glyphs)
            {
                if (c.Character == character)
                {
                    return c.Index;
                }
            }
            return 0;
        }
    }
}

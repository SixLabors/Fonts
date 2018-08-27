using System.Collections.Generic;
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

        public override ushort GetGlyphId(int codePoint)
        {
            foreach (FakeGlyphSource c in this.glyphs)
            {
                if (c.CodePoint == codePoint)
                {
                    return c.Index;
                }
            }
            return 0;
        }
    }
}

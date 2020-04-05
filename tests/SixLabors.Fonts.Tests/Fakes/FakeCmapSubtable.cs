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

        public override bool TryGetGlyphId(int codePoint, out ushort glyphId)
        {
            foreach (FakeGlyphSource c in this.glyphs)
            {
                if (c.CodePoint == codePoint)
                {
                    glyphId = c.Index;
                    return true;
                }
            }
            glyphId = 0;
            return false;
        }
    }
}

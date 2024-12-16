// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Fakes;

internal class FakeCmapSubtable : CMapSubTable
{
    private readonly List<FakeGlyphSource> glyphs;

    public FakeCmapSubtable(List<FakeGlyphSource> glyphs)
        => this.glyphs = glyphs;

    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
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

    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        foreach (FakeGlyphSource c in this.glyphs)
        {
            if (c.Index == glyphId)
            {
                codePoint = c.CodePoint;
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    public override IEnumerable<int> GetAvailableCodePoints()
        => this.glyphs.Select(x => x.CodePoint.Value);
}

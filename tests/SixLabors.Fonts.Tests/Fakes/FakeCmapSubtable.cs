// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Fakes
{
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
    }
}

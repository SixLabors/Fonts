// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal readonly struct GlyphNameMap
{
    public readonly ushort GlyphIndex;

    public readonly string GlyphName;

    public GlyphNameMap(ushort glyphIndex, string glyphName)
    {
        this.GlyphIndex = glyphIndex;
        this.GlyphName = glyphName;
    }
}

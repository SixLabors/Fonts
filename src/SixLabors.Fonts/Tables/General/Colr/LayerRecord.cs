// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct LayerRecord
{
    public LayerRecord(ushort glyphId, ushort paletteIndex)
    {
        this.GlyphId = glyphId;
        this.PaletteIndex = paletteIndex;
    }

    public ushort GlyphId { get; }

    public ushort PaletteIndex { get; }
}

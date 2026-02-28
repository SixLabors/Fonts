// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct BaseGlyphPaintRecord
{
    public BaseGlyphPaintRecord(ushort glyphId, uint paintOffset)
    {
        this.GlyphId = glyphId;
        this.PaintOffset = paintOffset;
    }

    public ushort GlyphId { get; }

    public uint PaintOffset { get; }
}

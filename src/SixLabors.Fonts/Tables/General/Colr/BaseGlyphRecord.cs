// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct BaseGlyphRecord
{
    public BaseGlyphRecord(ushort glyphId, ushort firstLayerIndex, ushort layerCount)
    {
        this.GlyphId = glyphId;
        this.FirstLayerIndex = firstLayerIndex;
        this.LayerCount = layerCount;
    }

    public ushort GlyphId { get; }

    public ushort FirstLayerIndex { get; }

    public ushort LayerCount { get; }
}

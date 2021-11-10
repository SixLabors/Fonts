// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General.Colr
{
    internal sealed class BaseGlyphRecord
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

    internal sealed class LayerRecord
    {
        public LayerRecord(ushort glyphId, ushort paletteIndex)
        {
            this.GlyphId = glyphId;
            this.PaletteIndex = paletteIndex;
        }

        public ushort GlyphId { get; }

        public ushort PaletteIndex { get; }
    }
}

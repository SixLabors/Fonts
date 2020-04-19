// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

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
        public LayerRecord(ushort glyphId, ushort palletteIndex)
        {
            this.GlyphId = glyphId;
            this.PalletteIndex = palletteIndex;
        }

        public ushort GlyphId { get; }

        public ushort PalletteIndex { get; }
    }
}

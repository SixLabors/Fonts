// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Cff2Font : CffFont
    {
        public Cff2Font(string name, CffTopDictionary metrics, CffGlyphData[] glyphs, ItemVariationStore itemVariationStore)
            : base(name, metrics, glyphs) => this.ItemVariationStore = itemVariationStore;

        public ItemVariationStore? ItemVariationStore { get; set; }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a parsed CFF2 font with an associated Item Variation Store for font variations.
/// </summary>
internal class Cff2Font : CffFont
{
    public Cff2Font(string name, CffTopDictionary metrics, CffGlyphData[] glyphs, ItemVariationStore itemVariationStore)
        : base(name, metrics, glyphs) => this.ItemVariationStore = itemVariationStore;

    /// <summary>
    /// Gets or sets the Item Variation Store used for CFF2 blend interpolation.
    /// </summary>
    public ItemVariationStore ItemVariationStore { get; set; }
}

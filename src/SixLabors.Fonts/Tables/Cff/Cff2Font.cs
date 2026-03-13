// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a parsed CFF2 font with an associated Item Variation Store for font variations.
/// </summary>
internal class Cff2Font : CffFont
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cff2Font"/> class.
    /// </summary>
    /// <param name="name">The PostScript font name.</param>
    /// <param name="metrics">The Top DICT data.</param>
    /// <param name="glyphs">The parsed glyph data array.</param>
    /// <param name="itemVariationStore">The item variation store for blend interpolation.</param>
    public Cff2Font(string name, CffTopDictionary metrics, CffGlyphData[] glyphs, ItemVariationStore itemVariationStore)
        : base(name, metrics, glyphs) => this.ItemVariationStore = itemVariationStore;

    /// <summary>
    /// Gets or sets the Item Variation Store used for CFF2 blend interpolation.
    /// </summary>
    public ItemVariationStore ItemVariationStore { get; set; }
}

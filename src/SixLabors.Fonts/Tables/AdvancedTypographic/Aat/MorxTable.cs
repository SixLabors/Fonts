// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Represents the extended glyph metamorphosis table.
/// </summary>
internal sealed class MorxTable : MorphTable
{
    /// <summary>
    /// The SFNT table tag.
    /// </summary>
    internal const string TableName = "morx";

    /// <summary>
    /// Initializes a new instance of the <see cref="MorxTable"/> class.
    /// </summary>
    /// <param name="chains">The feature chains in application order.</param>
    /// <param name="features">The feature types and language tags in the font.</param>
    private MorxTable(Chain[] chains, AatFeatures features)
        : base(chains, features)
    {
    }

    /// <summary>
    /// Loads the 'morx' table from the font.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The morx table, or <see langword="null"/> if it cannot be read.</returns>
    public static MorxTable? Load(FontReader reader)
    {
        Chain[]? chains = LoadChains(reader, TableName, true);
        return chains is null ? null : new(chains, AatFeatures.Load(reader));
    }
}

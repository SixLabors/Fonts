// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Represents the classic glyph metamorphosis table.
/// </summary>
internal sealed class MortTable : MorphTable
{
    /// <summary>
    /// The SFNT table tag.
    /// </summary>
    internal const string TableName = "mort";

    /// <summary>
    /// Initializes a new instance of the <see cref="MortTable"/> class.
    /// </summary>
    /// <param name="chains">The feature chains in application order.</param>
    /// <param name="features">The feature types and language tags in the font.</param>
    private MortTable(Chain[] chains, AatFeatures features)
        : base(chains, features)
    {
    }

    /// <summary>
    /// Loads the 'mort' table from the font.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The mort table, or <see langword="null"/> if it cannot be read.</returns>
    public static MortTable? Load(FontReader reader)
    {
        Chain[]? chains = LoadChains(reader, TableName, false);
        return chains is null ? null : new(chains, AatFeatures.Load(reader));
    }
}

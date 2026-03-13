// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// A placeholder lookup subtable used when the lookup type is not implemented.
/// This subtable always returns <see langword="false"/> for substitution attempts.
/// </summary>
internal class NotImplementedSubTable : LookupSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotImplementedSubTable"/> class.
    /// </summary>
    public NotImplementedSubTable()
        : base(default, 0)
    {
    }

    /// <inheritdoc />
    public override bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        GlyphSubstitutionCollection collection,
        Tag feature,
        int index,
        int count)
        => false;
}

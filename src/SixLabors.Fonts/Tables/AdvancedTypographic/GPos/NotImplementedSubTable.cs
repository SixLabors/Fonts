// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// A placeholder subtable used for unimplemented or unrecognized GPOS lookup types.
/// Always returns <see langword="false"/> from <see cref="TryUpdatePosition"/>.
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

    /// <inheritdoc/>
    public override bool TryUpdatePosition(
        FontMetrics fontMetrics,
        GPosTable table,
        GlyphPositioningCollection collection,
        Tag feature,
        int index,
        int count)
        => false;
}

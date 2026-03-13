// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// An AttachPoint table contains an array of contour point indices for the attachment points on a single glyph.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachpoint-table"/>
/// </summary>
internal struct AttachPoint
{
    /// <summary>
    /// The array of contour point indices for this glyph's attachment points.
    /// </summary>
    public ushort[] PointIndices;
}

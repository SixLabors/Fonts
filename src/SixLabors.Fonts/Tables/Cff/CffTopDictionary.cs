// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents the Top DICT data from a CFF or CFF2 font, containing font-wide
/// metadata such as name strings, bounding box, underline metrics, and the FontMatrix.
/// </summary>
internal class CffTopDictionary
{
    public CffTopDictionary() => this.CidFontInfo = new();

    /// <summary>
    /// Gets or sets the font version string (SID).
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the font notice/trademark string (SID).
    /// </summary>
    public string? Notice { get; set; }

    /// <summary>
    /// Gets or sets the font copyright string (SID).
    /// </summary>
    public string? CopyRight { get; set; }

    /// <summary>
    /// Gets or sets the font full name string (SID).
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the font family name string (SID).
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Gets or sets the font weight string (SID), e.g. "Bold".
    /// </summary>
    public string? Weight { get; set; }

    /// <summary>
    /// Gets or sets the underline position in design units.
    /// </summary>
    public double UnderlinePosition { get; set; }

    /// <summary>
    /// Gets or sets the underline thickness in design units.
    /// </summary>
    public double UnderlineThickness { get; set; }

    /// <summary>
    /// Gets or sets the font bounding box [xMin, yMin, xMax, yMax] in design units.
    /// </summary>
    public double[] FontBBox { get; set; } = [];

    /// <summary>
    /// Gets or sets the font matrix that transforms charstring coordinates to user space.
    /// Default is [0.001, 0, 0, 0.001, 0, 0] which maps 1000 charstring units to 1 user-space unit.
    /// </summary>
    public double[] FontMatrix { get; set; } = [0.001, 0, 0, 0.001, 0, 0];

    /// <summary>
    /// Gets or sets the CIDFont-specific information (ROS, FDSelect, FDArray).
    /// </summary>
    public CidFontInfo CidFontInfo { get; set; }
}

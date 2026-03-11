// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Contains CIDFont-specific information from the Top DICT of a CFF CIDFont.
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf"/>
/// </summary>
internal class CidFontInfo
{
    /// <summary>
    /// Gets or sets the CIDFont Registry string from the ROS operator.
    /// </summary>
    public string? ROS_Register { get; set; }

    /// <summary>
    /// Gets or sets the CIDFont Ordering string from the ROS operator.
    /// </summary>
    public string? ROS_Ordering { get; set; }

    /// <summary>
    /// Gets or sets the CIDFont Supplement value from the ROS operator.
    /// </summary>
    public string? ROS_Supplement { get; set; }

    /// <summary>
    /// Gets or sets the CIDFont version number.
    /// </summary>
    public double CIDFontVersion { get; set; }

    /// <summary>
    /// Gets or sets the number of CIDs in the font (CIDCount operator).
    /// </summary>
    public int CIDFountCount { get; set; }

    /// <summary>
    /// Gets or sets the offset to the FDSelect structure that maps glyphs to Font DICTs.
    /// </summary>
    public int FDSelect { get; set; }

    /// <summary>
    /// Gets or sets the offset to the Font DICT (FDArray) INDEX.
    /// </summary>
    public int FDArray { get; set; }

    /// <summary>
    /// Gets or sets the FDSelect format (0, 3, or 4).
    /// </summary>
    public int FdSelectFormat { get; set; }

    /// <summary>
    /// Gets or sets the parsed FDSelect ranges for format 3/4.
    /// </summary>
    public FDRange[] FdRanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the FDSelect map for format 0, mapping glyph index to Font DICT index.
    /// </summary>
    public Dictionary<int, byte> FdSelectMap { get; set; } = [];
}

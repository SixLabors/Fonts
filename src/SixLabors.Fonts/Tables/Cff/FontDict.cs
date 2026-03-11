// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a Font DICT entry from the FDArray in a CIDFont.
/// Each Font DICT contains a reference to its own Private DICT and local subroutines.
/// </summary>
internal class FontDict
{
    public FontDict(int name, int dictSize, int dictOffset)
    {
        this.FontName = name;
        this.PrivateDicSize = dictSize;
        this.PrivateDicOffset = dictOffset;
    }

    /// <summary>
    /// Gets or sets the Font DICT name SID.
    /// </summary>
    public int FontName { get; set; }

    /// <summary>
    /// Gets the size in bytes of the associated Private DICT.
    /// </summary>
    public int PrivateDicSize { get; }

    /// <summary>
    /// Gets the offset to the associated Private DICT.
    /// </summary>
    public int PrivateDicOffset { get; }

    /// <summary>
    /// Gets or sets the local subroutine buffers from this Font DICT's Private DICT.
    /// </summary>
    public byte[][]? LocalSubr { get; set; }

    /// <summary>
    /// Gets or sets the variation store index (CFF2 vsindex operator) for this Font DICT.
    /// </summary>
    public int VsIndex { get; set; }
}

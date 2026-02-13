// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff;

internal class CidFontInfo
{
    public string? ROS_Register { get; set; }

    public string? ROS_Ordering { get; set; }

    public string? ROS_Supplement { get; set; }

    public double CIDFontVersion { get; set; }

    public int CIDFountCount { get; set; }

    public int FDSelect { get; set; }

    public int FDArray { get; set; }

    public int FdSelectFormat { get; set; }

    public FDRange[] FdRanges { get; set; } = Array.Empty<FDRange>();

    /// <summary>
    /// Gets or sets the fd select map, which maps glyph # to font #.
    /// </summary>
    public Dictionary<int, byte> FdSelectMap { get; set; } = new();
}

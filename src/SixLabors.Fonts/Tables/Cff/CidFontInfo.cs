// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
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

        public FDRange3[] FdRanges { get; set; } = Array.Empty<FDRange3>();
    }
}

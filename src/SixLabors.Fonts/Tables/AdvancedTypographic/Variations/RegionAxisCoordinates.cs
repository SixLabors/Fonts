// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    [DebuggerDisplay("StartCoord: {StartCoord}, PeakCoord: {PeakCoord}, EndCoord: {EndCoord}")]
    public struct RegionAxisCoordinates
    {
        public float StartCoord;

        public float PeakCoord;

        public float EndCoord;
    }
}

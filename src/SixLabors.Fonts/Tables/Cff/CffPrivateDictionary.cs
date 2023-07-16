// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal class CffPrivateDictionary
    {
        public CffPrivateDictionary(byte[][]? localSubrRawBuffers, int defaultWidthX, int nominalWidthX)
        {
            this.LocalSubrRawBuffers = localSubrRawBuffers;
            this.DefaultWidthX = defaultWidthX;
            this.NominalWidthX = nominalWidthX;
        }

        public byte[][]? LocalSubrRawBuffers { get; set; }

        public int DefaultWidthX { get; set; }

        public int NominalWidthX { get; set; }
    }
}

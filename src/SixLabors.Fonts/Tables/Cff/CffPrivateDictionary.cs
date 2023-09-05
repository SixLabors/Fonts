// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal class CffPrivateDictionary
{
    public CffPrivateDictionary(byte[][] localSubrRawBuffers, int defaultWidthX, int nominalWidthX)
    {
        this.LocalSubrRawBuffers = localSubrRawBuffers;
        this.DefaultWidthX = defaultWidthX;
        this.NominalWidthX = nominalWidthX;
    }

    public byte[][] LocalSubrRawBuffers { get; set; }

    public int DefaultWidthX { get; set; }

    public int NominalWidthX { get; set; }
}

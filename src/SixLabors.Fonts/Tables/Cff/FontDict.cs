// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal class FontDict
{
    public FontDict(int name, int dictSize, int dictOffset)
    {
        this.FontName = name;
        this.PrivateDicSize = dictSize;
        this.PrivateDicOffset = dictOffset;
    }

    public int FontName { get; set; }

    public int PrivateDicSize { get; }

    public int PrivateDicOffset { get; }

    public byte[][]? LocalSubr { get; set; }
}

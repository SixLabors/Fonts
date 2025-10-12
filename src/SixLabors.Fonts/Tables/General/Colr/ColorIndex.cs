// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct ColorIndex
{
    public ColorIndex(ushort paletteIndex, float alpha)
    {
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
    }

    public ushort PaletteIndex { get; }

    public float Alpha { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorIndex Load(BigEndianBinaryReader reader)
        => new(reader.ReadUInt16(), reader.ReadF2Dot14());
}

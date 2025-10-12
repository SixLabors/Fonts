// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct ColorStop
{
    public ColorStop(float stopOffset, ushort paletteIndex, float alpha)
    {
        this.StopOffset = stopOffset;
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
    }

    public float StopOffset { get; }

    public ushort PaletteIndex { get; }

    public float Alpha { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorStop Load(BigEndianBinaryReader reader)
        => new(reader.ReadF2Dot14(), reader.ReadUInt16(), reader.ReadF2Dot14());
}

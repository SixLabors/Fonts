// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct VarColorIndex
{
    public VarColorIndex(ushort paletteIndex, float alpha, uint varIndexBase)
    {
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
        this.VarIndexBase = varIndexBase;
    }

    public ushort PaletteIndex { get; }

    public float Alpha { get; }

    public uint VarIndexBase { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VarColorIndex Load(BigEndianBinaryReader reader)
        => new(reader.ReadUInt16(), reader.ReadF2Dot14(), reader.ReadUInt32());
}

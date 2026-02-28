// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct VarColorStop
{
    public VarColorStop(float stopOffset, ushort paletteIndex, float alpha, uint varIndexBase)
    {
        this.StopOffset = stopOffset;
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
        this.VarIndexBase = varIndexBase;
    }

    public float StopOffset { get; }

    public ushort PaletteIndex { get; }

    public float Alpha { get; }

    public uint VarIndexBase { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VarColorStop Load(BigEndianBinaryReader reader)
        => new(reader.ReadF2Dot14(), reader.ReadUInt16(), reader.ReadF2Dot14(), reader.ReadUInt32());
}

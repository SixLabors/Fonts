// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal sealed class LayerList
{
    public LayerList(uint[] paintOffsets)
        => this.PaintOffsets = paintOffsets;

    public uint[] PaintOffsets { get; }

    public int Count => this.PaintOffsets.Length;

    public static LayerList? Load(BigEndianBinaryReader reader, uint offset)
    {
        if (offset == 0)
        {
            return null;
        }

        reader.Seek(offset, SeekOrigin.Begin);
        uint count = reader.ReadUInt32();

        if (count == 0)
        {
            return null;
        }

        // Offsets are relative to the table start; convert to COLR-relative.
        uint[] offsets = new uint[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = offset + reader.ReadOffset32();
        }

        return new LayerList(offsets);
    }
}

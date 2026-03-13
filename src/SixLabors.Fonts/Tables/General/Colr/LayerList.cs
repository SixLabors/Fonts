// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents the COLR v1 LayerList, which stores an array of offsets to paint tables.
/// PaintColrLayers references ranges within this list to compose multi-layer color glyphs.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#baseglyphlist-layerlist-and-colrglyphs"/>
/// </summary>
internal sealed class LayerList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerList"/> class.
    /// </summary>
    /// <param name="paintOffsets">The array of paint table offsets relative to the beginning of the COLR table.</param>
    public LayerList(uint[] paintOffsets)
        => this.PaintOffsets = paintOffsets;

    /// <summary>
    /// Gets the array of paint table offsets relative to the beginning of the COLR table.
    /// </summary>
    public uint[] PaintOffsets { get; }

    /// <summary>
    /// Gets the number of paint offsets in the list.
    /// </summary>
    public int Count => this.PaintOffsets.Length;

    /// <summary>
    /// Loads a <see cref="LayerList"/> from the given reader at the specified offset.
    /// </summary>
    /// <param name="reader">The binary reader positioned within the COLR table.</param>
    /// <param name="offset">The offset from the beginning of the COLR table to the LayerList.</param>
    /// <returns>The loaded <see cref="LayerList"/>, or <see langword="null"/> if the offset is zero or the list is empty.</returns>
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

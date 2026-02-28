// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal sealed class BaseGlyphList
{
    public BaseGlyphList(BaseGlyphPaintRecord[] records)
        => this.Records = records;

    public BaseGlyphPaintRecord[] Records { get; }

    public int Count => this.Records.Length;

    public static BaseGlyphList? Load(BigEndianBinaryReader reader, uint offset)
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
        BaseGlyphPaintRecord[] records = new BaseGlyphPaintRecord[count];
        for (int i = 0; i < count; i++)
        {
            ushort glyphId = reader.ReadUInt16();
            records[i] = new BaseGlyphPaintRecord(glyphId, offset + reader.ReadOffset32());
        }

        // Spec says records are sorted by glyphId; assume font is correct
        return new BaseGlyphList(records);
    }
}

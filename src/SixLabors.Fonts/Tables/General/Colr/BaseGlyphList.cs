// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents the BaseGlyphList table in COLR v1, which maps glyph IDs to their root paint table offsets.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#baseglyphlist-layerlist-and-டிcolrglyphs"/>
/// </summary>
internal sealed class BaseGlyphList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGlyphList"/> class.
    /// </summary>
    /// <param name="records">The array of base glyph paint records.</param>
    public BaseGlyphList(BaseGlyphPaintRecord[] records)
        => this.Records = records;

    /// <summary>
    /// Gets the array of base glyph paint records, sorted by glyph ID.
    /// </summary>
    public BaseGlyphPaintRecord[] Records { get; }

    /// <summary>
    /// Gets the number of base glyph paint records.
    /// </summary>
    public int Count => this.Records.Length;

    /// <summary>
    /// Loads a <see cref="BaseGlyphList"/> from the given reader at the specified offset.
    /// </summary>
    /// <param name="reader">The binary reader positioned within the COLR table.</param>
    /// <param name="offset">The offset from the beginning of the COLR table to the BaseGlyphList.</param>
    /// <returns>The loaded <see cref="BaseGlyphList"/>, or <see langword="null"/> if the offset is zero or the list is empty.</returns>
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

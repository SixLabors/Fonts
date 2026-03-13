// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tables.TrueType;

/// <summary>
/// Represents the 'loca' (Index to Location) table which maps glyph IDs to byte offsets
/// within the 'glyf' table, enabling random access to individual glyph outlines.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca"/>
/// </summary>
internal sealed class IndexLocationTable : Table
{
    /// <summary>
    /// The table tag name.
    /// </summary>
    internal const string TableName = "loca";

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexLocationTable"/> class.
    /// </summary>
    /// <param name="convertedData">The array of glyph byte offsets into the 'glyf' table.</param>
    public IndexLocationTable(uint[] convertedData)
        => this.GlyphOffsets = convertedData;

    /// <summary>
    /// Gets the array of byte offsets into the 'glyf' table, one per glyph plus a trailing entry.
    /// </summary>
    public uint[] GlyphOffsets { get; }

    /// <summary>
    /// Loads the 'loca' table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="IndexLocationTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static IndexLocationTable? Load(FontReader fontReader)
    {
        HeadTable head = fontReader.GetTable<HeadTable>();

        MaximumProfileTable maxp = fontReader.GetTable<MaximumProfileTable>();

        // Must not get a binary reader until all depended data is retrieved in case they need to use the stream.
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader, maxp.GlyphCount, head.IndexLocationFormat);
        }
    }

    /// <summary>
    /// Loads the 'loca' table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="format">The index location format (short or long offsets) from the 'head' table.</param>
    /// <returns>The <see cref="IndexLocationTable"/>.</returns>
    public static IndexLocationTable Load(BigEndianBinaryReader reader, int glyphCount, HeadTable.IndexLocationFormats format)
    {
        int entryCount = glyphCount + 1;

        if (format == HeadTable.IndexLocationFormats.Offset16)
        {
            // Type     | Name        | Description
            // ---------|-------------|---------------------------------------
            // Offset16 | offsets[n]  | The actual local offset divided by 2 is stored. The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.
            using Buffer<ushort> dataBuffer = new(entryCount);
            Span<ushort> data = dataBuffer.GetSpan();
            reader.ReadUInt16Array(data);

            uint[] convertedData = new uint[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                convertedData[i] = (uint)(data[i] * 2);
            }

            return new IndexLocationTable(convertedData);
        }
        else if (format == HeadTable.IndexLocationFormats.Offset32)
        {
            // Type     | Name        | Description
            // ---------|-------------|---------------------------------------
            // Offset32 | offsets[n]  | The actual local offset is stored. The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.
            uint[] data = reader.ReadUInt32Array(entryCount);

            return new IndexLocationTable(data);
        }
        else
        {
            throw new InvalidFontTableException("indexToLocFormat an invalid value", "head");
        }
    }
}

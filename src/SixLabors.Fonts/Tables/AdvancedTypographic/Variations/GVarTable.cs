// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the Font Variations Table `gvar`.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/gvar"/>
/// </summary>
internal class GVarTable : Table
{
    internal const string TableName = "gvar";

    public GVarTable(ushort axisCount, ushort glyphCount, float[,] sharedTuples, GlyphVariationData[] glyphVariations)
    {
        this.AxisCount = axisCount;
        this.GlyphCount = glyphCount;
        this.SharedTuples = sharedTuples;
        this.GlyphVariations = glyphVariations;
    }

    public ushort AxisCount { get; }

    public ushort GlyphCount { get; }

    public float[,] SharedTuples { get; }

    public GlyphVariationData[] GlyphVariations { get; }

    public static GVarTable? Load(FontReader reader)
    {
        if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader, out TableHeader? header))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader, header);
        }
    }

    public static GVarTable Load(BigEndianBinaryReader reader, TableHeader header)
    {
        // VariationsTable `gvar`
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                             |
        // +=================+========================================+=========================================================================+
        // | uint16          | majorVersion                           | Major version number of the font variations table — set to 1.           |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | uint16          | minorVersion                           | Minor version number of the font variations table — set to 0.           |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | uint16          | axisCount                              | The number of variation axes in the font                                |
        // |                 |                                        | (the number of records in the axes array).                              |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | uint16          | sharedTupleCount                       | The number of shared tuple records. Shared tuple records can            |
        // |                 |                                        | be referenced within glyph variation data tables for multiple glyphs,   |
        // |                 |                                        | as opposed to other tuple records stored directly within a glyph        |
        // |                 |                                        | variation data table.                                                   |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset32        | sharedTuplesOffset                     | Offset from the start of this table to the shared tuple records.        |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | uint16          | glyphCount                             | The number of glyphs in this font. This must match the number of glyphs |
        // |                 |                                        | stored elsewhere in the font.                                           |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | uint16          | flags                                  | Bit-field that gives the format of the offset array that follows.       |
        // |                 |                                        | If bit 0 is clear, the offsets are uint16; if bit 0 is set,             |
        // |                 |                                        | the offsets are uint32.                                                 |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset32        | glyphVariationDataArrayOffset          | Offset from the start of this table to the array of GlyphVariationData  |
        // |                 |                                        | tables.                                                                 |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset16 or     | glyphVariationDataOffsets[glyphCount+1]| Offsets from the start of the GlyphVariationData array to each          |
        // | Offset32        |                                        | GlyphVariationData table.                                               |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        uint gvarTableLength = header.Length;
        ushort major = reader.ReadUInt16();
        ushort minor = reader.ReadUInt16();
        ushort axisCount = reader.ReadUInt16();
        ushort sharedTupleCount = reader.ReadUInt16();
        uint sharedTuplesOffset = reader.ReadOffset32();
        ushort glyphCount = reader.ReadUInt16();
        ushort flags = reader.ReadUInt16();
        bool is32BitOffset = (flags & 1) == 1;
        uint glyphVariationDataArrayOffset = reader.ReadOffset32();

        if (major != 1)
        {
            throw new NotSupportedException("Only version 1 of gvar table is supported");
        }

        // Read glyphVariationDataOffsets[glyphCount + 1] immediately after the header,
        // as required by the spec and as done by FreeType.
        int offsetCount = glyphCount + 1;
        uint[] glyphVariationOffsets = new uint[offsetCount];

        for (int i = 0; i < offsetCount; i++)
        {
            // If offsets are 16-bit, values are stored in units of 2 bytes.
            glyphVariationOffsets[i] = is32BitOffset
                ? reader.ReadUInt32()
                : (uint)(reader.ReadUInt16() * 2);
        }

        // Shared tuple records
        float[,] sharedTuples = new float[sharedTupleCount, axisCount];

        if (sharedTupleCount > 0 && axisCount > 0)
        {
            long tuplesPos = sharedTuplesOffset;
            long tuplesLimit = glyphVariationDataArrayOffset;
            long bytesPerTuple = (long)axisCount * 2;
            long bytesAvailable = tuplesLimit - tuplesPos;

            long maxTuples = bytesAvailable > 0
                ? bytesAvailable / bytesPerTuple
                : 0;

            int tuplesToRead = (int)Math.Min(sharedTupleCount, maxTuples);

            reader.Seek(tuplesPos, SeekOrigin.Begin);

            for (int i = 0; i < tuplesToRead; i++)
            {
                for (int j = 0; j < axisCount; j++)
                {
                    sharedTuples[i, j] = reader.ReadF2Dot14();
                }
            }

            // Any remaining tuples default to 0.0F.
        }

        // GlyphVariationData tables
        long glyphDataBase = glyphVariationDataArrayOffset;
        GlyphVariationData[] glyphVariations = new GlyphVariationData[glyphCount];

        // Reader is positioned at table start
        long gvarEnd = gvarTableLength;

        for (int i = 0; i < glyphCount; i++)
        {
            long start = glyphDataBase + glyphVariationOffsets[i];
            long end = glyphDataBase + glyphVariationOffsets[i + 1]; // spec gives glyphCount+1 offsets

            // Validate range (must be within table and non-decreasing)
            if (start < glyphDataBase || end < start || end > gvarEnd || start + 2 > gvarEnd)
            {
                glyphVariations[i] = new GlyphVariationData(); // or null if allowed
                continue;
            }

            glyphVariations[i] = GlyphVariationData.Load(reader, start, is32BitOffset, axisCount);
        }

        return new GVarTable(axisCount, glyphCount, sharedTuples, glyphVariations);
    }
}

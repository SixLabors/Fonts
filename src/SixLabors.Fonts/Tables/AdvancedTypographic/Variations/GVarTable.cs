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
        if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    public static GVarTable Load(BigEndianBinaryReader reader)
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
        long startOffset = reader.BaseStream.Position;
        ushort major = reader.ReadUInt16();
        ushort minor = reader.ReadUInt16();
        ushort axisCount = reader.ReadUInt16();
        ushort sharedTupleCount = reader.ReadUInt16();
        ushort sharedTuplesOffset = reader.ReadOffset16();
        ushort glyphCount = reader.ReadUInt16();
        ushort flags = reader.ReadUInt16();
        bool is32BitOffset = (flags & 1) == 1;
        ushort glyphVariationDataArrayOffset = reader.ReadOffset16();

        if (major != 1)
        {
            throw new NotSupportedException("Only version 1 of gvar table is supported");
        }

        reader.Seek(startOffset + sharedTuplesOffset, SeekOrigin.Begin);
        float[,] sharedTuples = new float[sharedTupleCount, axisCount];
        for (int i = 0; i < sharedTupleCount; i++)
        {
            for (int j = 0; j < axisCount; j++)
            {
                sharedTuples[i, j] = reader.ReadF2Dot14();
            }
        }

        int glyphVariationsCount = glyphCount + 1;
        GlyphVariationData[] glyphVariations = new GlyphVariationData[glyphVariationsCount];
        for (int i = 0; i < glyphVariationsCount; i++)
        {
            glyphVariations[i] = GlyphVariationData.Load(reader, glyphVariationDataArrayOffset, is32BitOffset, axisCount);
        }

        return new GVarTable(axisCount, glyphCount, sharedTuples, glyphVariations);
    }
}

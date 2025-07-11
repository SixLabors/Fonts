// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Each subtable (except an Extension LookupType subtable) in a lookup references a Coverage table (Coverage),
/// which specifies all the glyphs affected by a substitution or positioning operation described in the subtable.
/// The GSUB, GPOS, and GDEF tables rely on this notion of coverage.
/// If a glyph does not appear in a Coverage table, the client can skip that subtable and move
/// immediately to the next subtable.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table"/>
/// </summary>
internal abstract class CoverageTable
{
    public abstract int CoverageIndexOf(ushort glyphId);

    public static CoverageTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort coverageFormat = reader.ReadUInt16();
        return coverageFormat switch
        {
            1 => CoverageFormat1Table.Load(reader),
            2 => CoverageFormat2Table.Load(reader),
            _ => throw new InvalidFontFileException($"Invalid value for 'coverageFormat' {coverageFormat}. Should be '1' or '2'.")
        };
    }

    public static CoverageTable[] LoadArray(BigEndianBinaryReader reader, long offset, ReadOnlySpan<ushort> coverageOffsets)
    {
        CoverageTable[] tables = new CoverageTable[coverageOffsets.Length];
        for (int i = 0; i < tables.Length; i++)
        {
            tables[i] = Load(reader, offset + coverageOffsets[i]);
        }

        return tables;
    }
}

internal sealed class CoverageFormat1Table : CoverageTable
{
    private readonly ushort[] glyphArray;

    private CoverageFormat1Table(ushort[] glyphArray)
        => this.glyphArray = glyphArray;

    public override int CoverageIndexOf(ushort glyphId)
    {
        int n = Array.BinarySearch(this.glyphArray, glyphId);
        return n < 0 ? -1 : n;
    }

    public static CoverageFormat1Table Load(BigEndianBinaryReader reader)
    {
        // +--------+------------------------+-----------------------------------------+
        // | Type   | Name                   | Description                             |
        // +========+========================+=========================================+
        // | uint16 | coverageFormat         | Format identifier — format = 1          |
        // +--------+------------------------+-----------------------------------------+
        // | uint16 | glyphCount             | Number of glyphs in the glyph array     |
        // +--------+------------------------+-----------------------------------------+
        // | uint16 | glyphArray[glyphCount] | Array of glyph IDs — in numerical order |
        // +--------+------------------------+-----------------------------------------+
        ushort glyphCount = reader.ReadUInt16();
        ushort[] glyphArray = reader.ReadUInt16Array(glyphCount);

        return new CoverageFormat1Table(glyphArray);
    }
}

internal sealed class CoverageFormat2Table : CoverageTable
{
    private readonly CoverageRangeRecord[] records;

    private CoverageFormat2Table(CoverageRangeRecord[] records)
        => this.records = records;

    public override int CoverageIndexOf(ushort glyphId)
    {
        for (int i = 0; i < this.records.Length; i++)
        {
            CoverageRangeRecord rec = this.records[i];
            if (rec.StartGlyphId <= glyphId && glyphId <= rec.EndGlyphId)
            {
                return rec.Index + glyphId - rec.StartGlyphId;
            }
        }

        return -1;
    }

    public static CoverageFormat2Table Load(BigEndianBinaryReader reader)
    {
        // +-------------+--------------------------+--------------------------------------------------+
        // | Type        | Name                     | Description                                      |
        // +=============+==========================+==================================================+
        // | uint16      | coverageFormat           | Format identifier — format = 2                   |
        // +-------------+--------------------------+--------------------------------------------------+
        // | uint16      | rangeCount               | Number of RangeRecords                           |
        // +-------------+--------------------------+--------------------------------------------------+
        // | RangeRecord | rangeRecords[rangeCount] | Array of glyph ranges — ordered by startGlyphID. |
        // +-------------+--------------------------+--------------------------------------------------+
        ushort rangeCount = reader.ReadUInt16();
        CoverageRangeRecord[] records = new CoverageRangeRecord[rangeCount];

        for (int i = 0; i < records.Length; i++)
        {
            // +--------+--------------------+-------------------------------------------+
            // | Type   | Name               | Description                               |
            // +========+====================+===========================================+
            // | uint16 | startGlyphID       | First glyph ID in the range               |
            // +--------+--------------------+-------------------------------------------+
            // | uint16 | endGlyphID         | Last glyph ID in the range                |
            // +--------+--------------------+-------------------------------------------+
            // | uint16 | startCoverageIndex | Coverage Index of first glyph ID in range |
            // +--------+--------------------+-------------------------------------------+
            records[i] = new CoverageRangeRecord(
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16());
        }

        return new CoverageFormat2Table(records);
    }
}

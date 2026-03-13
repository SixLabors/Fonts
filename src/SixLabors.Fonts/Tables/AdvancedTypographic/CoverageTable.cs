// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using static SixLabors.Fonts.Tables.AdvancedTypographic.CoverageFormat2Table;

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
    /// <summary>
    /// Gets the coverage index for the specified glyph, or -1 if the glyph is not covered.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The zero-based coverage index, or -1 if not found.</returns>
    public abstract int CoverageIndexOf(ushort glyphId);

    /// <summary>
    /// Loads a <see cref="CoverageTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the table.</param>
    /// <returns>The <see cref="CoverageTable"/>.</returns>
    public static CoverageTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort coverageFormat = reader.ReadUInt16();
        return coverageFormat switch
        {
            1 => CoverageFormat1Table.Load(reader),
            2 => CoverageFormat2Table.Load(reader),

            // Harfbuzz (Coverage.hh) treats this as an empty table and does not throw.
            // SofiaSans Condensed can trigger this. See https://github.com/SixLabors/Fonts/issues/470
            _ => EmptyCoverageTable.Instance
        };
    }

    /// <summary>
    /// Loads an array of <see cref="CoverageTable"/> values from the binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The base offset from which coverage offsets are relative.</param>
    /// <param name="coverageOffsets">The array of offsets to individual coverage tables.</param>
    /// <returns>The array of <see cref="CoverageTable"/>.</returns>
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

/// <summary>
/// Coverage Format 1: individual glyph indices listed in numerical order.
/// </summary>
internal sealed class CoverageFormat1Table : CoverageTable
{
    private readonly ushort[] glyphArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageFormat1Table"/> class.
    /// </summary>
    /// <param name="glyphArray">The array of glyph IDs in numerical order.</param>
    private CoverageFormat1Table(ushort[] glyphArray)
        => this.glyphArray = glyphArray;

    /// <inheritdoc />
    public override int CoverageIndexOf(ushort glyphId)
    {
        int n = Array.BinarySearch(this.glyphArray, glyphId);
        return n < 0 ? -1 : n;
    }

    /// <summary>
    /// Loads a <see cref="CoverageFormat1Table"/> from the binary reader.
    /// The format identifier has already been read.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="CoverageFormat1Table"/>.</returns>
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

/// <summary>
/// Coverage Format 2: ranges of consecutive glyph IDs, ordered by startGlyphID.
/// </summary>
internal sealed class CoverageFormat2Table : CoverageTable
{
    private readonly CoverageRangeRecord[] records;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageFormat2Table"/> class.
    /// </summary>
    /// <param name="records">The array of coverage range records.</param>
    private CoverageFormat2Table(CoverageRangeRecord[] records)
        => this.records = records;

    /// <inheritdoc />
    public override int CoverageIndexOf(ushort glyphId)
    {
        // Records are ordered by StartGlyphId, so use binary search to find the
        // candidate range whose StartGlyphId is <= glyphId.
        CoverageRangeRecord[] records = this.records;
        int lo = 0;
        int hi = records.Length - 1;
        while (lo <= hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            CoverageRangeRecord rec = records[mid];
            if (glyphId < rec.StartGlyphId)
            {
                hi = mid - 1;
            }
            else if (glyphId > rec.EndGlyphId)
            {
                lo = mid + 1;
            }
            else
            {
                return rec.Index + glyphId - rec.StartGlyphId;
            }
        }

        return -1;
    }

    /// <summary>
    /// Loads a <see cref="CoverageFormat2Table"/> from the binary reader.
    /// The format identifier has already been read.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="CoverageFormat2Table"/>.</returns>
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

    /// <summary>
    /// An empty coverage table that never matches any glyph. Used as a fallback for invalid coverage formats.
    /// </summary>
    internal sealed class EmptyCoverageTable : CoverageTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyCoverageTable"/> class.
        /// </summary>
        private EmptyCoverageTable()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the empty coverage table.
        /// </summary>
        public static EmptyCoverageTable Instance { get; } = new();

        /// <inheritdoc />
        public override int CoverageIndexOf(ushort glyphId) => -1;
    }
}

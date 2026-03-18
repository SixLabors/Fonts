// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// A single adjustment positioning subtable (SinglePos) is used to adjust the placement or advance of a single glyph,
/// such as a subscript or superscript. In addition, a SinglePos subtable is commonly used to implement lookup data for contextual positioning.
/// A SinglePos subtable will have one of two formats: one that applies the same adjustment to a series of glyphs(Format 1),
/// and one that applies a different adjustment for each unique glyph(Format 2).
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable"/>
/// </summary>
internal static class LookupType1SubTable
{
    /// <summary>
    /// Loads the single adjustment positioning subtable from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort posFormat = reader.ReadUInt16();

        return posFormat switch
        {
            1 => LookupType1Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType1Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Single Adjustment Positioning Format 1: applies the same positioning value to all glyphs in the Coverage table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#single-adjustment-positioning-format-1-single-positioning-value"/>
/// </summary>
internal sealed class LookupType1Format1SubTable : LookupSubTable
{
    private readonly ValueRecord valueRecord;
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType1Format1SubTable"/> class.
    /// </summary>
    /// <param name="valueRecord">The positioning value record applied to all covered glyphs.</param>
    /// <param name="coverageTable">The coverage table.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    private LookupType1Format1SubTable(ValueRecord valueRecord, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.valueRecord = valueRecord;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the Format 1 single adjustment positioning subtable.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupType1Format1SubTable"/>.</returns>
    public static LookupType1Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // SinglePosFormat1
        // +-------------+----------------+-----------------------------------------------+
        // | Type        | Name           | Description                                   |
        // +=============+================+===============================================+
        // | uint16      | posFormat      | Format identifier: format = 1                 |
        // +-------------+----------------+-----------------------------------------------+
        // | Offset16    | coverageOffset | Offset to Coverage table, from beginning      |
        // |             |                | of SinglePos subtable.                        |
        // +-------------+----------------+-----------------------------------------------+
        // | uint16      | valueFormat    | Defines the types of data in the ValueRecord. |
        // +-------------+----------------+-----------------------------------------------+
        // | ValueRecord | valueRecord    | Defines positioning value(s) — applied to     |
        // |             |                | all glyphs in the Coverage table.             |
        // +-------------+----------------+-----------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ValueFormat valueFormat = reader.ReadUInt16<ValueFormat>();
        ValueRecord valueRecord = new(reader, valueFormat, offset);

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType1Format1SubTable(valueRecord, coverageTable, lookupFlags, markFilteringSet);
    }

    /// <inheritdoc/>
    public override bool TryUpdatePosition(
        FontMetrics fontMetrics,
        GPosTable table,
        GlyphPositioningCollection collection,
        Tag feature,
        int index,
        int count)
    {
        ushort glyphId = collection[index].GlyphId;
        if (glyphId == 0)
        {
            return false;
        }

        int coverage = this.coverageTable.CoverageIndexOf(glyphId);
        if (coverage > -1)
        {
            ValueRecord record = this.valueRecord;
            AdvancedTypographicUtils.ApplyPosition(fontMetrics, collection, index, record, feature);

            return true;
        }

        return false;
    }
}

/// <summary>
/// Single Adjustment Positioning Format 2: applies a unique positioning value to each glyph in the Coverage table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#single-adjustment-positioning-format-2-array-of-positioning-values"/>
/// </summary>
internal sealed class LookupType1Format2SubTable : LookupSubTable
{
    private readonly CoverageTable coverageTable;
    private readonly ValueRecord[] valueRecords;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType1Format2SubTable"/> class.
    /// </summary>
    /// <param name="valueRecords">The array of positioning value records, one per covered glyph.</param>
    /// <param name="coverageTable">The coverage table.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    private LookupType1Format2SubTable(ValueRecord[] valueRecords, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.valueRecords = valueRecords;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the Format 2 single adjustment positioning subtable.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupType1Format2SubTable"/>.</returns>
    public static LookupType1Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // SinglePosFormat2
        // +-------------+--------------------------+-----------------------------------------------+
        // |    Type     |   Name                   | Description                                   |
        // +=============+==========================+===============================================+
        // |    uint16   |   posFormat              | Format identifier: format = 2                 |
        // +-------------+--------------------------+-----------------------------------------------+
        // | Offset16    | coverageOffset           | Offset to Coverage table, from beginning      |
        // |             |                          | of SinglePos subtable.                        |
        // +-------------+--------------------------+-----------------------------------------------+
        // | uint16      | valueFormat              | Defines the types of data in the ValueRecords.|
        // +-------------+--------------------------+-----------------------------------------------+
        // | uint16      | valueCount               | Number of ValueRecords — must equal glyphCount|
        // |             |                          | in the Coverage table.                        |
        // | ValueRecord | valueRecords[valueCount] | Array of ValueRecords — positioning values    |
        // |             |                          | applied to glyphs.                            |
        // +-------------+--------------------------+-----------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ValueFormat valueFormat = reader.ReadUInt16<ValueFormat>();
        ushort valueCount = reader.ReadUInt16();
        ValueRecord[] valueRecords = new ValueRecord[valueCount];
        for (int i = 0; i < valueCount; i++)
        {
            valueRecords[i] = new ValueRecord(reader, valueFormat, offset);
        }

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType1Format2SubTable(valueRecords, coverageTable, lookupFlags, markFilteringSet);
    }

    /// <inheritdoc/>
    public override bool TryUpdatePosition(
        FontMetrics fontMetrics,
        GPosTable table,
        GlyphPositioningCollection collection,
        Tag feature,
        int index,
        int count)
    {
        ushort glyphId = collection[index].GlyphId;
        if (glyphId == 0)
        {
            return false;
        }

        int coverage = this.coverageTable.CoverageIndexOf(glyphId);
        if (coverage > -1 && coverage < this.valueRecords.Length)
        {
            ValueRecord record = this.valueRecords[coverage];
            AdvancedTypographicUtils.ApplyPosition(fontMetrics, collection, index, record, feature);

            return true;
        }

        return false;
    }
}

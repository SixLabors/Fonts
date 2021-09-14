// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// A single adjustment positioning subtable (SinglePos) is used to adjust the placement or advance of a single glyph,
    /// such as a subscript or superscript. In addition, a SinglePos subtable is commonly used to implement lookup data for contextual positioning.
    /// A SinglePos subtable will have one of two formats: one that applies the same adjustment to a series of glyphs(Format 1),
    /// and one that applies a different adjustment for each unique glyph(Format 2).
    /// <see cref="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable"/>
    /// </summary>
    internal sealed class LookupType1SubTable
    {
        private LookupType1SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType1Format1SubTable.Load(reader, offset),
                2 => LookupType1Format2SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'substFormat' {substFormat}. Should be '1' or '2'.")
            };
        }
    }

    internal sealed class LookupType1Format1SubTable : LookupSubTable
    {
        private readonly ValueRecord valueRecord;
        private readonly CoverageTable coverageTable;

        private LookupType1Format1SubTable(ValueRecord valueRecord, CoverageTable coverageTable)
        {
            this.valueRecord = valueRecord;
            this.coverageTable = coverageTable;
        }

        public static LookupType1Format1SubTable Load(BigEndianBinaryReader reader, long offset)
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
            var valueRecord = new ValueRecord(reader, valueFormat);

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType1Format1SubTable(valueRecord, coverageTable);
        }

        public override bool TryUpdatePosition(
            IFontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            ushort index,
            int count)
        {
            for (ushort i = 0; i < count; i++)
            {
                int glyphId = collection.GetGlyphIds(i + index)[0];
                if (glyphId < 0)
                {
                    return false;
                }

                int coverage = this.coverageTable.CoverageIndexOf((ushort)glyphId);
                if (coverage > -1)
                {
                    ValueRecord record = this.valueRecord;
                    collection.Offset(fontMetrics, i, (ushort)glyphId, record.XPlacement, record.YPlacement);
                    collection.Advance(fontMetrics, i, (ushort)glyphId, record.XAdvance, record.YAdvance);
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LookupType1Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ValueRecord[] valueRecord;

        private LookupType1Format2SubTable(ValueRecord[] valueRecord, CoverageTable coverageTable)
        {
            this.valueRecord = valueRecord;
            this.coverageTable = coverageTable;
        }

        public static LookupType1Format2SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SinglePosFormat1
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
            var valueRecords = new ValueRecord[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                valueRecords[i] = new ValueRecord(reader, valueFormat);
            }

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType1Format2SubTable(valueRecords, coverageTable);
        }

        public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
            => throw new System.NotImplementedException();
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// A pair adjustment positioning subtable (PairPos) is used to adjust the placement or advances of two glyphs in relation to one another —
    /// for instance, to specify kerning data for pairs of glyphs. Compared to a typical kerning table, however,
    /// a PairPos subtable offers more flexibility and precise control over glyph positioning.
    /// The PairPos subtable can adjust each glyph in a pair independently in both the X and Y directions,
    /// and it can explicitly describe the particular type of adjustment applied to each glyph.
    /// PairPos subtables can be either of two formats: one that identifies glyphs individually by index(Format 1), and one that identifies glyphs by class (Format 2).
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable"/>
    /// </summary>
    internal sealed class LookupType2SubTable
    {
        internal LookupType2SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType2Format1SubTable.Load(reader, offset),
                2 => LookupType1Format2SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'substFormat' {substFormat}. Should be '1' or '2'.")
            };
        }

        internal sealed class LookupType2Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly PairValueRecord[] valueRecords;

            public LookupType2Format1SubTable(CoverageTable coverageTable, PairValueRecord[] valueRecords)
            {
                this.coverageTable = coverageTable;
                this.valueRecords = valueRecords;
            }

            public static LookupType2Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Pair Adjustment Positioning Subtable format 1.
                // +-------------+------------------------------+------------------------------------------------+
                // | Type        |  Name                        | Description                                    |
                // +=============+==============================+================================================+
                // | uint16      | posFormat                    | Format identifier: format = 1                  |
                // +-------------+------------------------------+------------------------------------------------+
                // | Offset16    | coverageOffset               | Offset to Coverage table, from beginning of    |
                // |             |                              | PairPos subtable.                              |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | valueFormat1                 | Defines the types of data in valueRecord1 —    |
                // |             |                              | for the first glyph in the pair (may be zero). |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | valueFormat2                 | Defines the types of data in valueRecord2 —    |
                // |             |                              | for the second glyph in the pair (may be zero).|
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | pairSetCount                 | Number of PairSet tables                       |
                // +-------------+------------------------------+------------------------------------------------+
                // | Offset16    | pairSetOffsets[pairSetCount] | Array of offsets to PairSet tables.            |
                // |             |                              | Offsets are from beginning of PairPos subtable,|
                // |             |                              | ordered by Coverage Index.                     |
                // +-------------+------------------------------+------------------------------------------------+
                ushort coverageOffset = reader.ReadOffset16();
                ValueFormat valueFormat1 = reader.ReadUInt16<ValueFormat>();
                ValueFormat valueFormat2 = reader.ReadUInt16<ValueFormat>();
                ushort pairSetCount = reader.ReadUInt16();
                var pairValueRecords = new PairValueRecord[pairSetCount];
                for (int i = 0; i < pairSetCount; i++)
                {
                    ushort pairSetOffset = reader.ReadOffset16();
                    reader.Seek(pairSetOffset, SeekOrigin.Begin);
                    pairValueRecords[i] = new PairValueRecord(reader, valueFormat1, valueFormat2);
                }

                var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

                return new LookupType2Format1SubTable(coverageTable, pairValueRecords);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }

        internal sealed class LookupType2Format2SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly Class2Record[] class2Records;
            private readonly ClassDefinitionTable classDefinitionTable1;
            private readonly ClassDefinitionTable classDefinitionTable2;

            public LookupType2Format2SubTable(CoverageTable coverageTable, Class2Record[] class2Records, ClassDefinitionTable classDefinitionTable1, ClassDefinitionTable classDefinitionTable2)
            {
                this.coverageTable = coverageTable;
                this.class2Records = class2Records;
                this.classDefinitionTable1 = classDefinitionTable1;
                this.classDefinitionTable2 = classDefinitionTable2;
            }

            public static LookupType2Format2SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Pair Adjustment Positioning Subtable format 2.
                // +-------------+------------------------------+------------------------------------------------+
                // | Type        |  Name                        | Description                                    |
                // +=============+==============================+================================================+
                // | uint16      | posFormat                    | Format identifier: format = 2                  |
                // +-------------+------------------------------+------------------------------------------------+
                // | Offset16    | coverageOffset               | Offset to Coverage table, from beginning of    |
                // |             |                              | PairPos subtable.                              |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | valueFormat1                 | Defines the types of data in valueRecord1 —    |
                // |             |                              | for the first glyph in the pair (may be zero). |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | valueFormat2                 | Defines the types of data in valueRecord2 —    |
                // |             |                              | for the second glyph in the pair (may be zero).|
                // +-------------+------------------------------+------------------------------------------------+
                // | Offset16    | classDef1Offset              | Offset to ClassDef table, from beginning of    |
                // |             |                              | PairPos subtable —                             |
                // |             |                              | for the first glyph of the pair.               |
                // +-------------+------------------------------+------------------------------------------------+
                // | Offset16    | classDef2Offset              | Offset to ClassDef table, from beginning of    |
                // |             |                              | PairPos subtable —                             |
                // |             |                              | for the second glyph of the pair. —            |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | class1Count                  | Number of classes in classDef1 table —         |
                // |             |                              | includes Class 0.                              |
                // +-------------+------------------------------+------------------------------------------------+
                // | uint16      | class2Count                  | Number of classes in classDef2 table —         |
                // |             |                              | includes Class 0.                              |
                // +-------------+------------------------------+------------------------------------------------+
                // | Class1Record| class1Records[class1Count]   | Array of Class1 records,                       |
                // |             |                              | ordered by classes in classDef1.               |
                // +-------------+------------------------------+------------------------------------------------+
                ushort coverageOffset = reader.ReadOffset16();
                ValueFormat valueFormat1 = reader.ReadUInt16<ValueFormat>();
                ValueFormat valueFormat2 = reader.ReadUInt16<ValueFormat>();
                ushort classDef1Offset = reader.ReadOffset16();
                ushort classDef2Offset = reader.ReadOffset16();
                ushort class1Count = reader.ReadUInt16();

                // TODO: review this again, there is something wrong still.
                ushort class2Count = reader.ReadUInt16();
                var class2Records = new Class2Record[class1Count];
                for (int i = 0; i < class1Count; i++)
                {
                    class2Records[i] = new Class2Record(reader, valueFormat1, valueFormat2);
                }

                var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
                var classDefTable1 = ClassDefinitionTable.Load(reader, offset + classDef1Offset);
                var classDefTable2 = ClassDefinitionTable.Load(reader, offset + classDef2Offset);

                return new LookupType2Format2SubTable(coverageTable, class2Records, classDefTable1, classDefTable2);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count) => throw new System.NotImplementedException();
        }
    }
}

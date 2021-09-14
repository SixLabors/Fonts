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

                // 2 => LookupType1Format2SubTable.Load(reader, offset),
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
                ushort posFormat = reader.ReadUInt16();
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
    }
}

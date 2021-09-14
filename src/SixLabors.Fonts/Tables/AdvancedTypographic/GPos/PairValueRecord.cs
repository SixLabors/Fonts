// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// PairValueRecords are used in pair adjustment positioning subtables to adjust the placement or advances of two glyphs in relation to one another.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable"/>
    /// </summary>
    internal readonly struct PairValueRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PairValueRecord"/> struct.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="valueFormat1">The types of data in valueRecord1 — for the first glyph in the pair (may be zero).</param>
        /// <param name="valueFormat2">The types of data in valueRecord2 — for the first glyph in the pair (may be zero).</param>
        public PairValueRecord(BigEndianBinaryReader reader, ValueFormat valueFormat1, ValueFormat valueFormat2)
        {
            // +--------------+------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name             | Description                                                                          |
            // +==============+==================+======================================================================================+
            // | uint16       | secondGlyph      | Glyph ID of second glyph in the pair (first glyph is listed in the Coverage table).  |
            // +--------------+------------------+--------------------------------------------------------------------------------------+
            // | ValueRecord  | valueRecord1     | Positioning data for the first glyph in the pair.                                    |
            // +--------------+------------------+--------------------------------------------------------------------------------------+
            // | ValueRecord  | valueRecord2     | Positioning data for the second glyph in the pair.                                   |
            // +--------------+------------------+--------------------------------------------------------------------------------------+
            this.SecondGlyph = reader.ReadInt16();
            this.ValueRecord1 = new ValueRecord(reader, valueFormat1);
            this.ValueRecord2 = new ValueRecord(reader, valueFormat2);
        }

        /// <summary>
        /// Gets the second glyph ID.
        /// </summary>
        public short SecondGlyph { get; }

        /// <summary>
        /// Gets the Positioning data for the first glyph in the pair.
        /// </summary>
        public ValueRecord ValueRecord1 { get; }

        /// <summary>
        /// Gets the Positioning data for the second glyph in the pair.
        /// </summary>
        public ValueRecord ValueRecord2 { get; }
    }
}

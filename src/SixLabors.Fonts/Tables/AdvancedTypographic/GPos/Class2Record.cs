// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Class2Record used in Pair Adjustment Positioning Format 2.
    /// A Class2Record consists of two ValueRecords, one for the first glyph in a class pair (valueRecord1) and one for the second glyph (valueRecord2).
    /// Note that both fields of a Class2Record are optional: If the PairPos subtable has a value of zero (0) for valueFormat1 or valueFormat2,
    /// then the corresponding record (valueRecord1 or valueRecord2) will be empty — that is, not present.
    /// </summary>
    internal readonly struct Class2Record
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Class2Record"/> struct.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="valueFormat1">The value format for value record 1.</param>
        /// <param name="valueFormat2">The value format for value record 2.</param>
        public Class2Record(BigEndianBinaryReader reader, ValueFormat valueFormat1, ValueFormat valueFormat2)
        {
            this.ValueRecord1 = new ValueRecord(reader, valueFormat1);
            this.ValueRecord2 = new ValueRecord(reader, valueFormat2);
        }

        /// <summary>
        /// Gets the positioning for the first glyph.
        /// </summary>
        public ValueRecord ValueRecord1 { get; }

        /// <summary>
        /// Gets the positioning for second glyph.
        /// </summary>
        public ValueRecord ValueRecord2 { get; }
    }
}

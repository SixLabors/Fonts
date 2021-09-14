// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal readonly struct Class2Record
    {
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

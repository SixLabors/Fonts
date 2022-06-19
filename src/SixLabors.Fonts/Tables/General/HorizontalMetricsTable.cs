// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General
{
    internal sealed class HorizontalMetricsTable : Table
    {
        internal const string TableName = "hmtx";
        private readonly short[] leftSideBearings;
        private readonly ushort[] advancedWidths;

        public HorizontalMetricsTable(ushort[] advancedWidths, short[] leftSideBearings)
        {
            this.advancedWidths = advancedWidths;
            this.leftSideBearings = leftSideBearings;
        }

        public ushort GetAdvancedWidth(int glyphIndex)
        {
            if (glyphIndex >= this.advancedWidths.Length)
            {
                return this.advancedWidths[0];
            }

            return this.advancedWidths[glyphIndex];
        }

        internal short GetLeftSideBearing(int glyphIndex)
        {
            if (glyphIndex >= this.leftSideBearings.Length)
            {
                return this.leftSideBearings[0];
            }

            return this.leftSideBearings[glyphIndex];
        }

        public static HorizontalMetricsTable Load(FontReader reader)
        {
            // you should load all dependent tables prior to manipulating the reader
            HorizontalHeadTable headTable = reader.GetTable<HorizontalHeadTable>();
            MaximumProfileTable profileTable = reader.GetTable<MaximumProfileTable>();

            // Move to start of table
            using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
            return Load(binaryReader, headTable.NumberOfHMetrics, profileTable.GlyphCount);
        }

        public static HorizontalMetricsTable Load(BigEndianBinaryReader reader, int metricCount, int glyphCount)
        {
            // Type           | Name                                          | Description
            // longHorMetric  | hMetrics[numberOfHMetrics]                    | Paired advance width and left side bearing values for each glyph. Records are indexed by glyph ID.
            // int16          | leftSideBearing[numGlyphs - numberOfHMetrics] | Left side bearings for glyph IDs greater than or equal to numberOfHMetrics.
            int bearingCount = glyphCount - metricCount;
            ushort[] advancedWidth = new ushort[metricCount];
            short[] leftSideBearings = new short[glyphCount];

            for (int i = 0; i < metricCount; i++)
            {
                // longHorMetric Record:
                // Type   | Name         | Description
                // uint16 | advanceWidth | Glyph advance width, in font design units.
                // int16  | lsb          | Glyph left side bearing, in font design units.
                advancedWidth[i] = reader.ReadUInt16();
                leftSideBearings[i] = reader.ReadInt16();
            }

            for (int i = 0; i < bearingCount; i++)
            {
                leftSideBearings[metricCount + i] = reader.ReadInt16();
            }

            return new HorizontalMetricsTable(advancedWidth, leftSideBearings);
        }
    }
}

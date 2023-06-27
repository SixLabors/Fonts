// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General
{
    internal sealed class VerticalMetricsTable : Table
    {
        internal const string TableName = "vmtx";
        private readonly short[] topSideBearings;
        private readonly ushort[] advancedHeights;

        public VerticalMetricsTable(ushort[] advancedHeights, short[] topSideBearings)
        {
            this.advancedHeights = advancedHeights;
            this.topSideBearings = topSideBearings;
        }

        public ushort GetAdvancedHeight(int glyphIndex)
        {
            if (glyphIndex >= this.advancedHeights.Length)
            {
                return this.advancedHeights[0];
            }

            return this.advancedHeights[glyphIndex];
        }

        internal short GetTopSideBearing(int glyphIndex)
        {
            if (glyphIndex >= this.topSideBearings.Length)
            {
                return this.topSideBearings[0];
            }

            return this.topSideBearings[glyphIndex];
        }

        public static VerticalMetricsTable? Load(FontReader reader)
        {
            // You should load all dependent tables prior to manipulating the reader
            VerticalHeadTable headTable = reader.GetTable<VerticalHeadTable>();
            MaximumProfileTable profileTable = reader.GetTable<MaximumProfileTable>();

            // Move to start of table
            if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            return Load(binaryReader, headTable.NumberOfVMetrics, profileTable.GlyphCount);
        }

        public static VerticalMetricsTable Load(BigEndianBinaryReader reader, int metricCount, int glyphCount)
        {
            // Type           | Name                                          | Description
            // longVerMetric  | vMetrics[numberOfVMetrics]                    | Paired advance height and top side bearing values for each glyph. Records are indexed by glyph ID.
            // int16          | leftSideBearing[numGlyphs - numberOfVMetrics] | Top side bearings for glyph IDs greater than or equal to numberOfVMetrics.
            int bearingCount = glyphCount - metricCount;
            ushort[] advancedHeights = new ushort[metricCount];
            short[] topSideBearings = new short[glyphCount];

            for (int i = 0; i < metricCount; i++)
            {
                // longVerMetric Record:
                // Type   | Name          | Description
                // -------| ------------- | -----------------------------------------------------------
                // uint16 | advanceHeight | The advance height of the glyph.Signed integer in FUnits.
                // int16  | topSideBearing| The top side bearing of the glyph. Signed integer in FUnits
                advancedHeights[i] = reader.ReadUInt16();
                topSideBearings[i] = reader.ReadInt16();
            }

            for (int i = 0; i < bearingCount; i++)
            {
                topSideBearings[metricCount + i] = reader.ReadInt16();
            }

            return new VerticalMetricsTable(advancedHeights, topSideBearings);
        }
    }
}

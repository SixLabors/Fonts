using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General.HorizontalMetrics;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class HorizontalMetricsTable : Table
    {
        const string TableName = "hmtx";
        private short[] leftSideBearings;
        private HorizontalMetricRecord[] records;

        public HorizontalMetricsTable(HorizontalMetricRecord[] records, short[] leftSideBearings)
        {
            this.records = records;
            this.leftSideBearings = leftSideBearings;
        }

        public static HorizontalMetricsTable Load(FontReader reader)
        {
            // you should load all dependent tables prior to manipulating the reader
            var headTable = reader.GetTable<HoizontalHeadTable>();
            var profileTable = reader.GetTable<MaximumProfileTable>();

            //move to start of table
            var binaryReader = reader.GetReaderAtTablePosition(TableName);
            return Load(binaryReader, headTable.NumberOfHMetrics, profileTable.GlyphCount);
        }

        public static HorizontalMetricsTable Load(BinaryReader reader, int metricCount, int glyphCount)
        {
            // Type           | Name                                          | Description
            // longHorMetric  | hMetrics[numberOfHMetrics]                    | Paired advance width and left side bearing values for each glyph. Records are indexed by glyph ID.
            // int16          | leftSideBearing[numGlyphs - numberOfHMetrics] | Left side bearings for glyph IDs greater than or equal to numberOfHMetrics.


            var bearingCount = glyphCount - metricCount;
            var records = new HorizontalMetrics.HorizontalMetricRecord[metricCount];
            var leftSideBearings = new short[bearingCount];

            for (var i = 0; i < metricCount; i++)
            {
                records[i] = HorizontalMetricRecord.Load(reader);
            }

            for (var i = 0; i < bearingCount; i++)
            {
                leftSideBearings[i] = reader.ReadInt16();
            }

            return new HorizontalMetricsTable(records, leftSideBearings);
        }
    }
}
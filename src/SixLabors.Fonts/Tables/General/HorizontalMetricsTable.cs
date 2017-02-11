using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class HorizontalMetricsTable : Table
    {
        private const string TableName = "hmtx";
        private short[] leftSideBearings;
        private ushort[] advancedWidths;

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
        
        internal short GetLeftSideBearing(ushort glyphIndex)
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
            var headTable = reader.GetTable<HoizontalHeadTable>();
            var profileTable = reader.GetTable<MaximumProfileTable>();

            // move to start of table
            var binaryReader = reader.GetReaderAtTablePosition(TableName);
            return Load(binaryReader, headTable.NumberOfHMetrics, profileTable.GlyphCount);
        }

        public static HorizontalMetricsTable Load(BinaryReader reader, int metricCount, int glyphCount)
        {
            // Type           | Name                                          | Description
            // longHorMetric  | hMetrics[numberOfHMetrics]                    | Paired advance width and left side bearing values for each glyph. Records are indexed by glyph ID.
            // int16          | leftSideBearing[numGlyphs - numberOfHMetrics] | Left side bearings for glyph IDs greater than or equal to numberOfHMetrics.
            var bearingCount = glyphCount - metricCount;
            var advancedWidth = new ushort[metricCount];
            var leftSideBearings = new short[glyphCount];

            for (var i = 0; i < metricCount; i++)
            {
                // longHorMetric Record:
                // Type   | Name         | Description
                // uint16 | advanceWidth | Glyph advance width, in font design units.
                // int16  | lsb          | Glyph left side bearing, in font design units.
                advancedWidth[i] = reader.ReadUInt16();
                leftSideBearings[i] = reader.ReadInt16();
            }

            for (var i = 0; i < bearingCount; i++)
            {
                leftSideBearings[metricCount + i] = reader.ReadInt16();
            }

            return new HorizontalMetricsTable(advancedWidth, leftSideBearings);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.HorizontalMetrics
{
    internal class HorizontalMetricRecord
    {
        private ushort advanceWidth;
        private short leftSideBearing;

        public HorizontalMetricRecord(ushort advanceWidth, short leftSideBearing)
        {
            this.advanceWidth = advanceWidth;
            this.leftSideBearing = leftSideBearing;
        }

        public static HorizontalMetricRecord Load(BinaryReader reader)
        {
            // longHorMetric Record:
            // Type   | Name         | Description
            // uint16 | advanceWidth | Glyph advance width, in font design units.
            // int16  | lsb          | Glyph left side bearing, in font design units.
            var advanceWidth = reader.ReadUInt16();
            var leftSideBearing = reader.ReadInt16();

            return new HorizontalMetricRecord(advanceWidth, leftSideBearing);
        }
    }
}

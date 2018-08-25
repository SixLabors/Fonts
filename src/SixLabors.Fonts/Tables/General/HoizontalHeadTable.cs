// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class HoizontalHeadTable : Table
    {
        private const string TableName = "hhea";

        public HoizontalHeadTable(short ascender, short descender, short lineGap, ushort advanceWidthMax, short minLeftSideBearing, short minRightSideBearing, short xMaxExtent, short caretSlopeRise, short caretSlopeRun, short caretOffset, ushort numberOfHMetrics)
        {
            this.Ascender = ascender;
            this.Descender = descender;
            this.LineGap = lineGap;
            this.AdvanceWidthMax = advanceWidthMax;
            this.MinLeftSideBearing = minLeftSideBearing;
            this.MinRightSideBearing = minRightSideBearing;
            this.XMaxExtent = xMaxExtent;
            this.CaretSlopeRise = caretSlopeRise;
            this.CaretSlopeRun = caretSlopeRun;
            this.CaretOffset = caretOffset;
            this.NumberOfHMetrics = numberOfHMetrics;
        }

        public ushort AdvanceWidthMax { get; }

        public short Ascender { get; }

        public short CaretOffset { get; }

        public short CaretSlopeRise { get; }

        public short CaretSlopeRun { get; }

        public short Descender { get; }

        public short LineGap { get; }

        public short MinLeftSideBearing { get; }

        public short MinRightSideBearing { get; }

        public ushort NumberOfHMetrics { get; }

        public short XMaxExtent { get; }

        public static HoizontalHeadTable Load(FontReader reader)
        {
            using (BinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader);
            }
        }

        public static HoizontalHeadTable Load(BinaryReader reader)
        {
            // Type      | Name                 | Description
            // ----------|----------------------|----------------------------------------------------------------------------------------------------
            // uint16    | majorVersion         | Major version number of the horizontal header table — set to 1.
            // uint16    | minorVersion         | Minor version number of the horizontal header table — set to 0.
            // FWORD     | Ascender             | Typographic ascent (Distance from baseline of highest ascender).
            // FWORD     | Descender            | Typographic descent (Distance from baseline of lowest descender).
            // FWORD     | LineGap              | Typographic line gap. - Negative  LineGap values are treated as zero in Windows 3.1, and in Mac OS System 6 and System 7.
            // UFWORD    | advanceWidthMax      | Maximum advance width value in 'hmtx' table.
            // FWORD     | minLeftSideBearing   | Minimum left sidebearing value in 'hmtx' table.
            // FWORD     | minRightSideBearing  | Minimum right sidebearing value; calculated as Min(aw - lsb - (xMax - xMin)).
            // FWORD     | xMaxExtent           | Max(lsb + (xMax - xMin)).
            // int16     | caretSlopeRise       | Used to calculate the slope of the cursor (rise/run); 1 for vertical.
            // int16     | caretSlopeRun        | 0 for vertical.
            // int16     | caretOffset          | The amount by which a slanted highlight on a glyph needs to be shifted to produce the best appearance. Set to 0 for non-slanted fonts
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | metricDataFormat     | 0 for current format.
            // uint16    | numberOfHMetrics     | Number of hMetric entries in 'hmtx' table
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            short ascender = reader.ReadFWORD();
            short descender = reader.ReadFWORD();
            short lineGap = reader.ReadFWORD();
            ushort advanceWidthMax = reader.ReadUFWORD();
            short minLeftSideBearing = reader.ReadFWORD();
            short minRightSideBearing = reader.ReadFWORD();
            short xMaxExtent = reader.ReadFWORD();
            short caretSlopeRise = reader.ReadInt16();
            short caretSlopeRun = reader.ReadInt16();
            short caretOffset = reader.ReadInt16();
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            short metricDataFormat = reader.ReadInt16(); // 0
            if (metricDataFormat != 0)
            {
                throw new InvalidFontTableException($"Expected metricDataFormat = 0 found {metricDataFormat}", "hhea");
            }

            ushort numberOfHMetrics = reader.ReadUInt16();

            return new HoizontalHeadTable(
                ascender,
                descender,
                lineGap,
                advanceWidthMax,
                minLeftSideBearing,
                minRightSideBearing,
                xMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                numberOfHMetrics);
        }
    }
}
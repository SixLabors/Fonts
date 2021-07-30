// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class HorizontalHeadTable : Table
    {
        private const string TableName = TableNames.Hhea;

        public HorizontalHeadTable(
            short ascender,
            short descender,
            short lineGap,
            ushort advanceWidthMax,
            short minLeftSideBearing,
            short minRightSideBearing,
            short xMaxExtent,
            short caretSlopeRise,
            short caretSlopeRun,
            short caretOffset,
            ushort numberOfHMetrics)
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

        public static HorizontalHeadTable? Load(FontReader reader)
        {
            using BigEndianBinaryReader? binaryReader = reader.TryGetReaderAtTablePosition(TableName);
            if (binaryReader is null)
            {
                return null;
            }

            return Load(binaryReader);
        }

        public static HorizontalHeadTable Load(BigEndianBinaryReader reader)
        {
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | Type   | Name                | Description                                                                     |
            // +========+=====================+=================================================================================+
            // | Fixed  | version             | 0x00010000 (1.0)                                                                |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | ascent              | Distance from baseline of highest ascender                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | descent             | Distance from baseline of lowest descender                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | lineGap             | typographic line gap                                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | uFWord | advanceWidthMax     | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | minLeftSideBearing  | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | minRightSideBearing | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | xMaxExtent          | max(lsb + (xMax-xMin))                                                          |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | caretSlopeRise      | used to calculate the slope of the caret (rise/run) set to 1 for vertical caret |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | caretSlopeRun       | 0 for vertical                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | caretOffset         | set value to 0 for non-slanted fonts                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | metricDataFormat    | 0 for current format                                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | uint16 | numOfLongHorMetrics | number of advance widths in metrics table                                       |
            // +--------+---------------------+---------------------------------------------------------------------------------+
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
                throw new InvalidFontTableException($"Expected metricDataFormat = 0 found {metricDataFormat}", TableName);
            }

            ushort numberOfHMetrics = reader.ReadUInt16();

            return new HorizontalHeadTable(
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

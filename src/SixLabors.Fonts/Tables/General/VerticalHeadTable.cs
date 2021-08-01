// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class VerticalHeadTable : Table
    {
        public const string TableName = "vhea";

        public VerticalHeadTable(
            short ascender,
            short descender,
            short lineGap,
            short advanceHeightMax,
            short minTopSideBearing,
            short minBottomSideBearing,
            short yMaxExtent,
            short caretSlopeRise,
            short caretSlopeRun,
            short caretOffset,
            ushort numberOfVMetrics)
        {
            this.Ascender = ascender;
            this.Descender = descender;
            this.LineGap = lineGap;
            this.AdvanceHeightMax = advanceHeightMax;
            this.MinTopSideBearing = minTopSideBearing;
            this.MinBottomSideBearing = minBottomSideBearing;
            this.YMaxExtent = yMaxExtent;
            this.CaretSlopeRise = caretSlopeRise;
            this.CaretSlopeRun = caretSlopeRun;
            this.CaretOffset = caretOffset;
            this.NumberOfVMetrics = numberOfVMetrics;
        }

        public short Ascender { get; }

        public short Descender { get; }

        public short LineGap { get; }

        public short AdvanceHeightMax { get; }

        public short MinTopSideBearing { get; }

        public short MinBottomSideBearing { get; }

        public short YMaxExtent { get; }

        public short CaretSlopeRise { get; }

        public short CaretSlopeRun { get; }

        public short CaretOffset { get; }

        public ushort NumberOfVMetrics { get; }

        public static VerticalHeadTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader!);
            }
        }

        public static VerticalHeadTable Load(BigEndianBinaryReader reader)
        {
            // +---------+----------------------+----------------------------------------------------------------------+
            // | Type    | Name                 | Description                                                          |
            // +=========+======================+======================================================================+
            // | fixed32 | version              | Version number of the Vertical Header Table (0x00011000 for          |
            // |         |                      | the current version).                                                |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | vertTypoAscender     | The vertical typographic ascender for this font. It is the distance  |
            // |         |                      | in FUnits from the vertical center baseline to the right of the      |
            // |         |                      | design space. This will usually be set to half the horizontal        |
            // |         |                      | advance of full-width glyphs. For example, if the full width is      |
            // |         |                      | 1000 FUnits, this field will be set to 500.                          |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | vertTypoDescender    | The vertical typographic descender for this font. It is the          |
            // |         |                      | distance in FUnits from the vertical center baseline to the left of  |
            // |         |                      | the design space. This will usually be set to half the horizontal    |
            // |         |                      | advance of full-width glyphs. For example, if the full width is      |
            // |         |                      | 1000 FUnits, this field will be set to -500.                         |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | vertTypoLineGap      | The vertical typographic line gap for this font.                     |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | advanceHeightMax     | The maximum advance height measurement in FUnits found in            |
            // |         |                      | the font. This value must be consistent with the entries in the      |
            // |         |                      | vertical metrics table.                                              |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | minTopSideBearing    | The minimum top side bearing measurement in FUnits found in          |
            // |         |                      | the font, in FUnits. This value must be consistent with the          |
            // |         |                      | entries in the vertical metrics table.                               |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | minBottomSideBearing | The minimum bottom side bearing measurement in FUnits                |
            // |         |                      | found in the font, in FUnits. This value must be consistent with     |
            // |         |                      | the entries in the vertical metrics table.                           |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | yMaxExtent           | This is defined as the value of the minTopSideBearing field          |
            // |         |                      | added to the result of the value of the yMin field subtracted        |
            // |         |                      | from the value of the yMax field.                                    |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | caretSlopeRise       | The value of the caretSlopeRise field divided by the value of the    |
            // |         |                      | caretSlopeRun field determines the slope of the caret. A value       |
            // |         |                      | of 0 for the rise and a value of 1 for the run specifies a           |
            // |         |                      | horizontal caret. A value of 1 for the rise and a value of 0 for the |
            // |         |                      | run specifies a vertical caret. A value between 0 for the rise and   |
            // |         |                      | 1 for the run is desirable for fonts whose glyphs are oblique or     |
            // |         |                      | italic. For a vertical font, a horizontal caret is best.             |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | caretSlopeRun        | See the caretSlopeRise field. Value = 0 for non-slanted fonts.       |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | caretOffset          | The amount by which the highlight on a slanted glyph needs to        |
            // |         |                      | be shifted away from the glyph in order to produce the best          |
            // |         |                      | appearance. Set value equal to 0 for non-slanted fonts.              |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | reserved             | Set to 0.                                                            |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | reserved             | Set to 0.                                                            |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | reserved             | Set to 0.                                                            |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | reserved             | Set to 0.                                                            |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | int16   | metricDataFormat     | Set to 0.                                                            |
            // +---------+----------------------+----------------------------------------------------------------------+
            // | uint16  | numOfLongVerMetrics  | Number of advance heights in the Vertical Metrics table.             |
            // +---------+----------------------+----------------------------------------------------------------------+
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            short vertTypoAscender = reader.ReadInt16();
            short vertTypoDescender = reader.ReadInt16();
            short vertTypoLineGap = reader.ReadInt16();
            short advanceHeightMax = reader.ReadInt16();
            short minTopSideBearing = reader.ReadInt16();
            short minBottomSideBearing = reader.ReadInt16();
            short yMaxExtent = reader.ReadInt16();
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

            ushort numOfLongVerMetrics = reader.ReadUInt16();

            return new VerticalHeadTable(
                vertTypoAscender,
                vertTypoDescender,
                vertTypoLineGap,
                advanceHeightMax,
                minTopSideBearing,
                minBottomSideBearing,
                yMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                numOfLongVerMetrics);
        }
    }
}

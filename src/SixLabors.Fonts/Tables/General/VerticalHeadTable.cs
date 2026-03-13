// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the vertical header table, which contains information needed to lay out fonts
/// whose characters are written vertically.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
/// </summary>
internal sealed class VerticalHeadTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "vhea";

    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalHeadTable"/> class.
    /// </summary>
    /// <param name="ascender">The vertical typographic ascender.</param>
    /// <param name="descender">The vertical typographic descender.</param>
    /// <param name="lineGap">The vertical typographic line gap.</param>
    /// <param name="advanceHeightMax">The maximum advance height.</param>
    /// <param name="minTopSideBearing">The minimum top side bearing.</param>
    /// <param name="minBottomSideBearing">The minimum bottom side bearing.</param>
    /// <param name="yMaxExtent">The maximum y extent.</param>
    /// <param name="caretSlopeRise">The caret slope rise.</param>
    /// <param name="caretSlopeRun">The caret slope run.</param>
    /// <param name="caretOffset">The caret offset for slanted fonts.</param>
    /// <param name="numberOfVMetrics">The number of vertical metrics in the 'vmtx' table.</param>
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

    /// <summary>
    /// Gets the vertical typographic ascender.
    /// </summary>
    public short Ascender { get; }

    /// <summary>
    /// Gets the vertical typographic descender.
    /// </summary>
    public short Descender { get; }

    /// <summary>
    /// Gets the vertical typographic line gap.
    /// </summary>
    public short LineGap { get; }

    /// <summary>
    /// Gets the maximum advance height in font design units.
    /// </summary>
    public short AdvanceHeightMax { get; }

    /// <summary>
    /// Gets the minimum top side bearing in font design units.
    /// </summary>
    public short MinTopSideBearing { get; }

    /// <summary>
    /// Gets the minimum bottom side bearing in font design units.
    /// </summary>
    public short MinBottomSideBearing { get; }

    /// <summary>
    /// Gets the maximum y extent: minTopSideBearing + (yMin - yMax).
    /// </summary>
    public short YMaxExtent { get; }

    /// <summary>
    /// Gets the caret slope rise. A value of 0 for rise and 1 for run specifies a horizontal caret.
    /// </summary>
    public short CaretSlopeRise { get; }

    /// <summary>
    /// Gets the caret slope run. A value of 0 for non-slanted fonts.
    /// </summary>
    public short CaretSlopeRun { get; }

    /// <summary>
    /// Gets the caret offset for slanted fonts. Set to 0 for non-slanted fonts.
    /// </summary>
    public short CaretOffset { get; }

    /// <summary>
    /// Gets the number of vertical metrics in the 'vmtx' table.
    /// </summary>
    public ushort NumberOfVMetrics { get; }

    /// <summary>
    /// Loads the <see cref="VerticalHeadTable"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="VerticalHeadTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static VerticalHeadTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="VerticalHeadTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="VerticalHeadTable"/>.</returns>
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

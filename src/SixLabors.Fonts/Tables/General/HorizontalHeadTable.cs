// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the horizontal header table, which contains information needed to lay out fonts
/// whose characters are written horizontally.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea"/>
/// </summary>
internal class HorizontalHeadTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "hhea";

    /// <summary>
    /// Initializes a new instance of the <see cref="HorizontalHeadTable"/> class.
    /// </summary>
    /// <param name="ascender">The typographic ascender.</param>
    /// <param name="descender">The typographic descender.</param>
    /// <param name="lineGap">The typographic line gap.</param>
    /// <param name="advanceWidthMax">The maximum advance width in font design units.</param>
    /// <param name="minLeftSideBearing">The minimum left side bearing.</param>
    /// <param name="minRightSideBearing">The minimum right side bearing.</param>
    /// <param name="xMaxExtent">The maximum x extent: max(lsb + (xMax - xMin)).</param>
    /// <param name="caretSlopeRise">The caret slope rise used to calculate the slope of the caret.</param>
    /// <param name="caretSlopeRun">The caret slope run used to calculate the slope of the caret.</param>
    /// <param name="caretOffset">The caret offset for slanted fonts.</param>
    /// <param name="numberOfHMetrics">The number of horizontal metrics in the 'hmtx' table.</param>
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

    /// <summary>
    /// Gets the maximum advance width, in font design units.
    /// </summary>
    public ushort AdvanceWidthMax { get; }

    /// <summary>
    /// Gets the typographic ascender distance from the baseline.
    /// </summary>
    public short Ascender { get; }

    /// <summary>
    /// Gets the caret offset for slanted fonts. Set to 0 for non-slanted fonts.
    /// </summary>
    public short CaretOffset { get; }

    /// <summary>
    /// Gets the caret slope rise. Set to 1 for a vertical caret.
    /// </summary>
    public short CaretSlopeRise { get; }

    /// <summary>
    /// Gets the caret slope run. Set to 0 for a vertical caret.
    /// </summary>
    public short CaretSlopeRun { get; }

    /// <summary>
    /// Gets the typographic descender distance from the baseline (typically negative).
    /// </summary>
    public short Descender { get; }

    /// <summary>
    /// Gets the typographic line gap.
    /// </summary>
    public short LineGap { get; }

    /// <summary>
    /// Gets the minimum left side bearing value.
    /// </summary>
    public short MinLeftSideBearing { get; }

    /// <summary>
    /// Gets the minimum right side bearing value.
    /// </summary>
    public short MinRightSideBearing { get; }

    /// <summary>
    /// Gets the number of horizontal metrics in the 'hmtx' table.
    /// </summary>
    public ushort NumberOfHMetrics { get; }

    /// <summary>
    /// Gets the maximum x extent: max(lsb + (xMax - xMin)).
    /// </summary>
    public short XMaxExtent { get; }

    /// <summary>
    /// Loads the <see cref="HorizontalHeadTable"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="HorizontalHeadTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static HorizontalHeadTable? Load(FontReader fontReader)
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
    /// Loads the <see cref="HorizontalHeadTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="HorizontalHeadTable"/>.</returns>
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

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the horizontal metrics table, which contains the horizontal layout metrics
/// (advance widths and left side bearings) for each glyph.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx"/>
/// </summary>
internal sealed class HorizontalMetricsTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "hmtx";

    /// <summary>
    /// The left side bearings array for all glyphs.
    /// </summary>
    private readonly short[] leftSideBearings;

    /// <summary>
    /// The advance widths array for glyphs with full metric records.
    /// </summary>
    private readonly ushort[] advancedWidths;

    /// <summary>
    /// Initializes a new instance of the <see cref="HorizontalMetricsTable"/> class.
    /// </summary>
    /// <param name="advancedWidths">The advance widths for each glyph.</param>
    /// <param name="leftSideBearings">The left side bearings for each glyph.</param>
    public HorizontalMetricsTable(ushort[] advancedWidths, short[] leftSideBearings)
    {
        this.advancedWidths = advancedWidths;
        this.leftSideBearings = leftSideBearings;
    }

    /// <summary>
    /// Gets the advance width for the specified glyph. If the glyph index exceeds the
    /// number of metric records, the last record's advance width is returned.
    /// </summary>
    /// <param name="glyphIndex">The glyph index.</param>
    /// <returns>The advance width in font design units.</returns>
    public ushort GetAdvancedWidth(int glyphIndex)
    {
        if (glyphIndex >= this.advancedWidths.Length)
        {
            // Records are indexed by glyph ID. As an optimization, the number of records can
            // be less than the number of glyphs, in which case the advance width value of the
            // last record applies to all remaining glyph IDs.
            return this.advancedWidths[^1];
        }

        return this.advancedWidths[glyphIndex];
    }

    /// <summary>
    /// Gets the left side bearing for the specified glyph.
    /// </summary>
    /// <param name="glyphIndex">The glyph index.</param>
    /// <returns>The left side bearing in font design units.</returns>
    internal short GetLeftSideBearing(int glyphIndex)
    {
        if (glyphIndex >= this.leftSideBearings.Length)
        {
            return this.leftSideBearings[^1];
        }

        return this.leftSideBearings[glyphIndex];
    }

    /// <summary>
    /// Loads the <see cref="HorizontalMetricsTable"/> from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="HorizontalMetricsTable"/>.</returns>
    public static HorizontalMetricsTable Load(FontReader reader)
    {
        // you should load all dependent tables prior to manipulating the reader
        HorizontalHeadTable headTable = reader.GetTable<HorizontalHeadTable>();
        MaximumProfileTable profileTable = reader.GetTable<MaximumProfileTable>();

        // Move to start of table
        using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
        return Load(binaryReader, headTable.NumberOfHMetrics, profileTable.GlyphCount);
    }

    /// <summary>
    /// Loads the <see cref="HorizontalMetricsTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="metricCount">The number of horizontal metric records (from 'hhea').</param>
    /// <param name="glyphCount">The total number of glyphs in the font (from 'maxp').</param>
    /// <returns>The <see cref="HorizontalMetricsTable"/>.</returns>
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

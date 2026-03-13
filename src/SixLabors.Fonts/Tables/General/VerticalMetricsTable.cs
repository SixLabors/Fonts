// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the vertical metrics table, which contains the vertical layout metrics
/// (advance heights and top side bearings) for each glyph.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
/// </summary>
internal sealed class VerticalMetricsTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "vmtx";

    /// <summary>
    /// The top side bearings array for all glyphs.
    /// </summary>
    private readonly short[] topSideBearings;

    /// <summary>
    /// The advance heights array for glyphs with full metric records.
    /// </summary>
    private readonly ushort[] advancedHeights;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalMetricsTable"/> class.
    /// </summary>
    /// <param name="advancedHeights">The advance heights for each glyph.</param>
    /// <param name="topSideBearings">The top side bearings for each glyph.</param>
    public VerticalMetricsTable(ushort[] advancedHeights, short[] topSideBearings)
    {
        this.advancedHeights = advancedHeights;
        this.topSideBearings = topSideBearings;
    }

    /// <summary>
    /// Gets the advance height for the specified glyph. If the glyph index exceeds the
    /// number of metric records, the first record's advance height is returned.
    /// </summary>
    /// <param name="glyphIndex">The glyph index.</param>
    /// <returns>The advance height in font design units.</returns>
    public ushort GetAdvancedHeight(int glyphIndex)
    {
        if (glyphIndex >= this.advancedHeights.Length)
        {
            return this.advancedHeights[0];
        }

        return this.advancedHeights[glyphIndex];
    }

    /// <summary>
    /// Gets the top side bearing for the specified glyph.
    /// </summary>
    /// <param name="glyphIndex">The glyph index.</param>
    /// <returns>The top side bearing in font design units.</returns>
    internal short GetTopSideBearing(int glyphIndex)
    {
        if (glyphIndex >= this.topSideBearings.Length)
        {
            return this.topSideBearings[0];
        }

        return this.topSideBearings[glyphIndex];
    }

    /// <summary>
    /// Loads the <see cref="VerticalMetricsTable"/> from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="VerticalMetricsTable"/>, or <see langword="null"/> if the table is not present.</returns>
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

        using (binaryReader)
        {
            return Load(binaryReader, headTable.NumberOfVMetrics, profileTable.GlyphCount);
        }
    }

    /// <summary>
    /// Loads the <see cref="VerticalMetricsTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="metricCount">The number of vertical metric records (from 'vhea').</param>
    /// <param name="glyphCount">The total number of glyphs in the font (from 'maxp').</param>
    /// <returns>The <see cref="VerticalMetricsTable"/>.</returns>
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

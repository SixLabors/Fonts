// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Variation data is comprised of delta adjustment values that have effect over particular regions within the font’s variation space.
/// In a tuple variation store (described earlier in this chapter), the deltas are organized into groupings by region of applicability, with each grouping associated with a given region.
/// In contrast, the item variation store format organizes deltas into groupings by the target items to which they apply, with each grouping having deltas for several regions.
/// Accordingly, the item variation store uses different formats for describing the regions in which a set of deltas apply.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#variation-regions"/>
/// </summary>
[DebuggerDisplay("AxisCount: {AxisCount}, RegionCount: {RegionCount}")]
internal class VariationRegionList
{
    public static readonly VariationRegionList EmptyVariationRegionList = new(0, 0, new[] { Array.Empty<RegionAxisCoordinates>() });

    private VariationRegionList(ushort axisCount, ushort regionCount, RegionAxisCoordinates[][] variationRegions)
    {
        this.AxisCount = axisCount;
        this.RegionCount = regionCount;
        this.VariationRegions = variationRegions;
    }

    public ushort AxisCount { get; }

    public ushort RegionCount { get; }

    public RegionAxisCoordinates[][] VariationRegions { get; }

    public static VariationRegionList Load(BigEndianBinaryReader reader, long offset)
    {
        // VariationRegionList
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                    |
        // +=================+========================================+================================================================+
        // | uint16          | axisCount                              | The number of variation axes for this font.                    |
        // |                 |                                        | This must be the same number as axisCount in the 'fvar' table. |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | regionCount                            | The number of variation region tables in the variation region  |
        // |                 |                                        | list. Must be less than 32,768.                                |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // + VariationRegion | variationRegions[regionCount]          | Array of variation regions.                                    |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort axisCount = reader.ReadUInt16();
        ushort regionCount = reader.ReadUInt16();
        var variationRegions = new RegionAxisCoordinates[regionCount][];
        for (int i = 0; i < regionCount; i++)
        {
            variationRegions[i] = new RegionAxisCoordinates[axisCount];
            for (int j = 0; j < axisCount; j++)
            {
                float startCoord = reader.ReadF2Dot14();
                float peakCoord = reader.ReadF2Dot14();
                float endCoord = reader.ReadF2Dot14();

                if (startCoord > peakCoord || peakCoord > endCoord)
                {
                    throw new InvalidFontFileException("Region axis coordinates out of order");
                }

                if (startCoord < -0x4000 || endCoord > 0x4000)
                {
                    throw new InvalidFontFileException("Region axis coordinate out of range");
                }

                if ((peakCoord < 0 && endCoord > 0) || (peakCoord > 0 && startCoord < 0))
                {
                    throw new InvalidFontFileException("Invalid region axis coordinates");
                }

                variationRegions[i][j] = new RegionAxisCoordinates()
                {
                    StartCoord = startCoord,
                    PeakCoord = peakCoord,
                    EndCoord = endCoord
                };
            }
        }

        return new VariationRegionList(axisCount, regionCount, variationRegions);
    }
}

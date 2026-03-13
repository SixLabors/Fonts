// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Represents a segment map record from the avar table, containing an array of axis value
/// mapping pairs for a single variation axis.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
/// </summary>
internal class SegmentMapRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentMapRecord"/> class.
    /// </summary>
    /// <param name="axisValueMap">The array of axis value map records for this axis.</param>
    public SegmentMapRecord(AxisValueMapRecord[] axisValueMap) => this.AxisValueMap = axisValueMap;

    /// <summary>
    /// Gets the array of axis value map records defining the piecewise linear mapping for this axis.
    /// </summary>
    public AxisValueMapRecord[] AxisValueMap { get; }

    /// <summary>
    /// Loads a <see cref="SegmentMapRecord"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="SegmentMapRecord"/>.</returns>
    public static SegmentMapRecord Load(BigEndianBinaryReader reader)
    {
        // SegmentMapRecord
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                             |
        // +=================+========================================+=========================================================================+
        // | uint16          | positionMapCount                       | The number of correspondence pairs for this axis.                       |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | AxisValueMap    | axisValueMaps[positionMapCount]        | The array of axis value map records for this axis.                      |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        ushort positionMapCount = reader.ReadUInt16();
        var axisValueMap = new AxisValueMapRecord[positionMapCount];
        for (int i = 0; i < positionMapCount; i++)
        {
            axisValueMap[i] = AxisValueMapRecord.Load(reader);
        }

        return new SegmentMapRecord(axisValueMap);
    }
}

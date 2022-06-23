// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    internal class SegmentMapRecord
    {
        public SegmentMapRecord(AxisValueMapRecord[] axisValueMap) => this.AxisValueMap = axisValueMap;

        public AxisValueMapRecord[] AxisValueMap { get; }

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
}

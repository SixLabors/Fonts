// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// Implements reading the Font Variations Table `avar`.
    /// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/avar"/>
    /// </summary>
    internal class AVarTable : Table
    {
        internal const string TableName = "avar";

        public AVarTable(uint axisCount, SegmentMapRecord[] segmentMaps)
        {
            this.AxisCount = axisCount;
            this.SegmentMaps = segmentMaps;
        }

        public uint AxisCount { get; }

        public SegmentMapRecord[] SegmentMaps { get; }

        public static AVarTable? Load(FontReader reader)
        {
            if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader);
            }
        }

        public static AVarTable Load(BigEndianBinaryReader reader)
        {
            // VariationsTable `avar`
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Type            | Name                                   | Description                                                             |
            // +=================+========================================+=========================================================================+
            // | uint16          | majorVersion                           | Major version number of the font variations table — set to 1.           |
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            // | uint16          | minorVersion                           | Minor version number of the font variations table — set to 0.           |
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            // | uint16          | (reserved)                             | This field is permanently reserved. Set to zero.                        |
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            // | uint16          | axisCount                              | The number of variation axes in the font                                |
            // |                 |                                        | (the number of records in the axes array).                              |
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            // | SegmentMaps     | axisSegmentMaps[axisCount]             | The segment maps array — one segment map for each axis, in the order of |
            // |                 |                                        | axes specified in the 'fvar' table.                                     |
            // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
            ushort major = reader.ReadUInt16();
            ushort minor = reader.ReadUInt16();
            ushort reserved = reader.ReadUInt16();
            ushort axisCount = reader.ReadUInt16();

            if (major != 1)
            {
                throw new NotSupportedException("Only version 1 of avar table is supported");
            }

            var segmentMaps = new SegmentMapRecord[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                segmentMaps[i] = SegmentMapRecord.Load(reader);
            }

            return new AVarTable(axisCount, segmentMaps);
        }
    }
}

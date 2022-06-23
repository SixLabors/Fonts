// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// Defines a VariationAxisRecord.
    /// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/fvar#variationaxisrecord"/>
    /// </summary>
    internal class VariationAxisRecord
    {
        internal VariationAxisRecord(string tag, float minValue, float defaultValue, float maxValue, ushort flags, ushort axisNameId)
        {
            this.Tag = tag;
            this.MinValue = minValue;
            this.DefaultValue = defaultValue;
            this.MaxValue = maxValue;
            this.Flags = flags;
            this.AxisNameId = axisNameId;
        }

        public string Tag { get; }

        public float MinValue { get; }

        public float DefaultValue { get; }

        public float MaxValue { get; }

        public ushort Flags { get; }

        public ushort AxisNameId { get; }

        public static VariationAxisRecord Load(BigEndianBinaryReader reader, long offset)
        {
            // VariationAxisRecord
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Type            | Name                                   | Description                                                    |
            // +=================+========================================+================================================================+
            // | Tag             | axisTag                                | Tag identifying the design variation for the axis.             |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Fixed           | minValue                               | The minimum coordinate value for the axis.                     |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Fixed           | defaultValue                           | The default coordinate value for the axis.                     |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Fixed           | maxValue                               | The maximum coordinate value for the axis.                     |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | uint16          | flags                                  | Axis qualifiers — see details below.                           |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | uint16          | axisNameID                             | The name ID for entries in the 'name' table that provide       |
            // |                 |                                        | a display name for this axis.                                  |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            string tag = reader.ReadTag();
            float minValue = reader.ReadFixed();
            float defaultValue = reader.ReadFixed();
            float maxValue = reader.ReadFixed();
            ushort flags = reader.ReadUInt16();
            ushort axisNameID = reader.ReadUInt16();

            return new VariationAxisRecord(tag, minValue, defaultValue, maxValue, flags, axisNameID);
        }
    }
}

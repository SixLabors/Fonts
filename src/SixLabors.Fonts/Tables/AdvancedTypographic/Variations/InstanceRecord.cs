// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// Defines a InstanceRecord.
    /// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/fvar#instancerecord"/>
    /// </summary>
    internal class InstanceRecord
    {
        public InstanceRecord(ushort subfamilyNameId, ushort postScriptNameId, float[] coordinates)
        {
            this.SubfamilyNameId = subfamilyNameId;
            this.PostScriptNameId = postScriptNameId;
            this.Coordinates = coordinates;
        }

        public ushort SubfamilyNameId { get; }

        public ushort PostScriptNameId { get; }

        public float[] Coordinates { get; }

        public static InstanceRecord Load(BigEndianBinaryReader reader, long offset, ushort axisCount)
        {
            // InstanceRecord
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Type            | Name                                   | Description                                                    |
            // +=================+========================================+================================================================+
            // | uint16          | subfamilyNameID                        | The name ID for entries in the 'name' table that provide       |
            // |                 |                                        | subfamily names for this instance.                             |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | uint16          | flags                                  | Reserved for future use — set to 0.                            |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | UserTuple       | coordinates                            | The coordinates array for this instance.                       |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | uint16          | postScriptNameID                       | Optional. The name ID for entries in the 'name' table that     |
            // |                 |                                        | provide PostScript names for this instance.                    |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort subfamilyNameId = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();

            float[] coordinates = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                coordinates[i] = reader.ReadFixed();
            }

            ushort postScriptNameId = reader.ReadUInt16();

            return new InstanceRecord(subfamilyNameId, postScriptNameId, coordinates);
        }
    }
}

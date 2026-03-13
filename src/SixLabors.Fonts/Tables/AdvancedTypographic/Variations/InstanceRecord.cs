// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Defines a InstanceRecord.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/fvar#instancerecord"/>
/// </summary>
internal class InstanceRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceRecord"/> class.
    /// </summary>
    /// <param name="subfamilyNameId">The name ID for the subfamily name of this instance.</param>
    /// <param name="postScriptNameId">The name ID for the PostScript name of this instance.</param>
    /// <param name="coordinates">The design-space coordinates for this instance, one per axis.</param>
    public InstanceRecord(ushort subfamilyNameId, ushort postScriptNameId, float[] coordinates)
    {
        this.SubfamilyNameId = subfamilyNameId;
        this.PostScriptNameId = postScriptNameId;
        this.Coordinates = coordinates;
    }

    /// <summary>
    /// Gets the name ID for entries in the 'name' table that provide subfamily names for this instance.
    /// </summary>
    public ushort SubfamilyNameId { get; }

    /// <summary>
    /// Gets the name ID for entries in the 'name' table that provide PostScript names for this instance.
    /// </summary>
    public ushort PostScriptNameId { get; }

    /// <summary>
    /// Gets the design-space coordinates array for this instance, one value per axis.
    /// </summary>
    public float[] Coordinates { get; }

    /// <summary>
    /// Loads an <see cref="InstanceRecord"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset from the start of the fvar table to this instance record.</param>
    /// <param name="axisCount">The number of variation axes.</param>
    /// <returns>The <see cref="InstanceRecord"/>.</returns>
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

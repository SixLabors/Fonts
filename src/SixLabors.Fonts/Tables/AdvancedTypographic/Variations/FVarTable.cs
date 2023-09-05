// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the Font Variations Table `fvar`.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/fvar"/>
/// </summary>
internal class FVarTable : Table
{
    internal const string TableName = "fvar";

    public FVarTable(ushort axisCount, VariationAxisRecord[] axes, InstanceRecord[] instances)
    {
        this.AxisCount = axisCount;
        this.Axes = axes;
        this.Instances = instances;
    }

    public ushort AxisCount { get; }

    public VariationAxisRecord[] Axes { get; }

    public InstanceRecord[] Instances { get; }

    public static FVarTable? Load(FontReader reader)
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

    public static FVarTable Load(BigEndianBinaryReader reader)
    {
        // VariationsTable `fvar`
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                    |
        // +=================+========================================+================================================================+
        // | uint16          | majorVersion                           | Major version number of the font variations table — set to 1.  |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | minorVersion                           | Minor version number of the font variations table — set to 0.  |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Offset16        | axesArrayOffset                        | Offset in bytes from the beginning of the table to the start   |
        // |                 |                                        | of the VariationAxisRecord array.                              |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | (reserved)                             | This field is permanently reserved. Set to 2.                  |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | axisCount                              | The number of variation axes in the font                       |
        // |                 |                                        | (the number of records in the axes array).                     |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | axisSize                               | The size in bytes of each VariationAxisRecord                  |
        // |                 |                                        | — set to 20 (0x0014) for this version.                         |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | instanceCount                          | The number of named instances defined in the font              |
        // |                 |                                        | (the number of records in the instances array).                |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | instanceSize                           | The size in bytes of each InstanceRecord                       |
        // |                 |                                        | — set to either axisCount * sizeof(Fixed) + 4,                 |
        // |                 |                                        | or to axisCount * sizeof(Fixed) + 6.                           |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        long startOffset = reader.BaseStream.Position;
        ushort major = reader.ReadUInt16();
        ushort minor = reader.ReadUInt16();
        ushort axesArrayOffset = reader.ReadOffset16();
        ushort reserved = reader.ReadUInt16();
        ushort axisCount = reader.ReadUInt16();
        ushort axisSize = reader.ReadUInt16();
        ushort instanceCount = reader.ReadUInt16();
        ushort instanceSize = reader.ReadUInt16();

        if (major != 1)
        {
            throw new NotSupportedException("Only version 1 of fvar table is supported");
        }

        var axesArray = new VariationAxisRecord[axisCount];
        for (int i = 0; i < axisCount; i++)
        {
            axesArray[i] = VariationAxisRecord.Load(reader, axesArrayOffset + (axisSize * i));
        }

        var instances = new InstanceRecord[instanceCount];
        long instancesOffset = reader.BaseStream.Position - startOffset;
        for (int i = 0; i < instanceCount; i++)
        {
            instances[i] = InstanceRecord.Load(reader, instancesOffset + (i * instanceSize), axisCount);
        }

        return new FVarTable(axisCount, axesArray, instances);
    }
}

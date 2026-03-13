// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the Font Variations Table `avar`.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/avar"/>
/// </summary>
internal class AVarTable : Table
{
    /// <summary>
    /// The table name identifier for the avar table.
    /// </summary>
    internal const string TableName = "avar";

    /// <summary>
    /// Initializes a new instance of the <see cref="AVarTable"/> class.
    /// </summary>
    /// <param name="axisCount">The number of variation axes.</param>
    /// <param name="segmentMaps">The segment maps array, one per axis.</param>
    public AVarTable(uint axisCount, SegmentMapRecord[] segmentMaps)
    {
        this.AxisCount = axisCount;
        this.SegmentMaps = segmentMaps;
    }

    /// <summary>
    /// Gets the number of variation axes for the font.
    /// </summary>
    public uint AxisCount { get; }

    /// <summary>
    /// Gets the segment maps array, one segment map for each axis, in the order of axes specified in the fvar table.
    /// </summary>
    public SegmentMapRecord[] SegmentMaps { get; }

    /// <summary>
    /// Loads the avar table from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="AVarTable"/>, or <see langword="null"/> if the table is not present.</returns>
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

    /// <summary>
    /// Loads the avar table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the avar table.</param>
    /// <returns>The <see cref="AVarTable"/>.</returns>
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

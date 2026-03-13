// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the font variations table `MVAR`.
/// The MVAR table is used in variable fonts to provide variations for global font metric values
/// such as ascender, descender, line gap, caret metrics, and other font-wide measurements.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
/// </summary>
internal class MVarTable : Table
{
    /// <summary>
    /// The table name identifier for the MVAR table.
    /// </summary>
    internal const string TableName = "MVAR";

    /// <summary>
    /// Initializes a new instance of the <see cref="MVarTable"/> class.
    /// </summary>
    /// <param name="itemVariationStore">The item variation store containing delta data.</param>
    /// <param name="valueRecords">The array of metric value records.</param>
    public MVarTable(ItemVariationStore itemVariationStore, MetricValueRecord[] valueRecords)
    {
        this.ItemVariationStore = itemVariationStore;
        this.ValueRecords = valueRecords;
    }

    /// <summary>
    /// Gets the item variation store containing the variation delta data.
    /// </summary>
    public ItemVariationStore ItemVariationStore { get; }

    /// <summary>
    /// Gets the array of metric value records, sorted by tag for binary search.
    /// </summary>
    public MetricValueRecord[] ValueRecords { get; }

    /// <summary>
    /// Loads the MVAR table from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="MVarTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static MVarTable? Load(FontReader reader)
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
    /// Loads the MVAR table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the MVAR table.</param>
    /// <returns>The <see cref="MVarTable"/>.</returns>
    public static MVarTable Load(BigEndianBinaryReader reader)
    {
        // MVAR — Metrics Variations Table
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | Type                     | Name                                     | Description                                        |
        // +==========================+==========================================+====================================================+
        // | uint16                   | majorVersion                             | Major version — set to 1.                          |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | uint16                   | minorVersion                             | Minor version — set to 0.                          |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | uint16                   | reserved                                 | Not used; set to 0.                                |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | uint16                   | valueRecordSize                          | Size in bytes of each value record.                |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | uint16                   | valueRecordCount                         | Number of value records.                           |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | Offset16                 | itemVariationStoreOffset                 | Offset to ItemVariationStore.                      |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        // | ValueRecord[]            | valueRecords[valueRecordCount]           | Array of value records.                            |
        // +--------------------------+------------------------------------------+----------------------------------------------------+
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();
        ushort reserved = reader.ReadUInt16();
        ushort valueRecordSize = reader.ReadUInt16();
        ushort valueRecordCount = reader.ReadUInt16();
        ushort itemVariationStoreOffset = reader.ReadOffset16();

        if (majorVersion != 1)
        {
            throw new NotSupportedException("Only version 1 of MVAR table is supported");
        }

        // Read the value records. Each is typically 8 bytes (Tag + outerIndex + innerIndex).
        MetricValueRecord[] valueRecords = new MetricValueRecord[valueRecordCount];
        for (int i = 0; i < valueRecordCount; i++)
        {
            long recordStart = reader.BaseStream.Position;
            uint tag = reader.ReadUInt32();
            ushort outerIndex = reader.ReadUInt16();
            ushort innerIndex = reader.ReadUInt16();
            valueRecords[i] = new MetricValueRecord(tag, outerIndex, innerIndex);

            // Skip any extra bytes if valueRecordSize > 8 (future compatibility).
            long consumed = reader.BaseStream.Position - recordStart;
            if (consumed < valueRecordSize)
            {
                reader.BaseStream.Position += valueRecordSize - consumed;
            }
        }

        // Load the ItemVariationStore.
        ItemVariationStore itemVariationStore = ItemVariationStore.Load(reader, itemVariationStoreOffset);

        return new MVarTable(itemVariationStore, valueRecords);
    }

    /// <summary>
    /// Finds the value record for the given tag using binary search.
    /// Returns true if found, with the outer and inner indices set.
    /// </summary>
    /// <param name="tag">The 4-byte metric tag to look up.</param>
    /// <param name="outerIndex">The outer index into the ItemVariationStore.</param>
    /// <param name="innerIndex">The inner index into the ItemVariationStore.</param>
    /// <returns>True if the tag was found; false otherwise.</returns>
    public bool TryGetIndices(Tag tag, out ushort outerIndex, out ushort innerIndex)
    {
        // ValueRecords are sorted by tag per the spec, so binary search is valid.
        int lo = 0;
        int hi = this.ValueRecords.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            Tag midTag = this.ValueRecords[mid].Tag;
            if (midTag == tag)
            {
                outerIndex = this.ValueRecords[mid].DeltaSetOuterIndex;
                innerIndex = this.ValueRecords[mid].DeltaSetInnerIndex;
                return true;
            }

            if (midTag.Value < tag.Value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        outerIndex = 0;
        innerIndex = 0;
        return false;
    }
}

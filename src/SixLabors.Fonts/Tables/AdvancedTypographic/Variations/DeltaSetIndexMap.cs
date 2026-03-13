// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Represents a single entry in a DeltaSetIndexMap, mapping a glyph ID to an outer/inner index pair
/// into an <see cref="ItemVariationStore"/>.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats"/>
/// </summary>
internal class DeltaSetIndexMap
{
    /// <summary>
    /// Mask for the low 4 bits of the entry format, giving the number of inner index bits minus one.
    /// </summary>
    private const int InnerIndexBitCountMask = 0x0F;

    /// <summary>
    /// Mask for bits 4-5 of the entry format, giving the entry size minus one.
    /// </summary>
    private const int MapEntrySizeMask = 0x30;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeltaSetIndexMap"/> class.
    /// </summary>
    /// <param name="outerIndex">The outer index into the ItemVariationStore.</param>
    /// <param name="innerIndex">The inner index into the ItemVariationStore.</param>
    public DeltaSetIndexMap(int outerIndex, int innerIndex)
    {
        this.OuterIndex = outerIndex;
        this.InnerIndex = innerIndex;
    }

    /// <summary>
    /// Gets the outer index into the ItemVariationStore (selects the ItemVariationData subtable).
    /// </summary>
    public int OuterIndex { get; }

    /// <summary>
    /// Gets the inner index into the ItemVariationStore (selects the delta set within the subtable).
    /// </summary>
    public int InnerIndex { get; }

    /// <summary>
    /// Loads an array of <see cref="DeltaSetIndexMap"/> entries from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The byte offset from the start of the stream to this map. If zero, no map is present.</param>
    /// <returns>The array of <see cref="DeltaSetIndexMap"/> entries, or <see langword="null"/> if the offset is zero.</returns>
    public static DeltaSetIndexMap[]? Load(BigEndianBinaryReader reader, long offset)
    {
        // This can be null if the offset is zero.
        if (offset == 0)
        {
            return null;
        }

        // DeltaSetIndexMap.
        // +-----------------+----------------------------------------+-----------------------------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                                       |
        // +=================+========================================+===================================================================================+
        // | uint8           | format                                 | DeltaSetIndexMap format. Either 0 or 1                                            |
        // +-----------------+----------------------------------------+-----------------------------------------------------------------------------------+
        // | uint8           | entryFormat                            | A packed field that describes the compressed representation of delta-set indices. |
        // +-----------------+----------------------------------------+-----------------------------------------------------------------------------------+
        // | uint16 or uin32 | mapCount                               | The number of mapping entries. uint16 for format0, uint32 for format 1            |
        // +-----------------+----------------------------------------+-----------------------------------------------------------------------------------+
        // | uint8           | mapData[variable]                      | The delta-set index mapping data.                                                 |
        // +-----------------+----------------------------------------+-----------------------------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        byte format = reader.ReadUInt8();
        byte entryFormat = reader.ReadUInt8();

        if (format is not (0 or 1))
        {
            throw new NotSupportedException("Only format 0 or 1 of DeltaSetIndexMap is supported");
        }

        // Format 0 uses uint16 for mapCount, format 1 uses uint32.
        int mapCount = format == 0 ? reader.ReadUInt16() : (int)reader.ReadUInt32();

        int entrySize = ((entryFormat & MapEntrySizeMask) >> 4) + 1;
        int innerBitCount = (entryFormat & InnerIndexBitCountMask) + 1;
        int innerIndexMask = (1 << innerBitCount) - 1;

        DeltaSetIndexMap[] deltaSetIndexMaps = new DeltaSetIndexMap[mapCount];
        for (int i = 0; i < mapCount; i++)
        {
            int entry = entrySize switch
            {
                1 => reader.ReadByte(),
                2 => (reader.ReadByte() << 8) | reader.ReadByte(),
                3 => (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte(),
                4 => (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte(),
                _ => throw new NotSupportedException("unsupported delta set index map"),
            };
            deltaSetIndexMaps[i] = new DeltaSetIndexMap(entry >> innerBitCount, entry & innerIndexMask);
        }

        return deltaSetIndexMaps;
    }
}

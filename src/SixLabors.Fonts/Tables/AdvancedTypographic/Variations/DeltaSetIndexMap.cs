// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

internal class DeltaSetIndexMap
{
    private const int InnerIndexBitCountMask = 0x0F;

    private const int MapEntrySizeMask = 0x30;

    public DeltaSetIndexMap(int outerIndex, int innerIndex)
    {
        this.OuterIndex = outerIndex;
        this.InnerIndex = innerIndex;
    }

    public int OuterIndex { get; }

    public int InnerIndex { get; }

    public static DeltaSetIndexMap[] Load(BigEndianBinaryReader reader, long offset)
    {
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
        ushort mapCount = reader.ReadUInt16();

        if (format is not 0 or 1)
        {
            throw new NotSupportedException("Only format 0 or 1 of DeltaSetIndexMap is supported");
        }

        int entrySize = ((entryFormat & MapEntrySizeMask) >> 4) + 1;
        int outerIndex = entrySize >> ((entryFormat & InnerIndexBitCountMask) + 1);
        int innerIndex = entrySize & ((1 << ((entryFormat & InnerIndexBitCountMask) + 1)) - 1);

        var deltaSetIndexMaps = new DeltaSetIndexMap[mapCount];
        for (int i = 0; i < mapCount; i++)
        {
            int entry;
            switch (entrySize)
            {
                case 1:
                    entry = reader.ReadByte();
                    break;
                case 2:
                    entry = (reader.ReadByte() << 8) | reader.ReadByte();
                    break;
                case 3:
                    entry = (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte();
                    break;
                case 4:
                    entry = (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte();
                    break;
                default:
                    throw new NotSupportedException("unsupported delta set index map");
            }

            deltaSetIndexMaps[i] = new DeltaSetIndexMap((ushort)(entry & innerIndex), (ushort)(entry >> outerIndex));
        }

        return deltaSetIndexMaps;
    }
}

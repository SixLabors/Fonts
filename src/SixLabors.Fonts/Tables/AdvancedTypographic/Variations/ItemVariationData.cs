// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    [DebuggerDisplay("ItemCount: {ItemCount}, WordDeltaCount: {WordDeltaCount}, RegionIndexCount: {RegionIndexes.Length}")]
    internal class ItemVariationData
    {
        private ItemVariationData(ushort itemCount, ushort wordDeltaCount, ushort[] regionIndices)
        {
            this.ItemCount = itemCount;
            this.WordDeltaCount = wordDeltaCount;
            this.RegionIndexes = regionIndices;
        }

        public ushort ItemCount { get; }

        public ushort WordDeltaCount { get; }

        public ushort[] RegionIndexes { get; }

        public static ItemVariationData Load(BigEndianBinaryReader reader, long offset)
        {
            // ItemVariationData
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | Type            | Name                                   | Description                                                    |
            // +=================+========================================+================================================================+
            // | uint16          | itemCount                              | The number of delta sets for distinct items.                   |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // | uint16          | wordDeltaCount                         | A packed field: the high bit is a flag.                        |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // + uint16          | regionIndexCount                       | The number of variation regions referenced.                    |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // + uint16          | regionIndexes[regionIndexCount]        | Array of indices into the variation region list for            |
            // +                 |                                        | the regions referenced by this item variation data table.      |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            // + DeltaSet        | deltaSets[itemCount]                   | Delta-set rows.                                                |
            // +-----------------+----------------------------------------+----------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);
            ushort itemCount = reader.ReadUInt16();
            ushort wordDeltaCount = reader.ReadUInt16();
            ushort regionIndexCount = reader.ReadUInt16();
            ushort[] regionIndexes = new ushort[regionIndexCount];
            for (int i = 0; i < regionIndexCount; i++)
            {
                regionIndexes[i] = reader.ReadUInt16();
            }

            // TODO: how to deal with delta sets?
            for (int i = 0; i < itemCount; i++)
            {
            }

            return new ItemVariationData(itemCount, wordDeltaCount, regionIndexes);
        }
    }
}

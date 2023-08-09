// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    [DebuggerDisplay("ItemCount: {ItemCount}, WordDeltaCount: {WordDeltaCount}, RegionIndexCount: {RegionIndexes.Length}")]
    internal sealed class ItemVariationData
    {
        /// <summary>
        /// Count of "word" deltas.
        /// </summary>
        private const int WordDeltaCountMask = 0x7FFF;

        /// <summary>
        /// Flag indicating that "word" deltas are long (int32).
        /// </summary>
        private const int LongWordsMask = 0x8000;

        private ItemVariationData(ushort itemCount, ushort wordDeltaCount, ushort[] regionIndices, uint[] longDeltas, ushort[] shortDeltas)
        {
            this.ItemCount = itemCount;
            this.WordDeltaCount = wordDeltaCount;
            this.RegionIndexes = regionIndices;
            this.LongDeltas = longDeltas;
            this.ShortDeltas = shortDeltas;

            this.Deltas = new uint[longDeltas.Length + shortDeltas.Length];
            int offset = 0;
            for (int i = 0; i < longDeltas.Length; i++)
            {
                this.Deltas[offset++] = longDeltas[i];
            }

            for (int i = 0; i < shortDeltas.Length; i++)
            {
                this.Deltas[offset++] = shortDeltas[i];
            }
        }

        public ushort ItemCount { get; }

        public ushort WordDeltaCount { get; }

        public ushort[] RegionIndexes { get; }

        public uint[] LongDeltas { get; }

        public ushort[] ShortDeltas { get; }

        public uint[] Deltas { get; }

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

            // The deltaSets array represents a logical two-dimensional table of delta values with itemCount rows and regionIndexCount columns.
            // Logically, each DeltaSet record has regionIndexCount number of elements. The elements are represented using long and short types.
            // These are either int16 and int8, or int32 and int16, according to whether the LONG_WORDS flag is set.
            // The delta array has a sequence of deltas using the long type followed by a sequence of deltas using the short type.
            bool longWords = (wordDeltaCount & LongWordsMask) != 0;
            int wordDeltas = wordDeltaCount & WordDeltaCountMask;
            uint[] longDeltas = new uint[wordDeltas];
            for (int i = 0; i < wordDeltas; i++)
            {
                longDeltas[i] = longWords ? reader.ReadUInt32() : reader.ReadUInt16();
            }

            int remaining = regionIndexCount - wordDeltas;
            ushort[] shortDeltas = new ushort[remaining];
            for (int i = 0; i < remaining; i++)
            {
                shortDeltas[i] = longWords ? reader.ReadUInt16() : reader.ReadUInt8();
            }

            return new ItemVariationData(itemCount, wordDeltaCount, regionIndexes, longDeltas, shortDeltas);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(this.ItemCount, this.WordDeltaCount, this.RegionIndexes);
    }
}

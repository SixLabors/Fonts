// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.General.Kern
{
    internal class Format0SubTable : KerningSubTable
    {
        private readonly KearningPair[] pairs;

        public Format0SubTable(KearningPair[] pairs, KerningCoverage coverage)
            : base(coverage)
        {
            this.pairs = pairs;
        }

        public static Format0SubTable Load(BinaryReader reader, KerningCoverage coverage)
        {
            // Type   | Field         | Description
            // -------|---------------|--------------------------------------------------------
            // uint16 | nPairs        | This gives the number of kerning pairs in the table.
            // uint16 | searchRange   | The largest power of two less than or equal to the value of nPairs, multiplied by the size in bytes of an entry in the table.
            // uint16 | entrySelector | This is calculated as log2 of the largest power of two less than or equal to the value of nPairs.This value indicates how many iterations of the search loop will have to be made. (For example, in a list of eight items, there would have to be three iterations of the loop).
            // uint16 | rangeShift    | The value of nPairs minus the largest power of two less than or equal to nPairs, and then multiplied by the size in bytes of an entry in the table.
            ushort pairCount = reader.ReadUInt16();
            ushort searchRange = reader.ReadUInt16();
            ushort entrySelector = reader.ReadUInt16();
            ushort rangeShift = reader.ReadUInt16();

            Kern.KearningPair[] pairs = new KearningPair[pairCount];
            for (int i = 0; i < pairCount; i++)
            {
                pairs[i] = KearningPair.Read(reader);
            }

            return new Format0SubTable(pairs, coverage);
        }

        protected override bool TryGetOffset(ushort index1, ushort index2, out short offset)
        {
            int index = Array.BinarySearch(this.pairs, new KearningPair(index1, index2, 0));
            if (index >= 0)
            {
                KearningPair pair = this.pairs[index];
                offset = pair.Offset;
                return true;
            }

            offset = 0;
            return false;
        }
    }
}

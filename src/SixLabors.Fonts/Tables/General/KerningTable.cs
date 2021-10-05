// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.General.Kern;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class KerningTable : Table
    {
        internal const string TableName = "kern";
        private readonly KerningSubTable[] kerningSubTable;

        public KerningTable(KerningSubTable[] kerningSubTable)
            => this.kerningSubTable = kerningSubTable;

        public static KerningTable Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                // this table is optional.
                return new KerningTable(Array.Empty<KerningSubTable>());
            }

            using (binaryReader)
            {
                // Move to start of table.
                return Load(binaryReader);
            }
        }

        public static KerningTable Load(BigEndianBinaryReader reader)
        {
            // +--------+---------+-------------------------------------------+
            // | Type   | Field   | Description                               |
            // +========+=========+===========================================+
            // | uint16 | version | Table version number(0)                   |
            // +--------+---------+-------------------------------------------+
            // | uint16 | nTables | Number of subtables in the kerning table. |
            // +--------+---------+-------------------------------------------+
            ushort version = reader.ReadUInt16();
            ushort subTableCount = reader.ReadUInt16();

            var tables = new List<KerningSubTable>(subTableCount);
            for (int i = 0; i < subTableCount; i++)
            {
                var t = KerningSubTable.Load(reader); // returns null for unknown/supported table format
                if (t != null)
                {
                    tables.Add(t);
                }
            }

            return new KerningTable(tables.ToArray());
        }

        public void UpdatePositions(IFontMetrics fontMetrics, GlyphPositioningCollection collection, ushort left, ushort right)
        {
            ushort previous = collection[left][0];
            ushort current = collection[right][0];
            if (previous == 0 || current == 0)
            {
                return;
            }

            Vector2 result = Vector2.Zero;
            foreach (KerningSubTable sub in this.kerningSubTable)
            {
                sub.ApplyOffset(previous, current, ref result);
            }

            collection.Advance(fontMetrics, right, current, (short)result.X, (short)result.Y);
        }
    }
}

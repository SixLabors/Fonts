// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class CMapTable : Table
    {
        internal const string TableName = "cmap";

        private static readonly Dictionary<PlatformIDs, int> PreferredPlatformOrder = new Dictionary<PlatformIDs, int>
        {
            [PlatformIDs.Windows] = 0,
            [PlatformIDs.Unicode] = 1,
            [PlatformIDs.Macintosh] = 2,
        };

        private readonly Format14SubTable[] format14SubTables = Array.Empty<Format14SubTable>();

        public CMapTable(IEnumerable<CMapSubTable> tables)
        {
            this.Tables = tables.OrderBy(t => GetPreferredPlatformOrder(t.Platform)).ToArray();
            this.format14SubTables = this.Tables.OfType<Format14SubTable>().ToArray();
        }

        internal CMapSubTable[] Tables { get; }

        private static int GetPreferredPlatformOrder(PlatformIDs platform)
            => PreferredPlatformOrder.TryGetValue(platform, out var order) ? order : int.MaxValue;

        public bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
        {
            foreach (CMapSubTable t in this.Tables)
            {
                // Keep looking until we have an index that's not the fallback.
                if (t.TryGetGlyphId(codePoint, out glyphId))
                {
                    return true;
                }
            }

            glyphId = 0;
            return false;
        }

        public bool TryGetGlyphId(CodePoint codePoint, CodePoint nextCodePoint, out ushort glyphId, out bool skipNextCodePoint)
        {
            skipNextCodePoint = false;
            if (this.TryGetGlyphId(codePoint, out glyphId))
            {
                // If there is a second codepoint, we are asked whether this is an UVS sequence
                // - If true, return a glyph Id.
                // - Otherwise, return 0.
                if (nextCodePoint != default && this.format14SubTables.Length > 0)
                {
                    foreach (Format14SubTable? cmap14 in this.format14SubTables)
                    {
                        ushort pairGlyphId = cmap14.CharacterPairToGlyphId(codePoint, glyphId, nextCodePoint);
                        if (pairGlyphId > 0)
                        {
                            glyphId = pairGlyphId;
                            skipNextCodePoint = true;
                            return true;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static CMapTable Load(FontReader reader)
        {
            using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
            return Load(binaryReader);
        }

        public static CMapTable Load(BigEndianBinaryReader reader)
        {
            ushort version = reader.ReadUInt16();
            ushort numTables = reader.ReadUInt16();

            var encodings = new EncodingRecord[numTables];
            for (int i = 0; i < numTables; i++)
            {
                encodings[i] = EncodingRecord.Read(reader);
            }

            // foreach encoding we move forward looking for th subtables
            var tables = new List<CMapSubTable>(numTables);
            foreach (IGrouping<uint, EncodingRecord> encoding in encodings.GroupBy(x => x.Offset))
            {
                reader.Seek(encoding.Key, System.IO.SeekOrigin.Begin);

                // Subtable format.
                switch (reader.ReadUInt16())
                {
                    case 0:
                        tables.AddRange(Format0SubTable.Load(encoding, reader));
                        break;
                    case 4:
                        tables.AddRange(Format4SubTable.Load(encoding, reader));
                        break;
                    case 12:
                        tables.AddRange(Format12SubTable.Load(encoding, reader));
                        break;
                    case 14:
                        tables.AddRange(Format14SubTable.Load(encoding, reader));
                        break;
                }
            }

            return new CMapTable(tables);
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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

        public CMapTable(IEnumerable<CMapSubTable> tables)
        {
            this.Tables = tables.OrderBy(t => GetPreferredPlatformOrder(t.Platform)).ToArray();
        }

        internal CMapSubTable[] Tables { get; }

        private static int GetPreferredPlatformOrder(PlatformIDs platform)
            => PreferredPlatformOrder.TryGetValue(platform, out var order) ? order : int.MaxValue;

        public bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
        {
            foreach (CMapSubTable t in this.Tables)
            {
                // keep looking until we have an index that's not the fallback.
                if (t.TryGetGlyphId(codePoint, out glyphId))
                {
                    return true;
                }
            }

            glyphId = 0;
            return false;
        }

        public static CMapTable Load(FontReader reader)
        {
            using (BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader);
            }
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
                }
            }

            return new CMapTable(tables);
        }
    }
}

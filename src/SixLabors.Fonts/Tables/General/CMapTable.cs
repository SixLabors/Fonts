// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class CMapTable : Table
    {
        private const string TableName = "cmap";
        private readonly CMapSubTable[] platformTables;

        public CMapTable(CMapSubTable[] tables)
        {
            this.Tables = tables;

            // lets just pick the best table for us.. lets jsut treat everything as windows and get the format 4 if possible
            var tbls = new List<CMapSubTable>();
            foreach (CMapSubTable t in this.Tables)
            {
                if (t != null)
                {
                    if (t.Platform == PlatformIDs.Windows)
                    {
                        tbls.Add(t);
                    }
                }
            }

            this.platformTables = tbls.ToArray();
        }

        internal CMapSubTable[] Tables { get; }

        public bool TryGetGlyphId(int codePoint, out ushort glyphId)
        {
            // use the best match only
            foreach (CMapSubTable t in this.platformTables)
            {
                // keep looking until we have an index thats not the fallback.
                if (t.TryGetGlyphId(codePoint, out glyphId))
                {
                    return true;
                }
            }

            // didn't have a windows match just use any and hope for the best
            foreach (CMapSubTable t in this.Tables)
            {
                // keep looking until we have an index thats not the fallback.
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
            using (BinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader);
            }
        }

        public static CMapTable Load(BinaryReader reader)
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
            foreach (IGrouping<uint, EncodingRecord> encoding in encodings.Where(x => x.PlatformID == PlatformIDs.Windows).GroupBy(x => x.Offset))
            {
                reader.Seek(encoding.Key, System.IO.SeekOrigin.Begin);

                ushort subTypeTableFormat = reader.ReadUInt16();

                switch (subTypeTableFormat)
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

            return new CMapTable(tables.ToArray());
        }
    }
}

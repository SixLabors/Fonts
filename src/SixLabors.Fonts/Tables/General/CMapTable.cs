using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class CMapTable : Table
    {
        const string TableName = "cmap";
        private readonly CMapSubTable table;

        internal CMapSubTable[] Tables { get; }

        public CMapTable(CMapSubTable[] tables)
        {
            this.Tables = tables;
            // lets just pick the best table for us.. lets jsut treat everything as windows and get the format 4 if possible

            CMapSubTable table = null;
            foreach (var t in this.Tables)
            {
                if (t.Platform == PlatformIDs.Windows)
                {
                    ushort format = table?.Format ?? 0;
                    if(t.Format > format)
                    {
                        table = t;
                    }
                }
            }
            this.table = table;
        }

        public ushort GetGlyphId(char character)
        {
            // use the best match only
            if(table != null)
            {
                return table.GetGlyphId(character);
            }

            // didn't have a windows match just use any and hope for the best
            foreach (var t in this.Tables)
            {
                // keep looking until we have an index thats not the fallback.
                var index = t.GetGlyphId(character);
                if (index > 0)
                {
                    return index;
                }
            }

            return 0;
        }


        public static CMapTable Load(FontReader reader)
        {   
            return Load(reader.GetReaderAtTablePosition(TableName));
        }

        public static CMapTable Load(BinaryReader reader)
        {
            var startOfTable = reader.BaseStream.Position;
            ushort version = reader.ReadUInt16();
            ushort numTables = reader.ReadUInt16();

            EncodingRecord[] encodings = new EncodingRecord[numTables];
            for (var i = 0; i < numTables; i++)
            {
                encodings[i] = EncodingRecord.Read(reader);
            }

            // foreach encoding we move forward looking for th subtables
            SixLabors.Fonts.Tables.General.CMap.CMapSubTable[] tables = new SixLabors.Fonts.Tables.General.CMap.CMapSubTable[numTables];
            for (var i = 0; i < numTables; i++)
            {
                var encoding = encodings[i];
                var finalPostion = startOfTable + encoding.Offset;
                var seekDistance = finalPostion - reader.BaseStream.Position;
                reader.Seek((int)seekDistance, System.IO.SeekOrigin.Current);

                var subTypeTableFormat = reader.ReadUInt16();

                switch (subTypeTableFormat)
                {
                    case 0:
                        tables[i] = SixLabors.Fonts.Tables.General.CMap.Format0SubTable.Load(encoding, reader);
                        break;
                    case 4:
                        tables[i] = SixLabors.Fonts.Tables.General.CMap.Format4SubTable.Load(encoding, reader);
                        break;
                }
            }

            return new CMapTable(tables);
        }
    }
}
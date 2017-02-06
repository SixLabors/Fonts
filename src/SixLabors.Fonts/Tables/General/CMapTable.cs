using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName("cmap")]
    internal class CMapTable : Table
    {
        internal CMapSubTable[] tables;

        public CMapTable(CMapSubTable[] tables)
        {
            this.tables = tables;
        }

        public ushort GetGlyphId(char character, PlatformIDs platform = PlatformIDs.Windows)
        {
            // find the most efficient way for storng this lookup after load
            
            foreach(var t in tables)
            {
                if(t.Platform == platform)
                {
                    //keep looking until we have an index thats not the fallback.
                    var index = t.GetGlyphId(character);
                    if(index > 0)
                    {
                        return index;
                    }
                }
            }

            return 0;
        }

        public static CMapTable Load(BinaryReader reader)
        {
            var startOfTable = reader.BaseStream.Position;
            ushort version = reader.ReadUInt16();
            ushort numTables = reader.ReadUInt16();

            EncodingRecord[] encodings = new EncodingRecord[numTables];
            for(var i = 0; i< numTables; i++)
            {
                encodings[i] = EncodingRecord.Read(reader);
            }

            //// foreach encoding we move forward looking for th subtables
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
                }
            }

            return new CMapTable(tables);
        }
    }
}
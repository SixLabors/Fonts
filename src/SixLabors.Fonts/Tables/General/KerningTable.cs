using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class KerningTable : Table
    {
        private const string TableName = "kern";
        private KerningSubTable[] kerningSubTable;

        public KerningTable(KerningSubTable[] kerningSubTable)
        {
            this.kerningSubTable = kerningSubTable;
        }

        public static KerningTable Load(FontReader reader)
        {
            var binaryReader = reader.GetReaderAtTablePosition(TableName);
            if (binaryReader == null)
            {
                // this table is optional.
                return new KerningTable(new KerningSubTable[0]);
            }

            // move to start of table
            return Load(binaryReader);
        }

        public static KerningTable Load(BinaryReader reader)
        {
            // Type   | Field    | Description
            // -------|----------|-----------------------------------------
            // uint16 | version  | Table version number(0)
            // uint16 | nTables  | Number of subtables in the kerning table.
            var version = reader.ReadUInt16();
            var subtableCount = reader.ReadUInt16();

            List<Kern.KerningSubTable> tables = new List<Kern.KerningSubTable>(subtableCount);
            for (var i = 0; i < subtableCount; i++)
            {
                var t = KerningSubTable.Load(reader); // returns null for unknown/supported table format
                if (t != null)
                {
                    tables.Add(t);
                }
            }

            return new KerningTable(tables.ToArray());
        }

        public Vector2 GetOffset(ushort left, ushort right)
        {
            var result = Vector2.Zero;
            foreach (var sub in this.kerningSubTable)
            {
                sub.ApplyOffset(left, right, ref result);
            }

            return result;
        }
    }
}
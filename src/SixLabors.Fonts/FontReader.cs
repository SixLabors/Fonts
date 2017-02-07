using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;

using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    internal class FontReader
    {
        internal FontReader(Stream stream, Type[] tableTypes, TableLoader loader)
        {
            BinaryReader reader;
            var startOfFilePosition = stream.Position;

            reader = new BinaryReader(stream);

            // we should immediately read the table header to learn which tables we have and what order they are in
            uint version = reader.ReadUInt32();
            this.OutlineType = (OutlineTypes)version;
            ushort tableCount = reader.ReadUInt16();
            ushort searchRange = reader.ReadUInt16();
            ushort entrySelector = reader.ReadUInt16();
            ushort rangeShift = reader.ReadUInt16();

            TableHeader[] headers = new TableHeader[tableCount];
            for (int i = 0; i < tableCount; i++)
            {
                headers[i] = TableHeader.Read(reader);
            }

            var tablesToLoad = tableTypes?.Select(loader.GetTag).Where(x => x != null).ToArray();

            var tableCountToLoad = tablesToLoad?.Length ?? tableCount;

            List<Table> tables = new List<Table>(tableCountToLoad);

            foreach (var header in headers.OrderBy(x => x.Offset))
            {
                if (tablesToLoad == null || tablesToLoad.Contains(header.Tag))
                {
                    var startOfString = header.Offset + startOfFilePosition;
                    var diff = startOfString - reader.BaseStream.Position;

                    // only seek forward, if we find issues with this we will consume forwards as the idea is we will never need to backtrack
                    reader.BaseStream.Seek(diff, SeekOrigin.Current);

                    Table t = loader.Load(header.Tag, reader);
                    tables.Add(t);
                }
            }

            this.Tables = ImmutableArray.Create(tables.ToArray());
        }

        public OutlineTypes OutlineType { get; }

        public ImmutableArray<Table> Tables { get; }

        public FontReader(Stream stream)
            : this(stream, null)
        {
        }

        public FontReader(Stream stream, params Type[] tableToLoad)
            : this(stream, tableToLoad, TableLoader.Default)
        {
        }

        public TTableType GetTable<TTableType>()
            where TTableType : Table
        {
            foreach (var table in this.Tables)
            {
                if (table is TTableType)
                {
                    return (TTableType)table;
                }
            }

            return null;
        }

        public enum OutlineTypes : uint
        {
            TrueType = 0x00010000,
            CFF = 0x4F54544F
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    internal class FontReader
    {
        private readonly Dictionary<Type, Table> loadedTables = new Dictionary<Type, Table>();

        private readonly TableLoader loader;

        private readonly BinaryReader reader;

        public IReadOnlyDictionary<string, TableHeader> Headers { get; }

        internal FontReader(Stream stream, TableLoader loader)
        {
            this.loader = loader;
            var startOfFilePosition = stream.Position;

            this.reader = new BinaryReader(stream);

            // we should immediately read the table header to learn which tables we have and what order they are in
            uint version = this.reader.ReadUInt32();
            this.OutlineType = (OutlineTypes)version;
            ushort tableCount = this.reader.ReadUInt16();
            ushort searchRange = this.reader.ReadUInt16();
            ushort entrySelector = this.reader.ReadUInt16();
            ushort rangeShift = this.reader.ReadUInt16();

            var allknowntables = loader.RegisterdTags().ToArray();

            Dictionary<string, TableHeader> headers = new Dictionary<string, Tables.TableHeader>(tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                var tbl = TableHeader.Read(this.reader);
                if (allknowntables.Contains(tbl.Tag))
                {
                    headers.Add(tbl.Tag, tbl);
                }
            }

            this.Headers = new ReadOnlyDictionary<string, TableHeader>(headers);
        }

        public OutlineTypes OutlineType { get; }

        public FontReader(Stream stream)
            : this(stream, TableLoader.Default)
        {
        }

        public virtual TTableType GetTable<TTableType>()
            where TTableType : Table
        {
            if (!this.loadedTables.ContainsKey(typeof(TTableType)))
            {
                this.loadedTables.Add(typeof(TTableType), this.loader.Load<TTableType>(this));
            }

            return (TTableType)this.loadedTables[typeof(TTableType)];
        }

        public virtual TableHeader GetHeader(string tag)
        {
            if (this.Headers.ContainsKey(tag))
            {
                return this.Headers[tag];
            }

            return null;
        }

        public virtual BinaryReader GetReader()
        {
            return this.reader;
        }

        public virtual BinaryReader GetReaderAtTablePosition(string tableName)
        {
            var header = this.GetHeader(tableName);
            this.reader.Seek(header);
            return this.reader;
        }

        public virtual BinaryReader GetReaderAtTablePosition(TableHeader header)
        {
            this.reader.Seek(header);
            return this.reader;
        }

        public enum OutlineTypes : uint
        {
            TrueType = 0x00010000,
            CFF = 0x4F54544F
        }
    }
}

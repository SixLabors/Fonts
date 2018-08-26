// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    internal class FontReader
    {
        private readonly Dictionary<Type, Table> loadedTables = new Dictionary<Type, Table>();

        private readonly Stream stream;
        private readonly TableLoader loader;

        internal FontReader(Stream stream, TableLoader loader)
        {
            this.loader = loader;

            Func<BinaryReader, TableHeader> loadHeader = TableHeader.Read;
            long startOfFilePosition = stream.Position;

            this.stream = stream;
            var reader = new BinaryReader(stream, true);

            // we should immediately read the table header to learn which tables we have and what order they are in
            uint version = reader.ReadUInt32();
            ushort tableCount = 0;
            if (version == 0x774F4646)
            {
                // this is a woff file
                // WOFFHeader
                // UInt32 | signature      | 0x774F4646 'wOFF'
                // UInt32 | flavor         | The "sfnt version" of the input font.
                // UInt32 | length         | Total size of the WOFF file.
                // UInt16 | numTables      | Number of entries in directory of font tables.
                // UInt16 | reserved       | Reserved; set to zero.
                // UInt32 | totalSfntSize  | Total size needed for the uncompressed font data, including the sfnt header, directory, and font tables(including padding).
                // UInt16 | majorVersion   | Major version of the WOFF file.
                // UInt16 | minorVersion   | Minor version of the WOFF file.
                // UInt32 | metaOffset     | Offset to metadata block, from beginning of WOFF file.
                // UInt32 | metaLength     | Length of compressed metadata block.
                // UInt32 | metaOrigLength | Uncompressed size of metadata block.
                // UInt32 | privOffset     | Offset to private data block, from beginning of WOFF file.
                // UInt32 | privLength     | Length of private data block.
                uint flavor = reader.ReadUInt32();
                this.OutlineType = (OutlineTypes)flavor;
                uint length = reader.ReadUInt32();
                tableCount = reader.ReadUInt16();
                ushort reserved = reader.ReadUInt16();
                uint totalSfntSize = reader.ReadUInt32();
                ushort majorVersion = reader.ReadUInt16();
                ushort minorVersion = reader.ReadUInt16();
                uint metaOffset = reader.ReadUInt32();
                uint metaLength = reader.ReadUInt32();
                uint metaOrigLength = reader.ReadUInt32();
                uint privOffset = reader.ReadUInt32();
                uint privLength = reader.ReadUInt32();
                this.CompressedTableData = true;
                loadHeader = WoffTableHeader.Read;
            }
            else
            {
                // this is a standard *.otf file
                this.OutlineType = (OutlineTypes)version;
                tableCount = reader.ReadUInt16();
                ushort searchRange = reader.ReadUInt16();
                ushort entrySelector = reader.ReadUInt16();
                ushort rangeShift = reader.ReadUInt16();
                this.CompressedTableData = false;
            }

            if (this.OutlineType != OutlineTypes.TrueType)
            {
                throw new Exceptions.InvalidFontFileException("Invalid glyph format, only TTF glyph outlines supported.");
            }

            var headers = new Dictionary<string, TableHeader>(tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                TableHeader tbl = loadHeader(reader);
                headers.Add(tbl.Tag, tbl);
            }

            this.Headers = new ReadOnlyDictionary<string, TableHeader>(headers);
        }

        public FontReader(Stream stream)
            : this(stream, TableLoader.Default)
        {
        }

        public IReadOnlyDictionary<string, TableHeader> Headers { get; }

        public bool CompressedTableData { get; }

        public OutlineTypes OutlineType { get; }

        public virtual TTableType GetTable<TTableType>()
            where TTableType : Table
        {
            if (this.loadedTables.TryGetValue(typeof(TTableType), out Table table))
            {
                return (TTableType)table;
            }
            else
            {
                table = this.loader.Load<TTableType>(this);

                this.loadedTables.Add(typeof(TTableType), table);
            }

            return (TTableType)table;
        }

        public virtual TableHeader GetHeader(string tag)
        {
            return this.Headers.TryGetValue(tag, out TableHeader header)
                ? header
                : null;
        }

        public virtual BinaryReader GetReaderAtTablePosition(string tableName)
        {
            TableHeader header = this.GetHeader(tableName);
            return header?.CreateReader(this.stream);
        }

        internal enum OutlineTypes : uint
        {
            TrueType = 0x00010000,
            CFF = 0x4F54544F
        }
    }
}
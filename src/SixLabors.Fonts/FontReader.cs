// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    internal sealed class FontReader
    {
        private readonly Stream stream;
        private readonly Dictionary<Type, Table> loadedTables = new Dictionary<Type, Table>();
        private readonly TableLoader loader;

        internal FontReader(Stream stream, TableLoader loader)
        {
            this.loader = loader;

            Func<BigEndianBinaryReader, TableHeader> loadHeader = TableHeader.Read;
            long startOfFilePosition = stream.Position;

            this.stream = stream;
            var reader = new BigEndianBinaryReader(stream, true);

            // we should immediately read the table header to learn which tables we have and what order they are in
            uint version = reader.ReadUInt32();
            ushort tableCount;
            if (version == 0x774F4646)
            {
                // This is a woff file.
                this.TableFormat = TableFormat.Woff;

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
            else if (version == 0x774F4632)
            {
                // This is a woff2 file.
                this.TableFormat = TableFormat.Woff2;

#if NETSTANDARD2_0 || NETSTANDARD1_3
                throw new NotSupportedException("Brotli compression is not available and is required for decoding woff2");
#else

                uint flavor = reader.ReadUInt32();
                this.OutlineType = (OutlineTypes)flavor;
                uint length = reader.ReadUInt32();
                tableCount = reader.ReadUInt16();
                ushort reserved = reader.ReadUInt16();
                uint totalSfntSize = reader.ReadUInt32();
                uint totalCompressedSize = reader.ReadUInt32();
                ushort majorVersion = reader.ReadUInt16();
                ushort minorVersion = reader.ReadUInt16();
                uint metaOffset = reader.ReadUInt32();
                uint metaLength = reader.ReadUInt32();
                uint metaOrigLength = reader.ReadUInt32();
                uint privOffset = reader.ReadUInt32();
                uint privLength = reader.ReadUInt32();
                this.CompressedTableData = true;
                this.Headers = Woff2Utils.ReadWoff2Headers(reader, tableCount);

                byte[] compressedBuffer = reader.ReadBytes((int)totalCompressedSize);
                var decompressedStream = new MemoryStream();
                using var input = new MemoryStream(compressedBuffer);
                using var decompressor = new BrotliStream(input, CompressionMode.Decompress);
                decompressor.CopyTo(decompressedStream);
                decompressedStream.Position = 0;
                this.stream.Dispose();
                this.stream = decompressedStream;
                return;
#endif
            }
            else
            {
                // This is a standard *.otf file (this is named the Offset Table).
                this.TableFormat = TableFormat.Otf;

                this.OutlineType = (OutlineTypes)version;
                tableCount = reader.ReadUInt16();
                ushort searchRange = reader.ReadUInt16();
                ushort entrySelector = reader.ReadUInt16();
                ushort rangeShift = reader.ReadUInt16();
                this.CompressedTableData = false;
            }

            if (this.OutlineType != OutlineTypes.TrueType)
            {
                throw new InvalidFontFileException("Invalid glyph format, only TTF glyph outlines supported.");
            }

            var headers = new Dictionary<string, TableHeader>(tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                TableHeader tbl = loadHeader(reader);
                headers[tbl.Tag] = tbl;
            }

            this.Headers = new ReadOnlyDictionary<string, TableHeader>(headers);
        }

        public FontReader(Stream stream)
            : this(stream, TableLoader.Default)
        {
        }

        internal enum OutlineTypes : uint
        {
            TrueType = 0x00010000,
            CFF = 0x4F54544F
        }

        public TableFormat TableFormat { get; }

        public IReadOnlyDictionary<string, TableHeader> Headers { get; }

        public bool CompressedTableData { get; }

        public OutlineTypes OutlineType { get; }

        public TTableType? TryGetTable<TTableType>()
            where TTableType : Table
        {
            if (this.loadedTables.TryGetValue(typeof(TTableType), out Table table))
            {
                return (TTableType)table;
            }
            else
            {
                TTableType? loadedTable = this.loader.Load<TTableType>(this);
                if (loadedTable is null)
                {
                    return null;
                }

                table = loadedTable;
                this.loadedTables.Add(typeof(TTableType), loadedTable);
            }

            return (TTableType)table;
        }

        public TTableType GetTable<TTableType>()
          where TTableType : Table
        {
            TTableType? tbl = this.TryGetTable<TTableType>();

            if (tbl is null)
            {
                string tag = this.loader.GetTag<TTableType>();
                throw new MissingFontTableException($"Table '{tag}' is missing", tag);
            }

            return tbl;
        }

        public TableHeader? GetHeader(string tag)
            => this.Headers.TryGetValue(tag, out TableHeader header)
                ? header
                : null;

        public BigEndianBinaryReader GetReaderAtTablePosition(string tableName)
        {
            if (this.TryGetReaderAtTablePosition(tableName, out BigEndianBinaryReader? reader))
            {
                throw new InvalidFontTableException("Unable to find table", tableName);
            }

            return reader;
        }

        public bool TryGetReaderAtTablePosition(string tableName, out BigEndianBinaryReader? reader)
        {
            TableHeader? header = this.GetHeader(tableName);
            if (header == null)
            {
                reader = null;
                return false;
            }

            reader = header?.CreateReader(this.stream);
            if (reader == null)
            {
                return false;
            }

            return true;
        }
    }
}

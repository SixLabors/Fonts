// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.Woff;

namespace SixLabors.Fonts;

internal sealed class FontReader : IDisposable
{
    private readonly Stream stream;
    private readonly Dictionary<Type, Table> loadedTables = new();
    private readonly TableLoader loader;

    private readonly bool isOwnedStream;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontReader"/> class.
    /// Reads the table directory and prepares the data source for table reads.
    /// </summary>
    /// <param name="stream">The stream that contains the font.</param>
    /// <param name="loader">The loader that maps font table types to table tags.</param>
    internal FontReader(Stream stream, TableLoader loader)
    {
        this.loader = loader;

        Func<BigEndianBinaryReader, TableHeader> loadHeader = TableHeader.Read;

        this.stream = stream;
        using BigEndianBinaryReader reader = new(stream, true);

        // The first four bytes identify the font container.
        uint version = reader.ReadUInt32();
        ushort tableCount;
        if (version == 0x774F4646)
        {
            // 'wOFF' identifies WOFF 1.
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
            this.OutlineType = (OutlineType)flavor;
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
            // 'wOF2' identifies WOFF 2.
            this.TableFormat = TableFormat.Woff2;

            uint flavor = reader.ReadUInt32();
            this.OutlineType = (OutlineType)flavor;
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

            this.isOwnedStream = true;

            // WOFF 2 stores all tables in one Brotli stream. The directory
            // offsets refer to the decompressed data that this reader owns.
            byte[] compressedBuffer = reader.ReadBytes((int)totalCompressedSize);
            MemoryStream decompressedStream = new();
            using MemoryStream input = new(compressedBuffer);
            using BrotliStream decompressor = new(input, CompressionMode.Decompress);
            decompressor.CopyTo(decompressedStream);
            decompressedStream.Position = 0;
            this.stream = decompressedStream;
            return;
        }
        else
        {
            // This is a standard *.otf file (this is named the Offset Table).
            this.TableFormat = TableFormat.Otf;

            this.OutlineType = (OutlineType)version;
            tableCount = reader.ReadUInt16();
            ushort searchRange = reader.ReadUInt16();
            ushort entrySelector = reader.ReadUInt16();
            ushort rangeShift = reader.ReadUInt16();
            this.CompressedTableData = false;
        }

        Dictionary<string, TableHeader> headers = new(tableCount);
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

    public TableFormat TableFormat { get; }

    public IReadOnlyDictionary<string, TableHeader> Headers { get; }

    public bool CompressedTableData { get; }

    public OutlineType OutlineType { get; }

    public TTableType? TryGetTable<TTableType>()
        where TTableType : Table
    {
        if (this.loadedTables.TryGetValue(typeof(TTableType), out Table? table))
        {
            return (TTableType)table;
        }

        TTableType? loadedTable = this.loader.Load<TTableType>(this);
        if (loadedTable is null)
        {
            return null;
        }

        table = loadedTable;
        this.loadedTables.Add(typeof(TTableType), loadedTable);

        return (TTableType)table;
    }

    public TTableType GetTable<TTableType>()
      where TTableType : Table
    {
        TTableType? tbl = this.TryGetTable<TTableType>();

        if (tbl is null)
        {
            string tag = this.loader.GetTag<TTableType>();
            throw new MissingFontTableException($"Table '{tag}' is missing", tag!);
        }

        return tbl;
    }

    public TableHeader? GetHeader(string tag)
        => this.Headers.TryGetValue(tag, out TableHeader? header)
            ? header
            : null;

    public BigEndianBinaryReader GetReaderAtTablePosition(string tableName)
    {
        if (!this.TryGetReaderAtTablePosition(tableName, out BigEndianBinaryReader? reader))
        {
            throw new InvalidFontTableException($"Unable to find table {tableName}", tableName);
        }

        return reader!;
    }

    /// <summary>
    /// Opens a reader at the start of the requested table.
    /// </summary>
    /// <param name="tableName">The tag of the requested table.</param>
    /// <param name="reader">The reader on success.</param>
    /// <returns>Whether the table directory contains the requested table.</returns>
    public bool TryGetReaderAtTablePosition(string tableName, [NotNullWhen(returnValue: true)] out BigEndianBinaryReader? reader)
        => this.TryGetReaderAtTablePosition(tableName, out reader, out _);

    /// <summary>
    /// Opens a reader at the start of the requested table and returns its directory entry.
    /// </summary>
    /// <param name="tableName">The tag of the requested table.</param>
    /// <param name="reader">The reader on success.</param>
    /// <param name="header">The table directory entry on success.</param>
    /// <returns>Whether the table directory contains the requested table.</returns>
    public bool TryGetReaderAtTablePosition(string tableName, [NotNullWhen(returnValue: true)] out BigEndianBinaryReader? reader, [NotNullWhen(returnValue: true)] out TableHeader? header)
    {
        header = this.GetHeader(tableName);
        if (header is null)
        {
            reader = null;
            return false;
        }

        reader = header.CreateReader(this.stream);
        return true;
    }

    /// <summary>
    /// Opens a reader that can read only the requested table's bytes.
    /// </summary>
    /// <param name="tableName">The tag of the requested table.</param>
    /// <param name="reader">The table reader on success.</param>
    /// <returns>Whether the reader opened the complete table.</returns>
    public bool TryGetBoundedReaderAtTablePosition(string tableName, [NotNullWhen(returnValue: true)] out BigEndianBinaryReader? reader)
    {
        reader = this.GetHeader(tableName)?.CreateBoundedReader(this.stream);
        return reader is not null;
    }

    public bool TryGetTableData(Tag tag, out ReadOnlyMemory<byte> table)
    {
        if (!this.TryGetReaderAtTablePosition(tag.ToString(), out BigEndianBinaryReader? reader, out TableHeader? header))
        {
            table = default;
            return false;
        }

        using (reader)
        {
            table = reader.ReadBytes(checked((int)header.Length));
        }

        return true;
    }

    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        if (this.isOwnedStream)
        {
            this.stream.Dispose();
            this.isDisposed = true;
        }
    }
}

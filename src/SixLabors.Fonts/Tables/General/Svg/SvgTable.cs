// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Svg;

/// <summary>
/// Represents the SVG table which contains SVG documents for glyph rendering.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"/>
/// </summary>
internal class SvgTable : Table
{
    /// <summary>
    /// The table name identifier for the SVG table.
    /// </summary>
    internal const string TableName = "SVG ";

    /// <summary>
    /// The raw byte data containing the SVG document payloads.
    /// </summary>
    private readonly byte[] tableData;

    /// <summary>
    /// The offset from the beginning of the SVG table to the SVG Document Index.
    /// </summary>
    private readonly uint svgDocIndexOffset;

    /// <summary>
    /// The absolute offset of the start of the table data buffer within the font stream.
    /// </summary>
    private readonly uint tableBaseOffset;

    /// <summary>
    /// The array of SVG Document Index entries, sorted by start glyph ID.
    /// </summary>
    private readonly SvgDocumentIndexEntry[] entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgTable"/> class.
    /// </summary>
    /// <param name="tableData">The raw byte data containing the SVG document payloads.</param>
    /// <param name="svgDocIndexOffset">The offset from the beginning of the SVG table to the SVG Document Index.</param>
    /// <param name="tableBaseOffset">The absolute offset of the start of the table data buffer within the font stream.</param>
    /// <param name="entries">The array of SVG Document Index entries.</param>
    private SvgTable(byte[] tableData, uint svgDocIndexOffset, uint tableBaseOffset, SvgDocumentIndexEntry[] entries)
    {
        this.tableData = tableData;
        this.svgDocIndexOffset = svgDocIndexOffset;
        this.tableBaseOffset = tableBaseOffset;
        this.entries = entries;
    }

    /// <summary>
    /// Loads the SVG table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader to read the table from.</param>
    /// <returns>The <see cref="SvgTable"/>, or <see langword="null"/> if the table is not present in the font.</returns>
    public static SvgTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the SVG table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the SVG table.</param>
    /// <returns>The <see cref="SvgTable"/>.</returns>
    public static SvgTable Load(BigEndianBinaryReader reader)
    {
        // HEADER
        // | Type     | Name              | Description                                               |
        // | ---------| ------------------| ----------------------------------------------------------|
        // | uint16   | version           | Table version number(starts at 0).                        |
        // | Offset32 | svgDocIndexOffset | Offset(from beginning of SVG table) to SVG Document Index.|
        // | uint32   | reserved          | Reserved; set to 0
        ushort version = reader.ReadUInt16();
        if (version != 0)
        {
            throw new NotSupportedException($"Only SVG table version 0 is supported. Found version {version}.");
        }

        uint svgDocIndexOffset = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // reserved

        // SVG Document Index
        // | Type              | Name       | Description                                                 |
        // | ------------------| -----------| ------------------------------------------------------------|
        // | uint16            | numEntries | Number of entries in the SVG Document Index.                |
        // | Entry[numEntries] | entries    | Array of SVG Document Index Entries(sorted by startGlyphID).|
        reader.Seek(svgDocIndexOffset, SeekOrigin.Begin);
        ushort numEntries = reader.ReadUInt16();
        SvgDocumentIndexEntry[] entries = new SvgDocumentIndexEntry[numEntries];

        // SVG Document Index Entry
        // | Type     | Name         | Description                                                                              |
        // | ---------| -------------| -----------------------------------------------------------------------------------------|
        // | uint16   | startGlyphID | First glyph ID in this range(inclusive).                                                 |
        // | uint16   | endGlyphID   | Last glyph ID in this range(inclusive).                                                  |
        // | Offset32 | svgDocOffset | Offset from the beginning of the SVG Document Index to an SVG document. Must be non-zero.|

        // Track min relative offset from the Document Index and absolute max end.
        uint minRelOffset = uint.MaxValue;
        uint maxEnd = 0;
        for (int i = 0; i < numEntries; i++)
        {
            ushort startGlyphId = reader.ReadUInt16();
            ushort endGlyphId = reader.ReadUInt16();
            uint svgDocOffset = reader.ReadUInt32();
            uint svgDocLength = reader.ReadUInt32();

            if (svgDocOffset == 0 || svgDocLength == 0)
            {
                throw new InvalidFontFileException("SVG table contains an entry with zero offset or length.");
            }

            if (svgDocOffset < minRelOffset)
            {
                minRelOffset = svgDocOffset;
            }

            // Track the farthest byte we need to cover in the table buffer.
            uint absEnd = svgDocIndexOffset + svgDocOffset + svgDocLength;
            if (absEnd > maxEnd)
            {
                maxEnd = absEnd;
            }

            entries[i] = new SvgDocumentIndexEntry(startGlyphId, endGlyphId, svgDocOffset, svgDocLength);
        }

        // Read exactly the covered range.
        uint tableStart = svgDocIndexOffset + minRelOffset;
        int byteCount = (int)(maxEnd - tableStart);

        reader.Seek(tableStart, SeekOrigin.Begin);
        byte[] tableData = reader.ReadBytes(byteCount);

        return new SvgTable(tableData, svgDocIndexOffset, tableStart, entries);
    }

    /// <summary>
    /// Returns true if the SVG Document Index contains a document for <paramref name="glyphId"/>.
    /// </summary>
    /// <param name="glyphId">The glyph identifier to look up.</param>
    /// <returns><see langword="true"/> if a document exists for the glyph; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsGlyph(ushort glyphId)
        => this.TryFindEntry(glyphId, out _);

    /// <summary>
    /// Gets the encoded document slice for a glyph without opening a stream.
    /// </summary>
    /// <param name="glyphId">The glyph identifier to look up.</param>
    /// <param name="start">When this method returns, contains the start offset within the table data buffer.</param>
    /// <param name="length">When this method returns, contains the length of the SVG document in bytes.</param>
    /// <returns><see langword="true"/> if a document was found for the glyph; otherwise, <see langword="false"/>.</returns>
    public bool TryGetDocumentSpan(ushort glyphId, out int start, out int length)
    {
        if (this.TryFindEntry(glyphId, out SvgDocumentIndexEntry e))
        {
            start = (int)((this.svgDocIndexOffset + e.SvgDocOffset) - this.tableBaseOffset);
            length = (int)e.SvgDocLength;
            return true;
        }

        start = 0;
        length = 0;
        return false;
    }

    /// <summary>
    /// Opens a decoding stream for the SVG document associated with the specified glyph.
    /// If the payload is gzip-compressed (RFC 1952), wraps it in a <see cref="GZipStream"/>;
    /// otherwise returns the raw memory stream. The caller owns the returned stream.
    /// </summary>
    /// <param name="glyphId">The glyph identifier to look up.</param>
    /// <param name="stream">When this method returns, contains the decoded SVG document stream, or <see cref="Stream.Null"/> if not found.</param>
    /// <returns><see langword="true"/> if a document was found and a stream was opened; otherwise, <see langword="false"/>.</returns>
    public bool TryOpenDecodedDocumentStream(ushort glyphId, out Stream stream)
    {
        if (!this.TryOpenEncodedDocumentStream(glyphId, out Stream encoded))
        {
            stream = Stream.Null;
            return false;
        }

        if (encoded is MemoryStream ms && ms.Length >= 2)
        {
            long pos = ms.Position;
            int b0 = ms.ReadByte();
            int b1 = ms.ReadByte();
            ms.Position = pos;

            // Start of GZIP (RFC1952)
            if (b0 == 0x1F && b1 == 0x8B)
            {
                stream = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: false);
                return true;
            }
        }

        stream = encoded; // plain UTF-8 XML
        return true;
    }

    /// <summary>
    /// Attempts to open a raw (potentially gzip-compressed) memory stream for the SVG document
    /// associated with the specified glyph.
    /// </summary>
    /// <param name="glyphId">The glyph identifier to look up.</param>
    /// <param name="stream">When this method returns, contains the encoded SVG document stream, or <see cref="Stream.Null"/> if not found.</param>
    /// <returns><see langword="true"/> if a document was found and a stream was opened; otherwise, <see langword="false"/>.</returns>
    private bool TryOpenEncodedDocumentStream(ushort glyphId, out Stream stream)
    {
        if (this.TryFindEntry(glyphId, out SvgDocumentIndexEntry e))
        {
            int start = (int)((this.svgDocIndexOffset + e.SvgDocOffset) - this.tableBaseOffset);
            int length = (int)e.SvgDocLength;
            stream = new MemoryStream(this.tableData, start, length, writable: false);
            return true;
        }

        stream = Stream.Null;
        return false;
    }

    /// <summary>
    /// Performs a binary search on the SVG Document Index entries to find the entry
    /// whose glyph ID range contains the specified glyph.
    /// </summary>
    /// <param name="glyphId">The glyph identifier to search for.</param>
    /// <param name="entry">When this method returns, contains the matching index entry, or the default value if not found.</param>
    /// <returns><see langword="true"/> if a matching entry was found; otherwise, <see langword="false"/>.</returns>
    private bool TryFindEntry(ushort glyphId, out SvgDocumentIndexEntry entry)
    {
        int lo = 0;
        int hi = this.entries.Length - 1;
        int candidate = -1;

        while (lo <= hi)
        {
            int mid = (int)((uint)(lo + hi) >> 1);
            ushort start = this.entries[mid].StartGlyphId;

            if (start <= glyphId)
            {
                candidate = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (candidate >= 0)
        {
            SvgDocumentIndexEntry e = this.entries[candidate];
            if (glyphId <= e.EndGlyphId)
            {
                entry = e;
                return true;
            }
        }

        entry = default;
        return false;
    }
}

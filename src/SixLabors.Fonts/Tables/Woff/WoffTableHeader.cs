// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Woff;

/// <summary>
/// Represents a table directory entry in a WOFF1 font file.
/// Each entry describes a single font table within the WOFF container,
/// including its compressed and original lengths.
/// See: <see href="https://www.w3.org/TR/WOFF/#TableDirectory"/>.
/// </summary>
internal sealed class WoffTableHeader : TableHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WoffTableHeader"/> class.
    /// </summary>
    /// <param name="tag">The 4-byte sfnt table identifier.</param>
    /// <param name="offset">The offset to the data from the beginning of the WOFF file.</param>
    /// <param name="compressedLength">The length of the compressed table data, excluding padding.</param>
    /// <param name="origLength">The length of the uncompressed table data, excluding padding.</param>
    /// <param name="checkSum">The checksum of the uncompressed table data.</param>
    public WoffTableHeader(string tag, uint offset, uint compressedLength, uint origLength, uint checkSum)
        : base(tag, checkSum, offset, origLength)
        => this.CompressedLength = compressedLength;

    /// <summary>
    /// Gets the length of the compressed table data, excluding padding.
    /// </summary>
    public uint CompressedLength { get; }

    /// <summary>
    /// Creates a <see cref="BigEndianBinaryReader"/> for the table data, decompressing with zlib if necessary.
    /// </summary>
    /// <param name="stream">The stream containing the WOFF font data.</param>
    /// <returns>A <see cref="BigEndianBinaryReader"/> positioned at the start of the uncompressed table data.</returns>
    public override BigEndianBinaryReader CreateReader(Stream stream)
    {
        // Stream is not compressed.
        if (this.Length == this.CompressedLength)
        {
            return base.CreateReader(stream);
        }

        // Read all data from the compressed stream.
        stream.Seek(this.Offset, SeekOrigin.Begin);
        using var compressedStream = new IO.ZlibInflateStream(stream);
        byte[] uncompressedBytes = new byte[this.Length];
        int totalBytesRead = 0;
        int bytesLeftToRead = uncompressedBytes.Length;
        while (totalBytesRead < this.Length)
        {
            int bytesRead = compressedStream.Read(uncompressedBytes, totalBytesRead, bytesLeftToRead);
            if (bytesRead <= 0)
            {
                throw new InvalidFontFileException($"Could not read compressed data! Expected bytes: {this.Length}, bytes read: {totalBytesRead}");
            }

            totalBytesRead += bytesRead;
            bytesLeftToRead -= bytesRead;
        }

        var memoryStream = new MemoryStream(uncompressedBytes);
        return new BigEndianBinaryReader(memoryStream, false);
    }

    // WOFF TableDirectoryEntry
    // UInt32 | tag          | 4-byte sfnt table identifier.
    // UInt32 | offset       | Offset to the data, from beginning of WOFF file.
    // UInt32 | compLength   | Length of the compressed data, excluding padding.
    // UInt32 | origLength   | Length of the uncompressed table, excluding padding.
    // UInt32 | origChecksum | Checksum of the uncompressed table.

    /// <summary>
    /// Reads a <see cref="WoffTableHeader"/> from the given reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The parsed <see cref="WoffTableHeader"/>.</returns>
    public static new WoffTableHeader Read(BigEndianBinaryReader reader) =>
        new WoffTableHeader(
            reader.ReadTag(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
}

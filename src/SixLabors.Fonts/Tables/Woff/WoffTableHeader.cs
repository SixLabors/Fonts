// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.Woff
{
    internal sealed class WoffTableHeader : TableHeader
    {
        public WoffTableHeader(string tag, uint offset, uint compressedLength, uint origLength, uint checkSum)
            : base(tag, checkSum, offset, origLength)
            => this.CompressedLength = compressedLength;

        public uint CompressedLength { get; }

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
            int bytesRead = compressedStream.Read(uncompressedBytes, 0, uncompressedBytes.Length);
            if (bytesRead < this.Length)
            {
                throw new InvalidFontFileException($"Could not read compressed data! Expected bytes: {this.Length}, bytes read: {bytesRead}");
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
        public static new WoffTableHeader Read(BigEndianBinaryReader reader) =>
            new WoffTableHeader(
                reader.ReadTag(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32());
    }
}

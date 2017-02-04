using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts.Tests
{
    internal static class WriterExtensions
    {
        public static void WriteTableHeader(this BinaryWriter writer, string tag, uint checksum, uint? offset, uint length)
        {
            // table header
            // Record Type | Name     | Description
            // ------------|----------|---------------------------------------------------
            // uint32      | tag      | 4 - byte identifier.
            // uint32      | checkSum | CheckSum for this table.
            // Offset32    | offset   | Offset from beginning of TrueType font file.
            // uint32      | length   | Length of this table.
            writer.WriteUint32(tag);
            writer.WriteUint32(checksum);
            writer.WriteOffset32(offset);
            writer.WriteUint32(length);
        }

        public static void WriteTrueTypeFileHeader(this BinaryWriter writer, ushort tableCount, ushort searchRange, ushort entrySelector, ushort rangeShift)
        {
            // uint32    | sfntVersion 0x00010000 or 0x4F54544F('OTTO') — see below.
            writer.WriteFileHeader(0x00010000, tableCount, searchRange, entrySelector, rangeShift);
        }

        public static void WriteCffFileHeader(this BinaryWriter writer, ushort tableCount, ushort searchRange, ushort entrySelector, ushort rangeShift)
        {
            // uint32    | sfntVersion 0x00010000 or 0x4F54544F('OTTO') — see below.
            writer.WriteFileHeader(0x4F54544F, tableCount, searchRange, entrySelector, rangeShift);
        }

        private static void WriteFileHeader(this BinaryWriter writer, uint version, ushort tableCount, ushort searchRange, ushort entrySelector, ushort rangeShift)
        {
            // file header
            // Type Name | Description
            // ----------|---------------------------------------------
            // uint32    | sfntVersion 0x00010000 or 0x4F54544F('OTTO') — see below.
            // uint16    | numTables   Number of tables.
            // uint16    | searchRange (Maximum power of 2 <= numTables) x 16.
            // uint16    | entrySelector Log2(maximum power of 2 <= numTables).
            // uint16    | rangeShift  NumTables x 16 - searchRange.
            writer.WriteUint32(version);
            writer.WriteUint16(tableCount);
            writer.WriteUint16(searchRange);
            writer.WriteUint16(entrySelector);
            writer.WriteUint16(rangeShift);
        }
    }
}

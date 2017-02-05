using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.WellKnownIds;

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
            // Type Name | name          | Description
            // ----------|---------------|------------------------------
            // uint32    | sfntVersion   | 0x00010000 or 0x4F54544F('OTTO') — see below.
            // uint16    | numTables     | Number of tables.
            // uint16    | searchRange   | (Maximum power of 2 <= numTables) x 16.
            // uint16    | entrySelector | Log2(maximum power of 2 <= numTables).
            // uint16    | rangeShift    | NumTables x 16 - searchRange.
            writer.WriteUint32(version);
            writer.WriteUint16(tableCount);
            writer.WriteUint16(searchRange);
            writer.WriteUint16(entrySelector);
            writer.WriteUint16(rangeShift);
        }

        public static void WriteNameTable(this BinaryWriter writer, Dictionary<NameIds, string> names, List<string> languages = null)
        {
            // Type          | Name                        | Description
            // --------------|-----------------------------|--------------------------------------------------------
            // uint16        | format                      | Format selector
            // uint16        | count                       | Number of name records.
            // Offset16      | stringOffset                | Offset to start of string storage (from start of table).
            // NameRecord    | nameRecord[count]           | The name records where count is the number of records.
            // additionally if format = 1
            // uint16        | langTagCount                | Number of language-tag records.
            // LangTagRecord | langTagRecord[langTagCount] | The language-tag records where langTagCount is the number of records.

            writer.WriteUint16((ushort)(languages == null ? 0 : 1));
            writer.WriteUint16((ushort)names.Count);

            int sizeOfHeader = 6;
            if (languages != null)
            {
                const int langRecordSize = 4;
                //format 1
                sizeOfHeader += 2;
                sizeOfHeader += langRecordSize * languages.Count;
            }
            const int nameRecordSize = 12;
            sizeOfHeader += nameRecordSize * names.Count;
            writer.WriteOffset16((ushort)sizeOfHeader);

            // write name records
            // Type     | Name       | Description
            // ---------|------------|----------------------------------------------------
            // uint16   | platformID | Platform ID.
            // uint16   | encodingID | Platform - specific encoding ID.
            // uint16   | languageID | Language ID.
            // uint16   | nameID     | Name ID.
            // uint16   | length     | String length (in bytes).
            // Offset16 | offset     | String offset from start of storage area(in bytes).
            Encoding encoding = Encoding.BigEndianUnicode; // this is Unicode2
            int stringOffset = 0;
            List<int> offsets = new List<int>();
            foreach (var n in names)
            {
                writer.WriteUint16(0); // hard code platform
                writer.WriteUint16((ushort)EncodingIDs.Unicode2); // hard code encoding
                writer.WriteUint16(0); //hard code language
                writer.WriteUint16((ushort)n.Key);

                var length = Encoding.BigEndianUnicode.GetBytes(n.Value).Length;
                writer.WriteUint16((ushort)length);
                writer.WriteOffset16((ushort)stringOffset);
                offsets.Add(stringOffset);
                stringOffset += length;
            }

            if (languages != null)
            {
                writer.WriteUint16((ushort)languages.Count);
                // language record
                // uint16   | length     | String length (in bytes).
                // Offset16 | offset     | String offset from start of storage area(in bytes).
                foreach (var n in languages)
                {
                    var length = Encoding.BigEndianUnicode.GetBytes(n).Length;
                    writer.WriteUint16((ushort)length);
                    writer.WriteOffset16((ushort)stringOffset);
                    offsets.Add(stringOffset);
                    stringOffset += length;
                }
            }
            int currentItem = 0;

            foreach (var n in names)
            {
                var expectedPosition = offsets[currentItem];
                currentItem++;
                writer.WriteNoLength(n.Value, Encoding.BigEndianUnicode);
            }

            if (languages != null)
            {
                foreach (var n in languages)
                {
                    var expectedPosition = offsets[currentItem];
                    currentItem++;
                    writer.WriteNoLength(n, Encoding.BigEndianUnicode);
                }
            }
        }
    }
}

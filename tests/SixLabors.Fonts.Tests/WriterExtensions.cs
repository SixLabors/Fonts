using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General.CMap;
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

        public static void WriteTrueTypeFileHeader(this BinaryWriter writer, params TableHeader[] headers)
        {
            // uint32    | sfntVersion 0x00010000 or 0x4F54544F('OTTO') — see below.
            writer.WriteFileHeader(0x00010000, headers);
        }

        public static void WriteCffFileHeader(this BinaryWriter writer, ushort tableCount, ushort searchRange, ushort entrySelector, ushort rangeShift)
        {
            // uint32    | sfntVersion 0x00010000 or 0x4F54544F('OTTO') — see below.
            writer.WriteFileHeader(0x4F54544F, tableCount, searchRange, entrySelector, rangeShift);
        }

        private static void WriteFileHeader(this BinaryWriter writer, uint version, params TableHeader[] headers)
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
            writer.WriteUint16((ushort)headers.Length);
            writer.WriteUint16(0);
            writer.WriteUint16(0);
            writer.WriteUint16(0);
            int offset = 12;
            offset += headers.Length * 16;
            foreach (var h in headers)
            {
                writer.WriteTableHeader(h.Tag, h.CheckSum, (uint)offset, h.Length);
                offset += (int)h.Length;
            }
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

                // format 1
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
                writer.WriteUint16(0); // hard code language
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

        public static void WriteCMapTable(this BinaryWriter writer, IEnumerable<CMapSubTable> subtables)
        {
            // 'cmap' Header:
            // Type           | Name                       | Description
            // ---------------|----------------------------|------------------------------------
            // uint16         | version                    |Table version number(0).
            // uint16         | numTables                  |Number of encoding tables that follow.
            // EncodingRecord | encodingRecords[numTables] |
            writer.WriteUint16(0);
            writer.WriteUint16((ushort)subtables.Count());

            int offset = 4; // for for the cmap header
            offset += 8 * subtables.Count(); // 8 bytes per encoding header
            foreach (var table in subtables)
            {
                // EncodingRecord:
                // Type     | Name       | Description
                // ---------|------------|-----------------------------------------------
                // uint16   | platformID | Platform ID.
                // uint16   | encodingID | Platform - specific encoding ID.
                // Offset32 | offset     | Byte offset from beginning of table to the subtable for this encoding.
                writer.WriteUint16((ushort)table.Platform);
                writer.WriteUint16(table.Encoding);
                writer.WriteUint32((uint)offset);

                offset += table.DataLength();

                // calculate the size of each format
            }

            foreach (var table in subtables)
            {
                writer.WriteCMapSubTable(table);
            }
        }

        public static void WriteCMapSubTable(this BinaryWriter writer, CMapSubTable subtable)
        {
            writer.WriteCMapSubTable(subtable as Format0SubTable);
            writer.WriteCMapSubTable(subtable as Format4SubTable);
        }

        public static void WriteCMapSubTable(this BinaryWriter writer, Format0SubTable subtable)
        {
            if (subtable == null)
            {
                return;
            }

            // Format 0 SubTable
            // Type     |Name              | Description
            // ---------|------------------|--------------------------------------------------------------------------
            // uint16   |format            | Format number is set to 0.
            // uint16   |length            | This is the length in bytes of the subtable.
            // uint16   |language          | Please see “Note on the language field in 'cmap' subtables“ in this document.
            // uint8    |glyphIdArray[glyphcount] | An array that maps character codes to glyph index values.
            writer.WriteUint16(0);
            writer.WriteUint16((ushort)subtable.DataLength());
            writer.WriteUint16(subtable.Language);

            foreach (var c in subtable.glyphIds)
            {
                writer.WriteUint8(c);
            }
        }

        public static void WriteCMapSubTable(this BinaryWriter writer, Format4SubTable subtable)
        {
            if (subtable == null)
            {
                return;
            }

            // 'cmap' Subtable Format 4:
            // Type   | Name                       | Description
            // -------|----------------------------|------------------------------------------------------------------------
            // uint16 | format                     | Format number is set to 4.
            // uint16 | length                     | This is the length in bytes of the subtable.
            // uint16 | language                   | Please see “Note on the language field in 'cmap' subtables“ in this document.
            // uint16 | segCountX2                 | 2 x segCount.
            // uint16 | searchRange                | 2 x (2**floor(log2(segCount)))
            // uint16 | entrySelector              | log2(searchRange/2)
            // uint16 | rangeShift                 | 2 x segCount - searchRange
            // uint16 | endCount[segCount]         | End characterCode for each segment, last=0xFFFF.
            // uint16 | reservedPad                | Set to 0.
            // uint16 | startCount[segCount]       | Start character code for each segment.
            // int16  | idDelta[segCount]           | Delta for all character codes in segment.
            // uint16 | idRangeOffset[segCount]    | Offsets into glyphIdArray or 0
            // uint16 | glyphIdArray[ ]            | Glyph index array (arbitrary length)
            writer.WriteUint16(4);
            writer.WriteUint16((ushort)subtable.DataLength());
            writer.WriteUint16(subtable.Language);
            var segCount = subtable.segments.Length;
            writer.WriteUint16((ushort)(subtable.segments.Length * 2));
            var searchRange = Math.Pow(2, Math.Floor(Math.Log(segCount, 2)));
            writer.WriteUint16((ushort)searchRange);
            var entrySelector = Math.Log(searchRange / 2, 2);
            writer.WriteUint16((ushort)entrySelector);
            var rangeShift = (2 * segCount) - searchRange;
            writer.WriteUint16((ushort)rangeShift);
            foreach (var seg in subtable.segments)
            {
                writer.WriteUint16(seg.End);
            }

            writer.WriteUint16(0);
            foreach (var seg in subtable.segments)
            {
                writer.WriteUint16(seg.Start);
            }

            foreach (var seg in subtable.segments)
            {
                writer.WriteInt16(seg.Delta);
            }

            foreach (var seg in subtable.segments)
            {
                writer.WriteUint16(seg.Offset);
            }

            foreach (var c in subtable.glyphIds)
            {
                writer.WriteUint16(c);
            }
        }

        private static int DataLength(this CMapSubTable subtable)
        {
            if (subtable is Format0SubTable)
            {
                return 6 + ((Format0SubTable)subtable).glyphIds.Length;
            }

            if (subtable is Format4SubTable)
            {
                var segs = ((Format4SubTable)subtable).segments;
                var glyphs = ((Format4SubTable)subtable).glyphIds;
                return 16 + (segs.Length * 8) + (glyphs.Length * 2);
            }

            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
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
            writer.WriteUInt32(tag);
            writer.WriteUInt32(checksum);
            writer.WriteOffset32(offset);
            writer.WriteUInt32(length);
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
            writer.WriteUInt32(version);
            writer.WriteUInt16((ushort)headers.Length);
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);
            int offset = 12;
            offset += headers.Length * 16;
            foreach (TableHeader h in headers)
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
            writer.WriteUInt32(version);
            writer.WriteUInt16(tableCount);
            writer.WriteUInt16(searchRange);
            writer.WriteUInt16(entrySelector);
            writer.WriteUInt16(rangeShift);
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
            writer.WriteUInt16((ushort)(languages == null ? 0 : 1));
            writer.WriteUInt16((ushort)names.Count);

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
            foreach (KeyValuePair<NameIds, string> n in names)
            {
                writer.WriteUInt16(0); // hard code platform
                writer.WriteUInt16((ushort)EncodingIDs.Unicode2); // hard code encoding
                writer.WriteUInt16(0); // hard code language
                writer.WriteUInt16((ushort)n.Key);

                int length = Encoding.BigEndianUnicode.GetBytes(n.Value).Length;
                writer.WriteUInt16((ushort)length);
                writer.WriteOffset16((ushort)stringOffset);
                offsets.Add(stringOffset);
                stringOffset += length;
            }

            if (languages != null)
            {
                writer.WriteUInt16((ushort)languages.Count);

                // language record
                // uint16   | length     | String length (in bytes).
                // Offset16 | offset     | String offset from start of storage area(in bytes).
                foreach (string n in languages)
                {
                    int length = Encoding.BigEndianUnicode.GetBytes(n).Length;
                    writer.WriteUInt16((ushort)length);
                    writer.WriteOffset16((ushort)stringOffset);
                    offsets.Add(stringOffset);
                    stringOffset += length;
                }
            }

            int currentItem = 0;

            foreach (KeyValuePair<NameIds, string> n in names)
            {
                int expectedPosition = offsets[currentItem];
                currentItem++;
                writer.WriteNoLength(n.Value, Encoding.BigEndianUnicode);
            }

            if (languages != null)
            {
                foreach (string n in languages)
                {
                    int expectedPosition = offsets[currentItem];
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
            writer.WriteUInt16(0);
            writer.WriteUInt16((ushort)subtables.Count());

            int offset = 4; // for for the cmap header
            offset += 8 * subtables.Count(); // 8 bytes per encoding header
            foreach (CMapSubTable table in subtables)
            {
                // EncodingRecord:
                // Type     | Name       | Description
                // ---------|------------|-----------------------------------------------
                // uint16   | platformID | Platform ID.
                // uint16   | encodingID | Platform - specific encoding ID.
                // Offset32 | offset     | Byte offset from beginning of table to the subtable for this encoding.
                writer.WriteUInt16((ushort)table.Platform);
                writer.WriteUInt16(table.Encoding);
                writer.WriteUInt32((uint)offset);

                offset += table.DataLength();

                // calculate the size of each format
            }

            foreach (CMapSubTable table in subtables)
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
            writer.WriteUInt16(0);
            writer.WriteUInt16((ushort)subtable.DataLength());
            writer.WriteUInt16(subtable.Language);

            foreach (byte c in subtable.GlyphIds)
            {
                writer.WriteUInt8(c);
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
            writer.WriteUInt16(4);
            writer.WriteUInt16((ushort)subtable.DataLength());
            writer.WriteUInt16(subtable.Language);
            int segCount = subtable.Segments.Length;
            writer.WriteUInt16((ushort)(subtable.Segments.Length * 2));
            double searchRange = Math.Pow(2, Math.Floor(Math.Log(segCount, 2)));
            writer.WriteUInt16((ushort)searchRange);
            double entrySelector = Math.Log(searchRange / 2, 2);
            writer.WriteUInt16((ushort)entrySelector);
            double rangeShift = (2 * segCount) - searchRange;
            writer.WriteUInt16((ushort)rangeShift);
            foreach (Format4SubTable.Segment seg in subtable.Segments)
            {
                writer.WriteUInt16(seg.End);
            }

            writer.WriteUInt16(0);
            foreach (Format4SubTable.Segment seg in subtable.Segments)
            {
                writer.WriteUInt16(seg.Start);
            }

            foreach (Format4SubTable.Segment seg in subtable.Segments)
            {
                writer.WriteInt16(seg.Delta);
            }

            foreach (Format4SubTable.Segment seg in subtable.Segments)
            {
                writer.WriteUInt16(seg.Offset);
            }

            foreach (ushort c in subtable.GlyphIds)
            {
                writer.WriteUInt16(c);
            }
        }

        private static int DataLength(this CMapSubTable subtable)
        {
            if (subtable is Format0SubTable)
            {
                return 6 + ((Format0SubTable)subtable).GlyphIds.Length;
            }

            if (subtable is Format4SubTable)
            {
                Format4SubTable.Segment[] segs = ((Format4SubTable)subtable).Segments;
                ushort[] glyphs = ((Format4SubTable)subtable).GlyphIds;
                return 16 + (segs.Length * 8) + (glyphs.Length * 2);
            }

            return 0;
        }

        public static void WriteHoizontalHeadTable(this BinaryWriter writer, HoizontalHeadTable table)
        {
            // Type      | Name                 | Description
            // ----------|----------------------|----------------------------------------------------------------------------------------------------
            // uint16    | majorVersion         | Major version number of the horizontal header table — set to 1.
            // uint16    | minorVersion         | Minor version number of the horizontal header table — set to 0.
            // FWORD     | Ascender             | Typographic ascent (Distance from baseline of highest ascender).
            // FWORD     | Descender            | Typographic descent (Distance from baseline of lowest descender).
            // FWORD     | LineGap              | Typographic line gap. - Negative  LineGap values are treated as zero in Windows 3.1, and in Mac OS System 6 and System 7.
            // UFWORD    | advanceWidthMax      | Maximum advance width value in 'hmtx' table.
            // FWORD     | minLeftSideBearing   | Minimum left sidebearing value in 'hmtx' table.
            // FWORD     | minRightSideBearing  | Minimum right sidebearing value; calculated as Min(aw - lsb - (xMax - xMin)).
            // FWORD     | xMaxExtent           | Max(lsb + (xMax - xMin)).
            // int16     | caretSlopeRise       | Used to calculate the slope of the cursor (rise/run); 1 for vertical.
            // int16     | caretSlopeRun        | 0 for vertical.
            // int16     | caretOffset          | The amount by which a slanted highlight on a glyph needs to be shifted to produce the best appearance. Set to 0 for non-slanted fonts
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | (reserved)           | set to 0
            // int16     | metricDataFormat     | 0 for current format.
            // uint16    | numberOfHMetrics     | Number of hMetric entries in 'hmtx' table

            writer.WriteUInt16(1);
            writer.WriteUInt16(1);
            writer.WriteFWORD(table.Ascender);
            writer.WriteFWORD(table.Descender);
            writer.WriteFWORD(table.LineGap);
            writer.WriteUFWORD(table.AdvanceWidthMax);
            writer.WriteFWORD(table.MinLeftSideBearing);
            writer.WriteFWORD(table.MinRightSideBearing);
            writer.WriteFWORD(table.XMaxExtent);
            writer.WriteInt16(table.CaretSlopeRise);
            writer.WriteInt16(table.CaretSlopeRun);
            writer.WriteInt16(table.CaretOffset);
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // metricDataFormat should be 0
            writer.WriteUInt16(table.NumberOfHMetrics);
        }

        public static void WriteHeadTable(this BinaryWriter writer, HeadTable table)
        {


            // Type         | Name               | Description
            // -------------|--------------------|----------------------------------------------------------------------------------------------------
            // uint16       | majorVersion       | Major version number of the font header table — set to 1.
            // uint16       | minorVersion       | Minor version number of the font header table — set to 0.
            // Fixed        | fontRevision       | Set by font manufacturer.
            // uint32       | checkSumAdjustment | To compute: set it to 0, sum the entire font as uint32, then store 0xB1B0AFBA - sum.If the font is used as a component in a font collection file, the value of this field will be invalidated by changes to the file structure and font table directory, and must be ignored.
            // uint32       | magicNumber        | Set to 0x5F0F3CF5.
            // uint16       | flags              |    Bit 0: Baseline for font at y = 0;
            //                                            Bit 1: Left sidebearing point at x = 0(relevant only for TrueType rasterizers) — see the note below regarding variable fonts;
            //                                            Bit 2: Instructions may depend on point size;
            //                                            Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear; 
            //                                            Bit 4: Instructions may alter advance width(the advance widths might not scale linearly);
            //                                            Bit 5: This bit is not used in OpenType, and should not be set in order to ensure compatible behavior on all platforms.If set, it may result in different behavior for vertical layout in some platforms. (See Apple's specification for details regarding behavior in Apple platforms.)
            //                                            Bits 6–10: These bits are not used in Opentype and should always be cleared. (See Apple's specification for details regarding legacy used in Apple platforms.) 
            //                                            Bit 11: Font data is ‘lossless’ as a results of having been subjected to optimizing transformation and/or compression (such as e.g.compression mechanisms defined by ISO/IEC 14496-18, MicroType Express, WOFF 2.0 or similar) where the original font functionality and features are retained but the binary compatibility between input and output font files is not guaranteed.As a result of the applied transform, the ‘DSIG’ Table may also be invalidated.
            //                                            Bit 12: Font converted (produce compatible metrics)
            //                                            Bit 13: Font optimized for ClearType™. Note, fonts that rely on embedded bitmaps (EBDT) for rendering should not be considered optimized for ClearType, and therefore should keep this bit cleared.
            //                                            Bit 14: Last Resort font.If set, indicates that the glyphs encoded in the cmap subtables are simply generic symbolic representations of code point ranges and don’t truly represent support for those code points.If unset, indicates that the glyphs encoded in the cmap subtables represent proper support for those code points.
            //                                            Bit 15: Reserved, set to 0 
            // uint16       | unitsPerEm         | Valid range is from 16 to 16384. This value should be a power of 2 for fonts that have TrueType outlines.
            // LONGDATETIME | created            | Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
            // LONGDATETIME | modified           | Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
            // int16        | xMin               | For all glyph bounding boxes.
            // int16        | yMin               | For all glyph bounding boxes.
            // int16        | xMax               | For all glyph bounding boxes.
            // int16        | yMax               | For all glyph bounding boxes.
            // uint16       | macStyle           |   Bit 0: Bold (if set to 1);
            //                                       Bit 1: Italic(if set to 1)
            //                                       Bit 2: Underline(if set to 1)
            //                                       Bit 3: Outline(if set to 1)
            //                                       Bit 4: Shadow(if set to 1)
            //                                       Bit 5: Condensed(if set to 1)
            //                                       Bit 6: Extended(if set to 1)
            //                                       Bits 7–15: Reserved(set to 0).
            // uint16       |lowestRecPPEM       |  Smallest readable size in pixels.
            // int16        | fontDirectionHint  |  Deprecated(Set to 2). 
            //                                          0: Fully mixed directional glyphs; 
            //                                          1: Only strongly left to right; 
            //                                          2: Like 1 but also contains neutrals; 
            //                                          -1: Only strongly right to left; 
            //                                          -2: Like -1 but also contains neutrals. 1
            // int16        | indexToLocFormat   | 0 for short offsets (Offset16), 1 for long (Offset32).
            // int16        | glyphDataFormat    | 0 for current format.

            writer.WriteUInt16(1);
            writer.WriteUInt16(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0x5F0F3CF5);

            writer.WriteUInt16((ushort)table.Flags);
            writer.WriteUInt16(table.UnitsPerEm);

            DateTime startDate = new DateTime(1904, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            writer.WriteInt64((long)table.Created.Subtract(startDate).TotalSeconds);
            writer.WriteInt64((long)table.Modified.Subtract(startDate).TotalSeconds);
            writer.WriteInt16((short)table.Bounds.Min.X);
            writer.WriteInt16((short)table.Bounds.Min.Y);
            writer.WriteInt16((short)table.Bounds.Max.X);
            writer.WriteInt16((short)table.Bounds.Max.Y);
            writer.WriteUInt16((ushort)table.MacStyle);
            writer.WriteUInt16(table.LowestRecPPEM);
            writer.WriteInt16(2);
            writer.WriteInt16((short)table.IndexLocationFormat);
            writer.WriteInt16(0);
        }
    }
}

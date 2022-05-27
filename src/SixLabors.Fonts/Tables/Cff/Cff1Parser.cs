// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Parses a Compact Font Format (CFF) font program as described in The Compact Font Format specification (Adobe Technical Note #5176).
    /// A CFF font may contain multiple fonts and achieves compression by sharing details between fonts in the set.
    /// </summary>
    internal class Cff1Parser
    {
        /// <summary>
        /// Latin 1 Encoding: ISO 8859-1 is a single-byte encoding that can represent the first 256 Unicode characters.
        /// </summary>
        private static readonly Encoding Iso88591 = Encoding.GetEncoding("ISO-8859-1");

        private readonly StringBuilder pooledStringBuilder = new();
        private readonly bool useCompactInstruction = true;
        private readonly Type2InstructionCompacter instCompacter = new();
        private long offset;
        private int charStringsOffset;
        private int charsetOffset;
        private int encodingOffset = -1;
        private int privateDICTOffset;
        private int privateDICTLength;

        // from: Adobe's The Compact Font Format Specification, version1.0, Dec 2003

        // Table 2 CFF Data Types
        // Name       Range          Description
        // Card8      0 – 255           1-byte unsigned number
        // Card16     0 – 65535         2-byte unsigned number
        // Offset     varies        1, 2, 3, or 4 byte offset(specified by  OffSize field)
        // OffSize   1–4            1-byte unsigned number specifies the
        //                          size of an Offset field or fields
        // SID      0 – 64999       2-byte string identifier
        //-----------------

        // Table 1 CFF Data Layout
        // Entry                     Comments
        // Header                   –
        // Name INDEX               –
        // Top DICT INDEX           –
        // String INDEX             –
        // Global Subr INDEX            –
        // Encodings                    –
        // Charsets                 –
        // FDSelect                  CIDFonts only
        // CharStrings INDEX         per-font
        // Font DICT INDEX           per-font, CIDFonts only
        // Private DICT              per-font
        // Local Subr INDEX          per-font or per-Private DICT for CIDFonts
        // Copyright and Trademark  -
        public Cff1Font Load(BigEndianBinaryReader reader, long offset)
        {
            this.offset = offset;

            string fontName = this.ReadNameIndex(reader);
            List<CffDataDicEntry>? dataDicEntries = this.ReadTopDICTIndex(reader);
            string[] stringIndex = this.ReadStringIndex(reader);
            CffTopDictionary topDictionary = this.ResolveTopDictInfo(dataDicEntries, stringIndex);
            byte[][] globalSubrRawBuffers = this.ReadGlobalSubrIndex(reader);
            this.ReadFDSelect(reader, topDictionary.CidFontInfo);
            FontDict[] fontDicts = this.ReadFDArray(reader, topDictionary.CidFontInfo);
            CffPrivateDictionary? privateDictionary = this.ReadPrivateDict(reader);
            Cff1GlyphData[] glyphs = this.ReadCharStringsIndex(reader, topDictionary, globalSubrRawBuffers, fontDicts, privateDictionary);

            this.ReadCharsets(reader, stringIndex, glyphs);
            this.ReadEncodings(reader);

            return new(fontName, topDictionary, glyphs);
        }

        private string ReadNameIndex(BigEndianBinaryReader reader)
        {
            // 7. Name INDEX
            // This contains the PostScript language names(FontName or
            // CIDFontName) of all the fonts in the FontSet stored in an INDEX
            // structure.The font names are sorted, thereby permitting a
            // binary search to be performed when locating a specific font
            // within a FontSet. The sort order is based on character codes
            // treated as 8 - bit unsigned integers. A given font name precedes
            //  another font name having the first name as its prefix.There
            //  must be at least one entry in this INDEX, i.e.the FontSet must
            // contain at least one font.

            // For compatibility with client software, such as PostScript
            // interpreters and Acrobat®, font names should be no longer
            // than 127 characters and should not contain any of the following
            // ASCII characters: [, ], (, ), {, }, <, >, /, %, null(NUL), space, tab,
            // carriage return, line feed, form feed.It is recommended that
            // font names be restricted to the printable ASCII subset, codes 33
            // through 126.Adobe Type Manager® (ATM®) software imposes
            // a further restriction on the font name length of 63 characters.

            // Note 3
            // For compatibility with earlier PostScript
            // interpreters, see Technical Note
            // #5088, “Font Naming Issues.”

            // A font may be deleted from a FontSet without removing its data
            // by setting the first byte of its name in the Name INDEX to 0
            // (NUL).This kind of deletion offers a simple way to handle font
            // upgrades without rebuilding entire fontsets.Binary search
            // software must detect deletions and restart the search at the
            // previous or next name in the INDEX to ensure that all
            // appropriate names are matched.
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                throw new InvalidFontFileException("No name index found.");
            }

            // For Open Type the Name INDEX in the CFF data must contain only one entry;
            // that is, there must be only one font in the CFF FontSet.
            CffIndexOffset offset = offsets[0];
            return Iso88591.GetString(reader.ReadBytes(offset.Length), 0, offset.Length);
        }

        private List<CffDataDicEntry> ReadTopDICTIndex(BigEndianBinaryReader reader)
        {
            // 8. Top DICT INDEX
            // This contains the top - level DICTs of all the fonts in the FontSet
            // stored in an INDEX structure.Objects contained within this
            // INDEX correspond to those in the Name INDEX in both order
            // and number. Each object is a DICT structure that corresponds to
            // the top-level dictionary of a PostScript font.
            // A font is identified by an entry in the Name INDEX and its data
            // is accessed via the corresponding Top DICT
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                throw new InvalidFontFileException("No Top DICT index found.");
            }

            // 9. Top DICT Data
            // The names of the Top DICT operators shown in
            // Table 9 are, where possible, the same as the corresponding Type 1 dict key.
            // Operators that have no corresponding Type1 dict key are noted
            // in the table below along with a default value, if any. (Several
            // operators have been derived from FontInfo dict keys but have
            // been grouped together with the Top DICT operators for
            // simplicity.The keys from the FontInfo dict are indicated in the
            // Default, notes  column of Table 9)
            return this.ReadDICTData(reader, offsets[0].Length);
        }

        private string[] ReadStringIndex(BigEndianBinaryReader reader)
        {
            // 10 String INDEX
            // All the strings, with the exception of the FontName and
            // CIDFontName strings which appear in the Name INDEX, used by
            // different fonts within the FontSet are collected together into an
            // INDEX structure and are referenced by a 2 - byte unsigned
            // number called a string identifier or SID.

            // Only unique strings are stored in the table
            // thereby removing duplication across fonts.

            // Further space saving is obtained by allocating commonly
            // occurring strings to predefined SIDs.
            // These strings, known as the standard strings,
            // describe all the names used in the ISOAdobe and
            // Expert character sets along with a few other strings
            // common to Type 1 fonts.

            // A complete list of standard strings is given in Appendix A

            // The client program will contain an array of standard strings with
            // nStdStrings elements.
            // Thus, the standard strings take SIDs in the
            // range 0 to(nStdStrings –1).

            // The first string in the String INDEX
            // corresponds to the SID whose value is equal to nStdStrings, the
            // first non - standard string, and so on.

            // When the client needs to
            // determine the string that corresponds to a particular SID it
            // performs the following: test if SID is in standard range then
            // fetch from internal table,
            // otherwise, fetch string from the String
            // INDEX using a value of(SID – nStdStrings) as the index.
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                return Array.Empty<string>();
            }

            string[] stringIndex = new string[offsets.Length];

            // Allow reusing the same buffer for shorter reads.
            using var buffer = new Buffer<byte>(512);
            Span<byte> bufferSpan = buffer.GetSpan();

            for (int i = 0; i < offsets.Length; ++i)
            {
                int length = offsets[i].Length;
                if (length < bufferSpan.Length)
                {
                    Span<byte> slice = bufferSpan.Slice(0, length);
                    int actualRead = reader.BaseStream.Read(slice);
#if DEBUG
                    if (actualRead != length)
                    {
                        throw new NotSupportedException();
                    }
#endif
                    stringIndex[i] = Iso88591.GetString(slice);
                }
                else
                {
                    stringIndex[i] = Iso88591.GetString(reader.ReadBytes(length), 0, length);
                }
            }

            return stringIndex;
        }

        private string GetSid(int index, string[] stringIndex)
        {
            if (index >= 0 && index <= CffStandardStrings.Count - 1)
            {
                // Use standard name
                return CffStandardStrings.GetName(index);
            }

            if (index - CffStandardStrings.Count < stringIndex.Length)
            {
                return stringIndex[index - CffStandardStrings.Count];
            }

            // Technically this maps to .notdef, but PDFBox uses this
            return "SID" + index;
        }

        private CffTopDictionary ResolveTopDictInfo(List<CffDataDicEntry> entries, string[] stringIndex)
        {
            // TODO: Is CID mandatory?
            CffTopDictionary metrics = new();
            foreach (CffDataDicEntry entry in entries)
            {
                switch (entry.Operator.Name)
                {
                    default:
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("topdic:" + entry.Operator.Name);
#endif
                        break;
                    case "XUID":
                        break; // nothing
                    case "version":
                        metrics.Version = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "Notice":
                        metrics.Notice = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "Copyright":
                        metrics.CopyRight = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "FullName":
                        metrics.FullName = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "FamilyName":
                        metrics.FamilyName = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "Weight":
                        metrics.Weight = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        break;
                    case "UnderlinePosition":
                        metrics.UnderlinePosition = entry.Operands[0].RealNumValue;
                        break;
                    case "UnderlineThickness":
                        metrics.UnderlineThickness = entry.Operands[0].RealNumValue;
                        break;
                    case "FontBBox":
                        metrics.FontBBox = new double[]
                        {
                            entry.Operands[0].RealNumValue,
                            entry.Operands[1].RealNumValue,
                            entry.Operands[2].RealNumValue,
                            entry.Operands[3].RealNumValue
                        };
                        break;
                    case "CharStrings":
                        this.charStringsOffset = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "charset":
                        this.charsetOffset = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "Encoding":
                        this.encodingOffset = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "Private":
                        // private DICT size and offset
                        this.privateDICTLength = (int)entry.Operands[0].RealNumValue;
                        this.privateDICTOffset = (int)entry.Operands[1].RealNumValue;
                        break;
                    case "ROS":
                        // http://wwwimages.adobe.com/www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5176.CFF.pdf
                        // A CFF CIDFont has the CIDFontName in the Name INDEX and a corresponding Top DICT.
                        // The Top DICT begins with ROS operator which specifies the Registry-Ordering - Supplement for the font.
                        // This will indicate to a CFF parser that special CID processing should be applied to this font. Specifically:

                        // ROS operator combines the Registry, Ordering, and Supplement keys together.
                        // see Adobe Cmap resource , https://github.com/adobe-type-tools/cmap-resources
                        metrics.CidFontInfo.ROS_Register = this.GetSid((int)entry.Operands[0].RealNumValue, stringIndex);
                        metrics.CidFontInfo.ROS_Ordering = this.GetSid((int)entry.Operands[1].RealNumValue, stringIndex);
                        metrics.CidFontInfo.ROS_Supplement = this.GetSid((int)entry.Operands[2].RealNumValue, stringIndex);

                        break;
                    case "CIDFontVersion":
                        metrics.CidFontInfo.CIDFontVersion = entry.Operands[0].RealNumValue;
                        break;
                    case "CIDCount":
                        metrics.CidFontInfo.CIDFountCount = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "FDSelect":
                        metrics.CidFontInfo.FDSelect = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "FDArray":
                        metrics.CidFontInfo.FDArray = (int)entry.Operands[0].RealNumValue;
                        break;
                }
            }

            return metrics;
        }

        private byte[][] ReadGlobalSubrIndex(BigEndianBinaryReader reader)

           // 16. Local / Global Subrs INDEXes
           // Both Type 1 and Type 2 charstrings support the notion of
           // subroutines or subrs.

           // A subr is typically a sequence of charstring
           // bytes representing a sub - program that occurs in more than one
           //  place in a font’s charstring data.

           // This subr may be stored once
           // but referenced many times from within one or more charstrings
           // by the use of the call subr  operator whose operand is the
           // number of the subr to be called.

           // The subrs are local to a  particular font and
           // cannot be shared between fonts.

           // Type 2 charstrings also permit global subrs which function in the same
           // way but are called by the call gsubr operator and may be shared
           // across fonts.

           // Local subrs are stored in an INDEX structure which is located via
           // the offset operand of the Subrs  operator in the Private DICT.
           // A font without local subrs has no Subrs operator in the Private DICT.

           // Global subrs are stored in an INDEX structure which follows the
           // String INDEX. A FontSet without any global subrs is represented
           // by an empty Global Subrs INDEX.
           => this.ReadSubrBuffer(reader);

        private byte[][] ReadLocalSubrs(BigEndianBinaryReader reader) => this.ReadSubrBuffer(reader);

        private void ReadEncodings(BigEndianBinaryReader reader)
        {
            // Encoding data is located via the offset operand to the
            // Encoding operator in the Top DICT.

            // Only one Encoding operator can be
            // specified per font except for CIDFonts which specify no
            // encoding.

            // A glyph’s encoding is specified by a 1 - byte code that
            // permits values in the range 0 - 255.

            // Each encoding is described by a format-type identifier byte
            // followed by format-specific data.Two formats are currently
            // defined as specified in Tables 11(Format 0) and 12(Format 1).
            byte format = reader.ReadByte();
            switch (format)
            {
                default:
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("cff_parser_read_encodings:" + format);
#endif
                    break;
                case 0:
                    this.ReadFormat0Encoding(reader);
                    break;
                case 1:
                    this.ReadFormat1Encoding(reader);
                    break;
            }

            // TODO: ...
        }

        private void ReadCharsets(BigEndianBinaryReader reader, string[] stringIndex, Cff1GlyphData[] glyphs)
        {
            // Charset data is located via the offset operand to the
            // charset operator in the Top DICT.

            // Each charset is described by a format-
            // type identifier byte followed by format-specific data.
            // Three formats are currently defined as shown in Tables
            // 17, 18, and 20.
            reader.BaseStream.Position = this.offset + this.charsetOffset;
            switch (reader.ReadByte())
            {
                default:
                    throw new NotSupportedException();
                case 0:
                    this.ReadCharsetsFormat0(reader, stringIndex, glyphs);
                    break;
                case 1:
                    this.ReadCharsetsFormat1(reader, stringIndex, glyphs);
                    break;
                case 2:
                    this.ReadCharsetsFormat2(reader, stringIndex, glyphs);
                    break;
            }
        }

        private void ReadCharsetsFormat0(BigEndianBinaryReader reader, string[] stringIndex, Cff1GlyphData[] glyphs)
        {
            // Table 17: Format 0
            // Type     Name                Description
            // Card8     format             =0
            // SID       glyph[nGlyphs-1]   Glyph name array

            // Each element of the glyph array represents the name of the
            // corresponding glyph. This format should be used when the SIDs
            // are in a fairly random order. The number of glyphs (nGlyphs) is
            // the value of the count field in the
            // CharStrings INDEX. (There is
            // one less element in the glyph name array than nGlyphs because
            // the .notdef glyph name is omitted.)
            for (int i = 1; i < glyphs.Length; ++i)
            {
                ref Cff1GlyphData data = ref glyphs[i];
                data.GlyphName = this.GetSid(reader.ReadUInt16(), stringIndex);
            }
        }

        private void ReadCharsetsFormat1(BigEndianBinaryReader reader, string[] stringIndex, Cff1GlyphData[] glyphs)
        {
            // Table 18 Format 1
            // Type     Name                Description
            // Card8        format              =1
            // struct   Range1[<varies>]    Range1 array (see Table  19)

            // Table 19 Range1 Format (Charset)
            // Type      Name          Description
            // SID       first         First glyph in range
            // Card8     nLeft         Glyphs left in range(excluding first)

            // Each Range1 describes a group of sequential SIDs. The number
            // of ranges is not explicitly specified in the font. Instead, software
            // utilizing this data simply processes ranges until all glyphs in the
            // font are covered. This format is particularly suited to charsets
            // that are well ordered
            for (int i = 1; i < glyphs.Length;)
            {
                int sid = reader.ReadUInt16(); // First glyph in range
                int count = reader.ReadByte() + 1; // since it does not include first element.
                do
                {
                    ref Cff1GlyphData data = ref glyphs[i];
                    data.GlyphName = this.GetSid(sid, stringIndex);

                    count--;
                    i++;
                    sid++;
                }
                while (count > 0);
            }
        }

        private void ReadCharsetsFormat2(BigEndianBinaryReader reader, string[] stringIndex, Cff1GlyphData[] glyphs)
        {
            // note:eg, Adobe's source-code-pro font

            // Table 20 Format 2
            // Type          Name              Description
            // Card8         format            2
            // struct        Range2[<varies>]  Range2 array (see Table 21)
            //
            //-----------------------------------------------
            // Table 21 Range2 Format
            // Type          Name             Description
            // SID           first            First glyph in range
            // Card16        nLeft            Glyphs left in range (excluding first)
            //-----------------------------------------------

            // Format 2 differs from format 1 only in the size of the nLeft field in each range.
            // This format is most suitable for fonts with a large well - ordered charset — for example, for Asian CIDFonts.
            for (int i = 1; i < glyphs.Length;)
            {
                int sid = reader.ReadUInt16(); // First glyph in range
                int count = reader.ReadUInt16() + 1; // since it does not include first element.
                do
                {
                    ref Cff1GlyphData data = ref glyphs[i];
                    data.GlyphName = this.GetSid(sid, stringIndex);

                    count--;
                    i++;
                    sid++;
                }
                while (count > 0);
            }
        }

        private void ReadFDSelect(BigEndianBinaryReader reader, CidFontInfo cidFontInfo)
        {
            // http://wwwimages.adobe.com/www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5176.CFF.pdf
            // 19. FDSelect

            // The FDSelect associates an FD(Font DICT) with a glyph by
            // specifying an FD index for that glyph. The FD index is used to
            // access one of the Font DICTs stored in the Font DICT INDEX.

            // FDSelect data is located via the offset operand to the
            // FDSelect operator in the Top DICT.FDSelect data specifies a format - type
            // identifier byte followed by format-specific data.Two formats
            // are currently defined, as shown in Tables  27 and 28.
            // TODO: ...

            // FDSelect   12 37  number      –, FDSelect offset
            if (cidFontInfo.FDSelect == 0)
            {
                return;
            }

            // Table 27 Format 0
            //------------
            // Type    Name              Description
            //------------
            // Card8   format            =0
            // Card8   fd[nGlyphs]       FD selector array

            // Each element of the fd array(fds) represents the FD index of the corresponding glyph.
            // This format should be used when the FD indexes are in a fairly random order.
            // The number of glyphs(nGlyphs) is the value of the count field in the CharStrings INDEX.
            // (This format is identical to charset format 0 except that the.notdef glyph is included in this case.)

            // Table 28 Format 3
            //------------
            // Type    Name              Description
            //------------
            // Card8   format            =3
            // Card16  nRanges           Number of ranges
            // struct  Range3[nRanges]   Range3 array (see Table 29)
            // Card16  sentinel          Sentinel GID (see below)
            //------------

            // Table 29 Range3
            //------------
            // Type    Name              Description
            //------------
            // Card16  first             First glyph index in range
            // Card8   fd                FD index for all glyphs in range

            // Each Range3 describes a group of sequential GIDs that have the same FD index.
            // Each range includes GIDs from the ‘first’ GID up to, but not including,
            // the ‘first’ GID of the next range element.
            // Thus, elements of the Range3 array are ordered by increasing ‘first’ GIDs.
            // The first range must have a ‘first’ GID of 0.
            // A sentinel GID follows the last range element and serves to delimit the last range in the array.
            // (The sentinel GID is set equal to the number of glyphs in the font.
            // That is, its value is 1 greater than the last GID in the font.)
            // This format is particularly suited to FD indexes that are well ordered(the usual case).
            reader.BaseStream.Position = this.offset + cidFontInfo.FDSelect;
            byte format = reader.ReadByte();

            switch (format)
            {
                case 3:
                    ushort nRanges = reader.ReadUInt16();
                    var ranges = new FDRange3[nRanges + 1];

                    cidFontInfo.FdSelectFormat = 3;
                    cidFontInfo.FdRanges = ranges;
                    for (int i = 0; i < nRanges; ++i)
                    {
                        ranges[i] = new FDRange3(reader.ReadUInt16(), reader.ReadByte());
                    }

                    // end with //sentinel
                    ranges[nRanges] = new FDRange3(reader.ReadUInt16(), 0); // sentinel
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private FontDict[] ReadFDArray(BigEndianBinaryReader reader, CidFontInfo cidFontInfo)
        {
            if (cidFontInfo.FDArray == 0)
            {
                return Array.Empty<FontDict>();
            }

            reader.BaseStream.Position = this.offset + cidFontInfo.FDArray;

            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                return Array.Empty<FontDict>();
            }

            var fontDicts = new FontDict[offsets.Length];
            for (int i = 0; i < fontDicts.Length; ++i)
            {
                // read DICT data
                List<CffDataDicEntry> dic = this.ReadDICTData(reader, offsets[i].Length);

                // translate
                int offset = 0;
                int size = 0;
                int name = 0;

                foreach (CffDataDicEntry entry in dic)
                {
                    switch (entry.Operator.Name)
                    {
                        default:
                            throw new NotSupportedException();
                        case "FontName":
                            name = (int)entry.Operands[0].RealNumValue;
                            break;
                        case "Private": // private dic
                            size = (int)entry.Operands[0].RealNumValue;
                            offset = (int)entry.Operands[1].RealNumValue;
                            break;
                    }
                }

                fontDicts[i] = new FontDict(name, size, offset);
            }

            foreach (FontDict fdict in fontDicts)
            {
                reader.BaseStream.Position = this.offset + fdict.PrivateDicOffset;

                List<CffDataDicEntry> dicData = this.ReadDICTData(reader, fdict.PrivateDicSize);

                if (dicData.Count > 0)
                {
                    // Interpret the values of private dict
                    foreach (CffDataDicEntry dicEntry in dicData)
                    {
                        switch (dicEntry.Operator.Name)
                        {
                            case "Subrs":
                                int localSubrsOffset = (int)dicEntry.Operands[0].RealNumValue;
                                reader.BaseStream.Position = this.offset + fdict.PrivateDicOffset + localSubrsOffset;
                                fdict.LocalSubr = this.ReadSubrBuffer(reader);
                                break;

                            case "defaultWidthX":
                            case "nominalWidthX":
                                break;

                            default:
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("cff_pri_dic:" + dicEntry.Operator.Name);
#endif
                                break;
                        }
                    }
                }
            }

            return fontDicts;
        }

        private Cff1GlyphData[] ReadCharStringsIndex(
            BigEndianBinaryReader reader,
            CffTopDictionary topDictionary,
            byte[][] globalSubrRawBuffers,
            FontDict[] fontDicts,
            CffPrivateDictionary? privateDictionary)
        {
            // 14. CharStrings INDEX

            // This contains the charstrings of all the glyphs in a font stored in
            // an INDEX structure.

            // Charstring objects contained within this
            // INDEX are accessed by GID.

            // The first charstring(GID 0) must be
            // the.notdef glyph.

            // The number of glyphs available in a font may
            // be determined from the count field in the INDEX.

            //

            // The format of the charstring data, and therefore the method of
            // interpretation, is specified by the
            // CharstringType  operator in the Top DICT.

            // The CharstringType operator has a default value
            // of 2 indicating the Type 2 charstring format which was designed
            // in conjunction with CFF.

            // Type 1 charstrings are documented in
            // the “Adobe Type 1 Font Format” published by Addison - Wesley.

            // Type 2 charstrings are described in Adobe Technical Note #5177:
            // “Type 2 Charstring Format.” Other charstring types may also be
            // supported by this method.
            reader.BaseStream.Position = this.offset + this.charStringsOffset;
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                throw new InvalidFontFileException("No glyph data found.");
            }

#if DEBUG
            if (offsets.Length > ushort.MaxValue)
            {
                throw new NotSupportedException();
            }
#endif
            int glyphCount = offsets.Length;
            var glyphs = new Cff1GlyphData[glyphCount];
            Type2CharStringParser type2Parser = new(globalSubrRawBuffers, privateDictionary);

#if DEBUG
            double total = 0;
#endif

            // cid font or not
            var fdRangeProvider = new FDRangeProvider(topDictionary.CidFontInfo.FdRanges);
            bool isCidFont = topDictionary.CidFontInfo.FdRanges.Length > 0;

            for (int i = 0; i < glyphCount; ++i)
            {
                CffIndexOffset offset = offsets[i];
                byte[] buffer = reader.ReadBytes(offset.Length);
#if DEBUG
                // check
                byte lastByte = buffer[offset.Length - 1];
                if (lastByte is not (byte)Type2Operator1.Endchar and
                    not (byte)Type2Operator1.Callgsubr and
                    not (byte)Type2Operator1.Callsubr)
                {
                    // 5177.Type2
                    // Note 6 The charstring itself may end with a call(g)subr; the subroutine must
                    // then end with an endchar operator
                    // endchar
                    throw new Exception("invalid end byte?");
                }
#endif

                // Now we can parse the raw glyph instructions
#if DEBUG
                type2Parser.DbugCurrentGlyphIndex = (ushort)i;
#endif

                if (isCidFont)
                {
                    // select  proper local private dict
                    fdRangeProvider.SetCurrentGlyphIndex((ushort)i);
                    type2Parser.SetCidFontDict(fontDicts[fdRangeProvider.SelectedFDArray]);
                }

                Type2Instruction[]? instructions = null;
                Type2GlyphInstructionList instList = type2Parser.ParseType2CharString(buffer);
                if (instList != null)
                {
                    // use compact form or not
                    if (this.useCompactInstruction)
                    {
                        // this is our extension,
                        // if you don't want compact version
                        // just use original
                        instructions = this.instCompacter.Compact(instList.InnerInsts);

#if DEBUG
                        total += instructions.Length / (float)instList.InnerInsts.Count;
#endif

                    }
                    else
                    {
                        instructions = instList.InnerInsts.ToArray();
                    }
                }

                glyphs[i] = new((ushort)i, instructions ?? Array.Empty<Type2Instruction>());
            }

#if DEBUG
            if (this.useCompactInstruction)
            {
                double avg = total / glyphCount;
                System.Diagnostics.Debug.WriteLine("cff instruction compact avg:" + avg + "%");
            }
#endif

            return glyphs;
        }

        private void ReadFormat0Encoding(BigEndianBinaryReader reader)
        {
            // Table 11: Format 0
            // Type      Name            Description
            // Card8     format          = 0
            // Card8     nCodes          Number of encoded glyphs
            // Card8     code[nCodes]    Code array
            //-------
            // Each element of the code array represents the encoding for the
            // corresponding glyph. This format should be used when the
            // codes are in a fairly random order

            // we have read format field( 1st field) ..
            // so start with 2nd field
            int nCodes = reader.ReadByte();
            byte[] codes = reader.ReadBytes(nCodes);

            // TODO: Implement based on PDFPig
        }

        private void ReadFormat1Encoding(BigEndianBinaryReader reader)
        {
            // Table 12 Format 1
            // Type      Name              Description
            // Card8     format             = 1
            // Card8     nRanges           Number of code ranges
            // struct    Range1[nRanges]   Range1 array(see Table  13)
            //--------------
            int nRanges = reader.ReadByte();

            // Table 13 Range1 Format(Encoding)
            // Type        Name        Description
            // Card8       first       First code in range
            // Card8       nLeft       Codes left in range(excluding first)
            //--------------
            // Each Range1 describes a group of sequential codes. For
            // example, the codes 51 52 53 54 55 could be represented by the
            // Range1: 51 4, and a perfectly ordered encoding of 256 codes can
            // be described with the Range1: 0 255.

            // This format is particularly suited to encodings that are well ordered.

            // A few fonts have multiply - encoded glyphs which are not
            // supported directly by any of the above formats. This situation is
            // indicated by setting the high - order bit in the format byte and
            // supplementing the encoding, regardless of format type, as
            // shown in Table 14.

            // Table 14 Supplemental Encoding Data
            // Type         Name                Description
            // Card8        nSups               Number of supplementary mappings
            // struct    Supplement[nSups]   Supplementary encoding array(see Table  15 below)

            // Table 15 Supplement Format
            // Type      Name        Description
            // Card8     code        Encoding
            // SID       glyph       Name
        }
        
        private CffPrivateDictionary? ReadPrivateDict(BigEndianBinaryReader reader)
        {
            // per-font
            if (this.privateDICTLength == 0)
            {
                return null;
            }

            reader.BaseStream.Position = this.offset + this.privateDICTOffset;
            List<CffDataDicEntry> dicData = this.ReadDICTData(reader, this.privateDICTLength);
            byte[][] localSubrRawBuffers = Array.Empty<byte[]>();
            int defaultWidthX = 0;
            int nominalWidthX = 0;

            if (dicData.Count > 0)
            {
                // Interpret the values of private dict
                foreach (CffDataDicEntry dicEntry in dicData)
                {
                    switch (dicEntry.Operator.Name)
                    {
                        case "Subrs":
                            int localSubrsOffset = (int)dicEntry.Operands[0].RealNumValue;
                            reader.BaseStream.Position = this.offset + this.privateDICTOffset + localSubrsOffset;
                            localSubrRawBuffers = this.ReadLocalSubrs(reader);
                            break;

                        case "defaultWidthX":
                            defaultWidthX = (int)dicEntry.Operands[0].RealNumValue;
                            break;

                        case "nominalWidthX":
                            nominalWidthX = (int)dicEntry.Operands[0].RealNumValue;
                            break;

                        default:
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("cff_pri_dic:" + dicEntry.Operator.Name);
#endif
                            break;
                    }
                }
            }

            return new CffPrivateDictionary(localSubrRawBuffers, defaultWidthX, nominalWidthX);
        }

        private byte[][] ReadSubrBuffer(BigEndianBinaryReader reader)
        {
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                return Array.Empty<byte[]>();
            }

            byte[][] rawBufferList = new byte[offsets.Length][];

            for (int i = 0; i < rawBufferList.Length; ++i)
            {
                CffIndexOffset offset = offsets[i];
                rawBufferList[i] = reader.ReadBytes(offset.Length);
            }

            return rawBufferList;
        }

        private List<CffDataDicEntry> ReadDICTData(BigEndianBinaryReader reader, int length)
        {
            // 4. DICT Data

            // Font dictionary data comprising key-value pairs is represented
            // in a compact tokenized format that is similar to that used to
            // represent Type 1 charstrings.

            // Dictionary keys are encoded as 1- or 2-byte operators and dictionary values are encoded as
            // variable-size numeric operands that represent either integer or
            // real values.

            //-----------------------------
            // A DICT is simply a sequence of
            // operand(s)/operator bytes concatenated together.
            int maxIndex = (int)(reader.BaseStream.Position + length);
            List<CffDataDicEntry> dicData = new();
            while (reader.BaseStream.Position < maxIndex)
            {
                CffDataDicEntry dicEntry = this.ReadEntry(reader);
                dicData.Add(dicEntry);
            }

            return dicData;
        }

        private CffDataDicEntry ReadEntry(BigEndianBinaryReader reader)
        {
            //-----------------------------
            // An operator is preceded by the operand(s) that
            // specify its value.
            //--------------------------------

            //-----------------------------
            // Operators and operands may be distinguished by inspection of
            // their first byte:
            // 0–21 specify operators and
            // 28, 29, 30, and 32–254 specify operands(numbers).
            // Byte values 22–27, 31, and 255 are reserved.

            // An operator may be preceded by up to a maximum of 48 operands
            CffDataDicEntry dicEntry = new();
            List<CffOperand> operands = new();

            while (true)
            {
                byte b0 = reader.ReadByte();

                if (b0 is >= 0 and <= 21)
                {
                    // operators
                    dicEntry.Operator = this.ReadOperator(reader, b0);
                    break; // **break after found operator
                }
                else if (b0 is 28 or 29)
                {
                    int num = this.ReadIntegerNumber(reader, b0);
                    operands.Add(new CffOperand(num, OperandKind.IntNumber));
                }
                else if (b0 == 30)
                {
                    double num = this.ReadRealNumber(reader);
                    operands.Add(new CffOperand(num, OperandKind.RealNumber));
                }
                else if (b0 is >= 32 and <= 254)
                {
                    int num = this.ReadIntegerNumber(reader, b0);
                    operands.Add(new CffOperand(num, OperandKind.IntNumber));
                }
                else
                {
                    throw new NotSupportedException("invalid DICT data b0 byte: " + b0);
                }
            }

            dicEntry.Operands = operands.ToArray();
            return dicEntry;
        }

        private CFFOperator ReadOperator(BigEndianBinaryReader reader, byte b0)
        {
            // read operator key
            byte b1 = 0;
            if (b0 == 12)
            {
                // 2 bytes
                b1 = reader.ReadByte();
            }

            // get registered operator by its key
            return CFFOperator.GetOperatorByKey(b0, b1);
        }

        private double ReadRealNumber(BigEndianBinaryReader reader)
        {
            // from https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
            // A real number operand is provided in addition to integer
            // operands.This operand begins with a byte value of 30 followed
            // by a variable-length sequence of bytes.Each byte is composed
            // of two 4 - bit nibbles asdefined in Table 5.

            // The first nibble of a
            // pair is stored in the most significant 4 bits of a byte and the
            // second nibble of a pair is stored in the least significant 4 bits of a byte
            StringBuilder sb = this.pooledStringBuilder;
            sb.Clear(); // reset

            bool done = false;
            bool exponentMissing = false;
            while (!done)
            {
                int b = reader.ReadByte();

                int nb_0 = (b >> 4) & 0xf;
                int nb_1 = b & 0xf;

                for (int i = 0; !done && i < 2; ++i)
                {
                    int nibble = (i == 0) ? nb_0 : nb_1;

                    switch (nibble)
                    {
                        case 0x0:
                        case 0x1:
                        case 0x2:
                        case 0x3:
                        case 0x4:
                        case 0x5:
                        case 0x6:
                        case 0x7:
                        case 0x8:
                        case 0x9:
                            sb.Append(nibble);
                            exponentMissing = false;
                            break;
                        case 0xa:
                            sb.Append(".");
                            break;
                        case 0xb:
                            sb.Append("E");
                            exponentMissing = true;
                            break;
                        case 0xc:
                            sb.Append("E-");
                            exponentMissing = true;
                            break;
                        case 0xd:
                            break;
                        case 0xe:
                            sb.Append("-");
                            break;
                        case 0xf:
                            done = true;
                            break;
                        default:
                            throw new Exception("IllegalArgumentException");
                    }
                }
            }

            if (exponentMissing)
            {
                // the exponent is missing, just append "0" to avoid an exception
                // not sure if 0 is the correct value, but it seems to fit
                // see PDFBOX-1522
                sb.Append("0");
            }

            if (sb.Length == 0)
            {
                return 0d;
            }

            if (!double.TryParse(
                sb.ToString(),
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out double value))
            {
                throw new NotSupportedException();
            }

            return value;
        }

        private int ReadIntegerNumber(BigEndianBinaryReader reader, byte b0)
        {
            if (b0 == 28)
            {
                return reader.ReadInt16();
            }
            else if (b0 == 29)
            {
                return reader.ReadInt32();
            }
            else if (b0 is >= 32 and <= 246)
            {
                return b0 - 139;
            }
            else if (b0 is >= 247 and <= 250)
            {
                int b1 = reader.ReadByte();
                return ((b0 - 247) * 256) + b1 + 108;
            }
            else if (b0 is >= 251 and <= 254)
            {
                int b1 = reader.ReadByte();
                return (-(b0 - 251) * 256) - b1 - 108;
            }
            else
            {
                throw new InvalidFontFileException("Invalid DICT data b0 byte: " + b0);
            }
        }

        private bool TryReadIndexDataOffsets(BigEndianBinaryReader reader, [NotNullWhen(true)] out CffIndexOffset[]? value)
        {
            // INDEX Data
            // An INDEX is an array of variable-sized objects.It comprises a
            // header, an offset array, and object data.
            // The offset array specifies offsets within the object data.
            // An object is retrieved by
            // indexing the offset array and fetching the object at the
            // specified offset.
            // The object’s length can be determined by subtracting its offset
            // from the next offset in the offset array.
            // An additional offset is added at the end of the offset array so the
            // length of the last object may be determined.
            // The INDEX format is shown in Table 7

            // Table 7 INDEX Format
            // Type        Name                  Description
            // Card16      count                 Number of objects stored in INDEX
            // OffSize     offSize               Offset array element size
            // Offset      offset[count + 1]     Offset array(from byte preceding object data)
            // Card8       data[<varies>]        Object data

            // Offsets in the offset array are relative to the byte that precedes
            // the object data. Therefore the first element of the offset array
            // is always 1. (This ensures that every object has a corresponding
            // offset which is always nonzero and permits the efficient
            // implementation of dynamic object loading.)

            // An empty INDEX is represented by a count field with a 0 value
            // and no additional fields.Thus, the total size of an empty INDEX
            // is 2 bytes.

            // Note 2
            // An INDEX may be skipped by jumping to the offset specified by the last
            // element of the offset array
            ushort count = reader.ReadUInt16();
            if (count == 0)
            {
                value = null;
                return false;
            }

            int offSize = reader.ReadByte();
            int[] offsets = new int[count + 1];
            var indexElems = new CffIndexOffset[count];
            for (int i = 0; i <= count; ++i)
            {
                offsets[i] = reader.ReadOffset(offSize);
            }

            for (int i = 0; i < count; ++i)
            {
                indexElems[i] = new CffIndexOffset(offsets[i], offsets[i + 1] - offsets[i]);
            }

            value = indexElems;
            return true;
        }

        private readonly struct CffIndexOffset
        {
            /// <summary>
            /// The starting offset
            /// </summary>
            public readonly int Start;

            /// <summary>
            /// The length
            /// </summary>
            public readonly int Length;

            public CffIndexOffset(int start, int len)
            {
                this.Start = start;
                this.Length = len;
            }

#if DEBUG
            public override string ToString() => "offset:" + this.Start + ",len:" + this.Length;
#endif
        }

        private struct FDRangeProvider
        {
            // helper class
            private readonly FDRange3[] ranges;
            private ushort currentGlyphIndex;
            private ushort endGlyphIndexMax;
            private FDRange3 currentRange;
            private int currentSelectedRangeIndex;

            public FDRangeProvider(FDRange3[] ranges)
            {
                this.ranges = ranges;
                this.currentGlyphIndex = 0;
                this.currentSelectedRangeIndex = 0;

                if (ranges != null)
                {
                    this.currentRange = ranges[0];
                    this.endGlyphIndexMax = ranges[1].First;
                }
                else
                {
                    // empty
                    this.currentRange = default;
                    this.endGlyphIndexMax = 0;
                }

                this.SelectedFDArray = 0;
            }

            public byte SelectedFDArray { get; private set; }

            public void SetCurrentGlyphIndex(ushort index)
            {
                // find proper range for selected index
                if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
                {
                    // ok, in current range
                    this.SelectedFDArray = this.currentRange.Fd;
                }
                else
                {
                    // move to next range
                    this.currentSelectedRangeIndex++;
                    this.currentRange = this.ranges[this.currentSelectedRangeIndex];

                    this.endGlyphIndexMax = this.ranges[this.currentSelectedRangeIndex + 1].First;
                    if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
                    {
                        this.SelectedFDArray = this.currentRange.Fd;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                this.currentGlyphIndex = index;
            }
        }
    }
}

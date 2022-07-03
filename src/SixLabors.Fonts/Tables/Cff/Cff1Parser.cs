// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Parses a Compact Font Format (CFF) font program as described in The Compact Font Format specification (Adobe Technical Note #5176).
    /// A CFF font may contain multiple fonts and achieves compression by sharing details between fonts in the set.
    /// </summary>
    internal class Cff1Parser : CffParserBase
    {
        /// <summary>
        /// Latin 1 Encoding: ISO 8859-1 is a single-byte encoding that can represent the first 256 Unicode characters.
        /// </summary>
        private static readonly Encoding Iso88591 = Encoding.GetEncoding("ISO-8859-1");

        private long offset;
        private int charStringsOffset;
        private int charsetOffset;
        private int encodingOffset = -1;
        private int privateDICTOffset;
        private int privateDICTLength;

        public CffFont Load(BigEndianBinaryReader reader, long offset)
        {
            this.offset = offset;

            string fontName = this.ReadNameIndex(reader);

            List<CffDataDicEntry> dataDicEntries = this.ReadTopDictIndex(reader);
            string[] stringIndex = this.ReadStringIndex(reader);

            CffTopDictionary topDictionary = this.ResolveTopDictInfo(dataDicEntries, stringIndex);

            byte[][] globalSubrRawBuffers = this.ReadGlobalSubrIndex(reader);

            this.ReadFdSelect(reader, this.offset, topDictionary.CidFontInfo);
            FontDict[] fontDicts = this.ReadFdArray(reader, this.offset, topDictionary.CidFontInfo.FDArray);

            CffPrivateDictionary? privateDictionary = this.ReadPrivateDict(reader);
            CffGlyphData[] glyphs = this.ReadCharStringsIndex(reader, topDictionary, globalSubrRawBuffers, fontDicts, privateDictionary);

            this.ReadCharsets(reader, stringIndex, glyphs);
            this.ReadEncodings(reader);

            return new(fontName, topDictionary, glyphs);
        }

        private string ReadNameIndex(BigEndianBinaryReader reader)
        {
            if (!this.TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
            {
                throw new InvalidFontFileException("No name index found.");
            }

            // For Open Type the Name INDEX in the CFF data must contain only one entry;
            // that is, there must be only one font in the CFF FontSet.
            CffIndexOffset offset = offsets[0];
            return reader.ReadString(offset.Length, Iso88591);
        }

        private List<CffDataDicEntry> ReadTopDictIndex(BigEndianBinaryReader reader)
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
            return this.ReadDictData(reader, offsets[0].Length);
        }

        private string[] ReadStringIndex(BigEndianBinaryReader reader)
        {
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
                    if (actualRead != length)
                    {
                        throw new InvalidFontFileException("Invalid string length.");
                    }

                    stringIndex[i] = Iso88591.GetString(slice);
                }
                else
                {
                    stringIndex[i] = reader.ReadString(length, Iso88591);
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

        // TODO: We don't actually need this right now. Will be important though if we ever introduce subsetting.
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
            if (this.encodingOffset != -1)
            {
                byte encoding = reader.ReadByte();
                switch (encoding)
                {
                    case 0:
                        this.ReadFormat0Encoding(reader);
                        break;
                    case 1:
                        this.ReadFormat1Encoding(reader);
                        break;
                    default:

                        // TODO: Seek.
                        break;
                }
            }
        }

        private void ReadCharsets(BigEndianBinaryReader reader, string[] stringIndex, CffGlyphData[] glyphs)
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

        private void ReadCharsetsFormat0(BigEndianBinaryReader reader, string[] stringIndex, CffGlyphData[] glyphs)
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
                ref CffGlyphData data = ref glyphs[i];
                data.GlyphName = this.GetSid(reader.ReadUInt16(), stringIndex);
            }
        }

        private void ReadCharsetsFormat1(BigEndianBinaryReader reader, string[] stringIndex, CffGlyphData[] glyphs)
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
                    ref CffGlyphData data = ref glyphs[i];
                    data.GlyphName = this.GetSid(sid, stringIndex);

                    count--;
                    i++;
                    sid++;
                }
                while (count > 0);
            }
        }

        private void ReadCharsetsFormat2(BigEndianBinaryReader reader, string[] stringIndex, CffGlyphData[] glyphs)
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
                    ref CffGlyphData data = ref glyphs[i];
                    data.GlyphName = this.GetSid(sid, stringIndex);

                    count--;
                    i++;
                    sid++;
                }
                while (count > 0);
            }
        }

        private CffGlyphData[] ReadCharStringsIndex(
            BigEndianBinaryReader reader,
            CffTopDictionary topDictionary,
            byte[][] globalSubrBuffers,
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

            int glyphCount = offsets.Length;
            var glyphs = new CffGlyphData[glyphCount];
            byte[][]? localSubBuffer = privateDictionary?.LocalSubrRawBuffers;

            // Is the font a CID font?
            FDRangeProvider fdRangeProvider = new(topDictionary.CidFontInfo);
            bool isCidFont = topDictionary.CidFontInfo.FdRanges.Length > 0;

            for (int i = 0; i < glyphCount; ++i)
            {
                CffIndexOffset offset = offsets[i];
                byte[] charstringsBuffer = reader.ReadBytes(offset.Length);

                // Now we can parse the raw glyph instructions
                if (isCidFont)
                {
                    // Select  proper local private dict
                    fdRangeProvider.SetCurrentGlyphIndex((ushort)i);
                    localSubBuffer = fontDicts[fdRangeProvider.SelectedFDArray].LocalSubr;
                }

                glyphs[i] = new CffGlyphData(
                    (ushort)i,
                    globalSubrBuffers,
                    localSubBuffer ?? Array.Empty<byte[]>(),
                    privateDictionary?.NominalWidthX ?? 0,
                    charstringsBuffer,
                    1);
            }

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
            List<CffDataDicEntry> dicData = this.ReadDictData(reader, this.privateDICTLength);
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
                    }
                }
            }

            return new CffPrivateDictionary(localSubrRawBuffers, defaultWidthX, nominalWidthX);
        }
    }
}

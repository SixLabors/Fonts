// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Text;

namespace SixLabors.Fonts.Tables.General.Post
{
    [TableName(TableName)]
    internal class PostTable : Table
    {
        internal const string TableName = "post";
        private static readonly string[] AppleGlyphNameMap
            = new[]
        {
            ".notdef", ".null", "nonmarkingreturn", "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quotesingle", "parenleft", "parenright",
            "asterisk", "plus", "comma", "hyphen", "period", "slash", "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "colon", "semicolon",
            "less", "equal", "greater", "question", "at", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W",
            "X", "Y", "Z", "bracketleft", "backslash", "bracketright", "asciicircum", "underscore", "grave", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
            "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "braceleft", "bar", "braceright", "asciitilde", "Adieresis", "Aring", "Ccedilla", "Eacute",
            "Ntilde", "Odieresis", "Udieresis", "aacute", "agrave", "acircumflex", "adieresis", "atilde", "aring", "ccedilla", "eacute", "egrave", "ecircumflex", "edieresis",
            "iacute", "igrave", "icircumflex", "idieresis", "ntilde", "oacute", "ograve", "ocircumflex", "odieresis", "otilde", "uacute", "ugrave", "ucircumflex", "udieresis",
            "dagger", "degree", "cent", "sterling", "section", "bullet", "paragraph", "germandbls", "registered", "copyright", "trademark", "acute", "dieresis", "notequal",
            "AE", "Oslash", "infinity", "plusminus", "lessequal", "greaterequal", "yen", "mu", "partialdiff", "summation", "product", "pi", "integral", "ordfeminine",
            "ordmasculine", "Omega", "ae", "oslash", "questiondown", "exclamdown", "logicalnot", "radical", "florin", "approxequal", "Delta", "guillemotleft", "guillemotright",
            "ellipsis", "nonbreakingspace", "Agrave", "Atilde", "Otilde", "OE", "oe", "endash", "emdash", "quotedblleft", "quotedblright", "quoteleft", "quoteright", "divide",
            "lozenge", "ydieresis", "Ydieresis", "fraction", "currency", "guilsinglleft", "guilsinglright", "fi", "fl", "daggerdbl", "periodcentered", "quotesinglbase", "quotedblbase",
            "perthousand", "Acircumflex", "Ecircumflex", "Aacute", "Edieresis", "Egrave", "Iacute", "Icircumflex", "Idieresis", "Igrave", "Oacute", "Ocircumflex", "apple", "Ograve",
            "Uacute", "Ucircumflex", "Ugrave", "dotlessi", "circumflex", "tilde", "macron", "breve", "dotaccent", "ring", "cedilla", "hungarumlaut", "ogonek", "caron", "Lslash",
            "lslash", "Scaron", "scaron", "Zcaron", "zcaron", "brokenbar", "Eth", "eth", "Yacute", "yacute", "Thorn", "thorn", "minus", "multiply", "onesuperior", "twosuperior",
            "threesuperior", "onehalf", "onequarter", "threequarters", "franc", "Gbreve", "gbreve", "Idotaccent", "Scedilla", "scedilla", "Cacute", "cacute", "Ccaron", "ccaron", "dcroat"
        };

        public PostTable(
            ushort formatMajor,
            ushort formatMinor,
            short underlinePosition,
            short underlineThickness,
            float italicAngle,
            uint isFixedPitch,
            uint minMemType42,
            uint maxMemType42,
            uint minMemType1,
            uint maxMemType1,
            PostNameRecord[] postRecords)
        {
            this.FormatMajor = formatMajor;
            this.FormatMinor = formatMinor;
            this.UnderlinePosition = underlinePosition;
            this.UnderlineThickness = underlineThickness;
            this.ItalicAngle = italicAngle;
            this.IsFixedPitch = isFixedPitch;
            this.MinMemType42 = minMemType42;
            this.MaxMemType42 = maxMemType42;
            this.MinMemType1 = minMemType1;
            this.MaxMemType1 = maxMemType1;
            this.PostRecords = postRecords;
        }

        public PostNameRecord[] PostRecords { get; }

        public ushort FormatMajor { get; }

        public ushort FormatMinor { get; }

        public short UnderlinePosition { get; }

        public short UnderlineThickness { get; }

        public float ItalicAngle { get; }

        public uint IsFixedPitch { get; }

        public uint MinMemType42 { get; }

        public uint MaxMemType42 { get; }

        public uint MinMemType1 { get; }

        public uint MaxMemType1 { get; }

        public static PostTable? Load(FontReader fontReader)
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

        public static PostTable Load(BigEndianBinaryReader reader)
        {
            // HEADER
            // Type            | Name                | Description
            // ----------------|---------------------|---------------------------------------------------------------
            // Version16Dot16  | version             | 0x00010000 for version 1.0, 0x00020000 for version 2.0, 0x00025000 for version 2.5 (deprecated), 0x00030000 for version 3.0
            // Fixed           | italicAngle         | Italic angle in counter-clockwise degrees from the vertical. Zero for upright text, negative for text that leans to the right (forward).
            // FWORD           | underlinePosition   | This is the suggested distance of the top of the underline from the baseline (negative values indicate below baseline). The PostScript definition of this FontInfo dictionary key (the y coordinate of the center of the stroke) is not used for historical reasons. The value of the PostScript key may be calculated by subtracting half the underlineThickness from the value of this field.
            // FWORD           | underlineThickness  | Suggested values for the underline thickness. In general, the underline thickness should match the thickness of the underscore character (U+005F LOW LINE), and should also match the strikeout thickness, which is specified in the OS/2 table.
            // uint32          | isFixedPitch        | Set to 0 if the font is proportionally spaced, non-zero if the font is not proportionally spaced (i.e. monospaced).
            // uint32          | minMemType42        | Minimum memory usage when an OpenType font is downloaded.
            // uint32          | maxMemType42        | Maximum memory usage when an OpenType font is downloaded.
            // uint32          | minMemType1         | Minimum memory usage when an OpenType font is downloaded as a Type 1 font.
            // uint32          | maxMemType1         | Maximum memory usage when an OpenType font is downloaded as a Type 1 font.

            // FORMAT 2.0
            // Type    | Name                        | Description
            // --------|-----------------------------|--------------------------------------------------------------
            // uint16  | numGlyphs                   | Number of glyphs (this should be the same as numGlyphs in 'maxp' table).
            // uint16  | glyphNameIndex[numGlyphs]   | Array of indices into the string data. See below for details.
            // uint8   | stringData[variable]        | Storage for the string data.
            ushort formatMajor = reader.ReadUInt16();
            ushort formatMinor = reader.ReadUInt16();
            float italicAngle = reader.ReadFixed();
            short underlinePosition = reader.ReadFWORD();
            short underlineThickness = reader.ReadFWORD();
            uint isFixedPitch = reader.ReadUInt32();
            uint minMemType42 = reader.ReadUInt32();
            uint maxMemType42 = reader.ReadUInt32();
            uint minMemType1 = reader.ReadUInt32();
            uint maxMemType1 = reader.ReadUInt32();

            var records = new PostNameRecord[0];

            if (formatMajor == 1)
            {
                // Supported, no extra subtables needed
            }
            else if (formatMajor == 2 && formatMinor == 0)
            {
                ushort numGlyphs = reader.ReadUInt16();
                records = new PostNameRecord[numGlyphs];

                ushort[] glyphIndices = reader.ReadUInt16Array(numGlyphs);

                for (int i = 0; i < numGlyphs; i++)
                {
                    ushort glyphNameIndex = glyphIndices[i];
                    string name;

                    // < 258 is a standard fixed apple mapping
                    if (glyphNameIndex <= 257)
                    {
                        name = AppleGlyphNameMap[glyphNameIndex];
                    }
                    else
                    {
                        byte strLength = reader.ReadByte();
                        name = reader.ReadString(strLength, Encoding.ASCII);
                    }

                    records[i] = new PostNameRecord(glyphNameIndex, name);
                }
            }
            else if (formatMajor > 3)
            {
                throw new NotSupportedException($"{TableName} table format {formatMajor}.{formatMinor} is not supported.");
            }

            // TODO: Validate maximum numbers against maxp table.
            return new PostTable(
                formatMajor,
                formatMinor,
                underlinePosition,
                underlineThickness,
                italicAngle,
                isFixedPitch,
                minMemType42,
                maxMemType42,
                minMemType1,
                maxMemType1,
                records);
        }

        public string? GetPostScriptName(int nameIndex)
        {
            if (this.PostRecords is not null)
            {
                for (int i = 0; i < this.PostRecords.Length; i++)
                {
                    PostNameRecord p = this.PostRecords[i];

                    if (p.NameIndex == nameIndex)
                    {
                        return p.Name;
                    }
                }
            }

            return null;
        }
    }
}

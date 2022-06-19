// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General
{
    internal sealed class OS2Table : Table
    {
        internal const string TableName = "OS/2";

        private readonly ushort styleType;
        private readonly byte[] panose;
        private readonly short capHeight;
        private readonly short familyClass;
        private readonly short heightX;
        private readonly string tag;
        private readonly ushort codePageRange1;
        private readonly ushort codePageRange2;
        private readonly uint unicodeRange1;
        private readonly uint unicodeRange2;
        private readonly uint unicodeRange3;
        private readonly uint unicodeRange4;
        private readonly ushort breakChar;
        private readonly ushort defaultChar;
        private readonly ushort firstCharIndex;
        private readonly ushort lastCharIndex;
        private readonly ushort lowerOpticalPointSize;
        private readonly ushort maxContext;
        private readonly ushort upperOpticalPointSize;
        private readonly ushort weightClass;
        private readonly ushort widthClass;
        private readonly short averageCharWidth;

        public OS2Table(
            short averageCharWidth,
            ushort weightClass,
            ushort widthClass,
            ushort styleType,
            short subscriptXSize,
            short subscriptYSize,
            short subscriptXOffset,
            short subscriptYOffset,
            short superscriptXSize,
            short superscriptYSize,
            short superscriptXOffset,
            short superscriptYOffset,
            short strikeoutSize,
            short strikeoutPosition,
            short familyClass,
            byte[] panose,
            uint unicodeRange1,
            uint unicodeRange2,
            uint unicodeRange3,
            uint unicodeRange4,
            string tag,
            FontStyleSelection fontStyle,
            ushort firstCharIndex,
            ushort lastCharIndex,
            short typoAscender,
            short typoDescender,
            short typoLineGap,
            ushort winAscent,
            ushort winDescent)
        {
            this.averageCharWidth = averageCharWidth;
            this.weightClass = weightClass;
            this.widthClass = widthClass;
            this.styleType = styleType;
            this.SubscriptXSize = subscriptXSize;
            this.SubscriptYSize = subscriptYSize;
            this.SubscriptXOffset = subscriptXOffset;
            this.SubscriptYOffset = subscriptYOffset;
            this.SuperscriptXSize = superscriptXSize;
            this.SuperscriptYSize = superscriptYSize;
            this.SuperscriptXOffset = superscriptXOffset;
            this.SuperscriptYOffset = superscriptYOffset;
            this.StrikeoutSize = strikeoutSize;
            this.StrikeoutPosition = strikeoutPosition;
            this.familyClass = familyClass;
            this.panose = panose;
            this.unicodeRange1 = unicodeRange1;
            this.unicodeRange2 = unicodeRange2;
            this.unicodeRange3 = unicodeRange3;
            this.unicodeRange4 = unicodeRange4;
            this.tag = tag;
            this.FontStyle = fontStyle;
            this.firstCharIndex = firstCharIndex;
            this.lastCharIndex = lastCharIndex;
            this.TypoAscender = typoAscender;
            this.TypoDescender = typoDescender;
            this.TypoLineGap = typoLineGap;
            this.WinAscent = winAscent;
            this.WinDescent = winDescent;
        }

        public OS2Table(
            OS2Table version0Table,
            ushort codePageRange1,
            ushort codePageRange2,
            short heightX,
            short capHeight,
            ushort defaultChar,
            ushort breakChar,
            ushort maxContext)
            : this(
                version0Table.averageCharWidth,
                version0Table.weightClass,
                version0Table.widthClass,
                version0Table.styleType,
                version0Table.SubscriptXSize,
                version0Table.SubscriptYSize,
                version0Table.SubscriptXOffset,
                version0Table.SubscriptYOffset,
                version0Table.SuperscriptXSize,
                version0Table.SuperscriptYSize,
                version0Table.SuperscriptXOffset,
                version0Table.SuperscriptYOffset,
                version0Table.StrikeoutSize,
                version0Table.StrikeoutPosition,
                version0Table.familyClass,
                version0Table.panose,
                version0Table.unicodeRange1,
                version0Table.unicodeRange2,
                version0Table.unicodeRange3,
                version0Table.unicodeRange4,
                version0Table.tag,
                version0Table.FontStyle,
                version0Table.firstCharIndex,
                version0Table.lastCharIndex,
                version0Table.TypoAscender,
                version0Table.TypoDescender,
                version0Table.TypoLineGap,
                version0Table.WinAscent,
                version0Table.WinDescent)
        {
            this.codePageRange1 = codePageRange1;
            this.codePageRange2 = codePageRange2;
            this.heightX = heightX;
            this.capHeight = capHeight;
            this.defaultChar = defaultChar;
            this.breakChar = breakChar;
            this.maxContext = maxContext;
        }

        public OS2Table(OS2Table versionLessThan5Table, ushort lowerOpticalPointSize, ushort upperOpticalPointSize)
            : this(
                versionLessThan5Table,
                versionLessThan5Table.codePageRange1,
                versionLessThan5Table.codePageRange2,
                versionLessThan5Table.heightX,
                versionLessThan5Table.capHeight,
                versionLessThan5Table.defaultChar,
                versionLessThan5Table.breakChar,
                versionLessThan5Table.maxContext)
        {
            this.lowerOpticalPointSize = lowerOpticalPointSize;
            this.upperOpticalPointSize = upperOpticalPointSize;
        }

        internal enum FontStyleSelection : ushort
        {
            // 0    bit 1   ITALIC  Font contains italic or oblique characters, otherwise they are upright.
            ITALIC = 1,

            // 1        UNDERSCORE  Characters are underscored.
            UNDERSCORE = 1 << 1,

            // 2        NEGATIVE    Characters have their foreground and background reversed.
            NEGATIVE = 1 << 2,

            // 3        OUTLINED    Outline (hollow) characters, otherwise they are solid.
            OUTLINED = 1 << 3,

            // 4        STRIKEOUT   Characters are overstruck.
            STRIKEOUT = 1 << 4,

            // 5    bit 0   BOLD    Characters are emboldened.
            BOLD = 1 << 5,

            // 6        REGULAR Characters are in the standard weight/style for the font.
            REGULAR = 1 << 6,

            // 7        USE_TYPO_METRICS    If set, it is strongly recommended to use OS/2.typoAscender - OS/2.typoDescender+ OS/2.typoLineGap as a value for default line spacing for this font.
            USE_TYPO_METRICS = 1 << 7,

            // 8        WWS The font has ‘name’ table strings consistent with a weight/width/slope family without requiring use of ‘name’ IDs 21 and 22. (Please see more detailed description below.)
            WWS = 1 << 8,

            // 9        OBLIQUE Font contains oblique characters.
            OBLIQUE = 1 << 9,

            // 10–15        <reserved>  Reserved; set to 0.
        }

        public FontStyleSelection FontStyle { get; }

        public short TypoAscender { get; }

        public short TypoDescender { get; }

        public short TypoLineGap { get; }

        public ushort WinAscent { get; }

        public ushort WinDescent { get; }

        public short StrikeoutPosition { get; }

        public short StrikeoutSize { get; }

        public short SubscriptXOffset { get; }

        public short SubscriptXSize { get; }

        public short SubscriptYOffset { get; }

        public short SubscriptYSize { get; }

        public short SuperscriptXOffset { get; }

        public short SuperscriptXSize { get; }

        public short SuperscriptYOffset { get; }

        public short SuperscriptYSize { get; }

        public static OS2Table? Load(FontReader fontReader)
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

        public static OS2Table Load(BigEndianBinaryReader reader)
        {
            // Version 1.0
            // Type   | Name                   | Comments
            // -------|------------------------|-----------------------
            // uint16 |version                 | 0x0005
            // int16  |xAvgCharWidth           |
            // uint16 |usWeightClass           |
            // uint16 |usWidthClass            |
            // uint16 |fsType                  |
            // int16  |ySubscriptXSize         |
            // int16  |ySubscriptYSize         |
            // int16  |ySubscriptXOffset       |
            // int16  |ySubscriptYOffset       |
            // int16  |ySuperscriptXSize       |
            // int16  |ySuperscriptYSize       |
            // int16  |ySuperscriptXOffset     |
            // int16  |ySuperscriptYOffset     |
            // int16  |yStrikeoutSize          |
            // int16  |yStrikeoutPosition      |
            // int16  |sFamilyClass            |
            // uint8  |panose[10]              |
            // uint32 |ulUnicodeRange1         | Bits 0–31
            // uint32 |ulUnicodeRange2         | Bits 32–63
            // uint32 |ulUnicodeRange3         | Bits 64–95
            // uint32 |ulUnicodeRange4         | Bits 96–127
            // Tag    |achVendID               |
            // uint16 |fsSelection             |
            // uint16 |usFirstCharIndex        |
            // uint16 |usLastCharIndex         |
            // int16  |sTypoAscender           |
            // int16  |sTypoDescender          |
            // int16  |sTypoLineGap            |
            // uint16 |usWinAscent             |
            // uint16 |usWinDescent            |
            // uint32 |ulCodePageRange1        | Bits 0–31
            // uint32 |ulCodePageRange2        | Bits 32–63
            // int16  |sxHeight                |
            // int16  |sCapHeight              |
            // uint16 |usDefaultChar           |
            // uint16 |usBreakChar             |
            // uint16 |usMaxContext            |
            // uint16 |usLowerOpticalPointSize |
            // uint16 |usUpperOpticalPointSize |
            ushort version = reader.ReadUInt16(); // assert 0x0005
            short averageCharWidth = reader.ReadInt16();
            ushort weightClass = reader.ReadUInt16();
            ushort widthClass = reader.ReadUInt16();
            ushort styleType = reader.ReadUInt16();
            short subscriptXSize = reader.ReadInt16();
            short subscriptYSize = reader.ReadInt16();
            short subscriptXOffset = reader.ReadInt16();
            short subscriptYOffset = reader.ReadInt16();

            short superscriptXSize = reader.ReadInt16();
            short superscriptYSize = reader.ReadInt16();
            short superscriptXOffset = reader.ReadInt16();
            short superscriptYOffset = reader.ReadInt16();

            short strikeoutSize = reader.ReadInt16();
            short strikeoutPosition = reader.ReadInt16();
            short familyClass = reader.ReadInt16();
            byte[] panose = reader.ReadUInt8Array(10);
            uint unicodeRange1 = reader.ReadUInt32(); // Bits 0–31
            uint unicodeRange2 = reader.ReadUInt32(); // Bits 32–63
            uint unicodeRange3 = reader.ReadUInt32(); // Bits 64–95
            uint unicodeRange4 = reader.ReadUInt32(); // Bits 96–127
            string tag = reader.ReadTag();
            FontStyleSelection fontStyle = reader.ReadUInt16<FontStyleSelection>();
            ushort firstCharIndex = reader.ReadUInt16();
            ushort lastCharIndex = reader.ReadUInt16();
            short typoAscender = reader.ReadInt16();
            short typoDescender = reader.ReadInt16();
            short typoLineGap = reader.ReadInt16();
            ushort winAscent = reader.ReadUInt16();
            ushort winDescent = reader.ReadUInt16();

            var version0Table = new OS2Table(
                    averageCharWidth,
                    weightClass,
                    widthClass,
                    styleType,
                    subscriptXSize,
                    subscriptYSize,
                    subscriptXOffset,
                    subscriptYOffset,
                    superscriptXSize,
                    superscriptYSize,
                    superscriptXOffset,
                    superscriptYOffset,
                    strikeoutSize,
                    strikeoutPosition,
                    familyClass,
                    panose,
                    unicodeRange1,
                    unicodeRange2,
                    unicodeRange3,
                    unicodeRange4,
                    tag,
                    fontStyle,
                    firstCharIndex,
                    lastCharIndex,
                    typoAscender,
                    typoDescender,
                    typoLineGap,
                    winAscent,
                    winDescent);

            if (version == 0)
            {
                return version0Table;
            }

            short heightX = 0;
            short capHeight = 0;

            ushort defaultChar = 0;
            ushort breakChar = 0;
            ushort maxContext = 0;

            ushort codePageRange1 = reader.ReadUInt16(); // Bits 0–31
            ushort codePageRange2 = reader.ReadUInt16(); // Bits 32–63

            // fields exist only in > v1 https://docs.microsoft.com/en-us/typography/opentype/spec/os2
            if (version > 1)
            {
                heightX = reader.ReadInt16();
                capHeight = reader.ReadInt16();
                defaultChar = reader.ReadUInt16();
                breakChar = reader.ReadUInt16();
                maxContext = reader.ReadUInt16();
            }

            var versionLessThan5Table = new OS2Table(
                    version0Table,
                    codePageRange1,
                    codePageRange2,
                    heightX,
                    capHeight,
                    defaultChar,
                    breakChar,
                    maxContext);

            if (version < 5)
            {
                return versionLessThan5Table;
            }

            ushort lowerOpticalPointSize = reader.ReadUInt16();
            ushort upperOpticalPointSize = reader.ReadUInt16();

            return new OS2Table(
                versionLessThan5Table,
                lowerOpticalPointSize,
                upperOpticalPointSize);
        }
    }
}

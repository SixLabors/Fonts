// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the OS/2 and Windows metrics table, which contains metrics required for Windows and OS/2.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2"/>
/// </summary>
internal sealed class OS2Table : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "OS/2";

    /// <summary>
    /// The font embedding licensing rights (fsType).
    /// </summary>
    private readonly ushort styleType;

    /// <summary>
    /// The PANOSE classification number.
    /// </summary>
    private readonly byte[] panose;

    /// <summary>
    /// The cap height in font design units.
    /// </summary>
    private readonly short capHeight;

    /// <summary>
    /// The font family class and subclass (sFamilyClass).
    /// </summary>
    private readonly short familyClass;

    /// <summary>
    /// The x-height in font design units.
    /// </summary>
    private readonly short heightX;

    /// <summary>
    /// The four-character font vendor identification tag.
    /// </summary>
    private readonly string tag;

    /// <summary>
    /// The code page range bits 0-31.
    /// </summary>
    private readonly ushort codePageRange1;

    /// <summary>
    /// The code page range bits 32-63.
    /// </summary>
    private readonly ushort codePageRange2;

    /// <summary>
    /// The Unicode range bits 0-31.
    /// </summary>
    private readonly uint unicodeRange1;

    /// <summary>
    /// The Unicode range bits 32-63.
    /// </summary>
    private readonly uint unicodeRange2;

    /// <summary>
    /// The Unicode range bits 64-95.
    /// </summary>
    private readonly uint unicodeRange3;

    /// <summary>
    /// The Unicode range bits 96-127.
    /// </summary>
    private readonly uint unicodeRange4;

    /// <summary>
    /// The break character (usBreakChar).
    /// </summary>
    private readonly ushort breakChar;

    /// <summary>
    /// The default character displayed when a requested character is not in the font.
    /// </summary>
    private readonly ushort defaultChar;

    /// <summary>
    /// The minimum Unicode index in this font.
    /// </summary>
    private readonly ushort firstCharIndex;

    /// <summary>
    /// The maximum Unicode index in this font.
    /// </summary>
    private readonly ushort lastCharIndex;

    /// <summary>
    /// The lower value of the size range for which this font is designed (version 5+).
    /// </summary>
    private readonly ushort lowerOpticalPointSize;

    /// <summary>
    /// The maximum length of a target glyph context for any feature in this font.
    /// </summary>
    private readonly ushort maxContext;

    /// <summary>
    /// The upper value of the size range for which this font is designed (version 5+).
    /// </summary>
    private readonly ushort upperOpticalPointSize;

    /// <summary>
    /// The visual weight class of the font (usWeightClass).
    /// </summary>
    private readonly ushort weightClass;

    /// <summary>
    /// The relative change from the normal aspect ratio (usWidthClass).
    /// </summary>
    private readonly ushort widthClass;

    /// <summary>
    /// The average weighted width of the lower case letters and space.
    /// </summary>
    private readonly short averageCharWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="OS2Table"/> class with version 0 fields.
    /// </summary>
    /// <param name="averageCharWidth">The average character width.</param>
    /// <param name="weightClass">The visual weight class.</param>
    /// <param name="widthClass">The relative width class.</param>
    /// <param name="styleType">The embedding licensing rights.</param>
    /// <param name="subscriptXSize">The horizontal size for subscripts.</param>
    /// <param name="subscriptYSize">The vertical size for subscripts.</param>
    /// <param name="subscriptXOffset">The horizontal offset for subscripts.</param>
    /// <param name="subscriptYOffset">The vertical offset for subscripts.</param>
    /// <param name="superscriptXSize">The horizontal size for superscripts.</param>
    /// <param name="superscriptYSize">The vertical size for superscripts.</param>
    /// <param name="superscriptXOffset">The horizontal offset for superscripts.</param>
    /// <param name="superscriptYOffset">The vertical offset for superscripts.</param>
    /// <param name="strikeoutSize">The width of the strikeout stroke.</param>
    /// <param name="strikeoutPosition">The position of the strikeout stroke relative to the baseline.</param>
    /// <param name="familyClass">The font family class and subclass.</param>
    /// <param name="panose">The PANOSE classification bytes.</param>
    /// <param name="unicodeRange1">Unicode range bits 0-31.</param>
    /// <param name="unicodeRange2">Unicode range bits 32-63.</param>
    /// <param name="unicodeRange3">Unicode range bits 64-95.</param>
    /// <param name="unicodeRange4">Unicode range bits 96-127.</param>
    /// <param name="tag">The four-character vendor identification tag.</param>
    /// <param name="fontStyle">The font style selection flags.</param>
    /// <param name="firstCharIndex">The minimum Unicode index.</param>
    /// <param name="lastCharIndex">The maximum Unicode index.</param>
    /// <param name="typoAscender">The typographic ascender.</param>
    /// <param name="typoDescender">The typographic descender.</param>
    /// <param name="typoLineGap">The typographic line gap.</param>
    /// <param name="winAscent">The Windows ascent metric.</param>
    /// <param name="winDescent">The Windows descent metric.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="OS2Table"/> class with version 1-4 fields.
    /// </summary>
    /// <param name="version0Table">The base version 0 table to extend.</param>
    /// <param name="codePageRange1">Code page range bits 0-31.</param>
    /// <param name="codePageRange2">Code page range bits 32-63.</param>
    /// <param name="heightX">The x-height.</param>
    /// <param name="capHeight">The cap height.</param>
    /// <param name="defaultChar">The default character index.</param>
    /// <param name="breakChar">The break character index.</param>
    /// <param name="maxContext">The maximum target glyph context length.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="OS2Table"/> class with version 5 fields.
    /// </summary>
    /// <param name="versionLessThan5Table">The base table (version &lt; 5) to extend.</param>
    /// <param name="lowerOpticalPointSize">The lower optical point size.</param>
    /// <param name="upperOpticalPointSize">The upper optical point size.</param>
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

    /// <summary>
    /// Font style selection flags (fsSelection).
    /// </summary>
    [Flags]
    internal enum FontStyleSelection : ushort
    {
        /// <summary>
        /// No style flags set.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Font contains italic or oblique characters.
        /// </summary>
        ITALIC = 1,

        /// <summary>
        /// Characters are underscored.
        /// </summary>
        UNDERSCORE = 1 << 1,

        /// <summary>
        /// Characters have their foreground and background reversed.
        /// </summary>
        NEGATIVE = 1 << 2,

        /// <summary>
        /// Outline (hollow) characters, otherwise they are solid.
        /// </summary>
        OUTLINED = 1 << 3,

        /// <summary>
        /// Characters are overstruck.
        /// </summary>
        STRIKEOUT = 1 << 4,

        /// <summary>
        /// Characters are emboldened.
        /// </summary>
        BOLD = 1 << 5,

        /// <summary>
        /// Characters are in the standard weight/style for the font.
        /// </summary>
        REGULAR = 1 << 6,

        /// <summary>
        /// If set, it is strongly recommended to use OS/2.typoAscender - OS/2.typoDescender + OS/2.typoLineGap
        /// as a value for default line spacing.
        /// </summary>
        USE_TYPO_METRICS = 1 << 7,

        /// <summary>
        /// The font has ‘name’ table strings consistent with a weight/width/slope family
        /// without requiring use of ‘name’ IDs 21 and 22.
        /// </summary>
        WWS = 1 << 8,

        /// <summary>
        /// Font contains oblique characters.
        /// </summary>
        OBLIQUE = 1 << 9,
    }

    /// <summary>
    /// Gets the font style selection flags.
    /// </summary>
    public FontStyleSelection FontStyle { get; }

    /// <summary>
    /// Gets the typographic ascender value.
    /// </summary>
    public short TypoAscender { get; }

    /// <summary>
    /// Gets the typographic descender value.
    /// </summary>
    public short TypoDescender { get; }

    /// <summary>
    /// Gets the typographic line gap value.
    /// </summary>
    public short TypoLineGap { get; }

    /// <summary>
    /// Gets the Windows ascent metric used for clipping.
    /// </summary>
    public ushort WinAscent { get; }

    /// <summary>
    /// Gets the Windows descent metric used for clipping.
    /// </summary>
    public ushort WinDescent { get; }

    /// <summary>
    /// Gets the position of the strikeout stroke relative to the baseline.
    /// </summary>
    public short StrikeoutPosition { get; }

    /// <summary>
    /// Gets the width of the strikeout stroke in font design units.
    /// </summary>
    public short StrikeoutSize { get; }

    /// <summary>
    /// Gets the horizontal offset for subscript characters.
    /// </summary>
    public short SubscriptXOffset { get; }

    /// <summary>
    /// Gets the horizontal size for subscript characters.
    /// </summary>
    public short SubscriptXSize { get; }

    /// <summary>
    /// Gets the vertical offset for subscript characters.
    /// </summary>
    public short SubscriptYOffset { get; }

    /// <summary>
    /// Gets the vertical size for subscript characters.
    /// </summary>
    public short SubscriptYSize { get; }

    /// <summary>
    /// Gets the horizontal offset for superscript characters.
    /// </summary>
    public short SuperscriptXOffset { get; }

    /// <summary>
    /// Gets the horizontal size for superscript characters.
    /// </summary>
    public short SuperscriptXSize { get; }

    /// <summary>
    /// Gets the vertical offset for superscript characters.
    /// </summary>
    public short SuperscriptYOffset { get; }

    /// <summary>
    /// Gets the vertical size for superscript characters.
    /// </summary>
    public short SuperscriptYSize { get; }

    /// <summary>
    /// Loads the <see cref="OS2Table"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="OS2Table"/>, or <see langword="null"/> if the table is not present.</returns>
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

    /// <summary>
    /// Loads the <see cref="OS2Table"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="OS2Table"/>.</returns>
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

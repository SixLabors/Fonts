// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the font header table, which contains global information about the font.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head"/>
/// </summary>
internal class HeadTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "head";

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadTable"/> class.
    /// </summary>
    /// <param name="flags">The font header flags.</param>
    /// <param name="macStyle">The Mac style flags.</param>
    /// <param name="unitsPerEm">The number of font design units per em.</param>
    /// <param name="created">The date the font was created.</param>
    /// <param name="modified">The date the font was last modified.</param>
    /// <param name="bounds">The bounding box for all glyphs in the font.</param>
    /// <param name="lowestRecPPEM">The smallest readable size in pixels.</param>
    /// <param name="indexToLocFormat">The format of the index-to-location table.</param>
    public HeadTable(
        HeadFlags flags,
        HeadMacStyle macStyle,
        ushort unitsPerEm,
        DateTime created,
        DateTime modified,
        Bounds bounds,
        ushort lowestRecPPEM,
        IndexLocationFormats indexToLocFormat)
    {
        this.Flags = flags;
        this.MacStyle = macStyle;
        this.UnitsPerEm = unitsPerEm;
        this.Created = created;
        this.Modified = modified;
        this.Bounds = bounds;
        this.LowestRecPPEM = lowestRecPPEM;
        this.IndexLocationFormat = indexToLocFormat;
    }

    /// <summary>
    /// Specifies the format of the index-to-location ('loca') table offsets.
    /// </summary>
    internal enum IndexLocationFormats : short
    {
        /// <summary>
        /// Short offsets (Offset16).
        /// </summary>
        Offset16 = 0,

        /// <summary>
        /// Long offsets (Offset32).
        /// </summary>
        Offset32 = 1,
    }

    /// <summary>
    /// Font header flags indicating various font characteristics.
    /// </summary>
    [Flags]
    internal enum HeadFlags : ushort
    {
        // Bit 0: Baseline for font at y = 0;
        // Bit 1: Left sidebearing point at x = 0(relevant only for TrueType rasterizers) — see the note below regarding variable fonts;
        // Bit 2: Instructions may depend on point size;
        // Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear;
        // Bit 4: Instructions may alter advance width(the advance widths might not scale linearly);
        // Bit 5: This bit is not used in OpenType, and should not be set in order to ensure compatible behavior on all platforms.If set, it may result in different behavior for vertical layout in some platforms. (See Apple's specification for details regarding behavior in Apple platforms.)
        // Bits 6–10: These bits are not used in Opentype and should always be cleared. (See Apple's specification for details regarding legacy used in Apple platforms.)
        // Bit 11: Font data is ‘lossless’ as a results of having been subjected to optimizing transformation and/or compression (such as e.g.compression mechanisms defined by ISO/IEC 14496-18, MicroType Express, WOFF 2.0 or similar) where the original font functionality and features are retained but the binary compatibility between input and output font files is not guaranteed.As a result of the applied transform, the ‘DSIG’ Table may also be invalidated.
        // Bit 12: Font converted (produce compatible metrics)
        // Bit 13: Font optimized for ClearType™. Note, fonts that rely on embedded bitmaps (EBDT) for rendering should not be considered optimized for ClearType, and therefore should keep this bit cleared.
        // Bit 14: Last Resort font.If set, indicates that the glyphs encoded in the cmap subtables are simply generic symbolic representations of code point ranges and don’t truly represent support for those code points.If unset, indicates that the glyphs encoded in the cmap subtables represent proper support for those code points.
        // Bit 15: Reserved, set to 0

        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Baseline for font at y = 0.
        /// </summary>
        BaselineY0 = 1 << 0,

        /// <summary>
        /// Left sidebearing point at x = 0 (relevant only for TrueType rasterizers).
        /// </summary>
        LeftSidebearingPointAtX0 = 1 << 1,

        /// <summary>
        /// Instructions may depend on point size.
        /// </summary>
        InstructionDependOnPointSize = 1 << 2,

        /// <summary>
        /// Force ppem to integer values for all internal scaler math.
        /// </summary>
        ForcePPEMToInt = 1 << 3,

        /// <summary>
        /// Instructions may alter advance width (the advance widths might not scale linearly).
        /// </summary>
        InstructionAlterAdvancedWidth = 1 << 4,

        // 1<<5 not used
        // 1<<6 - 1<<10 not used

        /// <summary>
        /// Font data is lossless as a result of having been compressed or optimized.
        /// </summary>
        FontDataLossLess = 1 << 11,

        /// <summary>
        /// Font converted (produce compatible metrics).
        /// </summary>
        FontConverted = 1 << 12,

        /// <summary>
        /// Font optimized for ClearType.
        /// </summary>
        OptimizedForClearType = 1 << 13,

        /// <summary>
        /// Last Resort font. Glyphs are generic symbolic representations of code point ranges.
        /// </summary>
        LastResortFont = 1 << 14,
    }

    /// <summary>
    /// Macintosh style flags for the font.
    /// </summary>
    [Flags]
    internal enum HeadMacStyle : ushort
    {
        /// <summary>
        /// No style flags set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bold style.
        /// </summary>
        Bold = 1 << 0,

        /// <summary>
        /// Italic style.
        /// </summary>
        Italic = 1 << 1,

        /// <summary>
        /// Underline style.
        /// </summary>
        Underline = 1 << 2,

        /// <summary>
        /// Outline (hollow) style.
        /// </summary>
        Outline = 1 << 3,

        /// <summary>
        /// Shadow style.
        /// </summary>
        Shadow = 1 << 4,

        /// <summary>
        /// Condensed style.
        /// </summary>
        Condensed = 1 << 5,

        /// <summary>
        /// Extended style.
        /// </summary>
        Extended = 1 << 6,
    }

    /// <summary>
    /// Gets the date the font was created.
    /// </summary>
    public DateTime Created { get; }

    /// <summary>
    /// Gets the font header flags.
    /// </summary>
    public HeadFlags Flags { get; }

    /// <summary>
    /// Gets the format of the index-to-location table.
    /// </summary>
    public IndexLocationFormats IndexLocationFormat { get; }

    /// <summary>
    /// Gets the smallest readable size in pixels.
    /// </summary>
    public ushort LowestRecPPEM { get; }

    /// <summary>
    /// Gets the Mac style flags.
    /// </summary>
    public HeadMacStyle MacStyle { get; }

    /// <summary>
    /// Gets the date the font was last modified.
    /// </summary>
    public DateTime Modified { get; }

    /// <summary>
    /// Gets the bounding box for all glyphs in the font.
    /// </summary>
    public Bounds Bounds { get; }

    /// <summary>
    /// Gets the number of font design units per em.
    /// </summary>
    public ushort UnitsPerEm { get; }

    /// <summary>
    /// Loads the <see cref="HeadTable"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="HeadTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static HeadTable? Load(FontReader fontReader)
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
    /// Loads the <see cref="HeadTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="HeadTable"/>.</returns>
    public static HeadTable Load(BigEndianBinaryReader reader)
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
        // uint16       | lowestRecPPEM      |  Smallest readable size in pixels.
        // int16        | fontDirectionHint  |  Deprecated(Set to 2).
        //                                          0: Fully mixed directional glyphs;
        //                                          1: Only strongly left to right;
        //                                          2: Like 1 but also contains neutrals;
        //                                          -1: Only strongly right to left;
        //                                          -2: Like -1 but also contains neutrals. 1
        // int16        | indexToLocFormat   | 0 for short offsets (Offset16), 1 for long (Offset32).
        // int16        | glyphDataFormat    | 0 for current format.
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();
        uint fontRevision = reader.ReadUInt32();
        uint checkSumAdjustment = reader.ReadUInt32();
        uint magicNumber = reader.ReadUInt32();
        if (magicNumber != 0x5F0F3CF5)
        {
            throw new InvalidFontFileException("invalid magic number in 'head'");
        }

        HeadFlags flags = reader.ReadUInt16<HeadFlags>();
        ushort unitsPerEm = reader.ReadUInt16();
        if (unitsPerEm < 16 || unitsPerEm > 16384)
        {
            throw new InvalidFontFileException($"invalid units per em expected value between 16 and 16384 but found {unitsPerEm} in 'head'");
        }

        var startDate = new DateTime(1904, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        long seconds = reader.ReadInt64();
        DateTime created = startDate;
        if (seconds > 0)
        {
            // Clear upper 32 bits, some fonts seem to have a non-zero upper 32 bits, like "C:\\Windows/Fonts\\cityb___.ttf"
            // The max date for UInt32.MaxValue seconds is {06/02/2040 06:28:15}, which should be plenty for the time being.
            seconds &= 0x00000000ffffffff;
            created = startDate.AddSeconds(seconds);
        }

        seconds = reader.ReadInt64();
        DateTime modified = startDate;
        if (seconds > 0)
        {
            // Clear upper 32 bits, some fonts seem to have a non-zero upper 32 bits, like "C:\\Windows/Fonts\\cityb___.ttf"
            // The max date for UInt32.MaxValue seconds is {06/02/2040 06:28:15}, which should be plenty for the time being.
            seconds &= 0x00000000ffffffff;
            modified = startDate.AddSeconds(seconds);
        }

        var bounds = Bounds.Load(reader); // xMin, yMin, xMax, yMax

        HeadMacStyle macStyle = reader.ReadUInt16<HeadMacStyle>();
        ushort lowestRecPPEM = reader.ReadUInt16();
        short fontDirectionHint = reader.ReadInt16();
        IndexLocationFormats indexToLocFormat = reader.ReadInt16<IndexLocationFormats>();
        short glyphDataFormat = reader.ReadInt16();

        return new HeadTable(
            flags,
            macStyle,
            unitsPerEm,
            created,
            modified,
            bounds,
            lowestRecPPEM,
            indexToLocFormat);
    }
}

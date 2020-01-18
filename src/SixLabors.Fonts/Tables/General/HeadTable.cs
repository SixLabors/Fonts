// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class HeadTable : Table
    {
        private const string TableName = "head";

        public HeadTable(HeadFlags flags, HeadMacStyle macStyle, ushort unitsPerEm, DateTime created, DateTime modified, Bounds bounds, ushort lowestRecPPEM, IndexLocationFormats indexToLocFormat)
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

        internal enum IndexLocationFormats : short
        {
            Offset16 = 0,
            Offset32 = 1,
        }

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
            None = 0,
            BaslineY0 = 1 << 0,
            LeftSidebearingPointAtX0 = 1 << 1,
            InstructionDependOnPointSize = 1 << 2,
            ForcePPEMToInt = 1 << 3,
            InstructionAlterAdvancedWidth = 1 << 4,

            // 1<<5 not used
            // 1<<6 - 1<<10 not used
            FontDataLossLess = 1 << 11,
            FontConverted = 1 << 12,
            OptimizedForClearType = 1 << 13,
            LastResortFont = 1 << 14,
        }

        [Flags]
        internal enum HeadMacStyle : ushort
        {
            // Bit 0: Bold (if set to 1);
            // Bit 1: Italic(if set to 1)
            // Bit 2: Underline(if set to 1)
            // Bit 3: Outline(if set to 1)
            // Bit 4: Shadow(if set to 1)
            // Bit 5: Condensed(if set to 1)
            // Bit 6: Extended(if set to 1)
            // Bits 7–15: Reserved(set to 0).
            None = 0,
            Bold = 1 << 0,
            Italic = 1 << 1,
            Underline = 1 << 2,
            Outline = 1 << 3,
            Shaddow = 1 << 4,
            Condensed = 1 << 5,
            Extended = 1 << 6,
        }

        public DateTime Created { get; }

        public HeadFlags Flags { get; }

        public IndexLocationFormats IndexLocationFormat { get; }

        public ushort LowestRecPPEM { get; }

        public HeadMacStyle MacStyle { get; }

        public DateTime Modified { get; }

        public Bounds Bounds { get; }

        public ushort UnitsPerEm { get; }

        public static HeadTable? Load(FontReader reader)
        {
            using (BinaryReader? binaryReader = reader.TryGetReaderAtTablePosition(TableName))
            {
                if (binaryReader is null)
                {
                    return null;
                }

                return Load(binaryReader);
            }
        }

        public static HeadTable Load(BinaryReader reader)
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
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            uint fontRevision = reader.ReadUInt32();
            uint checkSumAdjustment = reader.ReadUInt32();
            uint magincnumber = reader.ReadUInt32();
            if (magincnumber != 0x5F0F3CF5)
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
                seconds = seconds & 0x00000000ffffffff;
                created = startDate.AddSeconds(seconds);
            }

            seconds = reader.ReadInt64();
            DateTime modified = startDate;
            if (seconds > 0)
            {
                // Clear upper 32 bits, some fonts seem to have a non-zero upper 32 bits, like "C:\\Windows/Fonts\\cityb___.ttf"
                // The max date for UInt32.MaxValue seconds is {06/02/2040 06:28:15}, which should be plenty for the time being.
                seconds = seconds & 0x00000000ffffffff;
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
}

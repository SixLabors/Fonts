using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class MaximumProfileTable : Table
    {
        const string TableName = "maxp";
        internal ushort maxPoints;
        internal ushort maxContours;
        internal ushort maxCompositePoints;
        internal ushort maxCompositeContours;
        internal ushort maxZones;
        internal ushort maxTwilightPoints;
        internal ushort maxStorage;
        internal ushort maxFunctionDefs;
        internal ushort maxInstructionDefs;
        internal ushort maxStackElements;
        internal ushort maxSizeOfInstructions;
        internal ushort maxComponentElements;
        internal ushort maxComponentDepth;

        public ushort GlyphCount { get; }

        public MaximumProfileTable(ushort numGlyphs)
        {
            this.GlyphCount = numGlyphs;
        }

        public MaximumProfileTable(ushort numGlyphs, ushort maxPoints, ushort maxContours, ushort maxCompositePoints, ushort maxCompositeContours, ushort maxZones, ushort maxTwilightPoints, ushort maxStorage, ushort maxFunctionDefs, ushort maxInstructionDefs, ushort maxStackElements, ushort maxSizeOfInstructions, ushort maxComponentElements, ushort maxComponentDepth)
            : this(numGlyphs)
        {
            this.maxPoints = maxPoints;
            this.maxContours = maxContours;
            this.maxCompositePoints = maxCompositePoints;
            this.maxCompositeContours = maxCompositeContours;
            this.maxZones = maxZones;
            this.maxTwilightPoints = maxTwilightPoints;
            this.maxStorage = maxStorage;
            this.maxFunctionDefs = maxFunctionDefs;
            this.maxInstructionDefs = maxInstructionDefs;
            this.maxStackElements = maxStackElements;
            this.maxSizeOfInstructions = maxSizeOfInstructions;
            this.maxComponentElements = maxComponentElements;
            this.maxComponentDepth = maxComponentDepth;
        }

        public static MaximumProfileTable Load(FontReader reader)
        {
            return Load(reader.GetReaderAtTablePosition(TableName));
        }

        public static MaximumProfileTable Load(BinaryReader reader)
        {
            // This table establishes the memory requirements for this font.Fonts with CFF data must use Version 0.5 of this table, specifying only the numGlyphs field.Fonts with TrueType outlines must use Version 1.0 of this table, where all data is required.
            // Version 0.5
            // Type   | Name                 | Description
            // -------|----------------------|---------------------------------------
            // Fixed  | Table version number | 0x00005000 for version 0.5 (Note the difference in the representation of a non - zero fractional part, in Fixed numbers.)
            // uint16 | numGlyphs            | The number of glyphs in the font.

            var version = reader.ReadFixed();
            var numGlyphs = reader.ReadUInt16();
            if (version == 0.5)
            {
                return new MaximumProfileTable(numGlyphs);
            }

            // Version 1.0
            // Type   | Name                  | Description
            // -------|-----------------------|---------------------------------------
            // *Fixed | Table version number  | 0x00010000 for version 1.0.
            // *uint16| numGlyphs             | The number of glyphs in the font.
            // uint16 | maxPoints             | Maximum points in a non - composite glyph.
            // uint16 | maxContours           | Maximum contours in a non - composite glyph.
            // uint16 | maxCompositePoints    | Maximum points in a composite glyph.
            // uint16 | maxCompositeContours  | Maximum contours in a composite glyph.
            // uint16 | maxZones              | 1 if instructions do not use the twilight zone (Z0), or 2 if instructions do use Z0; should be set to 2 in most cases.
            // uint16 | maxTwilightPoints     | Maximum points used in Z0.
            // uint16 | maxStorage            | Number of Storage Area locations.
            // uint16 | maxFunctionDefs       | Number of FDEFs, equals to the highest function number +1.
            // uint16 | maxInstructionDefs    | Number of IDEFs.
            // uint16 | maxStackElements      | Maximum stack depth2.
            // uint16 | maxSizeOfInstructions | Maximum byte count for glyph instructions.
            // uint16 | maxComponentElements  | Maximum number of components referenced at “top level” for any composite glyph.
            // uint16 | maxComponentDepth     | Maximum levels of recursion; 1 for simple components.

            var maxPoints = reader.ReadUInt16();
            var maxContours = reader.ReadUInt16();
            var maxCompositePoints = reader.ReadUInt16();
            var maxCompositeContours = reader.ReadUInt16();

            var maxZones = reader.ReadUInt16();
            var maxTwilightPoints = reader.ReadUInt16();
            var maxStorage = reader.ReadUInt16();
            var maxFunctionDefs = reader.ReadUInt16();
            var maxInstructionDefs = reader.ReadUInt16();
            var maxStackElements = reader.ReadUInt16();
            var maxSizeOfInstructions = reader.ReadUInt16();
            var maxComponentElements = reader.ReadUInt16();
            var maxComponentDepth = reader.ReadUInt16();

            return new MaximumProfileTable(numGlyphs, maxPoints,
                maxContours,
                maxCompositePoints,
                maxCompositeContours,
                maxZones,
                maxTwilightPoints,
                maxStorage,
                maxFunctionDefs,
                maxInstructionDefs,
                maxStackElements,
                maxSizeOfInstructions,
                maxComponentElements,
                maxComponentDepth
                );
        }

        [Flags]
        public enum HeadFlags : ushort
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
        public enum HeadMacStyle : ushort
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
    }
}
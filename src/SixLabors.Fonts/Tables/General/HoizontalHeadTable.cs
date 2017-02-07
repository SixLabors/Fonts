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
    internal class HoizontalHeadTable : Table
    {
        const string TableName = "hhea";

        internal ushort AdvanceWidthMax { get; }

        internal short Ascender { get; }

        internal short CaretOffset { get; }

        internal short CaretSlopeRise { get; }

        internal short CaretSlopeRun { get; }

        internal short Descender { get; }

        internal short LineGap { get; }

        internal short MinLeftSideBearing { get; }

        internal short MinRightSideBearing { get; }

        internal ushort NumberOfHMetrics { get; }

        internal short XMaxExtent { get; }

        public HoizontalHeadTable(short ascender, short descender, short lineGap, ushort advanceWidthMax, short minLeftSideBearing, short minRightSideBearing, short xMaxExtent, short caretSlopeRise, short caretSlopeRun, short caretOffset, ushort numberOfHMetrics)
        {
            this.Ascender = ascender;
            this.Descender = descender;
            this.LineGap = lineGap;
            this.AdvanceWidthMax = advanceWidthMax;
            this.MinLeftSideBearing = minLeftSideBearing;
            this.MinRightSideBearing = minRightSideBearing;
            this.XMaxExtent = xMaxExtent;
            this.CaretSlopeRise = caretSlopeRise;
            this.CaretSlopeRun = caretSlopeRun;
            this.CaretOffset = caretOffset;
            this.NumberOfHMetrics = numberOfHMetrics;
        }

        public static HoizontalHeadTable Load(FontReader reader)
        {
            return Load(reader.GetReaderAtTablePosition(TableName));
        }

        public static HoizontalHeadTable Load(BinaryReader reader)
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

            var majorVersion = reader.ReadUInt16();
            var minorVersion = reader.ReadUInt16();
            var ascender = reader.ReadFWORD();
            var descender = reader.ReadFWORD();
            var lineGap = reader.ReadFWORD();
            var advanceWidthMax = reader.ReadUFWORD();
            var minLeftSideBearing = reader.ReadFWORD();
            var minRightSideBearing = reader.ReadFWORD();
            var xMaxExtent = reader.ReadFWORD();
            var caretSlopeRise = reader.ReadInt16();
            var caretSlopeRun = reader.ReadInt16();
            var caretOffset = reader.ReadInt16();
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            var metricDataFormat = reader.ReadInt16(); // 0
            if(metricDataFormat != 0)
            {
                throw new InvalidFontTableException($"Expected metricDataFormat = 0 found {metricDataFormat}", "hhea");
            }
            var numberOfHMetrics = reader.ReadUInt16();

            return new HoizontalHeadTable(ascender,
                descender,
                lineGap,
                advanceWidthMax,
                minLeftSideBearing,
                minRightSideBearing,
                xMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                numberOfHMetrics
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
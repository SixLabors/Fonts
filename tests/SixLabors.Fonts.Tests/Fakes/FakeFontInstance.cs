using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeFontInstance : FontInstance
    {
        internal FakeFontInstance(string text)
            : this(GetGlyphs(text))
        {
        }

        internal FakeFontInstance(List<FakeGlyphSource> glyphs)
            : base(GenerateNameTable(),
                  GenerateCMapTable(glyphs),
                  new FakeGlyphTable(glyphs),
                  GenerateOS2Table(),
                  GenerateHorizontalMetricsTable(glyphs),
                  GenerateHeadTable(glyphs),
                  new KerningTable(new Fonts.Tables.General.Kern.KerningSubTable[0]),
                  null,
                  null)
        {
        }

        internal FakeFontInstance(NameTable nameTable, CMapTable cmap, GlyphTable glyphs, OS2Table os2, HorizontalMetricsTable horizontalMetrics, HeadTable head, KerningTable kern)
            : base(nameTable, cmap, glyphs, os2, horizontalMetrics, head, kern, null, null)
        {
        }

        /// <summary>
        /// Creates a fake font with varying vertical font metrics to facilitate testing.
        /// </summary>
        public static FakeFontInstance CreateFontWithVaryingVerticalFontMetrics(string text)
        {
            List<FakeGlyphSource> glyphs = GetGlyphs(text);
            var result = new FakeFontInstance(
                GenerateNameTable(),
                GenerateCMapTable(glyphs),
                new FakeGlyphTable(glyphs),
                GenerateOS2TableWithVaryingVerticalFontMetrics(),
                GenerateHorizontalMetricsTable(glyphs),
                GenerateHeadTable(glyphs),
                new KerningTable(new Fonts.Tables.General.Kern.KerningSubTable[0])
            );
            return result;
        }

        private static List<FakeGlyphSource> GetGlyphs(string text)
        {
            var glyphs = text.Distinct().Select((x, i) => new FakeGlyphSource(x, (ushort)i)).ToList();
            return glyphs;
        }

        static NameTable GenerateNameTable()
        {
            return new NameTable(new[] {
                new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FullFontName, "name"),
                new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FontFamilyName, "name")
            }, new string[0]);
        }

        static CMapTable GenerateCMapTable(List<FakeGlyphSource> glyphs)
        {
            return new CMapTable(new[] { new FakeCmapSubtable(glyphs) });
        }

        static OS2Table GenerateOS2Table()
        {
            return new OS2Table(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, "", OS2Table.FontStyleSelection.ITALIC, 1, 1, 20, 10, 20, 1, 1);
        }

        static OS2Table GenerateOS2TableWithVaryingVerticalFontMetrics()
        {
            return new OS2Table(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, "", OS2Table.FontStyleSelection.ITALIC, 1, 1, 35, 8, 12, 33, 11);
        }

        static HorizontalMetricsTable GenerateHorizontalMetricsTable(List<FakeGlyphSource> glyphs)
        {
            return new HorizontalMetricsTable(glyphs.Select(x => (ushort)30).ToArray(), glyphs.Select(x => (short)10).ToArray());
        }

        static HeadTable GenerateHeadTable(List<FakeGlyphSource> glyphs)
        {
            return new HeadTable(HeadTable.HeadFlags.ForcePPEMToInt, HeadTable.HeadMacStyle.None, 30, DateTime.Now, DateTime.Now, new Bounds(10, 10, 20, 20), 1, HeadTable.IndexLocationFormats.Offset16);
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
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
            : base(
                  GenerateNameTable(),
                  GenerateCMapTable(glyphs),
                  new FakeGlyphTable(glyphs),
                  GenerateOS2Table(),
                  GenerateHorizontalMetricsTable(glyphs),
                  GenerateHeadTable(),
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
                GenerateHeadTable(),
                new KerningTable(new Fonts.Tables.General.Kern.KerningSubTable[0]));

            return result;
        }

        private static List<FakeGlyphSource> GetGlyphs(string text)
        {
            var glyphs = text.Distinct().Select((x, i) => new FakeGlyphSource(x, (ushort)i)).ToList();
            return glyphs;
        }

        private static NameTable GenerateNameTable()
            => new NameTable(
                new[]
                {
                    new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FullFontName, "name"),
                    new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FontFamilyName, "name")
                },
                Array.Empty<string>());

        private static CMapTable GenerateCMapTable(List<FakeGlyphSource> glyphs)
            => new CMapTable(new[] { new FakeCmapSubtable(glyphs) });

        private static OS2Table GenerateOS2Table()
            => new OS2Table(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, string.Empty, OS2Table.FontStyleSelection.ITALIC, 1, 1, 20, 10, 20, 1, 1);

        private static OS2Table GenerateOS2TableWithVaryingVerticalFontMetrics()
            => new OS2Table(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, string.Empty, OS2Table.FontStyleSelection.ITALIC, 1, 1, 35, 8, 12, 33, 11);

        private static HorizontalMetricsTable GenerateHorizontalMetricsTable(List<FakeGlyphSource> glyphs)
            => new HorizontalMetricsTable(glyphs.Select(x => (ushort)30).ToArray(), glyphs.Select(x => (short)10).ToArray());

        private static HeadTable GenerateHeadTable()
            => new HeadTable(
                HeadTable.HeadFlags.ForcePPEMToInt,
                HeadTable.HeadMacStyle.None,
                30,
                DateTime.Now,
                DateTime.Now,
                new Bounds(10, 10, 20, 20),
                1,
                HeadTable.IndexLocationFormats.Offset16);
    }
}

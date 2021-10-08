// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeFontInstance : StreamFontMetrics
    {
        internal FakeFontInstance(string text)
            : this(GetGlyphs(text))
        {
        }

        internal FakeFontInstance(List<FakeGlyphSource> glyphs)
            : base(
                  GenerateNameTable("name"),
                  GenerateCMapTable(glyphs),
                  new FakeGlyphTable(glyphs),
                  GenerateOS2Table(),
                  GenerateHorizontalHeadTable(),
                  GenerateHorizontalMetricsTable(glyphs),
                  GenerateVerticalHeadTable(),
                  GenerateVerticalMetricsTable(glyphs),
                  GenerateHeadTable(),
                  new KerningTable(Array.Empty<KerningSubTable>()),
                  null,
                  null,
                  null,
                  null,
                  null)
        {
        }

        internal FakeFontInstance(
            NameTable nameTable,
            CMapTable cmap,
            GlyphTable glyphs,
            OS2Table os2,
            HorizontalHeadTable horizontalHeadTable,
            HorizontalMetricsTable horizontalMetrics,
            VerticalHeadTable verticalHeadTable,
            VerticalMetricsTable verticalMetrics,
            HeadTable head,
            KerningTable kern)
            : base(
                  nameTable,
                  cmap,
                  glyphs,
                  os2,
                  horizontalHeadTable,
                  horizontalMetrics,
                  verticalHeadTable,
                  verticalMetrics,
                  head,
                  kern,
                  null,
                  null,
                  null,
                  null,
                  null)
        {
        }

        /// <summary>
        /// Creates a fake font with varying vertical font metrics to facilitate testing.
        /// </summary>
        public static FakeFontInstance CreateFontWithVaryingVerticalFontMetrics(string text, string name = "name")
        {
            List<FakeGlyphSource> glyphs = GetGlyphs(text);

            return new FakeFontInstance(
                GenerateNameTable(name),
                GenerateCMapTable(glyphs),
                new FakeGlyphTable(glyphs),
                GenerateOS2TableWithVaryingVerticalFontMetrics(),
                GenerateHorizontalHeadTable(),
                GenerateHorizontalMetricsTable(glyphs),
                GenerateVerticalHeadTable(),
                GenerateVerticalMetricsTable(glyphs),
                GenerateHeadTable(),
                new KerningTable(Array.Empty<KerningSubTable>()));
        }

        private static List<FakeGlyphSource> GetGlyphs(string text)
        {
            HashSet<CodePoint> codePoints = new()
            {
                // Regardless of the encoding scheme, character codes that do
                // not correspond to any glyph in the font should be mapped to glyph index 0.
                // The glyph at this location must be a special glyph representing a missing character, commonly known as .notdef.
                default // Add default at position 0;
            };

            foreach (CodePoint codePoint in text.AsSpan().EnumerateCodePoints())
            {
                codePoints.Add(codePoint);
            }

            return codePoints.Select((x, i) => new FakeGlyphSource(x, (ushort)i)).ToList();
        }

        private static NameTable GenerateNameTable(string name)
            => new(
                new[]
                {
                    new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FullFontName, name),
                    new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FontFamilyName, name)
                },
                Array.Empty<string>());

        private static CMapTable GenerateCMapTable(List<FakeGlyphSource> glyphs)
            => new(new[] { new FakeCmapSubtable(glyphs) });

        private static HorizontalHeadTable GenerateHorizontalHeadTable()
            => new(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        private static VerticalHeadTable GenerateVerticalHeadTable()
            => new(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        private static OS2Table GenerateOS2Table()
            => new(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, string.Empty, OS2Table.FontStyleSelection.USE_TYPO_METRICS, 1, 1, 20, 10, 20, 1, 1);

        private static OS2Table GenerateOS2TableWithVaryingVerticalFontMetrics()
            => new(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, string.Empty, OS2Table.FontStyleSelection.USE_TYPO_METRICS, 1, 1, 35, 8, 12, 33, 11);

        private static HorizontalMetricsTable GenerateHorizontalMetricsTable(List<FakeGlyphSource> glyphs)
            => new(glyphs.Select(_ => (ushort)30).ToArray(), glyphs.Select(_ => (short)10).ToArray());

        private static VerticalMetricsTable GenerateVerticalMetricsTable(List<FakeGlyphSource> glyphs)
            => new(glyphs.Select(_ => (ushort)30).ToArray(), glyphs.Select(_ => (short)10).ToArray());

        private static HeadTable GenerateHeadTable()
            => new(
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

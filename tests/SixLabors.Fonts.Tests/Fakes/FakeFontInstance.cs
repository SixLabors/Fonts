// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeFontInstance : StreamFontMetrics
    {
        internal FakeFontInstance(string text)
            : this(CreateTrueTypeFontTables(text))
        {
        }

        internal FakeFontInstance(TrueTypeFontTables tables)
            : base(tables)
        {
        }

        /// <summary>
        /// Creates a fake font with varying vertical font metrics to facilitate testing.
        /// </summary>
        public static FakeFontInstance CreateFontWithVaryingVerticalFontMetrics(string text, string fontName = "name")
        {
            List<FakeGlyphSource> glyphs = GetGlyphs(text);
            HeadTable head = GenerateHeadTable();
            HorizontalHeadTable hhea = GenerateHorizontalHeadTable();
            NameTable name = GenerateNameTable(fontName);
            MaximumProfileTable maxp = GenerateMaxpTable(glyphs);
            CMapTable cmap = GenerateCMapTable(glyphs);
            var glyf = new FakeGlyphTable(glyphs);
            PostTable post = GeneratePostTable();
            var kern = new KerningTable(Array.Empty<KerningSubTable>());
            OS2Table os2 = GenerateOS2TableWithVaryingVerticalFontMetrics();
            HorizontalMetricsTable htmx = GenerateHorizontalMetricsTable(glyphs);
            VerticalHeadTable vhea = GenerateVerticalHeadTable();
            VerticalMetricsTable vmtx = GenerateVerticalMetricsTable(glyphs);
            IndexLocationTable loca = GenerateIndexLocationTable(glyphs);

            TrueTypeFontTables tables = new(cmap, head, hhea, htmx, maxp, name, os2, post, glyf, loca)
            {
                Kern = kern,
                Vhea = vhea,
                Vmtx = vmtx
            };

            return new FakeFontInstance(tables);
        }

        private static TrueTypeFontTables CreateTrueTypeFontTables(string text, string fontName = "name")
        {
            List<FakeGlyphSource> glyphs = GetGlyphs(text);
            HeadTable head = GenerateHeadTable();
            HorizontalHeadTable hhea = GenerateHorizontalHeadTable();
            NameTable name = GenerateNameTable(fontName);
            MaximumProfileTable maxp = GenerateMaxpTable(glyphs);
            CMapTable cmap = GenerateCMapTable(glyphs);
            var glyf = new FakeGlyphTable(glyphs);
            PostTable post = GeneratePostTable();
            var kern = new KerningTable(Array.Empty<KerningSubTable>());
            OS2Table os2 = GenerateOS2Table();
            HorizontalMetricsTable htmx = GenerateHorizontalMetricsTable(glyphs);
            VerticalHeadTable vhea = GenerateVerticalHeadTable();
            VerticalMetricsTable vmtx = GenerateVerticalMetricsTable(glyphs);
            IndexLocationTable loca = GenerateIndexLocationTable(glyphs);

            return new(cmap, head, hhea, htmx, maxp, name, os2, post, glyf, loca)
            {
                Kern = kern,
                Vhea = vhea,
                Vmtx = vmtx
            };
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
                    new NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.KnownNameIds.FullFontName, name),
                    new NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.KnownNameIds.FontFamilyName, name)
                },
                Array.Empty<string>());

        private static CMapTable GenerateCMapTable(List<FakeGlyphSource> glyphs)
            => new(new[] { new FakeCmapSubtable(glyphs) });

        private static MaximumProfileTable GenerateMaxpTable(List<FakeGlyphSource> glyphs)
            => new((ushort)glyphs.Count);

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

        private static IndexLocationTable GenerateIndexLocationTable(List<FakeGlyphSource> glyphs)
            => new(new uint[glyphs.Count + 1]);

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

        private static PostTable GeneratePostTable() => new(2, 0, 0, 200, 35, 0, 0, 0, 0, 0, Array.Empty<PostNameRecord>());
    }
}

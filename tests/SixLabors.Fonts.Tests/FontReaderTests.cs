// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontReaderTests
    {
        [Fact]
        public void ReadTrueTypeOutlineType()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteTrueTypeFileHeader(0, 0, 0, 0);

            var reader = new FontReader(writer.GetStream());
            Assert.Equal(FontReader.OutlineTypes.TrueType, reader.OutlineType);
        }

        [Fact]
        public void ReadCcfOutlineType()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteCffFileHeader(0, 0, 0, 0);
            Assert.Throws<Exceptions.InvalidFontFileException>(
                () =>
                    {
                        var reader = new FontReader(writer.GetStream());
                    });
        }

        [Fact]
        public void ReadTableHeaders()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteTrueTypeFileHeader(2, 0, 0, 0);
            writer.WriteTableHeader("name", 0, 10, 0);
            writer.WriteTableHeader("cmap", 0, 1, 0);

            var reader = new FontReader(writer.GetStream());

            Assert.Equal(2, reader.Headers.Count);
        }

        [Fact]
        public void ReadCMapTable()
        {
            var writer = new BigEndianBinaryWriter();

            writer.WriteTrueTypeFileHeader(new TableHeader("cmap", 0, 0, 20));

            writer.WriteCMapTable(
                new[]
                {
                    new Fonts.Tables.General.CMap.Format0SubTable(
                        0,
                        WellKnownIds.PlatformIDs.Macintosh,
                        1,
                        new byte[] { 2, 9 })
                });

            var reader = new FontReader(writer.GetStream());
            CMapTable cmap = reader.GetTable<CMapTable>();
            Assert.NotNull(cmap);
        }

#if NETCOREAPP3_0_OR_GREATER
        [Fact]
        public void ReadGlyphsTable_WithWoff2Format()
        {
            bool[] expectedOnCurves = { true, true, true, true, true, true, true, true };
            ushort[] expectedEndPoints = { 3, 7 };
            var expectedBounds = new Bounds(193.0f, 0.0f, 1034.0f, 1462.0f);
            var expectedControlPoints = new Vector2[]
            {
                new Vector2(193.0f, 1462.0f), new Vector2(1034.0f, 1462.0f), new Vector2(1034.0f, 0.0f), new Vector2(193.0f, 0.0f),
                new Vector2(297.0f, 104.0f), new Vector2(930.0f, 104.0f), new Vector2(930.0f, 1358.0f), new Vector2(297.0f, 1358.0f)
            };
            var reader = new FontReader(TestFonts.FontFileWoff2Data());
            GlyphTable glyphs = reader.GetTable<GlyphTable>();
            Fonts.Tables.General.Glyphs.GlyphVector glyph = glyphs.GetGlyph(0);

            Assert.Equal(231, glyphs.GlyphCount);
            Assert.Equal(8, glyph.PointCount);
            Assert.Equal(8, glyph.ControlPoints.Length);
            Assert.Equal(2, glyph.EndPoints.Length);
            Assert.Equal(8, glyph.OnCurves.Length);
            Assert.Equal(expectedBounds, glyph.Bounds);
            Assert.True(expectedOnCurves.SequenceEqual(glyph.OnCurves));
            Assert.True(expectedEndPoints.SequenceEqual(glyph.EndPoints));
            Assert.True(expectedControlPoints.SequenceEqual(glyph.ControlPoints));
        }
#endif
    }
}

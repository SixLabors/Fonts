// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tests;

public class FontReaderTests
{
    [Fact]
    public void ReadTrueTypeOutlineType()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader(0, 0, 0, 0);

        using var reader = new FontReader(writer.GetStream());
        Assert.Equal(OutlineType.TrueType, reader.OutlineType);
    }

    [Fact]
    public void ReadCffOutlineType()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteCffFileHeader(0, 0, 0, 0);

        using var reader = new FontReader(writer.GetStream());
        Assert.Equal(OutlineType.CFF, reader.OutlineType);
    }

    [Fact]
    public void ReadTableHeaders()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader(2, 0, 0, 0);
        writer.WriteTableHeader("name", 0, 10, 0);
        writer.WriteTableHeader("cmap", 0, 1, 0);

        using var reader = new FontReader(writer.GetStream());

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

        using var reader = new FontReader(writer.GetStream());
        CMapTable cmap = reader.GetTable<CMapTable>();
        Assert.NotNull(cmap);
    }

    [Fact]
    public void ReadFont_WithWoffFormat_EqualsTtf()
    {
        using var fontReaderTtf = new FontReader(TestFonts.OpenSansTtfData());
        using var fontReaderWoff = new FontReader(TestFonts.OpensSansWoff1Data());

        Assert.Equal(fontReaderTtf.Headers.Count, fontReaderWoff.Headers.Count);
        foreach (string key in fontReaderTtf.Headers.Keys)
        {
            Assert.True(fontReaderWoff.Headers.ContainsKey(key));
        }
    }

    [Fact]
    public void GlyphsCount_WithWoffFormat_EqualsTtf()
    {
        using var fontReaderWoff = new FontReader(TestFonts.OpensSansWoff1Data());
        GlyphTable glyphsWoff = fontReaderWoff.GetTable<GlyphTable>();
        using var fontReaderTtf = new FontReader(TestFonts.OpenSansTtfData());
        GlyphTable glyphsTtf = fontReaderTtf.GetTable<GlyphTable>();

        Assert.Equal(glyphsTtf.GlyphCount, glyphsWoff.GlyphCount);
    }

    [Fact]
    public void ReadFont_WithWoff2Format_EqualsTtf()
    {
        using var fontReaderTtf = new FontReader(TestFonts.OpenSansTtfData());
        using var fontReaderWoff = new FontReader(TestFonts.OpensSansWoff2Data());

        Assert.Equal(fontReaderTtf.Headers.Count, fontReaderWoff.Headers.Count);
        foreach (string key in fontReaderTtf.Headers.Keys)
        {
            Assert.True(fontReaderWoff.Headers.ContainsKey(key));
        }
    }

    [Fact]
    public void GlyphsCount_WithWoff2Format_EqualsTtf()
    {
        using var fontReaderWoff = new FontReader(TestFonts.OpensSansWoff2Data());
        GlyphTable glyphsWoff = fontReaderWoff.GetTable<GlyphTable>();
        using var fontReaderTtf = new FontReader(TestFonts.OpenSansTtfData());
        GlyphTable glyphsTtf = fontReaderTtf.GetTable<GlyphTable>();

        Assert.Equal(glyphsTtf.GlyphCount, glyphsWoff.GlyphCount);
    }
}

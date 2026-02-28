// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class FontCodePointsTests
{
    [Fact]
    public void TtfTest()
    {
        FontCollection collection = new FontCollection();
        FontFamily family = collection.Add(TestFonts.SimpleFontFile);
        Font font = family.CreateFont(12);

        IReadOnlyList<CodePoint> codePoints = font.FontMetrics.GetAvailableCodePoints();
        IEnumerable<int> codePointValues = codePoints.Select(x => x.Value);

        // Compare with https://everythingfonts.com/ttfdump
        Assert.Equal(257, codePoints.Count);

        // Compare with https://fontdrop.info/
        Assert.Contains(0x0000, codePointValues);
        Assert.Contains(0x000D, codePointValues);
        Assert.Contains(0x0020, codePointValues);
        Assert.Contains(0x0041, codePointValues);
        Assert.Contains(0x0042, codePointValues);
        Assert.Contains(0x0061, codePointValues);
        Assert.Contains(0x0062, codePointValues);
        Assert.Contains(0xFFFF, codePointValues);

        HashSet<int> glyphIds = new();
        foreach (CodePoint codePoint in codePoints)
        {
            Assert.True(font.TryGetGlyphs(codePoint, out Glyph? glyph));
            glyphIds.Add(glyph.Value.GlyphMetrics.GlyphId);
        }

        // Compare with https://fontdrop.info/
        Assert.Equal(8, glyphIds.Count);
    }

    [Fact]
    public void WoffTest()
    {
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.SimpleFontFileWoff);
        Font font = family.CreateFont(12);

        IReadOnlyList<CodePoint> codePoints = font.FontMetrics.GetAvailableCodePoints();
        IEnumerable<int> codePointValues = codePoints.Select(x => x.Value);

        // Compare with https://everythingfonts.com/ttfdump
        Assert.Equal(257, codePoints.Count);

        // Compare with https://fontdrop.info/
        Assert.Contains(0x0000, codePointValues);
        Assert.Contains(0x000D, codePointValues);
        Assert.Contains(0x0020, codePointValues);
        Assert.Contains(0x0041, codePointValues);
        Assert.Contains(0x0042, codePointValues);
        Assert.Contains(0x0061, codePointValues);
        Assert.Contains(0x0062, codePointValues);
        Assert.Contains(0xFFFF, codePointValues);

        HashSet<int> glyphIds = new();
        foreach (CodePoint codePoint in codePoints)
        {
            Assert.True(font.TryGetGlyphs(codePoint, out Glyph? glyph));
            glyphIds.Add(glyph.Value.GlyphMetrics.GlyphId);
        }

        // Compare with https://fontdrop.info/
        Assert.Equal(8, glyphIds.Count);
    }
}

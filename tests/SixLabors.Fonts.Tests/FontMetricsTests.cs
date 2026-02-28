// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class FontMetricsTests
{
    [Fact]
    public void FontMetricsMatchesReference()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.OpenSansFile);
        Font font = family.CreateFont(12);

        Assert.Equal(2048, font.FontMetrics.UnitsPerEm);
        Assert.Equal(2189, font.FontMetrics.HorizontalMetrics.Ascender);
        Assert.Equal(-600, font.FontMetrics.HorizontalMetrics.Descender);
        Assert.Equal(0, font.FontMetrics.HorizontalMetrics.LineGap);
        Assert.Equal(2789, font.FontMetrics.HorizontalMetrics.LineHeight);
        Assert.Equal(2470, font.FontMetrics.HorizontalMetrics.AdvanceWidthMax);
        Assert.Equal(font.FontMetrics.HorizontalMetrics.LineHeight, font.FontMetrics.HorizontalMetrics.AdvanceHeightMax);

        Assert.Equal(1331, font.FontMetrics.SubscriptXSize);
        Assert.Equal(1229, font.FontMetrics.SubscriptYSize);
        Assert.Equal(0, font.FontMetrics.SubscriptXOffset);
        Assert.Equal(154, font.FontMetrics.SubscriptYOffset);

        Assert.Equal(1331, font.FontMetrics.SuperscriptXSize);
        Assert.Equal(1229, font.FontMetrics.SuperscriptYSize);
        Assert.Equal(0, font.FontMetrics.SuperscriptXOffset);
        Assert.Equal(717, font.FontMetrics.SuperscriptYOffset);

        Assert.Equal(50, font.FontMetrics.StrikeoutSize);
        Assert.Equal(658, font.FontMetrics.StrikeoutPosition);

        Assert.Equal(-100, font.FontMetrics.UnderlinePosition);
        Assert.Equal(50, font.FontMetrics.UnderlineThickness);

        Assert.Equal(0, font.FontMetrics.ItalicAngle);

        Assert.False(font.IsBold);
        Assert.False(font.IsItalic);
    }

    [Fact]
    public void FontMetricsVerticalFontMatchesReference()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.NotoSansSCThinBad);
        Font font = family.CreateFont(12);

        Assert.Equal(1000, font.FontMetrics.UnitsPerEm);

        Assert.Equal(806, font.FontMetrics.HorizontalMetrics.Ascender);
        Assert.Equal(-256, font.FontMetrics.HorizontalMetrics.Descender);
        Assert.Equal(90, font.FontMetrics.HorizontalMetrics.LineGap);
        Assert.Equal(1152, font.FontMetrics.HorizontalMetrics.LineHeight);
        Assert.Equal(1000, font.FontMetrics.HorizontalMetrics.AdvanceWidthMax);
        Assert.Equal(1000, font.FontMetrics.HorizontalMetrics.AdvanceHeightMax);

        Assert.Equal(451, font.FontMetrics.VerticalMetrics.Ascender);
        Assert.Equal(-453, font.FontMetrics.VerticalMetrics.Descender);
        Assert.Equal(90, font.FontMetrics.VerticalMetrics.LineGap);
        Assert.Equal(994, font.FontMetrics.VerticalMetrics.LineHeight);
        Assert.Equal(1000, font.FontMetrics.VerticalMetrics.AdvanceWidthMax);
        Assert.Equal(1000, font.FontMetrics.VerticalMetrics.AdvanceHeightMax);

        Assert.Equal(650, font.FontMetrics.SubscriptXSize);
        Assert.Equal(700, font.FontMetrics.SubscriptYSize);
        Assert.Equal(0, font.FontMetrics.SubscriptXOffset);
        Assert.Equal(140, font.FontMetrics.SubscriptYOffset);

        Assert.Equal(650, font.FontMetrics.SuperscriptXSize);
        Assert.Equal(700, font.FontMetrics.SuperscriptYSize);
        Assert.Equal(0, font.FontMetrics.SuperscriptXOffset);
        Assert.Equal(480, font.FontMetrics.SuperscriptYOffset);

        Assert.Equal(49, font.FontMetrics.StrikeoutSize);
        Assert.Equal(258, font.FontMetrics.StrikeoutPosition);

        Assert.Equal(-75, font.FontMetrics.UnderlinePosition);
        Assert.Equal(50, font.FontMetrics.UnderlineThickness);

        Assert.Equal(0, font.FontMetrics.ItalicAngle);

        Assert.False(font.IsBold);
        Assert.False(font.IsItalic);
    }

    [Fact]
    public void FontMetricsVerticalFontMatchesReferenceCFF()
    {
        // Compared to OpenTypeJS Font Inspector metrics
        // https://opentype.js.org/font-inspector.html
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.NotoSansKRRegular);
        Font font = family.CreateFont(12);

        Assert.Equal(1000, font.FontMetrics.UnitsPerEm);

        Assert.Equal(1160, font.FontMetrics.HorizontalMetrics.Ascender);
        Assert.Equal(-288, font.FontMetrics.HorizontalMetrics.Descender);
        Assert.Equal(0, font.FontMetrics.HorizontalMetrics.LineGap);
        Assert.Equal(1448, font.FontMetrics.HorizontalMetrics.LineHeight);
        Assert.Equal(3000, font.FontMetrics.HorizontalMetrics.AdvanceWidthMax);
        Assert.Equal(3000, font.FontMetrics.HorizontalMetrics.AdvanceHeightMax);

        Assert.Equal(500, font.FontMetrics.VerticalMetrics.Ascender);
        Assert.Equal(-500, font.FontMetrics.VerticalMetrics.Descender);
        Assert.Equal(0, font.FontMetrics.VerticalMetrics.LineGap);
        Assert.Equal(1000, font.FontMetrics.VerticalMetrics.LineHeight);
        Assert.Equal(3000, font.FontMetrics.VerticalMetrics.AdvanceWidthMax);
        Assert.Equal(3000, font.FontMetrics.VerticalMetrics.AdvanceHeightMax);

        Assert.Equal(650, font.FontMetrics.SubscriptXSize);
        Assert.Equal(600, font.FontMetrics.SubscriptYSize);
        Assert.Equal(0, font.FontMetrics.SubscriptXOffset);
        Assert.Equal(75, font.FontMetrics.SubscriptYOffset);

        Assert.Equal(650, font.FontMetrics.SuperscriptXSize);
        Assert.Equal(600, font.FontMetrics.SuperscriptYSize);
        Assert.Equal(0, font.FontMetrics.SuperscriptXOffset);
        Assert.Equal(350, font.FontMetrics.SuperscriptYOffset);

        Assert.Equal(50, font.FontMetrics.StrikeoutSize);
        Assert.Equal(325, font.FontMetrics.StrikeoutPosition);

        Assert.Equal(-125, font.FontMetrics.UnderlinePosition);
        Assert.Equal(50, font.FontMetrics.UnderlineThickness);

        Assert.Equal(0, font.FontMetrics.ItalicAngle);

        Assert.False(font.IsBold);
        Assert.False(font.IsItalic);
    }

    [Fact]
    public void GlyphMetricsMatchesReference()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.OpenSansFile);
        Font font = family.CreateFont(12);

        CodePoint codePoint = new('A');

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            codePoint,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out GlyphMetrics metrics));

        Assert.Equal(codePoint, metrics.CodePoint);
        Assert.Equal(font.FontMetrics.UnitsPerEm, metrics.UnitsPerEm);
        Assert.Equal(new Vector2(metrics.UnitsPerEm * 72F), metrics.ScaleFactor);
        Assert.Equal(1295, metrics.AdvanceWidth);
        Assert.Equal(2789, metrics.AdvanceHeight);
        Assert.Equal(1293, metrics.Width);
        Assert.Equal(1468, metrics.Height);
        Assert.Equal(0, metrics.LeftSideBearing);
        Assert.Equal(721, metrics.TopSideBearing);
        Assert.Equal(GlyphType.Standard, metrics.GlyphType);
    }

    [Fact]
    public void GlyphMetricsMatchesReference_WithWoff1format()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.OpenSansFileWoff1);
        Font font = family.CreateFont(12);

        CodePoint codePoint = new('A');

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            codePoint,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out GlyphMetrics metrics));

        Assert.Equal(codePoint, metrics.CodePoint);
        Assert.Equal(font.FontMetrics.UnitsPerEm, metrics.UnitsPerEm);
        Assert.Equal(new Vector2(metrics.UnitsPerEm * 72F), metrics.ScaleFactor);
        Assert.Equal(1295, metrics.AdvanceWidth);
        Assert.Equal(2789, metrics.AdvanceHeight);
        Assert.Equal(1293, metrics.Width);
        Assert.Equal(1468, metrics.Height);
        Assert.Equal(0, metrics.LeftSideBearing);
        Assert.Equal(721, metrics.TopSideBearing);
        Assert.Equal(GlyphType.Standard, metrics.GlyphType);
    }

    [Fact]
    public void GlyphMetricsMatchesReference_WithWoff2format()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.OpenSansFileWoff2);
        Font font = family.CreateFont(12);

        CodePoint codePoint = new('A');

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            codePoint,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out GlyphMetrics metrics));

        Assert.Equal(codePoint, metrics.CodePoint);
        Assert.Equal(font.FontMetrics.UnitsPerEm, metrics.UnitsPerEm);
        Assert.Equal(new Vector2(metrics.UnitsPerEm * 72F), metrics.ScaleFactor);
        Assert.Equal(1295, metrics.AdvanceWidth);
        Assert.Equal(2789, metrics.AdvanceHeight);
        Assert.Equal(1293, metrics.Width);
        Assert.Equal(1468, metrics.Height);
        Assert.Equal(0, metrics.LeftSideBearing);
        Assert.Equal(721, metrics.TopSideBearing);
        Assert.Equal(GlyphType.Standard, metrics.GlyphType);
    }

    [Fact]
    public void GlyphMetricsVerticalMatchesReference()
    {
        // Compared to EveryFonts TTFDump metrics
        // https://everythingfonts.com/ttfdump
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.NotoSansSCThinBad);
        Font font = family.CreateFont(12);

        CodePoint codePoint = new('A');

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            codePoint,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out GlyphMetrics metrics));

        // Position 0.
        Assert.Equal(codePoint, metrics.CodePoint);
        Assert.Equal(font.FontMetrics.UnitsPerEm, metrics.UnitsPerEm);
        Assert.Equal(new Vector2(metrics.UnitsPerEm * 72F), metrics.ScaleFactor);
        Assert.Equal(364, metrics.AdvanceWidth);
        Assert.Equal(1000, metrics.AdvanceHeight);
        Assert.Equal(265, metrics.Width);
        Assert.Equal(666, metrics.Height);
        Assert.Equal(33, metrics.LeftSideBearing);
        Assert.Equal(134, metrics.TopSideBearing);
        Assert.Equal(GlyphType.Fallback, metrics.GlyphType);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.Fonts.Tests;

public class TextLayoutTests
{
    private static readonly ApproximateFloatComparer Comparer = new(.1F);

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom, true)]
    [InlineData(LayoutMode.HorizontalBottomTop, true)]
    [InlineData(LayoutMode.VerticalLeftRight, false)]
    [InlineData(LayoutMode.VerticalRightLeft, false)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, false)]
    [InlineData(LayoutMode.VerticalMixedRightLeft, false)]
    public void CanDetectHorizontalLayoutMode(LayoutMode mode, bool vertical)
        => Assert.Equal(vertical, mode.IsHorizontal());

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom, false)]
    [InlineData(LayoutMode.HorizontalBottomTop, false)]
    [InlineData(LayoutMode.VerticalLeftRight, true)]
    [InlineData(LayoutMode.VerticalRightLeft, true)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, false)]
    [InlineData(LayoutMode.VerticalMixedRightLeft, false)]
    public void CanDetectVerticalLayoutMode(LayoutMode mode, bool vertical)
        => Assert.Equal(vertical, mode.IsVertical());

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom, false)]
    [InlineData(LayoutMode.HorizontalBottomTop, false)]
    [InlineData(LayoutMode.VerticalLeftRight, false)]
    [InlineData(LayoutMode.VerticalRightLeft, false)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, true)]
    [InlineData(LayoutMode.VerticalMixedRightLeft, true)]
    public void CanDetectVerticalMixedLayoutMode(LayoutMode mode, bool vertical)
        => Assert.Equal(vertical, mode.IsVerticalMixed());

    [Fact]
    public void FakeFontGetGlyph()
    {
        Font font = CreateFont("hello world");
        Assert.True(font.TryGetGlyphs(new CodePoint('h'), ColorFontSupport.None, out Glyph? glyph));
        Assert.NotNull(glyph);
    }

    [Theory]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Left,
        0,
        10)]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Right,
        0,
        -320)]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Center,
        0,
        -155)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Left,
        -60,
        10)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Right,
        -60,
        -320)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Center,
        -60,
        -155)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Left,
        -30,
        10)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Right,
        -30,
        -320)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Center,
        -30,
        -155)]
    public void CanAlignText(
        VerticalAlignment vertical,
        HorizontalAlignment horizontal,
        float top,
        float left)
    {
        const string text = "hello world\nhello";
        Font font = CreateFont(text);

        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical
        };

        FontRectangle bound = TextMeasurer.MeasureBounds(text.AsSpan(), options);

        Assert.Equal(310, bound.Width, 3F);
        Assert.Equal(40, bound.Height, 3F);
        Assert.Equal(left, bound.Left, 3F);
        Assert.Equal(top, bound.Top, 3F);
    }

    [Theory]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Left,
        0,
        180)]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Right,
        0,
        -320)]
    [InlineData(
        VerticalAlignment.Top,
        HorizontalAlignment.Center,
        0,
        -70)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Left,
        -60,
        180)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Right,
        -60,
        -320)]
    [InlineData(
        VerticalAlignment.Bottom,
        HorizontalAlignment.Center,
        -60,
        -70)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Left,
        -30,
        180)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Right,
        -30,
        -320)]
    [InlineData(
        VerticalAlignment.Center,
        HorizontalAlignment.Center,
        -30,
        -70)]
    public void CanAlignWithWrapping(
        VerticalAlignment vertical,
        HorizontalAlignment horizontal,
        float top,
        float left)
    {
        // Using a string with a forced line break shorter than the wrapping
        // width covers cases where the offset should be expanded for both single and multiple lines.
        const string text = "hello world\nhello";
        Font font = CreateFont(text);

        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            WrappingLength = 500,
            TextAlignment = TextAlignment.End
        };

        FontRectangle bound = TextMeasurer.MeasureBounds(text.AsSpan(), options);

        Assert.Equal(310, bound.Width, 3F);
        Assert.Equal(40, bound.Height, 3F);
        Assert.Equal(left, bound.Left, 3F);
        Assert.Equal(top, bound.Top, 3F);
    }

    [Fact]
    public void MeasureTextWithSpan()
    {
        string text = "hello";
        Font font = CreateFont(text);

        // 72 * emSize means 1pt = 1px
        FontRectangle size = TextMeasurer.MeasureBounds(text.AsSpan(), new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(10, size.Height, 4F);
        Assert.Equal(130, size.Width, 4F);
    }

    [Theory]
    [InlineData("h", 10, 10)]
    [InlineData("he", 10, 40)]
    [InlineData("hel", 10, 70)]
    [InlineData("hello", 10, 130)]
    [InlineData("hello world", 10, 310)]
    [InlineData("hello world\nhello world", 40, 310)]
    [InlineData("hello\nworld", 40, 130)]
    public void MeasureText(string text, float height, float width)
    {
        Font font = CreateFont(text);
        FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(height, size.Height, 4F);
        Assert.Equal(width, size.Width, 4F);
    }

    [Fact]
    public void GetGlyphMetrics()
    {
        const string text = "a b\nc";
        GlyphMetrics[] expectedGlyphMetrics =
        [
            new(new CodePoint('a'), FontRectangle.Empty, new FontRectangle(10, 0, 10, 10), FontRectangle.Empty, 0, 0),
            new(new CodePoint(' '), FontRectangle.Empty, new FontRectangle(40, 0, 30, 10), FontRectangle.Empty, 1, 1),
            new(new CodePoint('b'), FontRectangle.Empty, new FontRectangle(70, 0, 10, 10), FontRectangle.Empty, 2, 2),
            new(new CodePoint('c'), FontRectangle.Empty, new FontRectangle(10, 30, 10, 10), FontRectangle.Empty, 4, 4),
        ];

        Font font = CreateFont(text);

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(
            text.AsSpan(),
            new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor }).Span;

        // The hard break ends a non-empty line, so it is trimmed from visual
        // glyph bounds. The following glyph still carries its original source
        // string index.
        Assert.Equal(expectedGlyphMetrics.Length, glyphs.Length);

        for (int i = 0; i < expectedGlyphMetrics.Length; i++)
        {
            GlyphMetrics expected = expectedGlyphMetrics[i];
            GlyphMetrics actual = glyphs[i];
            Assert.Equal(expected.CodePoint, actual.CodePoint);

            // 4 dp as there is minor offset difference in the float values
            Assert.Equal(expected.Bounds.X, actual.Bounds.X, 4F);
            Assert.Equal(expected.Bounds.Y, actual.Bounds.Y, 4F);
            Assert.Equal(expected.Bounds.Height, actual.Bounds.Height, 4F);
            Assert.Equal(expected.Bounds.Width, actual.Bounds.Width, 4F);
        }
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void GetGlyphMetrics_HardBreakBoundsAreNotNegative(LayoutMode layoutMode)
    {
        const string text = "\n";
        Font font = CreateFont(text);

        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            LayoutMode = layoutMode
        };

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;
        Assert.Equal(1, glyphs.Length);
        Assert.True(CodePoint.IsNewLine(glyphs[0].CodePoint));
        Assert.True(glyphs[0].Bounds.X >= 0);
        Assert.True(glyphs[0].Bounds.Y >= 0);
    }

    [Theory]
    [InlineData("hello world", 10, 87.125F)]
    [InlineData("hello world hello world hello world", 11.438F, 279.13F)]
    [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
        "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
        62.625,
        318.86F)]
    public void MeasureTextWordWrappingHorizontalTopBottom(string text, float height, float width)
    {
        if (SystemFonts.TryGet("SimSun", out FontFamily family))
        {
            Font font = family.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalTopBottom
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { height, width });

            FontRectangle size = TextMeasurer.MeasureBounds(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("hello world", 10, 87.125F)]
    [InlineData("hello world hello world hello world", 11.438F, 279.13F)]
    [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
        "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
        62.625,
        318.86F)]
    public void MeasureTextWordWrappingHorizontalBottomTop(string text, float height, float width)
    {
        if (SystemFonts.TryGet("SimSun", out FontFamily family))
        {
            Font font = family.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalBottomTop
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { height, width });

            FontRectangle size = TextMeasurer.MeasureBounds(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("hello world", 171.25F, 10)]
    [InlineData("hello world hello world hello world", 267.25F, 23.875F)]
    [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 318.563F, 62.813F)]
    public void MeasureTextWordWrappingVerticalLeftRight(string text, float height, float width)
    {
        if (SystemFonts.TryGet("SimSun", out FontFamily family))
        {
            Font font = family.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalLeftRight
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { height, width });

            FontRectangle size = TextMeasurer.MeasureBounds(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("hello world", 171.25F, 10)]
    [InlineData("hello world hello world hello world", 267.25F, 23.875F)]
    [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 318.563F, 62.813F)]
    public void MeasureTextWordWrappingVerticalRightLeft(string text, float height, float width)
    {
        if (SystemFonts.TryGet("SimSun", out FontFamily family))
        {
            Font font = family.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalRightLeft
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { height, width });

            FontRectangle size = TextMeasurer.MeasureBounds(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("hello world", 87.125F, 10)]
    [InlineData("hello world hello world hello world", 279.125F, 11.438F)]
    [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 318.563F, 62.813F)]
    public void MeasureTextWordWrappingVerticalMixedLeftRight(string text, float height, float width)
    {
        if (SystemFonts.TryGet("SimSun", out FontFamily family))
        {
            Font font = family.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalMixedLeftRight
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { height, width });

            FontRectangle size = TextMeasurer.MeasureBounds(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.Standard, 100, 696.51F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakAll, 129.29F, 237.53F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakWord, 128, 237.53F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.KeepAll, 65.29F, 699)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.Standard, 96F, 696.51F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakAll, 129.29F, 237.53F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakWord, 128, 237.53F)]
    [InlineData("Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.KeepAll, 61, 699)]
    public void MeasureTextWordBreakMatchesMDN(string text, LayoutMode layoutMode, WordBreaking wordBreaking, float height, float width)
    {
        // See https://developer.mozilla.org/en-US/docs/Web/CSS/word-break
        if (SystemFonts.TryGet("Arial", out FontFamily arial) &&
            SystemFonts.TryGet("Microsoft JhengHei", out FontFamily jhengHei))
        {
            Font font = arial.CreateFont(16);
            TextOptions options = new(font)
            {
                WrappingLength = 238,
                LayoutMode = layoutMode,
                WordBreaking = wordBreaking,
                FallbackFontFamilies = new[] { jhengHei }
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { layoutMode, wordBreaking });

            FontRectangle size = TextMeasurer.MeasureAdvance(text, options);
            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Theory]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.Standard, 100, 870.635F)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakAll, 100, 500)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakWord, 120, 490.35F)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.KeepAll, 81.89F, 870.635F)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.Standard, 101, 870.635F)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakAll, 100, 500)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakWord, 121, 490.35F)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.KeepAll, 61, 699)]
    public void MeasureTextWordBreak(string text, LayoutMode layoutMode, WordBreaking wordBreaking, float height, float width)
    {
        // See https://developer.mozilla.org/en-US/docs/Web/CSS/word-break
        if (SystemFonts.TryGet("Arial", out FontFamily arial) &&
            SystemFonts.TryGet("Microsoft JhengHei", out FontFamily jhengHei))
        {
            Font font = arial.CreateFont(20);
            TextOptions options = new(font)
            {
                WrappingLength = 500,
                LayoutMode = layoutMode,
                WordBreaking = wordBreaking,
                FallbackFontFamilies = new[] { jhengHei }
            };

            FontRectangle size = TextMeasurer.MeasureAdvance(
                text,
                options);

            TextLayoutTestUtilities.TestLayout(text, options, properties: new { layoutMode, wordBreaking });

            Assert.Equal(width, size.Width, 4F);
            Assert.Equal(height, size.Height, 4F);
        }
    }

    [Fact]
    public void KeepAllSuppressesBreaksBetweenTypographicWordUnits()
    {
        const string text = "\u7A93\u304E\u308F\u306E\u30C8\u30C3\u30C8\u3061\u3083\u3093";
        Font font = TestFonts.GetFont(TestFonts.NotoSansJPRegular, 20);
        float wrappingLength = TextMeasurer.MeasureAdvance("\u7A93\u304E", new TextOptions(font)).Width;

        TextOptions standard = new(font)
        {
            WrappingLength = wrappingLength,
            WordBreaking = WordBreaking.Standard
        };

        TextOptions keepAll = new(font)
        {
            WrappingLength = wrappingLength,
            WordBreaking = WordBreaking.KeepAll
        };

        Assert.True(TextMeasurer.CountLines(text, standard) > 1);
        Assert.Equal(1, TextMeasurer.CountLines(text, keepAll));
    }

    [Fact]
    public void KeepAllAllowsBreaksAtWordSeparators()
    {
        const string text = "hello world";
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 20);
        float wrappingLength = TextMeasurer.MeasureAdvance("hello", new TextOptions(font)).Width * .95F;

        TextOptions options = new(font)
        {
            WrappingLength = wrappingLength,
            WordBreaking = WordBreaking.KeepAll
        };

        Assert.Equal(2, TextMeasurer.CountLines(text, options));
    }

    [Fact]
    public void StandardWordBreakingAllowsUrlBreakAfterNumericPathSegment()
    {
        const string text = "https://a/2024/05";
        const string expectedFirstLine = "https://a/2024/";
        Font font = CreateFont(text);
        TextOptions noWrap = new(font);
        float expectedWidth = TextMeasurer.MeasureAdvance(expectedFirstLine, noWrap).Width;

        TextOptions options = new(font)
        {
            WrappingLength = expectedWidth + 1.1F,
            WordBreaking = WordBreaking.Standard
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(2, metrics.LineCount);
        Assert.Equal(expectedFirstLine, GetSourceTextForLine(text, metrics, 0));
        Assert.Equal("05", GetSourceTextForLine(text, metrics, 1));
        Assert.Equal(expectedWidth, metrics.Advance.Width, Comparer);
    }

    [Fact]
    public void StandardWordBreakingDoesNotTreatNumericFractionAsUrl()
    {
        const string text = "1/2/3";
        Font font = CreateFont(text);
        TextOptions noWrap = new(font);
        float fullWidth = TextMeasurer.MeasureAdvance(text, noWrap).Width;
        float wrappingWidth = TextMeasurer.MeasureAdvance("1/2/", noWrap).Width + .01F;

        TextOptions options = new(font)
        {
            WrappingLength = wrappingWidth,
            WordBreaking = WordBreaking.Standard
        };

        FontRectangle size = TextMeasurer.MeasureAdvance(text, options);

        Assert.Equal(fullWidth, size.Width, Comparer);
    }

    [Fact]
    public void StandardWordBreakingKeepsNonUrlSolidusRunTogether()
    {
        const string text = "bbbbb/ccccc";
        Font font = CreateFont(text);
        TextOptions noWrap = new(font);
        float fullWidth = TextMeasurer.MeasureAdvance(text, noWrap).Width;
        float wrappingWidth = TextMeasurer.MeasureAdvance("bbbbb/", noWrap).Width + 1.1F;

        TextOptions options = new(font)
        {
            WrappingLength = wrappingWidth,
            WordBreaking = WordBreaking.Standard
        };

        FontRectangle size = TextMeasurer.MeasureAdvance(text, options);

        Assert.Equal(fullWidth, size.Width, Comparer);
    }

    [Theory]
    [InlineData("ab", 477, 1081, false)] // no kerning rules defined for lowercase ab so widths should stay the same
    [InlineData("ab", 477, 1081, true)]
    [InlineData("AB", 465, 1033, false)] // width changes between kerning enabled or not
    [InlineData("AB", 465, 654, true)]
    public void MeasureTextWithKerning(string text, float height, float width, bool applyKerning)
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);
        FontRectangle size = TextMeasurer.MeasureBounds(
            text,
            new TextOptions(new Font(font, 1))
            {
                Dpi = font.FontMetrics.ScaleFactor,
                KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            });

        Assert.Equal(height, size.Height, 4F);
        Assert.Equal(width, size.Width, 4F);
    }

    [Theory]
    [InlineData("a", 100, 100, 125, 396)]
    public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);

        GlyphRenderer glyphRenderer = new();
        TextRenderer renderer = new(glyphRenderer);
        renderer.RenderText(
            text,
            new TextOptions(new Font(font, 1))
            {
                Dpi = font.FontMetrics.ScaleFactor,
                Origin = new Vector2(x, y)
            });

        Assert.Equal(expectedX, glyphRenderer.GlyphRects[0].Location.X, 2F);
        Assert.Equal(expectedY, glyphRenderer.GlyphRects[0].Location.Y, 2F);
    }

    // https://github.com/SixLabors/Fonts/issues/244
    [Fact]
    public void MeasureTextLeadingFraction()
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);
        TextOptions textOptions = new(font);
        FontRectangle measurement = TextMeasurer.MeasureBounds("/ This will fail", textOptions);

        Assert.NotEqual(FontRectangle.Empty, measurement);
    }

    [Theory]
    [InlineData("hello world", 1)]
    [InlineData("hello world\nhello world", 2)]
    [InlineData("hello world\nhello world\nhello world", 3)]
    public void CountLines(string text, int usedLineMetrics)
    {
        Font font = CreateFont(text);
        int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(usedLineMetrics, count);
    }

    [Fact]
    public void CountLinesWithSpan()
    {
        Font font = CreateFont("hello\n!");

        Span<char> text =
        [
            'h',
            'e',
            'l',
            'l',
            'o',
            '\n',
            '!'
        ];
        int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void TextHyphenation_Custom_DrawsSelectedSoftHyphenMarker(LayoutMode layoutMode)
    {
        const string text = "extra\u00ADordinary next";
        Font font = TestFonts.GetFont(TestFonts.NotoSansRegular, 30);
        Vector2 origin = new(24, 28);

        TextOptions measureOptions = new(font)
        {
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*')
        };

        TextOptions hyphenMeasureOptions = new(font)
        {
            LayoutMode = layoutMode,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*')
        };

        FontRectangle beforeSoftHyphen = TextMeasurer.MeasureAdvance("extra", measureOptions);
        FontRectangle customHyphen = TextMeasurer.MeasureAdvance("*", hyphenMeasureOptions);
        float softHyphenBreakAdvance = layoutMode.IsHorizontal()
            ? beforeSoftHyphen.Width + customHyphen.Width
            : beforeSoftHyphen.Height + customHyphen.Height;

        TextOptions options = new(font)
        {
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*'),
            WrappingLength = softHyphenBreakAdvance + 1F
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: layoutMode);

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The wrap is based on the soft-hyphen break candidate: source prefix plus
        // marker. BreakLines rejects exact equality as overflow, so the extra pixel
        // lets this candidate fit without admitting the following grapheme.
        Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('*')));
        Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('-')));
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void TextHyphenation_Custom_DrawsBidiSoftHyphenMarker(LayoutMode layoutMode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        const string text = "Tall של\u00ADומשלוםשלוםשלום extra عرب next";
        Vector2 origin = new(24, 28);

        // Forced vertical layout intentionally does not enable generic horizontal features.
        // Request cursive positioning explicitly so the Arabic run remains a useful bidi
        // visual check in every layout mode.
        Tag[] featureTags = layoutMode.IsVertical()
            ? [KnownFeatureTags.CursivePositioning]
            : [];

        TextOptions measureOptions = new(font)
        {
            FeatureTags = featureTags,
            FallbackFontFamilies = [hebrew, arabic],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*')
        };

        TextOptions hyphenMeasureOptions = new(font)
        {
            FeatureTags = featureTags,
            FallbackFontFamilies = [hebrew, arabic],
            LayoutMode = layoutMode,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*')
        };

        FontRectangle beforeSoftHyphen = TextMeasurer.MeasureAdvance("Tall של", measureOptions);
        FontRectangle customHyphen = TextMeasurer.MeasureAdvance("*", hyphenMeasureOptions);
        float softHyphenBreakAdvance = layoutMode.IsHorizontal()
            ? beforeSoftHyphen.Width + customHyphen.Width
            : beforeSoftHyphen.Height + customHyphen.Height;

        TextOptions options = new(font)
        {
            FeatureTags = featureTags,
            FallbackFontFamilies = [hebrew, arabic],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Custom,
            CustomHyphen = new('*'),
            WrappingLength = softHyphenBreakAdvance + 1F
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: layoutMode);

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The line contains LTR Latin, a selected soft-hyphen break inside a long
        // RTL Hebrew fallback word, and RTL Arabic after the break. The marker
        // should still materialize exactly once after bidi reordering and fallback shaping.
        Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('*')));
        Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('-')));
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void TextHyphenation_Standard_DrawsFallbackSoftHyphenMarker(LayoutMode layoutMode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);

        Font font = latin.CreateFont(30);
        const string text = "Tall של\u00ADומשלוםשלוםשלום extra next";
        Vector2 origin = new(24, 28);

        TextOptions measureOptions = new(font)
        {
            FallbackFontFamilies = [hebrew],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Standard
        };

        TextOptions hyphenMeasureOptions = new(font)
        {
            FallbackFontFamilies = [hebrew],
            LayoutMode = layoutMode,
            TextHyphenation = TextHyphenation.Standard
        };

        FontRectangle beforeSoftHyphen = TextMeasurer.MeasureAdvance("Tall של", measureOptions);
        FontRectangle hardHyphen = TextMeasurer.MeasureAdvance("\u2010", hyphenMeasureOptions);
        float softHyphenBreakAdvance = layoutMode.IsHorizontal()
            ? beforeSoftHyphen.Width + hardHyphen.Width
            : beforeSoftHyphen.Height + hardHyphen.Height;

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.Standard,
            WrappingLength = softHyphenBreakAdvance
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: layoutMode);

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The selected U+00AD break sits inside the Hebrew word, which is drawn
        // through the fallback family. The standard marker must be visible at
        // that fallback-run break location, not only in Latin text.
        Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint(0x2010)));
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void TextHyphenation_None_DoesNotDrawFallbackSoftHyphenMarker(LayoutMode layoutMode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);

        Font font = latin.CreateFont(30);
        const string text = "Tall של\u00ADומשלוםשלוםשלום extra next";
        Vector2 origin = new(24, 28);

        TextOptions measureOptions = new(font)
        {
            FallbackFontFamilies = [hebrew],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.None
        };

        FontRectangle firstGraphemeAfterSoftHyphen = TextMeasurer.MeasureAdvance("Tall שלו", measureOptions);
        float firstGraphemeAdvance = layoutMode.IsHorizontal()
            ? firstGraphemeAfterSoftHyphen.Width
            : firstGraphemeAfterSoftHyphen.Height;

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew],
            LayoutMode = layoutMode,
            Origin = origin,
            TextHyphenation = TextHyphenation.None,
            WrappingLength = firstGraphemeAdvance - 0.5F
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: layoutMode);

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The source contains U+00AD inside the Hebrew fallback run. With hyphenation
        // disabled it remains non-rendering source text; no visible hyphen marker
        // should be emitted at that fallback-script location.
        Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint(0x2010)));
        Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('-')));
    }

    [Theory]
    [InlineData(TextEllipsis.Standard)]
    [InlineData(TextEllipsis.Custom)]
    [InlineData(TextEllipsis.None)]
    public void TextEllipsis_DrawsMaxLinesMarker(TextEllipsis ellipsis)
    {
        Font font = CreateRenderingFont(34);
        const string text = "one two three four five six";

        TextOptions options = new(font)
        {
            Origin = new(24, 42),
            WrappingLength = 210,
            MaxLines = 1,
            TextEllipsis = ellipsis,
            CustomEllipsis = new('*'),
            LayoutMode = LayoutMode.HorizontalTopBottom
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: ellipsis.ToString().ToLowerInvariant());

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // These assertions document the visible marker behavior shown by the
        // reference images: standard uses U+2026, custom uses the configured
        // marker, and none only clamps the visible line count.
        if (ellipsis == TextEllipsis.Standard)
        {
            Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint(0x2026)));
            Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('*')));
        }
        else if (ellipsis == TextEllipsis.Custom)
        {
            Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint(0x2026)));
            Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('*')));
        }
        else
        {
            Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint(0x2026)));
            Assert.Equal(0, CountGlyphs(metrics.GetGlyphMetrics().Span, new CodePoint('*')));
        }
    }

    [Fact]
    public void TextPlaceholder_SharesInsertionCodePointOffset()
    {
        const string text = "a\u0301b";
        Font font = CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            TextRuns =
            [
                new()
                {
                    Start = 1,
                    End = 1,
                    Placeholder = new(40, 24, TextPlaceholderAlignment.Baseline, 18)
                }
            ]
        };

        ShapedText shapedText = TextLayout.ShapeText(text.AsSpan(), options);
        LogicalTextLine logicalLine = TextLayout.ComposeLogicalLine(shapedText, text.AsSpan(), options);

        GlyphLayoutData placeholder = default;
        GlyphLayoutData following = default;
        for (int i = 0; i < logicalLine.TextLine.Count; i++)
        {
            GlyphLayoutData current = logicalLine.TextLine[i];
            if (current.CodePoint == CodePoint.ObjectReplacementChar)
            {
                placeholder = current;
                continue;
            }

            if (current.CodePoint == new CodePoint('b'))
            {
                following = current;
            }
        }

        // The placeholder is inserted after a grapheme made from two source
        // codepoints. It must therefore share the codepoint and UTF-16 offset
        // of the following source glyph instead of using the grapheme run index.
        Assert.Equal(2, placeholder.CodePointIndex);
        Assert.Equal(2, placeholder.StringIndex);
        Assert.Equal(following.CodePointIndex, placeholder.CodePointIndex);
        Assert.Equal(following.StringIndex, placeholder.StringIndex);
    }

    [Fact]
    public void TextPlaceholder_AddsInlineAdvanceWithoutConsumingSourceText()
    {
        const string text = "ab";
        Font font = CreateFont(text);
        TextOptions baselineOptions = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor
        };

        TextOptions placeholderOptions = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            TextRuns =
            [
                new()
                {
                    Start = 1,
                    End = 1,
                    Placeholder = new(40, 24, TextPlaceholderAlignment.Baseline, 18)
                }
            ]
        };

        FontRectangle baseline = TextMeasurer.MeasureAdvance(text, baselineOptions);
        FontRectangle withPlaceholder = TextMeasurer.MeasureAdvance(text, placeholderOptions);
        GlyphMetrics[] glyphs = TextMeasurer.GetGlyphMetrics(text, placeholderOptions).ToArray();

        // The placeholder contributes its own inline advance, but the source
        // text still has only the two original graphemes around the inserted object.
        Assert.Equal(baseline.Width + 40, withPlaceholder.Width, 0.1F);
        Assert.Equal(1, CountGlyphs(glyphs, CodePoint.ObjectReplacementChar));
        Assert.Equal(1, CountGlyphs(glyphs, new CodePoint('a')));
        Assert.Equal(1, CountGlyphs(glyphs, new CodePoint('b')));
    }

    [Theory]
    [InlineData(TextPlaceholderAlignment.Baseline)]
    [InlineData(TextPlaceholderAlignment.AboveBaseline)]
    [InlineData(TextPlaceholderAlignment.BelowBaseline)]
    [InlineData(TextPlaceholderAlignment.Top)]
    [InlineData(TextPlaceholderAlignment.Bottom)]
    [InlineData(TextPlaceholderAlignment.Middle)]
    public void TextPlaceholderAlignment_PositionsPlaceholderBounds(TextPlaceholderAlignment alignment)
    {
        const string text = "Alpha Omega";
        const float width = 56;
        const float height = 30;
        const float baselineOffset = 23;
        Font font = CreateRenderingFont(34);
        TextOptions surroundingOptions = new(font);

        TextOptions options = new(font)
        {
            TextRuns =
            [
                new()
                {
                    Start = 6,
                    End = 6,
                    Placeholder = new(width, height, alignment, baselineOffset)
                }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        LineMetrics surroundingLine = TextMeasurer.Measure(text, surroundingOptions).LineMetrics[0];
        ReadOnlySpan<GlyphMetrics> glyphs = metrics.GetGlyphMetrics().Span;
        FontRectangle placeholderBounds = FontRectangle.Empty;
        for (int i = 0; i < glyphs.Length; i++)
        {
            if (glyphs[i].CodePoint == CodePoint.ObjectReplacementChar)
            {
                placeholderBounds = glyphs[i].Bounds;
                break;
            }
        }

        float baseline = surroundingLine.Start.Y + surroundingLine.Baseline;
        float lineTop = surroundingLine.Start.Y;
        float lineBottom = surroundingLine.Start.Y + surroundingLine.LineHeight;
        float expectedTop;
        float expectedBottom;

        switch (alignment)
        {
            case TextPlaceholderAlignment.AboveBaseline:
                expectedTop = baseline - height;
                expectedBottom = baseline;
                break;

            case TextPlaceholderAlignment.BelowBaseline:
                expectedTop = baseline;
                expectedBottom = baseline + height;
                break;

            case TextPlaceholderAlignment.Top:
                expectedTop = lineTop;
                expectedBottom = expectedTop + height;
                break;

            case TextPlaceholderAlignment.Bottom:
                expectedBottom = lineBottom;
                expectedTop = expectedBottom - height;
                break;

            case TextPlaceholderAlignment.Middle:
                float center = (lineTop + lineBottom) * .5F;
                expectedTop = center - (height * .5F);
                expectedBottom = center + (height * .5F);
                break;

            default:
                expectedTop = baseline - baselineOffset;
                expectedBottom = baseline + height - baselineOffset;
                break;
        }

        // Each alignment changes only the placeholder's vertical placement. The
        // inline width remains the caller-provided atomic object width.
        Assert.Equal(width, placeholderBounds.Width, 0.1F);
        Assert.Equal(height, placeholderBounds.Height, 0.1F);
        Assert.Equal(expectedTop, placeholderBounds.Top, 0.1F);
        Assert.Equal(expectedBottom, placeholderBounds.Bottom, 0.1F);
    }

    [Fact]
    public void TextPlaceholder_RunMustBeInsertionPoint()
    {
        const string text = "ab";
        Font font = CreateFont(text);
        TextOptions options = new(font)
        {
            TextRuns =
            [
                new()
                {
                    Start = 1,
                    End = 2,
                    Placeholder = new(40, 24, TextPlaceholderAlignment.Baseline, 18)
                }
            ]
        };

        // Placeholder runs represent an inserted object at a source position.
        // Covering real source graphemes would make the object consume text.
        Assert.Throws<ArgumentException>(() => TextMeasurer.MeasureAdvance(text, options));
    }

    [Theory]
    [InlineData(TextPlaceholderAlignment.Baseline)]
    [InlineData(TextPlaceholderAlignment.AboveBaseline)]
    [InlineData(TextPlaceholderAlignment.BelowBaseline)]
    [InlineData(TextPlaceholderAlignment.Top)]
    [InlineData(TextPlaceholderAlignment.Bottom)]
    [InlineData(TextPlaceholderAlignment.Middle)]
    public void TextPlaceholder_DrawsInlineReservedSpace(TextPlaceholderAlignment alignment)
    {
        const string text = "Alpha  Omega\nAlpha  Omega";
        Font font = CreateRenderingFont(34);
        TextPlaceholder placeholder = new(56, 30, alignment, 23);
        Vector2 origin = new(24, 58);

        TextOptions optionsBase = new(font)
        {
            Origin = origin,
        };

        TextOptions options = new(optionsBase)
        {
            TextRuns =
            [
                new()
                {
                    Start = 6,
                    End = 6,
                    Placeholder = placeholder
                }
            ]
        };

        IReadOnlyList<GlyphPathCollection> glyphs = TextBuilder.GenerateGlyphs(text, options);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        LineMetrics line = metrics.LineMetrics[0];
        LineMetrics lineBase = TextMeasurer.Measure(text, optionsBase).LineMetrics[0];

        // Expected output:
        // - Baseline aligns the placeholder's internal baseline offset to the red baseline.
        // - AboveBaseline places the object immediately above the red baseline.
        // - BelowBaseline places the object immediately below the red baseline.
        // - Top, middle, and bottom align the object against the blue surrounding font line box.
        // - The green placeholder-layout line box may grow when the object extends beyond that blue box.
        ReadOnlySpan<GlyphMetrics> measuredGlyphs = metrics.GetGlyphMetrics().Span;
        FontRectangle placeholderBounds = FontRectangle.Empty;
        for (int i = 0; i < measuredGlyphs.Length; i++)
        {
            if (measuredGlyphs[i].CodePoint == CodePoint.ObjectReplacementChar)
            {
                placeholderBounds = measuredGlyphs[i].Bounds;
                break;
            }
        }

        TextLayoutTestUtilities.TestImage(
            340,
            180,
            image => image.Mutate(x => x.Paint(canvas =>
            {
                RectanglePolygon lineBox = new(
                    0,
                    line.Start.Y,
                    340,
                    line.LineHeight);

                RectanglePolygon lineBoxBase = new(
                    0,
                    lineBase.Start.Y,
                    340,
                    lineBase.LineHeight);

                // Green is the actual line box for the layout that contains the
                // placeholder. It shows whether the object caused this line to
                // reserve more vertical space.
                canvas.Fill(Brushes.Solid(Color.Green.WithAlpha(.15F)), lineBox);

                // Blue is the same text measured without the placeholder. This
                // gives a stable reference for the surrounding font line box
                // that top/middle/bottom alignment should use.
                canvas.Fill(Brushes.Solid(Color.LightBlue.WithAlpha(.95F)), lineBoxBase);

                canvas.Draw(Pens.Solid(Color.Gray, 1), lineBox);
                canvas.DrawGlyphs(Brushes.Solid(Color.Black), Pens.Solid(Color.Black, 1F), glyphs);

                // The black outline is the caller-owned inline object bounds
                // returned by the public glyph-bounds API.
                RectanglePolygon box = new(
                    placeholderBounds.X,
                    placeholderBounds.Y,
                    placeholderBounds.Width,
                    placeholderBounds.Height);

                canvas.Draw(Pens.Solid(Color.Black, 1), box);

                // Red is the baseline for the surrounding text without the
                // placeholder. Baseline-relative modes should align to this.
                float baseline = lineBase.Start.Y + lineBase.Baseline;
                canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(0, baseline), new PointF(340, baseline));
            })),
            properties: alignment.ToString().ToLowerInvariant());

        // The visual output shows the reserved object space; the measured data
        // also exposes one object-replacement glyph at the insertion point.
        Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, CodePoint.ObjectReplacementChar));
    }

    [Theory]
    [InlineData(TextPlaceholderAlignment.Baseline)]
    [InlineData(TextPlaceholderAlignment.AboveBaseline)]
    [InlineData(TextPlaceholderAlignment.BelowBaseline)]
    [InlineData(TextPlaceholderAlignment.Top)]
    [InlineData(TextPlaceholderAlignment.Bottom)]
    [InlineData(TextPlaceholderAlignment.Middle)]
    public void TextPlaceholder_DrawsOversizedInlineReservedSpace(TextPlaceholderAlignment alignment)
    {
        const string text = "Alpha  Omega\nAlpha  Omega";
        Font font = CreateRenderingFont(34);
        TextPlaceholder placeholder = new(56, 82, alignment, 23);
        Vector2 origin = new(24, 98);

        TextOptions optionsBase = new(font)
        {
            Origin = origin,
        };

        TextOptions options = new(optionsBase)
        {
            TextRuns =
            [
                new()
                {
                    Start = 6,
                    End = 6,
                    Placeholder = placeholder
                }
            ]
        };

        IReadOnlyList<GlyphPathCollection> glyphs = TextBuilder.GenerateGlyphs(text, options);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        LineMetrics line = metrics.LineMetrics[0];
        LineMetrics lineBase = TextMeasurer.Measure(text, optionsBase).LineMetrics[0];

        // Expected output for oversized objects:
        // - Top keeps the object top aligned to the blue surrounding line box and grows downward.
        // - Bottom keeps the object bottom aligned to the blue surrounding line box and grows upward.
        // - Middle centers the object on the blue surrounding line box and grows equally both ways.
        // - The green line box must expand far enough that the second line does not overlap the object.
        ReadOnlySpan<GlyphMetrics> measuredGlyphs = metrics.GetGlyphMetrics().Span;
        FontRectangle placeholderBounds = FontRectangle.Empty;
        for (int i = 0; i < measuredGlyphs.Length; i++)
        {
            if (measuredGlyphs[i].CodePoint == CodePoint.ObjectReplacementChar)
            {
                placeholderBounds = measuredGlyphs[i].Bounds;
                break;
            }
        }

        TextLayoutTestUtilities.TestImage(
            340,
            260,
            image => image.Mutate(x => x.Paint(canvas =>
            {
                RectanglePolygon lineBox = new(
                    0,
                    line.Start.Y,
                    340,
                    line.LineHeight);

                RectanglePolygon lineBoxBase = new(
                    0,
                    lineBase.Start.Y,
                    340,
                    lineBase.LineHeight);

                // Green is the expanded line box for the placeholder layout.
                // It should grow enough that the second line does not overlap
                // the oversized inline object.
                canvas.Fill(Brushes.Solid(Color.Green.WithAlpha(.15F)), lineBox);

                // Blue is the normal surrounding text line box. Top, middle,
                // and bottom align against this box before any oversized object
                // growth is applied to the actual line.
                canvas.Fill(Brushes.Solid(Color.LightBlue.WithAlpha(.95F)), lineBoxBase);

                canvas.Draw(Pens.Solid(Color.Gray, 1), lineBox);
                canvas.DrawGlyphs(Brushes.Solid(Color.Black), Pens.Solid(Color.Black, 1F), glyphs);

                // The black outline is intentionally taller than the normal
                // text line so this visual test shows both alignment and line
                // growth in the same output.
                RectanglePolygon box = new(
                    placeholderBounds.X,
                    placeholderBounds.Y,
                    placeholderBounds.Width,
                    placeholderBounds.Height);

                canvas.Draw(Pens.Solid(Color.Black, 1), box);

                // Red is the baseline for the surrounding text without the
                // placeholder; these modes should not align to it directly.
                float baseline = lineBase.Start.Y + lineBase.Baseline;
                canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(0, baseline), new PointF(340, baseline));
            })),
            properties: alignment.ToString().ToLowerInvariant());

        // The oversized visual still represents one atomic inline object.
        Assert.Equal(1, CountGlyphs(metrics.GetGlyphMetrics().Span, CodePoint.ObjectReplacementChar));
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void LineMetrics_StartAndExtent_DrawsLineBoxes(LayoutMode layoutMode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "Tall שלום عرب\nSmall مرحبا שלום";

        // Forced vertical layout intentionally does not enable generic horizontal features.
        // Request cursive positioning explicitly so this visual test still covers
        // feature-driven Arabic positioning in that mode.
        Tag[] featureTags = layoutMode.IsVertical()
            ? [KnownFeatureTags.CursivePositioning]
            : [];

        TextOptions options = new(font)
        {
            FeatureTags = featureTags,
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            LayoutMode = layoutMode,
            LineSpacing = 1.25F,
            TextRuns =
            [
                new() { Start = 0, End = 4, Font = largeFont },
                new() { Start = 20, End = 25, Font = largeFont }
            ]
        };

        LineMetrics[] metrics = TextMeasurer.GetLineMetrics(text, options).ToArray();

        void DrawLineBoxes(Image<Rgba32> image)
            => image.Mutate(x => x.Paint(canvas =>
            {
                for (int i = 0; i < metrics.Length; i++)
                {
                    LineMetrics m = metrics[i];
                    Color startColor = i == 0
                        ? Color.Lime
                        : Color.Cyan;

                    Color endColor = i == 0
                        ? Color.Magenta
                        : Color.Yellow;

                    PointF gradientStart = new(m.Start.X, m.Start.Y);
                    PointF gradientEnd = new(m.Start.X + m.Extent.X, m.Start.Y + m.Extent.Y);

                    LinearGradientBrush fill = new(
                        gradientStart,
                        gradientEnd,
                        GradientRepetitionMode.None,
                        new ColorStop(0, startColor),
                        new ColorStop(1, endColor));

                    RectanglePolygon box = new(m.Start.X, m.Start.Y, m.Extent.X, m.Extent.Y);

                    canvas.Fill(fill, box);
                    canvas.Draw(Pens.Solid(Color.Black, 2), box);
                }
            }));

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawLineBoxes,
            properties: layoutMode);
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void GraphemeMetrics_GetSelectionBounds_DrawsGraphemeSelections(LayoutMode layoutMode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "Tall שלום عرب\nSmall مرحبا שלום";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            LayoutMode = layoutMode,
            LineSpacing = 1.25F,
            TextRuns =
            [
                new() { Start = 0, End = 4, Font = largeFont },
                new() { Start = 20, End = 25, Font = largeFont }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        void DrawSelections(Image<Rgba32> image)
            => image.Mutate(x => x.Paint(canvas =>
            {
                ReadOnlySpan<GraphemeMetrics> graphemes = metrics.GraphemeMetrics;
                for (int i = 0; i < graphemes.Length; i++)
                {
                    GraphemeMetrics grapheme = graphemes[i];
                    ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(grapheme).Span;
                    if (selection.IsEmpty)
                    {
                        continue;
                    }

                    FontRectangle bounds = selection[0];
                    PointF gradientStart = new(bounds.Left, bounds.Top);
                    PointF gradientEnd = layoutMode.IsHorizontal()
                        ? new(bounds.Right, bounds.Top)
                        : new(bounds.Left, bounds.Bottom);

                    // Vary the gradient by visual grapheme order so bidi reordering is visible.
                    Color startColor = (i & 1) == 0
                        ? Color.Lime
                        : Color.Cyan;

                    Color endColor = (i & 1) == 0
                        ? Color.Magenta
                        : Color.Yellow;

                    LinearGradientBrush fill = new(
                        gradientStart,
                        gradientEnd,
                        GradientRepetitionMode.None,
                        new ColorStop(0, startColor),
                        new ColorStop(1, endColor));

                    RectanglePolygon box = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

                    canvas.Fill(fill, box);
                    canvas.Draw(Pens.Solid(Color.Black, 1), box);
                }
            }));

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawSelections,
            properties: layoutMode);
    }

    [Fact]
    public void GraphemeMetrics_GetSelectionBounds_DrawsSelectionWithBlankLine()
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "Tall عرب שלום\n\nSmall مرحبا שלום";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            LayoutMode = LayoutMode.HorizontalTopBottom,
            LineSpacing = 1.25F,
            TextRuns =
            [
                new() { Start = 0, End = 4, Font = largeFont },
                new() { Start = 15, End = 20, Font = largeFont }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        void DrawSelection(Image<Rgba32> image)
        {
            CaretPosition anchor = metrics.GetCaret(CaretPlacement.Start);
            CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.TextEnd);

            // The first hard break ends a measuring text line and should not paint
            // its own box. The second hard break owns the empty line between text
            // lines, so full-text selection should include a visible blank-line box.
            image.Mutate(x => x.Paint(canvas =>
            {
                ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;
                for (int i = 0; i < selection.Length; i++)
                {
                    FontRectangle bounds = selection[i];
                    RectanglePolygon box = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

                    canvas.Fill(Brushes.Solid(Color.LightBlue), box);
                }
            }));
        }

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawSelection);
    }

    [Fact]
    public void GraphemeMetrics_GetSelectionBounds_DrawsBidiDragSelection()
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "Tall שלום عرب";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            LayoutMode = LayoutMode.HorizontalTopBottom,
            TextRuns =
            [
                new() { Start = 0, End = 4, Font = largeFont }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        ReadOnlySpan<GraphemeMetrics> graphemes = metrics.GraphemeMetrics;
        GraphemeMetrics first = default;
        GraphemeMetrics finalHebrew = default;
        for (int i = 0; i < graphemes.Length; i++)
        {
            if (graphemes[i].GraphemeIndex == 0)
            {
                first = graphemes[i];
            }

            if (graphemes[i].GraphemeIndex == 8)
            {
                finalHebrew = graphemes[i];
            }
        }

        // The text source is "Tall " then Hebrew then Arabic. In LTR paragraph
        // layout the RTL run is painted with Arabic before Hebrew, so dragging
        // left-to-right from "T" to the visual left side of the final Hebrew
        // grapheme should select "Tall " and the Hebrew word while leaving the
        // visually intervening Arabic word unselected.
        Vector2 anchorPoint = new(first.Advance.Left, FontRectangle.Center(first.Advance).Y);
        Vector2 focusPoint = new(
            finalHebrew.Advance.Left + (finalHebrew.Advance.Width * 0.25F),
            FontRectangle.Center(finalHebrew.Advance).Y);

        TextHit anchor = metrics.HitTest(anchorPoint);
        TextHit focus = metrics.HitTest(focusPoint);

        void DrawSelection(Image<Rgba32> image)
            => image.Mutate(x => x.Paint(canvas =>
            {
                ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;
                for (int i = 0; i < selection.Length; i++)
                {
                    FontRectangle bounds = selection[i];
                    RectanglePolygon box = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

                    canvas.Fill(Brushes.Solid(Color.LightBlue), box);
                }
            }));

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawSelection);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight, TextBidiMode.Normal)]
    [InlineData(TextDirection.LeftToRight, TextBidiMode.Override)]
    [InlineData(TextDirection.RightToLeft, TextBidiMode.Normal)]
    [InlineData(TextDirection.RightToLeft, TextBidiMode.Override)]
    public void TextBidiMode_DrawsMixedBidiLayout(TextDirection direction, TextBidiMode mode)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(40);
        const string text = "abc שלום عرب def";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 52),
            WrappingLength = 430,
            TextDirection = direction,
            TextBidiMode = mode,
            LayoutMode = LayoutMode.HorizontalTopBottom
        };

        // Mixed-script text makes the distinction visible: Normal preserves each
        // script's bidi class inside the paragraph direction, while Override
        // forces every real text character through the paragraph direction.
        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            properties: $"{(direction == TextDirection.RightToLeft ? "rtl" : "ltr")}-{mode.ToString().ToLowerInvariant()}");
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void CaretPosition_DrawsStartAndEndCarets(TextDirection direction)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "Tall שלום عرب\nSmall مرحبا שלום";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            TextDirection = direction,
            LayoutMode = LayoutMode.HorizontalTopBottom,
            LineSpacing = 1.25F,
            TextRuns =
            [
                new() { Start = 0, End = 4, Font = largeFont },
                new() { Start = 20, End = 25, Font = largeFont }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        void DrawCarets(Image<Rgba32> image)
        {
            CaretPosition start = metrics.GetCaret(CaretPlacement.Start);
            CaretPosition end = metrics.GetCaret(CaretPlacement.End);

            image.Mutate(x => x.Paint(canvas =>
            {
                // Solid carets make absolute start/end placement easy to compare
                // between LTR and RTL paragraph directions.
                DrawCaret(canvas, start, Color.Lime, 3, dashed: false);
                DrawCaret(canvas, end, Color.Magenta, 3, dashed: false);
            }));
        }

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            afterAction: DrawCarets,
            properties: direction == TextDirection.RightToLeft ? "rtl" : "ltr");
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void CaretPosition_DrawsMovedCarets(TextDirection direction)
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(34);
        const string text = "abc שלום عرب def\n123 אבג xyz";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 34),
            TextDirection = direction,
            LayoutMode = LayoutMode.HorizontalTopBottom,
            LineSpacing = 1.25F
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        void DrawCarets(Image<Rgba32> image)
        {
            CaretPosition textStart = metrics.GetCaret(CaretPlacement.Start);
            CaretPosition textEnd = metrics.GetCaret(CaretPlacement.End);
            ReadOnlySpan<GraphemeMetrics> graphemes = metrics.GraphemeMetrics;
            int hebrewLamedStringIndex = text.IndexOf('ל');
            int arabicBehStringIndex = text.IndexOf('ب');
            int secondLineHebrewBetStringIndex = text.IndexOf('ב');
            GraphemeMetrics hebrewLamed = default;
            GraphemeMetrics arabicBeh = default;
            GraphemeMetrics secondLineHebrewBet = default;

            for (int i = 0; i < graphemes.Length; i++)
            {
                GraphemeMetrics grapheme = graphemes[i];
                if (grapheme.StringIndex == hebrewLamedStringIndex)
                {
                    hebrewLamed = grapheme;
                }
                else if (grapheme.StringIndex == arabicBehStringIndex)
                {
                    arabicBeh = grapheme;
                }
                else if (grapheme.StringIndex == secondLineHebrewBetStringIndex)
                {
                    secondLineHebrewBet = grapheme;
                }
            }

            // Previous/next movement is source-order navigation. In mixed bidi text the
            // visual jump can cross runs, so the variable names include the anchor caret.
            CaretPosition hebrewRunCaret = metrics.GetCaretPosition(metrics.HitTest(FontRectangle.Center(hebrewLamed.Advance)));
            CaretPosition arabicRunCaret = metrics.GetCaretPosition(metrics.HitTest(FontRectangle.Center(arabicBeh.Advance)));
            CaretPosition secondLineHebrewRunCaret = metrics.GetCaretPosition(metrics.HitTest(FontRectangle.Center(secondLineHebrewBet.Advance)));
            CaretPosition nextFromHebrewRun = metrics.MoveCaret(hebrewRunCaret, CaretMovement.Next);
            CaretPosition nextWordFromHebrewRun = metrics.MoveCaret(hebrewRunCaret, CaretMovement.NextWord);
            CaretPosition previousFromArabicRun = metrics.MoveCaret(arabicRunCaret, CaretMovement.Previous);
            CaretPosition previousWordFromArabicRun = metrics.MoveCaret(arabicRunCaret, CaretMovement.PreviousWord);

            // Line movement keeps the original horizontal preference, then line start/end
            // resolve against the paragraph direction on the destination visual line.
            CaretPosition lineStartFromHebrewRun = metrics.MoveCaret(secondLineHebrewRunCaret, CaretMovement.LineStart);
            CaretPosition lineEndFromHebrewRun = metrics.MoveCaret(secondLineHebrewRunCaret, CaretMovement.LineEnd);

            image.Mutate(x => x.Paint(canvas =>
            {
                // Blue starts from a hit on ל in שלום and moves to the caret after ו.
                DrawCaret(canvas, nextFromHebrewRun, Color.Blue, 3, dashed: true);

                // Cyan starts from a hit on ל in שלום and moves to the word boundary after ם.
                DrawCaret(canvas, nextWordFromHebrewRun, Color.Cyan, 3, dashed: true);

                // Red starts from a hit on ب in عرب and moves one source-order grapheme toward ر.
                DrawCaret(canvas, previousFromArabicRun, Color.Red, 3, dashed: true);

                // Purple starts from a hit on ب in عرب and moves to the word boundary before ع.
                DrawCaret(canvas, previousWordFromArabicRun, Color.Purple, 3, dashed: true);

                // Lime starts from a hit on ב in אבג and moves to the second-line start.
                DrawCaret(canvas, lineStartFromHebrewRun, Color.Lime, 2, dashed: true);

                // Magenta starts from a hit on ב in אבג and moves to the second-line end.
                DrawCaret(canvas, lineEndFromHebrewRun, Color.Magenta, 2, dashed: true);
            }));
        }

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            afterAction: DrawCarets,
            properties: direction == TextDirection.RightToLeft ? "rtl" : "ltr");
    }

    [Fact]
    public void LineLayoutEnumerator_DrawsManualFlowAroundCircle()
    {
        Font font = CreateRenderingFont(22);
        const string text =
            "Text can flow around arbitrary shapes when each line is measured independently. " +
            "The caller chooses the available width for the next row, places the returned line, " +
            "then asks for another line using the next open slot. This mirrors the Pretext-style " +
            "manual layout demos without reshaping the paragraph for every row.";

        TextOptions options = new(font)
        {
            Origin = Vector2.Zero,
            WrappingLength = -1,
            LineSpacing = 1.15F
        };

        TextBlock block = new(text, options);
        LineLayoutEnumerator enumerator = block.EnumerateLineLayouts();

        // The test owns all page geometry. TextBlock owns the expensive shaping
        // and Unicode layout preparation, while the caller chooses where each
        // successive line is allowed to fit.
        const float pageLeft = 28;
        const float pageTop = 28;
        const float pageRight = 592;
        const float pageBottom = 334;
        const float circleX = 340;
        const float circleY = 174;
        const float circleRadius = 74;
        const float circlePadding = 14;
        const float minSlotWidth = 112;
        float y = pageTop;
        bool hasMoreText = true;

        TextLayoutTestUtilities.TestImage(
            620,
            360,
            image => image.Mutate(x => x.Paint(canvas =>
            {
                canvas.Fill(Brushes.Solid(Color.White));
                canvas.Draw(Pens.Solid(Color.DarkSlateGray, 1), new RectanglePolygon(pageLeft, pageTop, pageRight - pageLeft, pageBottom - pageTop));
                canvas.Fill(Brushes.Solid(Color.SteelBlue.WithAlpha(.16F)), new EllipsePolygon(circleX, circleY, circleRadius, circleRadius));
                canvas.Draw(Pens.Solid(Color.SteelBlue, 2), new EllipsePolygon(circleX, circleY, circleRadius, circleRadius));

                // A horizontal band can be split by the obstacle into at most
                // two usable slots. Keep these buffers outside the row loop so
                // the test does not stackalloc on every line.
                Span<float> slotLefts = stackalloc float[2];
                Span<float> slotRights = stackalloc float[2];

                while (hasMoreText && y < pageBottom)
                {
                    float bandTop = y;
                    float bandBottom = y + 30;
                    float blockedLeft = float.NaN;
                    float blockedRight = float.NaN;

                    // The circle is converted into a blocked horizontal interval for
                    // the current line band. The remaining intervals become the
                    // widths passed to the line enumerator, so the text engine never
                    // needs to understand circles, columns, or obstacle geometry.
                    if (bandTop < circleY + circleRadius && bandBottom > circleY - circleRadius)
                    {
                        float closestY = Math.Clamp(circleY, bandTop, bandBottom);
                        float dy = Math.Abs(closestY - circleY);
                        float dx = MathF.Sqrt((circleRadius * circleRadius) - (dy * dy));
                        blockedLeft = circleX - dx - circlePadding;
                        blockedRight = circleX + dx + circlePadding;
                    }

                    int slotCount = 0;
                    if (float.IsNaN(blockedLeft))
                    {
                        // Rows outside the circle receive one full-width slot.
                        slotLefts[slotCount] = pageLeft;
                        slotRights[slotCount++] = pageRight;
                    }
                    else
                    {
                        // Rows crossing the circle receive the left and right
                        // slots only when there is enough room for useful text.
                        if (blockedLeft - pageLeft >= minSlotWidth)
                        {
                            slotLefts[slotCount] = pageLeft;
                            slotRights[slotCount++] = blockedLeft;
                        }

                        if (pageRight - blockedRight >= minSlotWidth)
                        {
                            slotLefts[slotCount] = blockedRight;
                            slotRights[slotCount++] = pageRight;
                        }
                    }

                    float rowHeight = 30;
                    for (int i = 0; i < slotCount && hasMoreText; i++)
                    {
                        float slotLeft = slotLefts[i];
                        float slotRight = slotRights[i];
                        float slotWidth = slotRight - slotLeft;

                        // This is the API behavior under test: each MoveNext call
                        // supplies the width for exactly one produced line. The
                        // next call can use a completely different width.
                        hasMoreText = enumerator.MoveNext(slotWidth);
                        if (!hasMoreText)
                        {
                            break;
                        }

                        LineLayout line = enumerator.Current;
                        ReadOnlySpan<GraphemeMetrics> graphemes = line.GraphemeMetrics;
                        int stringStart = graphemes[0].StringIndex;
                        int stringEnd = stringStart;

                        // Bidi reordering means visual order is not guaranteed to
                        // match source order. The test draws the original source
                        // slice for this line, so it derives the slice from the
                        // grapheme source indices rather than array position alone.
                        for (int j = 0; j < graphemes.Length; j++)
                        {
                            stringStart = Math.Min(stringStart, graphemes[j].StringIndex);
                            stringEnd = Math.Max(stringEnd, graphemes[j].StringIndex + 1);
                        }

                        // The blue slot boxes are the caller-owned placement regions.
                        // Text is drawn from the line's source mapping at the slot origin,
                        // showing that one prepared block can feed arbitrary row widths
                        // without re-preparing the original paragraph.
                        canvas.Fill(Brushes.Solid(Color.SteelBlue.WithAlpha(.28F)), new RectanglePolygon(slotLeft, y, slotWidth, line.LineMetrics.LineHeight));
                        canvas.DrawText(
                            new RichTextOptions(font)
                            {
                                Origin = new(slotLeft, y),
                                WrappingLength = -1,
                                LineSpacing = options.LineSpacing
                            },
                            text.AsSpan()[stringStart..stringEnd],
                            Brushes.Solid(Color.Black),
                            pen: null);

                        rowHeight = Math.Max(rowHeight, line.LineMetrics.LineHeight);
                    }

                    y += rowHeight;
                }
            })));
    }

    [Fact]
    public void WordMetrics_GetSelectionBounds_DrawsWordSelections()
    {
        FontCollection fontCollection = new();
        FontFamily latin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily hebrew = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansHebrewRegular);
        FontFamily arabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);

        Font font = latin.CreateFont(30);
        Font largeFont = latin.CreateFont(46);
        const string text = "can't stop שלום عرب";

        TextOptions options = new(font)
        {
            FallbackFontFamilies = [hebrew, arabic],
            Origin = new(24, 28),
            LayoutMode = LayoutMode.HorizontalTopBottom,
            TextRuns =
            [
                new() { Start = 0, End = 5, Font = largeFont },
                new() { Start = 11, End = 15, Font = largeFont }
            ]
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        void DrawSelections(Image<Rgba32> image)
            => image.Mutate(x => x.Paint(canvas =>
            {
                foreach (WordMetrics word in metrics.WordMetrics)
                {
                    ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(word).Span;

                    // UAX #29 word segments include separators. Drawing every returned
                    // range with an outline makes the apostrophe in "can't" and the
                    // intervening spaces visible in the rendered output.
                    for (int i = 0; i < selection.Length; i++)
                    {
                        FontRectangle bounds = selection[i];
                        RectanglePolygon box = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

                        canvas.Fill(Brushes.Solid(Color.LightBlue), box);
                        canvas.Draw(Pens.Solid(Color.Black, 1), box);
                    }
                }
            }));

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawSelections);
    }

    [Theory]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 25, 6)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 50, 4)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 100, 3)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 200, 3)]
    public void CountLinesWrappingLength(string text, int wrappingLength, int usedLineMetrics)
    {
        Font font = CreateRenderingFont();
        TextOptions options = new(font)
        {
            WrappingLength = wrappingLength
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: usedLineMetrics);

        int count = TextMeasurer.CountLines(text, options);
        Assert.Equal(usedLineMetrics, count);
    }

    [Fact]
    public void BuildTextRuns_EmptyReturnsDefaultRun()
    {
        const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
        Font font = CreateFont(text);
        TextOptions options = new(font);

        IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

        Assert.Single(runs);
        Assert.Equal(font, runs[0].Font);
        Assert.Equal(0, runs[0].Start);
        Assert.Equal(CodePoint.GetCodePointCount(text.AsSpan()), runs[0].End);
    }

    [Fact]
    public void BuildTextRuns_ReturnsCreatesInterimRuns()
    {
        const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
        Font font = CreateFont(text);
        Font font2 = CreateFont(text, 16);
        TextOptions options = new(font)
        {
            TextRuns = new List<TextRun>()
            {
                new() { Start = 9, End = 23, Font = font2 },
                new() { Start = 35, End = 54, Font = font2 },
                new() { Start = 68, End = 70, Font = font2 },
            }
        };

        IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

        Assert.Equal(7, runs.Count);

        Assert.Equal(0, runs[0].Start);
        Assert.Equal(9, runs[0].End);
        Assert.Equal(font, runs[0].Font);
        Assert.Equal(9, runs[0].Slice(text.AsSpan()).Length);

        Assert.Equal(9, runs[1].Start);
        Assert.Equal(23, runs[1].End);
        Assert.Equal(font2, runs[1].Font);
        Assert.Equal(14, runs[1].Slice(text.AsSpan()).Length);

        Assert.Equal(23, runs[2].Start);
        Assert.Equal(35, runs[2].End);
        Assert.Equal(font, runs[2].Font);
        Assert.Equal(12, runs[2].Slice(text.AsSpan()).Length);

        Assert.Equal(35, runs[3].Start);
        Assert.Equal(54, runs[3].End);
        Assert.Equal(font2, runs[3].Font);
        Assert.Equal(19, runs[3].Slice(text.AsSpan()).Length);

        Assert.Equal(54, runs[4].Start);
        Assert.Equal(68, runs[4].End);
        Assert.Equal(font, runs[4].Font);
        Assert.Equal(14, runs[4].Slice(text.AsSpan()).Length);

        Assert.Equal(68, runs[5].Start);
        Assert.Equal(70, runs[5].End);
        Assert.Equal(font2, runs[5].Font);
        Assert.Equal(2, runs[5].Slice(text.AsSpan()).Length);

        Assert.Equal(70, runs[6].Start);
        Assert.Equal(76, runs[6].End);
        Assert.Equal(font, runs[6].Font);
        Assert.Equal(6, runs[6].Slice(text.AsSpan()).Length);
    }

    [Fact]
    public void BuildTextRuns_PreventsOverlappingRun()
    {
        const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
        Font font = CreateFont(text);
        TextOptions options = new(font)
        {
            TextRuns = new List<TextRun>()
            {
                new() { Start = 0, End = 23 },
                new() { Start = 1, End = 76 },
            }
        };

        IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

        Assert.Equal(2, runs.Count);
        Assert.Equal(font, runs[0].Font);
        Assert.Equal(0, runs[0].Start);
        Assert.Equal(1, runs[0].End);

        Assert.Equal(font, runs[1].Font);
        Assert.Equal(1, runs[1].Start);
        Assert.Equal(76, runs[1].End);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void TextJustification_InterCharacter_Horizontal(TextDirection direction)
    {
        const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = TextJustification.InterCharacter
        };

        TextMetrics justifiedMetrics = TextMeasurer.Measure(text, options);

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft });

        Assert.Equal(wrappingLength, justifiedMetrics.LineMetrics[0].Extent.X, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.GetGlyphMetrics().Length, justifiedMetrics.GetGlyphMetrics().Length);

        bool foundWidenedCharacter = false;
        for (int i = 0; i < justifiedMetrics.GetGlyphMetrics().Length; i++)
        {
            float justifiedWidth = justifiedMetrics.GetGlyphMetrics().Span[i].Advance.Width;
            float unJustifiedWidth = unJustifiedMetrics.GetGlyphMetrics().Span[i].Advance.Width;

            Assert.True(justifiedWidth >= unJustifiedWidth);
            foundWidenedCharacter |= justifiedWidth > unJustifiedWidth;
        }

        Assert.True(foundWidenedCharacter);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight, TextJustification.InterCharacter)]
    [InlineData(TextDirection.LeftToRight, TextJustification.InterWord)]
    [InlineData(TextDirection.RightToLeft, TextJustification.InterCharacter)]
    [InlineData(TextDirection.RightToLeft, TextJustification.InterWord)]
    public void TextJustification_MultiParagraph_Horizontal_SkipsFinalLines(TextDirection direction, TextJustification justification)
    {
        const string paragraph = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        string text = $"{paragraph}\n{paragraph}";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = justification
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft, mode = justification });

        ReadOnlySpan<LineMetrics> justifiedLineMetrics = TextMeasurer.GetLineMetrics(text, options).Span;

        options.TextJustification = TextJustification.None;
        ReadOnlySpan<LineMetrics> unJustifiedLineMetrics = TextMeasurer.GetLineMetrics(text, options).Span;

        Assert.Equal(unJustifiedLineMetrics.Length, justifiedLineMetrics.Length);

        bool foundUnchangedNonLastLine = false;
        bool foundJustifiedNonParagraphLine = false;
        for (int i = 0; i < justifiedLineMetrics.Length; i++)
        {
            bool isLastLine = i == justifiedLineMetrics.Length - 1;
            bool linesMatch = MathF.Abs(justifiedLineMetrics[i].Extent.X - unJustifiedLineMetrics[i].Extent.X) <= .01F;

            if (isLastLine)
            {
                // The trailing line in the text box must never be justified.
                Assert.Equal(unJustifiedLineMetrics[i].Extent.X, justifiedLineMetrics[i].Extent.X, 4F);
            }
            else
            {
                // At least one earlier line should stay unchanged, proving that a
                // paragraph-final line created by the explicit newline was not justified.
                foundUnchangedNonLastLine |= linesMatch;

                // At least one other earlier line should still widen, proving that we
                // did not disable justification for all wrapped lines.
                foundJustifiedNonParagraphLine |= justifiedLineMetrics[i].Extent.X > unJustifiedLineMetrics[i].Extent.X;
            }
        }

        // We expect both behaviors in the same layout: one unchanged paragraph-final
        // line before the end, and one earlier wrapped line that still stretches.
        Assert.True(foundUnchangedNonLastLine);
        Assert.True(foundJustifiedNonParagraphLine);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight, TextJustification.InterCharacter)]
    [InlineData(TextDirection.LeftToRight, TextJustification.InterWord)]
    [InlineData(TextDirection.RightToLeft, TextJustification.InterCharacter)]
    [InlineData(TextDirection.RightToLeft, TextJustification.InterWord)]
    public void TextJustification_MultiParagraph_Vertical_SkipsFinalLines(TextDirection direction, TextJustification justification)
    {
        const string paragraph = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        string text = $"{paragraph}\n{paragraph}";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            LayoutMode = LayoutMode.VerticalLeftRight,
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = justification
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft, mode = justification });

        ReadOnlySpan<LineMetrics> justifiedLineMetrics = TextMeasurer.GetLineMetrics(text, options).Span;

        options.TextJustification = TextJustification.None;
        ReadOnlySpan<LineMetrics> unJustifiedLineMetrics = TextMeasurer.GetLineMetrics(text, options).Span;

        Assert.Equal(unJustifiedLineMetrics.Length, justifiedLineMetrics.Length);

        bool foundUnchangedNonLastLine = false;
        bool foundJustifiedNonParagraphLine = false;
        for (int i = 0; i < justifiedLineMetrics.Length; i++)
        {
            bool isLastLine = i == justifiedLineMetrics.Length - 1;
            bool linesMatch = MathF.Abs(justifiedLineMetrics[i].Extent.Y - unJustifiedLineMetrics[i].Extent.Y) <= .01F;

            if (isLastLine)
            {
                // The trailing line in the text box must remain ragged in vertical layout too.
                Assert.Equal(unJustifiedLineMetrics[i].Extent.Y, justifiedLineMetrics[i].Extent.Y, 4F);
            }
            else
            {
                // This captures a non-last line that still behaves like a paragraph end.
                foundUnchangedNonLastLine |= linesMatch;

                // This captures a wrapped line that continues to justify normally.
                foundJustifiedNonParagraphLine |= justifiedLineMetrics[i].Extent.Y > unJustifiedLineMetrics[i].Extent.Y;
            }
        }

        // Both conditions are required for the test to be meaningful.
        Assert.True(foundUnchangedNonLastLine);
        Assert.True(foundJustifiedNonParagraphLine);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void TextJustification_InterWord_Horizontal(TextDirection direction)
    {
        const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = TextJustification.InterWord
        };

        TextMetrics justifiedMetrics = TextMeasurer.Measure(text, options);

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft });

        Assert.Equal(wrappingLength, justifiedMetrics.LineMetrics[0].Extent.X, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.GetGlyphMetrics().Length, justifiedMetrics.GetGlyphMetrics().Length);

        bool foundWidenedWhitespace = false;
        for (int i = 0; i < justifiedMetrics.GetGlyphMetrics().Length; i++)
        {
            float justifiedWidth = justifiedMetrics.GetGlyphMetrics().Span[i].Advance.Width;
            float unJustifiedWidth = unJustifiedMetrics.GetGlyphMetrics().Span[i].Advance.Width;

            if (CodePoint.IsWhiteSpace(unJustifiedMetrics.GetGlyphMetrics().Span[i].CodePoint))
            {
                Assert.True(justifiedWidth >= unJustifiedWidth);
                foundWidenedWhitespace |= justifiedWidth > unJustifiedWidth;
            }
            else
            {
                Assert.Equal(unJustifiedWidth, justifiedWidth);
            }
        }

        Assert.True(foundWidenedWhitespace);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void TextJustification_InterCharacter_Vertical(TextDirection direction)
    {
        const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            LayoutMode = LayoutMode.VerticalLeftRight,
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = TextJustification.InterCharacter
        };

        TextMetrics justifiedMetrics = TextMeasurer.Measure(text, options);

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft });

        Assert.Equal(wrappingLength, justifiedMetrics.LineMetrics[0].Extent.Y, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.GetGlyphMetrics().Length, justifiedMetrics.GetGlyphMetrics().Length);

        bool foundWidenedCharacter = false;
        for (int i = 0; i < justifiedMetrics.GetGlyphMetrics().Length; i++)
        {
            float justifiedHeight = justifiedMetrics.GetGlyphMetrics().Span[i].Advance.Height;
            float unJustifiedHeight = unJustifiedMetrics.GetGlyphMetrics().Span[i].Advance.Height;

            Assert.True(justifiedHeight >= unJustifiedHeight);
            foundWidenedCharacter |= justifiedHeight > unJustifiedHeight;
        }

        Assert.True(foundWidenedCharacter);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRight)]
    [InlineData(TextDirection.RightToLeft)]
    public void TextJustification_InterWord_Vertical(TextDirection direction)
    {
        const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
        const float wrappingLength = 400;
        const float pointSize = 12;
        Font font = CreateRenderingFont(pointSize);
        TextOptions options = new(font)
        {
            LayoutMode = LayoutMode.VerticalLeftRight,
            TextDirection = direction,
            WrappingLength = wrappingLength,
            TextJustification = TextJustification.InterWord
        };

        TextMetrics justifiedMetrics = TextMeasurer.Measure(text, options);

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { rtl = direction == TextDirection.RightToLeft });

        Assert.Equal(wrappingLength, justifiedMetrics.LineMetrics[0].Extent.Y, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.GetGlyphMetrics().Length, justifiedMetrics.GetGlyphMetrics().Length);

        bool foundWidenedWhitespace = false;
        for (int i = 0; i < justifiedMetrics.GetGlyphMetrics().Length; i++)
        {
            float justifiedHeight = justifiedMetrics.GetGlyphMetrics().Span[i].Advance.Height;
            float unJustifiedHeight = unJustifiedMetrics.GetGlyphMetrics().Span[i].Advance.Height;

            if (CodePoint.IsWhiteSpace(unJustifiedMetrics.GetGlyphMetrics().Span[i].CodePoint))
            {
                Assert.True(justifiedHeight >= unJustifiedHeight);
                foundWidenedWhitespace |= justifiedHeight > unJustifiedHeight;
            }
            else
            {
                Assert.Equal(unJustifiedHeight, justifiedHeight);
            }
        }

        Assert.True(foundWidenedWhitespace);
    }

    public static TheoryData<char, FontRectangle> OpenSans_Data { get; }
        = new()
        {
            { '!', new(0F, 0F, 1.1621094F, 7.2753906F) },
            { '"', new(0F, 0F, 2.6660156F, 2.578125F) },
            { '#', new(0F, 0F, 5.9472656F, 7.138672F) },
            { '$', new(0F, 0F, 4.4921875F, 8.168945F) },
            { '%', new(0F, 0F, 7.270508F, 7.338867F) },
            { '&', new(0F, 0F, 6.689453F, 7.348633F) },
            { '\'', new(0F, 0F, 0.87890625F, 2.578125F) },
            { '(', new(0F, 0F, 2.2460938F, 8.720703F) },
            { ')', new(0F, 0F, 2.2460938F, 8.720703F) },
            { '*', new(0F, 0F, 4.614258F, 4.4433594F) },
            { '+', new(0F, 0F, 4.692383F, 4.814453F) },
            { ',', new(0F, 0F, 1.4404297F, 2.4511719F) },
            { '-', new(0F, 0F, 2.421875F, 0.72265625F) },
            { '.', new(0F, 0F, 1.1621094F, 1.2744141F) },
            { '/', new(0F, 0F, 3.4570312F, 7.138672F) },
            { '0', new(0F, 0F, 4.7070312F, 7.348633F) },
            { '1', new(0F, 0F, 2.6074219F, 7.138672F) },
            { '2', new(0F, 0F, 4.6777344F, 7.241211F) },
            { '3', new(0F, 0F, 4.6777344F, 7.338867F) },
            { '4', new(0F, 0F, 5.3125F, 7.1777344F) },
            { '5', new(0F, 0F, 4.4970703F, 7.236328F) },
            { '6', new(0F, 0F, 4.6679688F, 7.338867F) },
            { '7', new(0F, 0F, 4.760742F, 7.138672F) },
            { '8', new(0F, 0F, 4.6972656F, 7.338867F) },
            { '9', new(0F, 0F, 4.6777344F, 7.34375F) },
            { ':', new(0F, 0F, 1.1621094F, 5.6152344F) },
            { ';', new(0F, 0F, 1.5576172F, 6.767578F) },
            { '<', new(0F, 0F, 4.6972656F, 4.868164F) },
            { '=', new(0F, 0F, 4.580078F, 2.65625F) },
            { '>', new(0F, 0F, 4.6972656F, 4.868164F) },
            { '?', new(0F, 0F, 3.8916016F, 7.3779297F) },
            { '@', new(0F, 0F, 7.817383F, 8.032227F) },
            { 'A', new(0F, 0F, 6.3134766F, 7.1679688F) },
            { 'B', new(0F, 0F, 4.9414062F, 7.138672F) },
            { 'C', new(0F, 0F, 5.3808594F, 7.338867F) },
            { 'D', new(0F, 0F, 5.6689453F, 7.138672F) },
            { 'E', new(0F, 0F, 3.9746094F, 7.138672F) },
            { 'F', new(0F, 0F, 3.9746094F, 7.138672F) },
            { 'G', new(0F, 0F, 5.913086F, 7.338867F) },
            { 'H', new(0F, 0F, 5.4101562F, 7.138672F) },
            { 'I', new(0F, 0F, 0.8300781F, 7.138672F) },
            { 'J', new(0F, 0F, 2.5683594F, 9.018555F) },
            { 'K', new(0F, 0F, 5.1464844F, 7.138672F) },
            { 'L', new(0F, 0F, 3.9990234F, 7.138672F) },
            { 'M', new(0F, 0F, 7.0410156F, 7.138672F) },
            { 'N', new(0F, 0F, 5.5810547F, 7.138672F) },
            { 'O', new(0F, 0F, 6.557617F, 7.348633F) },
            { 'P', new(0F, 0F, 4.5214844F, 7.138672F) },
            { 'Q', new(0F, 0F, 6.557617F, 8.950195F) },
            { 'R', new(0F, 0F, 5.029297F, 7.138672F) },
            { 'S', new(0F, 0F, 4.4921875F, 7.338867F) },
            { 'T', new(0F, 0F, 5.317383F, 7.138672F) },
            { 'U', new(0F, 0F, 5.473633F, 7.236328F) },
            { 'V', new(0F, 0F, 5.961914F, 7.138672F) },
            { 'W', new(0F, 0F, 8.94043F, 7.138672F) },
            { 'X', new(0F, 0F, 5.7128906F, 7.138672F) },
            { 'Y', new(0F, 0F, 5.5908203F, 7.138672F) },
            { 'Z', new(0F, 0F, 4.9560547F, 7.138672F) },
            { '[', new(0F, 0F, 2.211914F, 8.720703F) },
            { '\\', new(0F, 0F, 3.4667969F, 7.138672F) },
            { ']', new(0F, 0F, 2.2167969F, 8.720703F) },
            { '^', new(0F, 0F, 4.9414062F, 4.5117188F) },
            { '_', new(0F, 0F, 4.4189453F, 0.60058594F) },
            { '`', new(0F, 0F, 1.9775391F, 1.6015625F) },
            { 'a', new(0F, 0F, 4.2822266F, 5.5371094F) },
            { 'b', new(0F, 0F, 4.7070312F, 7.6953125F) },
            { 'c', new(0F, 0F, 3.90625F, 5.546875F) },
            { 'd', new(0F, 0F, 4.7021484F, 7.6953125F) },
            { 'e', new(0F, 0F, 4.536133F, 5.546875F) },
            { 'f', new(0F, 0F, 3.671875F, 7.651367F) },
            { 'g', new(0F, 0F, 5.078125F, 7.861328F) },
            { 'h', new(0F, 0F, 4.4628906F, 7.5976562F) },
            { 'i', new(0F, 0F, 0.9765625F, 7.3535156F) },
            { 'j', new(0F, 0F, 2.3046875F, 9.755859F) },
            { 'k', new(0F, 0F, 4.321289F, 7.5976562F) },
            { 'l', new(0F, 0F, 0.8154297F, 7.5976562F) },
            { 'm', new(0F, 0F, 7.5927734F, 5.4492188F) },
            { 'n', new(0F, 0F, 4.4628906F, 5.4492188F) },
            { 'o', new(0F, 0F, 4.9121094F, 5.546875F) },
            { 'p', new(0F, 0F, 4.7070312F, 7.841797F) },
            { 'q', new(0F, 0F, 4.7021484F, 7.841797F) },
            { 'r', new(0F, 0F, 3.0810547F, 5.4492188F) },
            { 's', new(0F, 0F, 3.8134766F, 5.546875F) },
            { 't', new(0F, 0F, 3.178711F, 6.689453F) },
            { 'u', new(0F, 0F, 4.477539F, 5.4492188F) },
            { 'v', new(0F, 0F, 4.995117F, 5.3515625F) },
            { 'w', new(0F, 0F, 7.5146484F, 5.3515625F) },
            { 'x', new(0F, 0F, 4.8535156F, 5.3515625F) },
            { 'y', new(0F, 0F, 5F, 7.758789F) },
            { 'z', new(0F, 0F, 3.9013672F, 5.3515625F) },
            { '{', new(0F, 0F, 3.149414F, 8.720703F) },
            { '|', new(0F, 0F, 0.67871094F, 10.024414F) },
            { '}', new(0F, 0F, 3.149414F, 8.720703F) },
            { '~', new(0F, 0F, 4.6972656F, 1.2597656F) },
        };

    [Theory]
    [MemberData(nameof(OpenSans_Data))]
    public void TrueTypeHinting_CanHintSmallOpenSans(char c, FontRectangle expected)
    {
        TextOptions options = new(OpenSansTTF)
        {
            KerningMode = KerningMode.Standard,
            HintingMode = HintingMode.Standard
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(c.ToString(), options);
        Assert.Equal(expected.Width, actual.Width, Comparer);
        Assert.Equal(expected.Height, actual.Height, Comparer);

        options = new(OpenSansWoff)
        {
            KerningMode = KerningMode.Standard,
            HintingMode = HintingMode.Standard
        };

        actual = TextMeasurer.MeasureBounds(c.ToString(), options);
        Assert.Equal(expected.Width, actual.Width, Comparer);
        Assert.Equal(expected.Height, actual.Height, Comparer);
    }

    public static TheoryData<string, float, float, float[]> FontTrackingHorizontalData { get; }
        = new()
        {
            { "aaaa", 0.0f, 134.0f, [2.9f, 38.5f, 74.0f, 109.6f] },
            { "aaaa", 0.1f, 153.3f, [2.9f, 44.9f, 86.8f, 128.8f] },
            { "aaaa", 1.0f, 326.1f, [2.9f, 102.5f, 202.0f, 301.6f] },
            { "awwa", 0.0f, 162.1f, [2.9f, 36.3f, 85.9f, 137.6f] },
            { "awwa", 0.1f, 181.4f, [2.9f, 42.7f, 98.7f, 156.8f] },
            { "awwa", 1.0f, 354.1f, [2.9f, 100.3f, 213.9f, 329.6f] },
        };

    [Theory]
    [MemberData(nameof(FontTrackingHorizontalData))]
    public void FontTracking_SpaceCharacters_WithHorizontalLayout(string text, float tracking, float width, float[] characterPosition)
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        TextOptions options = new(font)
        {
            Tracking = tracking,
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(text, options);
        Assert.Equal(width, actual.Width, Comparer);

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;
        Assert.Equal(characterPosition, glyphs.ToArray().Select(x => x.Bounds.X), Comparer);
    }

    public static TheoryData<string, float, float, float[]> FontTrackingVerticalData { get; }
        = new()
        {
            { "aaaa", 0.0f, 296.9f, [33.5f, 120.7f, 207.9f, 295.0f] },
            { "aaaa", 0.1f, 316.1f, [33.5f, 127.1f, 220.7f, 314.2f] },
            { "aaaa", 1.0f, 488.9f, [33.5f, 184.7f, 335.9f, 487.0f] },
            { "awwa", 0.0f, 296.9f, [33.5f, 121.2f, 208.4f, 295.0f] },
            { "awwa", 0.1f, 316.1f, [33.5f, 127.6f, 221.2f, 314.2f] },
            { "awwa", 1.0f, 488.9f, [33.5f, 185.2f, 336.4f, 487.0f] },
        };

    [Theory]
    [MemberData(nameof(FontTrackingVerticalData))]
    public void FontTracking_SpaceCharacters_WithVerticalLayout(string text, float tracking, float width, float[] characterPosition)
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        TextOptions options = new(font)
        {
            Tracking = tracking,
            LayoutMode = LayoutMode.VerticalLeftRight,
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(text, options);
        Assert.Equal(width, actual.Height, Comparer);

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;
        Assert.Equal(characterPosition, glyphs.ToArray().Select(x => x.Bounds.Y), Comparer);
    }

    [Theory]
    [InlineData("\u1B3C", 1, 83.8)]
    [InlineData("\u1B3C\u1B3C", 1, 83.8)]
    public void FontTracking_DoNotAddSpacingAfterCharacterThatDidNotAdvance(string text, float tracking, float width)
    {
        Font font = TestFonts.GetFont(TestFonts.NotoSansBalineseRegular, 64);
        TextOptions options = new(font)
        {
            Tracking = tracking,
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(text, options);
        Assert.Equal(width, actual.Width, Comparer);
    }

    [Theory]
    [InlineData("\u093f", 1, 48.4)]
    [InlineData("\u0930\u094D\u0915\u093F", 1, 97.65625)]
    [InlineData("\u0930\u094D\u0915\u093F\u0930\u094D\u0915\u093F", 1, 227)]
    [InlineData("\u093fa", 1, 145.5f)]
    public void FontTracking_CorrectlyAddSpacingForComposedCharacter(string text, float tracking, float width)
    {
        Font font = TestFonts.GetFont(TestFonts.NotoSansDevanagariRegular, 64);
        TextOptions options = new(font)
        {
            Tracking = tracking,
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(text, options);
        Assert.Equal(width, actual.Width, Comparer);
    }

    [Theory]
    [InlineData("\u093f", 1)]
    [InlineData("\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u0930\u094D\u0915\u093F\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u093fa", 1)]
    public void FontTracking_CorrectlyAddSpacingForComposedCharacterHRef(string text, float tracking)
    {
        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansDevanagariRegular, 30);

        TextOptions options = new(mainFont)
        {
            Tracking = tracking,
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: text);
    }

    [Theory]
    [InlineData("\u093f", 1)]
    [InlineData("\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u0930\u094D\u0915\u093F\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u093fa", 1)]
    public void FontTracking_CorrectlyAddSpacingForComposedCharacterVRef(string text, float tracking)
    {
        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansDevanagariRegular, 30);

        TextOptions options = new(mainFont)
        {
            Tracking = tracking,
            LayoutMode = LayoutMode.VerticalLeftRight,
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: text);
    }

    [Theory]
    [InlineData("\u093f", 1)]
    [InlineData("\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u0930\u094D\u0915\u093F\u0930\u094D\u0915\u093F", 1)]
    [InlineData("\u093fa", 1)]
    public void FontTracking_CorrectlyAddSpacingForComposedCharacterVMRef(string text, float tracking)
    {
        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansDevanagariRegular, 30);

        TextOptions options = new(mainFont)
        {
            Tracking = tracking,
            LayoutMode = LayoutMode.VerticalMixedLeftRight,
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: text);
    }

    [Fact]
    public void CanMeasureTextAdvance()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1F
        };

        const string text = "Hello World!";
        FontRectangle first = TextMeasurer.MeasureAdvance(text, options);

        Assert.Equal(new FontRectangle(0, 0, 11729, 2048), first, Comparer);

        options.LineSpacing = 2F;
        FontRectangle second = TextMeasurer.MeasureAdvance(text, options);
        Assert.Equal(new FontRectangle(0, 0, 11729, 4096), second);
    }

    [Fact]
    public void CanMeasureGlyphLayouts()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "Hello World!";

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;

        for (int i = 0; i < glyphs.Length; i++)
        {
            Assert.Equal(FontRectangle.Union(glyphs[i].Advance, glyphs[i].Bounds), glyphs[i].RenderableBounds, Comparer);
        }
    }

    [Fact]
    public void CanMeasureMultilineGlyphLayouts()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "A\nA\nA\nA";

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;

        GlyphMetrics? lastAdvance = null;
        int lineBreakCount = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            GlyphMetrics advance = glyphs[i];

            if (CodePoint.IsNewLine(advance.CodePoint))
            {
                Assert.True(advance.Advance.Width > 0 || advance.Advance.Height > 0);
                lineBreakCount++;
                continue;
            }

            if (lastAdvance.HasValue)
            {
                Assert.Equal(lastAdvance.Value.Advance.Width, advance.Advance.Width, Comparer);
                Assert.Equal(lastAdvance.Value.Advance.Height, advance.Advance.Height, Comparer);
            }

            lastAdvance = advance;
        }

        // Hard breaks that terminate non-empty lines are trimmed from the visual
        // glyph stream. The four visible glyphs are still laid out on four lines.
        Assert.Equal(0, lineBreakCount);
        Assert.Equal(4, glyphs.Length);
    }

    [Fact]
    public void DoesMeasureGlyphLayoutIncludeStringIndex()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "The quick👩🏽‍🚒 brown fox jumps over \r\n the lazy dog";

        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;

        int stringIndex = -1;

        for (int i = 0; i < glyphs.Length; i++)
        {
            GlyphMetrics bound = glyphs[i];

            if (bound.CodePoint == new CodePoint("k"[0]))
            {
                stringIndex = text.IndexOf('k');
                Assert.Equal(stringIndex, bound.StringIndex);
                Assert.Equal(stringIndex, bound.GraphemeIndex);
            }

            // after emoji
            if (bound.CodePoint == new CodePoint("b"[0]))
            {
                stringIndex = text.IndexOf('b');
                Assert.NotEqual(bound.StringIndex, bound.GraphemeIndex);
                Assert.Equal(stringIndex, bound.StringIndex);
                Assert.Equal(11, bound.GraphemeIndex);
            }
        }

        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        int graphemeCount = 0;
        while (graphemeEnumerator.MoveNext())
        {
            graphemeCount += 1;
        }

        GlyphMetrics firstBound = glyphs[0];
        Assert.Equal(0, firstBound.StringIndex);
        Assert.Equal(0, firstBound.GraphemeIndex);

        GlyphMetrics lastBound = glyphs[^1];
        Assert.Equal(text.Length - 1, lastBound.StringIndex);
        Assert.Equal(graphemeCount - 1, lastBound.GraphemeIndex);
    }

    [Fact]
    public void DoesGetGlyphMetricsExtendForAdvanceMultipliers()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            TabWidth = 8
        };

        const string text = "H\tH";

        List<FontRectangle> glyphs = CaptureGlyphRectangleBuilder.GenerateGlyphRectangles(text, options);
        ReadOnlySpan<GlyphMetrics> glyphMetrics = TextMeasurer.GetGlyphMetrics(text, options).Span;

        Assert.Equal(glyphs.Count, glyphMetrics.Length);

        for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
        {
            FontRectangle renderGlyph = glyphs[glyphIndex];
            FontRectangle measureGlyph = glyphMetrics[glyphIndex].Bounds;
            GlyphMetrics metric = glyphMetrics[glyphIndex];

            if (CodePoint.IsWhiteSpace(metric.CodePoint))
            {
                Assert.Equal(renderGlyph.X, measureGlyph.X);
                Assert.Equal(renderGlyph.Y, measureGlyph.Y);
                Assert.Equal(metric.Advance.Width, measureGlyph.Width);
                Assert.Equal(renderGlyph.Height, measureGlyph.Height);
            }
            else
            {
                Assert.Equal(renderGlyph, measureGlyph);
            }
        }
    }

    [Fact]
    public void IsGetGlyphMetricsSameAsRenderBounds()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
        };

        const string text = "Hello WorLLd";

        List<FontRectangle> glyphs = CaptureGlyphRectangleBuilder.GenerateGlyphRectangles(text, options);
        ReadOnlySpan<GlyphMetrics> glyphMetrics = TextMeasurer.GetGlyphMetrics(text, options).Span;

        Assert.Equal(glyphs.Count, glyphMetrics.Length);

        for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
        {
            FontRectangle renderGlyph = glyphs[glyphIndex];
            FontRectangle measureGlyph = glyphMetrics[glyphIndex].Bounds;

            Assert.Equal(renderGlyph.X, measureGlyph.X);
            Assert.Equal(renderGlyph.Y, measureGlyph.Y);
            Assert.Equal(renderGlyph.Width, measureGlyph.Width);
            Assert.Equal(renderGlyph.Height, measureGlyph.Height);

            Assert.Equal(renderGlyph, measureGlyph);
        }
    }

    [Fact]
    public void BreakWordEnsuresSingleCharacterPerLine()
    {
        Font font = CreateRenderingFont(20);
        TextOptions options = new(font)
        {
            WordBreaking = WordBreaking.BreakWord,
            WrappingLength = 1
        };

        const string text = "Hello World!";

        TextLayoutTestUtilities.TestLayout(text, options);

        int lineCount = TextMeasurer.CountLines(text, options);
        Assert.Equal(text.Length - 1, lineCount);
    }

    private class CaptureGlyphRectangleBuilder : IGlyphRenderer
    {
        public static List<FontRectangle> GenerateGlyphRectangles(string text, TextOptions options)
        {
            CaptureGlyphRectangleBuilder glyphBuilder = new();
            TextRenderer renderer = new(glyphBuilder);
            renderer.RenderText(text, options);
            return glyphBuilder.GlyphRectangles;
        }

        public readonly List<FontRectangle> GlyphRectangles = [];

        public CaptureGlyphRectangleBuilder()
        {
        }

        bool IGlyphRenderer.BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.GlyphRectangles.Add(bounds);
            return true;
        }

        public void BeginFigure()
        {
        }

        public void MoveTo(Vector2 point)
        {
        }

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        {
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
        }

        public void LineTo(Vector2 point)
        {
        }

        public void EndFigure()
        {
        }

        public void EndGlyph()
        {
        }

        public void EndText()
        {
        }

        void IGlyphRenderer.BeginText(in FontRectangle bounds)
        {
        }

        public TextDecorations EnabledDecorations() => TextDecorations.None;

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        {
        }

        public void BeginLayer(Paint paint, FillRule fillRule, ClipQuad? clipBounds)
        {
        }

        public void EndLayer()
        {
        }
    }

    private static readonly Font OpenSansTTF = TestFonts.GetFont(TestFonts.OpenSansFile, 10);
    private static readonly Font OpenSansWoff = TestFonts.GetFont(TestFonts.OpenSansFile, 10);

#if OS_WINDOWS
    public static TheoryData<char, FontRectangle> SegoeUi_Data { get; }
        = new()
        {
            { '!', new(0F, 0F, 1.0839844F, 7.0898438F) },
            { '"', new(0F, 0F, 2.3291016F, 2.1826172F) },
            { '#', new(0F, 0F, 5.5322266F, 6.401367F) },
            { '$', new(0F, 0F, 3.9794922F, 8.911133F) },
            { '%', new(0F, 0F, 7.421875F, 7.216797F) },
            { '&', new(0F, 0F, 7.2216797F, 7.2314453F) },
            { '\'', new(0F, 0F, 0.7080078F, 2.1826172F) },
            { '(', new(0F, 0F, 2.2363281F, 8.59375F) },
            { ')', new(0F, 0F, 2.2460938F, 8.59375F) },
            { '*', new(0F, 0F, 3.4375F, 3.4423828F) },
            { '+', new(0F, 0F, 4.5898438F, 4.5898438F) },
            { ',', new(0F, 0F, 1.3525391F, 2.4023438F) },
            { '-', new(0F, 0F, 2.6660156F, 0.6298828F) },
            { '.', new(0F, 0F, 1.09375F, 1.0986328F) },
            { '/', new(0F, 0F, 4.1064453F, 8.1640625F) },
            { '0', new(0F, 0F, 4.560547F, 7.241211F) },
            { '1', new(0F, 0F, 2.421875F, 7.158203F) },
            { '2', new(0F, 0F, 4.321289F, 7.1191406F) },
            { '3', new(0F, 0F, 4.0527344F, 7.241211F) },
            { '4', new(0F, 0F, 4.9804688F, 7.001953F) },
            { '5', new(0F, 0F, 3.930664F, 7.1240234F) },
            { '6', new(0F, 0F, 4.448242F, 7.241211F) },
            { '7', new(0F, 0F, 4.453125F, 7.001953F) },
            { '8', new(0F, 0F, 4.5410156F, 7.2314453F) },
            { '9', new(0F, 0F, 4.4433594F, 7.241211F) },
            { ':', new(0F, 0F, 1.09375F, 5.2148438F) },
            { ';', new(0F, 0F, 1.4599609F, 6.3964844F) },
            { '<', new(0F, 0F, 4.1992188F, 4.7509766F) },
            { '=', new(0F, 0F, 4.5898438F, 2.7246094F) },
            { '>', new(0F, 0F, 4.1992188F, 4.7509766F) },
            { '?', new(0F, 0F, 3.3496094F, 7.2070312F) },
            { '@', new(0F, 0F, 7.890625F, 8.017578F) },
            { 'A', new(0F, 0F, 6.2304688F, 7.001953F) },
            { 'B', new(0F, 0F, 4.3115234F, 7.001953F) },
            { 'C', new(0F, 0F, 5.2246094F, 7.236328F) },
            { 'D', new(0F, 0F, 5.6347656F, 7.001953F) },
            { 'E', new(0F, 0F, 3.7109375F, 7.001953F) },
            { 'F', new(0F, 0F, 3.5546875F, 7.001953F) },
            { 'G', new(0F, 0F, 5.6933594F, 7.236328F) },
            { 'H', new(0F, 0F, 5.263672F, 7.001953F) },
            { 'I', new(0F, 0F, 0.8203125F, 7.001953F) },
            { 'J', new(0F, 0F, 2.6123047F, 7.1191406F) },
            { 'K', new(0F, 0F, 4.873047F, 7.001953F) },
            { 'L', new(0F, 0F, 3.6328125F, 7.001953F) },
            { 'M', new(0F, 0F, 7.138672F, 7.001953F) },
            { 'N', new(0F, 0F, 5.6445312F, 7.001953F) },
            { 'O', new(0F, 0F, 6.6210938F, 7.236328F) },
            { 'P', new(0F, 0F, 4.2822266F, 7.001953F) },
            { 'Q', new(0F, 0F, 7.2216797F, 8.061523F) },
            { 'R', new(0F, 0F, 5.0195312F, 7.001953F) },
            { 'S', new(0F, 0F, 4.243164F, 7.236328F) },
            { 'T', new(0F, 0F, 4.8583984F, 7.001953F) },
            { 'U', new(0F, 0F, 5.209961F, 7.1191406F) },
            { 'V', new(0F, 0F, 6.0351562F, 7.001953F) },
            { 'W', new(0F, 0F, 9.091797F, 7.001953F) },
            { 'X', new(0F, 0F, 5.625F, 7.001953F) },
            { 'Y', new(0F, 0F, 5.3808594F, 7.001953F) },
            { 'Z', new(0F, 0F, 5.3271484F, 7.001953F) },
            { '[', new(0F, 0F, 1.796875F, 8.59375F) },
            { '\\', new(0F, 0F, 4.0234375F, 8.173828F) },
            { ']', new(0F, 0F, 1.7919922F, 8.59375F) },
            { '^', new(0F, 0F, 4.609375F, 4.0722656F) },
            { '_', new(0F, 0F, 4.1503906F, 0.5810547F) },
            { '`', new(0F, 0F, 1.8994141F, 1.6015625F) },
            { 'a', new(0F, 0F, 3.9501953F, 5.234375F) },
            { 'b', new(0F, 0F, 4.5996094F, 7.5195312F) },
            { 'c', new(0F, 0F, 3.7597656F, 5.234375F) },
            { 'd', new(0F, 0F, 4.609375F, 7.5195312F) },
            { 'e', new(0F, 0F, 4.3603516F, 5.234375F) },
            { 'f', new(0F, 0F, 3.022461F, 7.5097656F) },
            { 'g', new(0F, 0F, 4.609375F, 7.470703F) },
            { 'h', new(0F, 0F, 4.1503906F, 7.4023438F) },
            { 'i', new(0F, 0F, 1.0449219F, 7.3095703F) },
            { 'j', new(0F, 0F, 2.7148438F, 9.663086F) },
            { 'k', new(0F, 0F, 4.1503906F, 7.4023438F) },
            { 'l', new(0F, 0F, 0.80078125F, 7.4023438F) },
            { 'm', new(0F, 0F, 7.0996094F, 5.1171875F) },
            { 'n', new(0F, 0F, 4.1503906F, 5.1171875F) },
            { 'o', new(0F, 0F, 4.921875F, 5.234375F) },
            { 'p', new(0F, 0F, 4.5996094F, 7.416992F) },
            { 'q', new(0F, 0F, 4.609375F, 7.416992F) },
            { 'r', new(0F, 0F, 2.6074219F, 5.0878906F) },
            { 's', new(0F, 0F, 3.3154297F, 5.234375F) },
            { 't', new(0F, 0F, 2.9199219F, 6.586914F) },
            { 'u', new(0F, 0F, 4.1503906F, 5.1171875F) },
            { 'v', new(0F, 0F, 4.6728516F, 5F) },
            { 'w', new(0F, 0F, 6.9921875F, 5F) },
            { 'x', new(0F, 0F, 4.3359375F, 5F) },
            { 'y', new(0F, 0F, 4.7216797F, 7.3535156F) },
            { 'z', new(0F, 0F, 4.135742F, 5F) },
            { '{', new(0F, 0F, 2.2607422F, 8.59375F) },
            { '|', new(0F, 0F, 0.72265625F, 10F) },
            { '}', new(0F, 0F, 2.2558594F, 8.59375F) },
            { '~', new(0F, 0F, 4.8095703F, 1.5136719F) }
        };

    [Theory]
    [MemberData(nameof(SegoeUi_Data))]
    public void TrueTypeHinting_CanHintSmallSegoeUi(char c, FontRectangle expected)
    {
        TextOptions options = new(SegoeUi)
        {
            KerningMode = KerningMode.Standard,
            HintingMode = HintingMode.Standard
        };

        FontRectangle actual = TextMeasurer.MeasureBounds(c.ToString(), options);

        Assert.Equal(expected.Width, actual.Width, Comparer);
        Assert.Equal(expected.Height, actual.Height, Comparer);
    }

    private static readonly Font SegoeUi = SystemFonts.CreateFont("Segoe Ui", 10);
#endif

    private static void DrawCaret(
        DrawingCanvas canvas,
        CaretPosition caret,
        Color color,
        float thickness,
        bool dashed)
    {
        DrawCaretLine(canvas, caret.Start, caret.End, color, thickness, dashed);

        if (caret.HasSecondary)
        {
            DrawCaretLine(canvas, caret.SecondaryStart, caret.SecondaryEnd, color, thickness, dashed);
        }
    }

    private static void DrawCaretLine(
        DrawingCanvas canvas,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness,
        bool dashed)
    {
        if (!dashed)
        {
            canvas.DrawLine(Pens.Solid(color, thickness), new PointF(start.X, start.Y), new PointF(end.X, end.Y));
            return;
        }

        Vector2 delta = end - start;
        Vector2 step = Vector2.Normalize(delta);
        float length = delta.Length();

        // Build movement carets from short line segments instead of using a pen
        // option so the dashed output is independent of ImageSharp.Drawing internals.
        const float dash = 5F;
        const float gap = 4F;

        for (float distance = 0; distance < length; distance += dash + gap)
        {
            Vector2 dashStart = start + (step * distance);
            Vector2 dashEnd = start + (step * MathF.Min(distance + dash, length));

            canvas.DrawLine(Pens.Solid(color, thickness), new PointF(dashStart.X, dashStart.Y), new PointF(dashEnd.X, dashEnd.Y));
        }
    }

    private static string GetSourceTextForLine(string text, TextMetrics metrics, int lineIndex)
    {
        int glyphIndex = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            glyphIndex = AdvanceGlyphIndex(metrics.GetGlyphMetrics().Span, glyphIndex, metrics.LineMetrics[i].GraphemeCount);
        }

        int startGlyphIndex = glyphIndex;
        int endGlyphIndex = AdvanceGlyphIndex(metrics.GetGlyphMetrics().Span, glyphIndex, metrics.LineMetrics[lineIndex].GraphemeCount);

        int start = int.MaxValue;
        int end = 0;
        for (int i = startGlyphIndex; i < endGlyphIndex; i++)
        {
            GlyphMetrics glyph = metrics.GetGlyphMetrics().Span[i];
            start = Math.Min(start, glyph.StringIndex);
            end = Math.Max(end, glyph.StringIndex + glyph.CodePoint.Utf16SequenceLength);
        }

        return text[start..end];
    }

    private static int AdvanceGlyphIndex(ReadOnlySpan<GlyphMetrics> glyphs, int glyphIndex, int graphemeCount)
    {
        int consumed = 0;
        int lastGraphemeIndex = -1;
        while (glyphIndex < glyphs.Length && consumed < graphemeCount)
        {
            int graphemeIndex = glyphs[glyphIndex].GraphemeIndex;
            if (graphemeIndex != lastGraphemeIndex)
            {
                consumed++;
                lastGraphemeIndex = graphemeIndex;
            }

            glyphIndex++;
        }

        while (glyphIndex < glyphs.Length && glyphs[glyphIndex].GraphemeIndex == lastGraphemeIndex)
        {
            glyphIndex++;
        }

        return glyphIndex;
    }

    private static int CountGlyphs(ReadOnlySpan<GlyphMetrics> glyphs, CodePoint codePoint)
    {
        int count = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            if (glyphs[i].CodePoint == codePoint)
            {
                count++;
            }
        }

        return count;
    }

#if OS_WINDOWS
    [Fact]
    public void BenchmarkTest()
    {
        Font font = Arial;
        _ = TextMeasurer.MeasureBounds("The quick brown fox jumped over the lazy dog", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
    }

    private static readonly Font Arial = SystemFonts.CreateFont("Arial", 12);
#endif

    public static Font CreateRenderingFont(float pointSize = 12)
        => TestFonts.GetFont(TestFonts.OpenSansFile, pointSize);

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }

    public static Font CreateFont(string text, float pointSize)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, pointSize);
    }
}

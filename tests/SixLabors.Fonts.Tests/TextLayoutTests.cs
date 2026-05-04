// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;

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
    public void TryMeasureCharacterBounds()
    {
        const string text = "a b\nc";
        GlyphBounds[] expectedGlyphMetrics =
        [
            new(new CodePoint('a'), new FontRectangle(10, 0, 10, 10), 0, 0),
            new(new CodePoint(' '), new FontRectangle(40, 0, 30, 10), 1, 1),
            new(new CodePoint('b'), new FontRectangle(70, 0, 10, 10), 2, 2),
            new(new CodePoint('c'), new FontRectangle(10, 30, 10, 10), 3, 3),
        ];
        Font font = CreateFont(text);

        Assert.True(TextMeasurer.TryMeasureCharacterBounds(
            text.AsSpan(),
            new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor },
            out ReadOnlySpan<GlyphBounds> bounds));

        // Newline should not be returned.
        Assert.Equal(text.Length - 1, bounds.Length);
        for (int i = 0; i < bounds.Length; i++)
        {
            GlyphBounds expected = expectedGlyphMetrics[i];
            GlyphBounds actual = bounds[i];
            Assert.Equal(expected.Codepoint, actual.Codepoint);

            // 4 dp as there is minor offset difference in the float values
            Assert.Equal(expected.Bounds.X, actual.Bounds.X, 4F);
            Assert.Equal(expected.Bounds.Y, actual.Bounds.Y, 4F);
            Assert.Equal(expected.Bounds.Height, actual.Bounds.Height, 4F);
            Assert.Equal(expected.Bounds.Width, actual.Bounds.Width, 4F);
        }
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
        FontRectangle measurement = TextMeasurer.MeasureSize("/ This will fail", textOptions);

        Assert.NotEqual(FontRectangle.Empty, measurement);
    }

    [Theory]
    [InlineData("hello world", 1)]
    [InlineData("hello world\nhello world", 2)]
    [InlineData("hello world\nhello world\nhello world", 3)]
    public void CountLines(string text, int usedLines)
    {
        Font font = CreateFont(text);
        int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(usedLines, count);
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
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 25, 6)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 50, 4)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 100, 3)]
    [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 200, 3)]
    public void CountLinesWrappingLength(string text, int wrappingLength, int usedLines)
    {
        Font font = CreateRenderingFont();
        TextOptions options = new(font)
        {
            WrappingLength = wrappingLength
        };

        TextLayoutTestUtilities.TestLayout(text, options, properties: usedLines);

        int count = TextMeasurer.CountLines(text, options);
        Assert.Equal(usedLines, count);
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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, options.TextJustification });

        Assert.Equal(wrappingLength, justifiedMetrics.Lines[0].Extent, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.CharacterAdvances.Count, justifiedMetrics.CharacterAdvances.Count);

        bool foundWidenedCharacter = false;
        for (int i = 0; i < justifiedMetrics.CharacterAdvances.Count; i++)
        {
            float justifiedWidth = justifiedMetrics.CharacterAdvances[i].Bounds.Width;
            float unJustifiedWidth = unJustifiedMetrics.CharacterAdvances[i].Bounds.Width;

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
    public void TextJustification_MultiParagraph_Horizontal_DoesNotJustifyParagraphFinalLines(TextDirection direction, TextJustification justification)
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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, justification });

        LineMetrics[] justifiedLines = TextMeasurer.GetLineMetrics(text, options);

        options.TextJustification = TextJustification.None;
        LineMetrics[] unJustifiedLines = TextMeasurer.GetLineMetrics(text, options);

        Assert.Equal(unJustifiedLines.Length, justifiedLines.Length);

        bool foundUnchangedNonLastLine = false;
        bool foundJustifiedNonParagraphLine = false;
        for (int i = 0; i < justifiedLines.Length; i++)
        {
            bool isLastLine = i == justifiedLines.Length - 1;
            bool linesMatch = MathF.Abs(justifiedLines[i].Extent - unJustifiedLines[i].Extent) <= .01F;

            if (isLastLine)
            {
                // The trailing line in the text box must never be justified.
                Assert.Equal(unJustifiedLines[i].Extent, justifiedLines[i].Extent, 4F);
            }
            else
            {
                // At least one earlier line should stay unchanged, proving that a
                // paragraph-final line created by the explicit newline was not justified.
                foundUnchangedNonLastLine |= linesMatch;

                // At least one other earlier line should still widen, proving that we
                // did not disable justification for all wrapped lines.
                foundJustifiedNonParagraphLine |= justifiedLines[i].Extent > unJustifiedLines[i].Extent;
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
    public void TextJustification_MultiParagraph_Vertical_DoesNotJustifyParagraphFinalLines(TextDirection direction, TextJustification justification)
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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, justification });

        LineMetrics[] justifiedLines = TextMeasurer.GetLineMetrics(text, options);

        options.TextJustification = TextJustification.None;
        LineMetrics[] unJustifiedLines = TextMeasurer.GetLineMetrics(text, options);

        Assert.Equal(unJustifiedLines.Length, justifiedLines.Length);

        bool foundUnchangedNonLastLine = false;
        bool foundJustifiedNonParagraphLine = false;
        for (int i = 0; i < justifiedLines.Length; i++)
        {
            bool isLastLine = i == justifiedLines.Length - 1;
            bool linesMatch = MathF.Abs(justifiedLines[i].Extent - unJustifiedLines[i].Extent) <= .01F;

            if (isLastLine)
            {
                // The trailing line in the text box must remain ragged in vertical layout too.
                Assert.Equal(unJustifiedLines[i].Extent, justifiedLines[i].Extent, 4F);
            }
            else
            {
                // This captures a non-last line that still behaves like a paragraph end.
                foundUnchangedNonLastLine |= linesMatch;

                // This captures a wrapped line that continues to justify normally.
                foundJustifiedNonParagraphLine |= justifiedLines[i].Extent > unJustifiedLines[i].Extent;
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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, options.TextJustification });

        Assert.Equal(wrappingLength, justifiedMetrics.Lines[0].Extent, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.CharacterAdvances.Count, justifiedMetrics.CharacterAdvances.Count);

        bool foundWidenedWhitespace = false;
        for (int i = 0; i < justifiedMetrics.CharacterAdvances.Count; i++)
        {
            float justifiedWidth = justifiedMetrics.CharacterAdvances[i].Bounds.Width;
            float unJustifiedWidth = unJustifiedMetrics.CharacterAdvances[i].Bounds.Width;

            if (CodePoint.IsWhiteSpace(unJustifiedMetrics.CharacterAdvances[i].Codepoint))
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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, options.TextJustification });

        Assert.Equal(wrappingLength, justifiedMetrics.Lines[0].Extent, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.CharacterAdvances.Count, justifiedMetrics.CharacterAdvances.Count);

        bool foundWidenedCharacter = false;
        for (int i = 0; i < justifiedMetrics.CharacterAdvances.Count; i++)
        {
            float justifiedHeight = justifiedMetrics.CharacterAdvances[i].Bounds.Height;
            float unJustifiedHeight = unJustifiedMetrics.CharacterAdvances[i].Bounds.Height;

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

        TextLayoutTestUtilities.TestLayout(text, options, properties: new { direction, options.TextJustification });

        Assert.Equal(wrappingLength, justifiedMetrics.Lines[0].Extent, 4F);

        options.TextJustification = TextJustification.None;
        TextMetrics unJustifiedMetrics = TextMeasurer.Measure(text, options);

        Assert.Equal(unJustifiedMetrics.CharacterAdvances.Count, justifiedMetrics.CharacterAdvances.Count);

        bool foundWidenedWhitespace = false;
        for (int i = 0; i < justifiedMetrics.CharacterAdvances.Count; i++)
        {
            float justifiedHeight = justifiedMetrics.CharacterAdvances[i].Bounds.Height;
            float unJustifiedHeight = unJustifiedMetrics.CharacterAdvances[i].Bounds.Height;

            if (CodePoint.IsWhiteSpace(unJustifiedMetrics.CharacterAdvances[i].Codepoint))
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

        FontRectangle actual = TextMeasurer.MeasureSize(c.ToString(), options);
        Assert.Equal(expected, actual, Comparer);

        options = new(OpenSansWoff)
        {
            KerningMode = KerningMode.Standard,
            HintingMode = HintingMode.Standard
        };

        actual = TextMeasurer.MeasureSize(c.ToString(), options);
        Assert.Equal(expected, actual, Comparer);
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

        FontRectangle actual = TextMeasurer.MeasureSize(text, options);
        Assert.Equal(width, actual.Width, Comparer);

        Assert.True(TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds));
        Assert.Equal(characterPosition, bounds.ToArray().Select(x => x.Bounds.X), Comparer);
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

        FontRectangle actual = TextMeasurer.MeasureSize(text, options);
        Assert.Equal(width, actual.Height, Comparer);

        Assert.True(TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds));
        Assert.Equal(characterPosition, bounds.ToArray().Select(x => x.Bounds.Y), Comparer);
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

        FontRectangle actual = TextMeasurer.MeasureSize(text, options);
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

        FontRectangle actual = TextMeasurer.MeasureSize(text, options);
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
    public void CanMeasureCharacterLayouts()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "Hello World!";

        Assert.True(TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances));
        Assert.True(TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> sizes));
        Assert.True(TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds));

        Assert.Equal(advances.Length, sizes.Length);
        Assert.Equal(advances.Length, bounds.Length);

        for (int i = 0; i < advances.Length; i++)
        {
            GlyphBounds advance = advances[i];
            GlyphBounds size = sizes[i];
            GlyphBounds bound = bounds[i];

            Assert.Equal(advance.Codepoint, size.Codepoint);
            Assert.Equal(advance.Codepoint, bound.Codepoint);

            // Since this is a single line starting at 0,0 the following should be predictable.
            Assert.Equal(advance.Bounds.X, size.Bounds.X);
            Assert.Equal(size.Bounds.Width, bound.Bounds.Width);
            Assert.Equal(size.Bounds.Height, bound.Bounds.Height);
        }
    }

    [Fact]
    public void CanMeasureMultilineCharacterLayouts()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "A\nA\nA\nA";

        Assert.True(TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances));
        Assert.True(TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> sizes));

        Assert.Equal(advances.Length, sizes.Length);

        GlyphBounds? lastAdvance = null;
        GlyphBounds? lastSize = null;
        for (int i = 0; i < advances.Length; i++)
        {
            GlyphBounds advance = advances[i];
            GlyphBounds size = sizes[i];

            Assert.Equal(advance.Codepoint, size.Codepoint);

            if (lastAdvance.HasValue)
            {
                Assert.Equal(lastAdvance.Value.Bounds, advance.Bounds);
                Assert.Equal(lastSize.Value.Bounds.Width, size.Bounds.Width, 2F);
                Assert.Equal(lastSize.Value.Bounds.Height, size.Bounds.Height, 2F);
            }

            lastAdvance = advance;
            lastSize = size;
        }
    }

    [Fact]
    public void DoesMeasureCharacterLayoutIncludeStringIndex()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            LineSpacing = 1.5F
        };

        const string text = "The quick👩🏽‍🚒 brown fox jumps over \r\n the lazy dog";

        Assert.True(TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances));
        Assert.True(TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> sizes));
        Assert.True(TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds));

        Assert.Equal(advances.Length, sizes.Length);
        Assert.Equal(advances.Length, bounds.Length);

        int stringIndex = -1;

        for (int i = 0; i < advances.Length; i++)
        {
            GlyphBounds advance = advances[i];
            GlyphBounds size = sizes[i];
            GlyphBounds bound = bounds[i];

            Assert.Equal(bound.StringIndex, advance.StringIndex);
            Assert.Equal(bound.StringIndex, size.StringIndex);

            Assert.Equal(bound.GraphemeIndex, advance.GraphemeIndex);
            Assert.Equal(bound.GraphemeIndex, size.GraphemeIndex);

            if (bound.Codepoint == new CodePoint("k"[0]))
            {
                stringIndex = text.IndexOf('k');
                Assert.Equal(stringIndex, bound.StringIndex);
                Assert.Equal(stringIndex, bound.GraphemeIndex);
            }

            // after emoji
            if (bound.Codepoint == new CodePoint("b"[0]))
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

        GlyphBounds firstBound = bounds[0];
        Assert.Equal(0, firstBound.StringIndex);
        Assert.Equal(0, firstBound.GraphemeIndex);

        GlyphBounds lastBound = bounds[^1];
        Assert.Equal(text.Length - 1, lastBound.StringIndex);
        Assert.Equal(graphemeCount - 1, lastBound.GraphemeIndex);
    }

    [Fact]
    public void DoesMeasureCharacterBoundsExtendForAdvanceMultipliers()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
            TabWidth = 8
        };

        const string text = "H\tH";

        List<FontRectangle> glyphs = CaptureGlyphBoundBuilder.GenerateGlyphsBoxes(text, options);
        TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds);
        TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances);

        Assert.Equal(glyphs.Count, bounds.Length);
        Assert.Equal(glyphs.Count, advances.Length);

        for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
        {
            FontRectangle renderGlyph = glyphs[glyphIndex];
            FontRectangle measureGlyph = bounds[glyphIndex].Bounds;
            GlyphBounds advance = advances[glyphIndex];

            if (CodePoint.IsWhiteSpace(advance.Codepoint))
            {
                Assert.Equal(renderGlyph.X, measureGlyph.X);
                Assert.Equal(renderGlyph.Y, measureGlyph.Y);
                Assert.Equal(advance.Bounds.Width, measureGlyph.Width);
                Assert.Equal(renderGlyph.Height, measureGlyph.Height);
            }
            else
            {
                Assert.Equal(renderGlyph, measureGlyph);
            }
        }
    }

    [Fact]
    public void IsMeasureCharacterBoundsSameAsRenderBounds()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
        {
        };

        const string text = "Hello WorLLd";

        List<FontRectangle> glyphs = CaptureGlyphBoundBuilder.GenerateGlyphsBoxes(text, options);
        TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds);

        Assert.Equal(glyphs.Count, bounds.Length);

        for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
        {
            FontRectangle renderGlyph = glyphs[glyphIndex];
            FontRectangle measureGlyph = bounds[glyphIndex].Bounds;

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

    private class CaptureGlyphBoundBuilder : IGlyphRenderer
    {
        public static List<FontRectangle> GenerateGlyphsBoxes(string text, TextOptions options)
        {
            CaptureGlyphBoundBuilder glyphBuilder = new();
            TextRenderer renderer = new(glyphBuilder);
            renderer.RenderText(text, options);
            return glyphBuilder.GlyphBounds;
        }

        public readonly List<FontRectangle> GlyphBounds = [];

        public CaptureGlyphBoundBuilder()
        {
        }

        bool IGlyphRenderer.BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.GlyphBounds.Add(bounds);
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

        FontRectangle actual = TextMeasurer.MeasureSize(c.ToString(), options);

        Assert.Equal(expected, actual, Comparer);
    }

    private static readonly Font SegoeUi = SystemFonts.CreateFont("Segoe Ui", 10);
#endif

    private static string GetSourceTextForLine(string text, TextMetrics metrics, int lineIndex)
    {
        int glyphIndex = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            glyphIndex = AdvanceGlyphIndex(metrics.CharacterAdvances, glyphIndex, metrics.Lines[i].GraphemeCount);
        }

        int startGlyphIndex = glyphIndex;
        int endGlyphIndex = AdvanceGlyphIndex(metrics.CharacterAdvances, glyphIndex, metrics.Lines[lineIndex].GraphemeCount);

        int start = int.MaxValue;
        int end = 0;
        for (int i = startGlyphIndex; i < endGlyphIndex; i++)
        {
            GlyphBounds glyph = metrics.CharacterAdvances[i];
            start = Math.Min(start, glyph.StringIndex);
            end = Math.Max(end, glyph.StringIndex + glyph.Codepoint.Utf16SequenceLength);
        }

        return text[start..end];
    }

    private static int AdvanceGlyphIndex(IReadOnlyList<GlyphBounds> glyphs, int glyphIndex, int graphemeCount)
    {
        int consumed = 0;
        int lastGraphemeIndex = -1;
        while (glyphIndex < glyphs.Count && consumed < graphemeCount)
        {
            int graphemeIndex = glyphs[glyphIndex].GraphemeIndex;
            if (graphemeIndex != lastGraphemeIndex)
            {
                consumed++;
                lastGraphemeIndex = graphemeIndex;
            }

            glyphIndex++;
        }

        while (glyphIndex < glyphs.Count && glyphs[glyphIndex].GraphemeIndex == lastGraphemeIndex)
        {
            glyphIndex++;
        }

        return glyphIndex;
    }

#if OS_WINDOWS
    [Fact]
    public void BenchmarkTest()
    {
        Font font = Arial;
        _ = TextMeasurer.MeasureSize("The quick brown fox jumped over the lazy dog", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
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

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tests;

public class TextOptionsTests
{
    private readonly Font fakeFont;
    private readonly TextOptions newTextOptions;
    private readonly TextOptions clonedTextOptions;

    public TextOptionsTests()
    {
        this.fakeFont = FakeFont.CreateFont("ABC");
        this.newTextOptions = new TextOptions(this.fakeFont);
        this.clonedTextOptions = new TextOptions(this.newTextOptions);
    }

    [Fact]
    public void ConstructorTest_FontOnly()
    {
        Font font = FakeFont.CreateFont("ABC");
        TextOptions options = new(font);

        Assert.Equal(72, options.Dpi);
        Assert.Empty(options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(Vector2.Zero, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithSingleDpi()
    {
        Font font = FakeFont.CreateFont("ABC");
        const float dpi = 123;
        TextOptions options = new(font) { Dpi = dpi };

        Assert.Equal(dpi, options.Dpi);
        Assert.Empty(options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(Vector2.Zero, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithOrigin()
    {
        Font font = FakeFont.CreateFont("ABC");
        Vector2 origin = new(123, 345);
        TextOptions options = new(font) { Origin = origin };

        Assert.Equal(72, options.Dpi);
        Assert.Empty(options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(origin, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithSingleDpiWithOrigin()
    {
        Font font = FakeFont.CreateFont("ABC");
        Vector2 origin = new(123, 345);
        const float dpi = 123;
        TextOptions options = new(font) { Dpi = dpi, Origin = origin };

        Assert.Equal(dpi, options.Dpi);
        Assert.Empty(options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(origin, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontOnly_WithFallbackFonts()
    {
        Font font = FakeFont.CreateFont("ABC");
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFont("DEF").Family,
            FakeFont.CreateFont("GHI").Family,
        ];

        TextOptions options = new(font)
        {
            FallbackFontFamilies = fontFamilies
        };

        Assert.Equal(72, options.Dpi);
        Assert.Equal(fontFamilies, options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(Vector2.Zero, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithSingleDpi_WithFallbackFonts()
    {
        Font font = FakeFont.CreateFont("ABC");
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFont("DEF").Family,
            FakeFont.CreateFont("GHI").Family,
        ];

        const float dpi = 123;
        TextOptions options = new(font)
        {
            Dpi = dpi,
            FallbackFontFamilies = fontFamilies
        };

        Assert.Equal(dpi, options.Dpi);
        Assert.Equal(fontFamilies, options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(Vector2.Zero, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithOrigin_WithFallbackFonts()
    {
        Font font = FakeFont.CreateFont("ABC");
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFont("DEF").Family,
            FakeFont.CreateFont("GHI").Family,
        ];

        Vector2 origin = new(123, 345);
        TextOptions options = new(font)
        {
            FallbackFontFamilies = fontFamilies,
            Origin = origin
        };

        Assert.Equal(72, options.Dpi);
        Assert.Equal(fontFamilies, options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(origin, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void ConstructorTest_FontWithSingleDpiWithOrigin_WithFallbackFonts()
    {
        Font font = FakeFont.CreateFont("ABC");
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFont("DEF").Family,
            FakeFont.CreateFont("GHI").Family,
        ];

        Vector2 origin = new(123, 345);
        const float dpi = 123;
        TextOptions options = new(font)
        {
            Dpi = dpi,
            FallbackFontFamilies = fontFamilies,
            Origin = origin
        };

        Assert.Equal(dpi, options.Dpi);
        Assert.Equal(fontFamilies, options.FallbackFontFamilies);
        Assert.Equal(font, options.Font);
        Assert.Equal(origin, options.Origin);
        VerifyPropertyDefault(options);
    }

    [Fact]
    public void GetMissingGlyphFromMainFont()
    {
        Font font = FakeFont.CreateFontWithInstance("ABC", "ABC", out Fakes.FakeFontInstance abcFontInstance);
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFontWithInstance("DEF", "DEF", out Fakes.FakeFontInstance _).Family,
            FakeFont.CreateFontWithInstance("GHI", "GHI", out Fakes.FakeFontInstance _).Family,
        ];

        TextOptions options = new(font)
        {
            FallbackFontFamilies = fontFamilies,
            ColorFontSupport = ColorFontSupport.None
        };

        ReadOnlySpan<char> text = "Z".AsSpan();
        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);

        GlyphRendererParameters glyph = Assert.Single(renderer.GlyphKeys);
        Assert.Equal(GlyphType.Fallback, glyph.GlyphType);
        Assert.Equal(abcFontInstance.Description.FontNameInvariantCulture.ToUpper(), glyph.Font);
    }

    [Theory]
    [InlineData('A', "abcFontInstance")]
    [InlineData('F', "defFontInstance")]
    [InlineData('H', "efghiFontInstance")]
    public void GetGlyphFromFirstAvailableInstance(char character, string instance)
    {
        Font font = FakeFont.CreateFontWithInstance("ABC", "ABC", out Fakes.FakeFontInstance abcFontInstance);
        FontFamily[] fontFamilies =
        [
            FakeFont.CreateFontWithInstance("DEF", "DEF", out Fakes.FakeFontInstance defFontInstance).Family,
            FakeFont.CreateFontWithInstance("EFGHI", "EFGHI", out Fakes.FakeFontInstance efghiFontInstance).Family,
        ];

        TextOptions options = new(font)
        {
            FallbackFontFamilies = fontFamilies,
            ColorFontSupport = ColorFontSupport.None
        };

        ReadOnlySpan<char> text = new[] { character };
        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        GlyphRendererParameters glyph = Assert.Single(renderer.GlyphKeys);
        Assert.Equal(GlyphType.Standard, glyph.GlyphType);
        Fakes.FakeFontInstance expectedInstance = instance switch
        {
            "abcFontInstance" => abcFontInstance,
            "defFontInstance" => defFontInstance,
            "efghiFontInstance" => efghiFontInstance,
            _ => throw new Exception("does not match")
        };

        Assert.Equal(expectedInstance.Description.FontNameInvariantCulture.ToUpper(), glyph.Font);
    }

    [Fact]
    public void CloneTextOptionsIsNotNull() => Assert.NotNull(this.clonedTextOptions);

    [Fact]
    public void DefaultTextOptionsApplyKerning()
    {
        const KerningMode expected = KerningMode.Standard;
        Assert.Equal(expected, this.newTextOptions.KerningMode);
        Assert.Equal(expected, this.clonedTextOptions.KerningMode);
    }

    [Fact]
    public void DefaultTextOptionsHorizontalAlignment()
    {
        const HorizontalAlignment expected = HorizontalAlignment.Left;
        Assert.Equal(expected, this.newTextOptions.HorizontalAlignment);
        Assert.Equal(expected, this.clonedTextOptions.HorizontalAlignment);
    }

    [Fact]
    public void DefaultTextOptionsVerticalAlignment()
    {
        const VerticalAlignment expected = VerticalAlignment.Top;
        Assert.Equal(expected, this.newTextOptions.VerticalAlignment);
        Assert.Equal(expected, this.clonedTextOptions.VerticalAlignment);
    }

    [Fact]
    public void DefaultTextOptionsDpi()
    {
        const float expected = 72F;
        Assert.Equal(expected, this.newTextOptions.Dpi);
        Assert.Equal(expected, this.clonedTextOptions.Dpi);
    }

    [Fact]
    public void DefaultTextOptionsTabWidth()
    {
        const float expected = -1F;
        Assert.Equal(expected, this.newTextOptions.TabWidth);
        Assert.Equal(expected, this.clonedTextOptions.TabWidth);
    }

    [Fact]
    public void DefaultTextOptionsWrappingLength()
    {
        const float expected = -1F;
        Assert.Equal(expected, this.newTextOptions.WrappingLength);
        Assert.Equal(expected, this.clonedTextOptions.WrappingLength);
    }

    [Fact]
    public void DefaultTextOptionsLineSpacing()
    {
        const float expected = 1F;
        Assert.Equal(expected, this.newTextOptions.LineSpacing);
        Assert.Equal(expected, this.clonedTextOptions.LineSpacing);
    }

    [Fact]
    public void DefaultTextOptionsTextJustification()
    {
        const TextJustification expected = TextJustification.None;
        Assert.Equal(expected, this.newTextOptions.TextJustification);
        Assert.Equal(expected, this.clonedTextOptions.TextJustification);
    }

    [Fact]
    public void NonDefaultClone()
    {
        TextOptions expected = new(this.fakeFont)
        {
            KerningMode = KerningMode.None,
            Dpi = 46F,
            HorizontalAlignment = HorizontalAlignment.Center,
            TabWidth = 3F,
            LineSpacing = -1F,
            VerticalAlignment = VerticalAlignment.Bottom,
            DecorationPositioningMode = DecorationPositioningMode.GlyphFont,
            WrappingLength = 42F,
            Tracking = 66F,
            FeatureTags = new List<Tag> { FeatureTags.OldstyleFigures }
        };

        TextOptions actual = new(expected);

        Assert.Equal(expected.KerningMode, actual.KerningMode);
        Assert.Equal(expected.Dpi, actual.Dpi);
        Assert.Equal(expected.LineSpacing, actual.LineSpacing);
        Assert.Equal(expected.HorizontalAlignment, actual.HorizontalAlignment);
        Assert.Equal(expected.TabWidth, actual.TabWidth);
        Assert.Equal(expected.VerticalAlignment, actual.VerticalAlignment);
        Assert.Equal(expected.WrappingLength, actual.WrappingLength);
        Assert.Equal(expected.DecorationPositioningMode, actual.DecorationPositioningMode);
        Assert.Equal(expected.FeatureTags, actual.FeatureTags);
        Assert.Equal(expected.Tracking, actual.Tracking);
    }

    [Fact]
    public void CloneIsDeep()
    {
        TextOptions expected = new(this.fakeFont);
        TextOptions actual = new(expected)
        {
            KerningMode = KerningMode.None,
            Dpi = 46F,
            HorizontalAlignment = HorizontalAlignment.Center,
            TabWidth = 3F,
            LineSpacing = 2F,
            VerticalAlignment = VerticalAlignment.Bottom,
            TextJustification = TextJustification.InterCharacter,
            DecorationPositioningMode = DecorationPositioningMode.GlyphFont,
            WrappingLength = 42F
            WrappingLength = 42F,
            Tracking = 66F,
        };

        Assert.NotEqual(expected.KerningMode, actual.KerningMode);
        Assert.NotEqual(expected.Dpi, actual.Dpi);
        Assert.NotEqual(expected.LineSpacing, actual.LineSpacing);
        Assert.NotEqual(expected.HorizontalAlignment, actual.HorizontalAlignment);
        Assert.NotEqual(expected.TabWidth, actual.TabWidth);
        Assert.NotEqual(expected.VerticalAlignment, actual.VerticalAlignment);
        Assert.NotEqual(expected.WrappingLength, actual.WrappingLength);
        Assert.NotEqual(expected.DecorationPositioningMode, actual.DecorationPositioningMode);
        Assert.NotEqual(expected.TextJustification, actual.TextJustification);
        Assert.NotEqual(expected.Tracking, actual.Tracking);
    }

    private static void VerifyPropertyDefault(TextOptions options)
    {
        Assert.Equal(-1, options.TabWidth);
        Assert.Equal(KerningMode.Standard, options.KerningMode);
        Assert.Equal(-1, options.WrappingLength);
        Assert.Equal(HorizontalAlignment.Left, options.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, options.VerticalAlignment);
        Assert.Equal(TextAlignment.Start, options.TextAlignment);
        Assert.Equal(TextJustification.None, options.TextJustification);
        Assert.Equal(TextDirection.Auto, options.TextDirection);
        Assert.Equal(LayoutMode.HorizontalTopBottom, options.LayoutMode);
        Assert.Equal(DecorationPositioningMode.PrimaryFont, options.DecorationPositioningMode);
        Assert.Equal(1, options.LineSpacing);
        Assert.Equal(0, options.Tracking);
    }
}

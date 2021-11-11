// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using Xunit;

namespace SixLabors.Fonts.Tests
{
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
            var options = new TextOptions(font);

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
            float dpi = 123;
            var options = new TextOptions(font, dpi);

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
            var origin = new Vector2(123, 345);
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
            var origin = new Vector2(123, 345);
            float dpi = 123;
            TextOptions options = new(font, dpi) { Origin = origin };

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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var options = new TextOptions(font)
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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            float dpi = 123;
            var options = new TextOptions(font, dpi)
            {
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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
            float dpi = 123;
            TextOptions options = new(font, dpi)
            {
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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFontWithInstance("DEF", "DEF", out Fakes.FakeFontInstance defFontInstance).Family,
                FakeFont.CreateFontWithInstance("GHI", "GHI", out Fakes.FakeFontInstance ghiFontInstance).Family
            };

            var options = new TextOptions(font)
            {
                FallbackFontFamilies = fontFamilies,
                ColorFontSupport = ColorFontSupport.None
            };

            ReadOnlySpan<char> text = "Z".AsSpan();
            var renderer = new GlyphRenderer();
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
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFontWithInstance("DEF", "DEF", out Fakes.FakeFontInstance defFontInstance).Family,
                FakeFont.CreateFontWithInstance("EFGHI", "EFGHI", out Fakes.FakeFontInstance efghiFontInstance).Family
            };

            var options = new TextOptions(font)
            {
                FallbackFontFamilies = fontFamilies,
                ColorFontSupport = ColorFontSupport.None
            };

            ReadOnlySpan<char> text = new[] { character };
            var renderer = new GlyphRenderer();
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
        public void CloneTextOptionsIsNotNull() => Assert.True(this.clonedTextOptions != null);

        [Fact]
        public void DefaultTextOptionsApplyKerning()
        {
            const KerningMode expected = KerningMode.Normal;
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
            const float expected = 4F;
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
                WrappingLength = 42F
            };

            TextOptions actual = new(expected);

            Assert.Equal(expected.KerningMode, actual.KerningMode);
            Assert.Equal(expected.Dpi, actual.Dpi);
            Assert.Equal(expected.LineSpacing, actual.LineSpacing);
            Assert.Equal(expected.HorizontalAlignment, actual.HorizontalAlignment);
            Assert.Equal(expected.TabWidth, actual.TabWidth);
            Assert.Equal(expected.VerticalAlignment, actual.VerticalAlignment);
            Assert.Equal(expected.WrappingLength, actual.WrappingLength);
        }

        [Fact]
        public void CloneIsDeep()
        {
            var expected = new TextOptions(this.fakeFont);
            TextOptions actual = new(expected);

            actual.KerningMode = KerningMode.None;
            actual.Dpi = 46F;
            actual.HorizontalAlignment = HorizontalAlignment.Center;
            actual.TabWidth = 3F;
            actual.LineSpacing = 2F;
            actual.VerticalAlignment = VerticalAlignment.Bottom;
            actual.WrappingLength = 42F;

            Assert.NotEqual(expected.KerningMode, actual.KerningMode);
            Assert.NotEqual(expected.Dpi, actual.Dpi);
            Assert.NotEqual(expected.LineSpacing, actual.LineSpacing);
            Assert.NotEqual(expected.HorizontalAlignment, actual.HorizontalAlignment);
            Assert.NotEqual(expected.TabWidth, actual.TabWidth);
            Assert.NotEqual(expected.VerticalAlignment, actual.VerticalAlignment);
            Assert.NotEqual(expected.WrappingLength, actual.WrappingLength);
        }

        private static void VerifyPropertyDefault(TextOptions options)
        {
            Assert.Equal(4, options.TabWidth);
            Assert.Equal(KerningMode.Normal, options.KerningMode);
            Assert.Equal(-1, options.WrappingLength);
            Assert.Equal(HorizontalAlignment.Left, options.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, options.VerticalAlignment);
            Assert.Equal(TextAlignment.Start, options.TextAlignment);
            Assert.Equal(TextDirection.Auto, options.TextDirection);
            Assert.Equal(LayoutMode.HorizontalTopBottom, options.LayoutMode);
            Assert.Equal(1, options.LineSpacing);
        }
    }
}

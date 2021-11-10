// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class RendererOptionsTests
    {
        private static void VerifyPropertyDefault(RendererOptions options)
        {
            Assert.Equal(4, options.TabWidth);
            Assert.Equal(KerningMode.Normal, options.KerningMode);
            Assert.Equal(-1, options.WrappingWidth);
            Assert.Equal(HorizontalAlignment.Left, options.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, options.VerticalAlignment);
            Assert.Equal(1, options.LineSpacing);
        }

        [Fact]
        public void ConstructorTest_FontOnly()
        {
            Font font = FakeFont.CreateFont("ABC");
            var options = new RendererOptions(font);

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
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
            var options = new RendererOptions(font, dpi);

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Empty(options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(Vector2.Zero, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ConstructorTest_FontWithXandYDpis()
        {
            Font font = FakeFont.CreateFont("ABC");
            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy);

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
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
            var options = new RendererOptions(font, origin);

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
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
            var options = new RendererOptions(font, dpi, origin);

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Empty(options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(origin, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ConstructorTest_FontWithXandYDpisWithOrigin()
        {
            Font font = FakeFont.CreateFont("ABC");
            var origin = new Vector2(123, 345);
            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy, origin);

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
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

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
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
            var options = new RendererOptions(font, dpi)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Equal(fontFamilies, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(Vector2.Zero, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ConstructorTest_FontWithXandYDpis_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
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
            var options = new RendererOptions(font, origin)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
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
            var options = new RendererOptions(font, dpi, origin)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Equal(fontFamilies, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(origin, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ConstructorTest_FontWithXandYDpisWithOrigin_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilies = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy, origin)
            {
                FallbackFontFamilies = fontFamilies
            };

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
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

            var options = new RendererOptions(font)
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

            var options = new RendererOptions(font)
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
    }
}

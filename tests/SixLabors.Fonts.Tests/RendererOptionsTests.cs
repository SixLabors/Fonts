// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class RendererOptionsTests
    {
        private static void VerifyPropertyDefault(RendererOptions options)
        {
            Assert.Equal(4, options.TabWidth);
            Assert.True(options.ApplyKerning);
            Assert.Equal(-1, options.WrappingWidth);
            Assert.Equal(HorizontalAlignment.Left, options.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, options.VerticalAlignment);
            Assert.Equal(1, options.LineSpacing);
        }

        [Fact]
        public void ContructorTest_FontOnly()
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
        public void ContructorTest_FontWithSingleDpi()
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
        public void ContructorTest_FontWithXandYDpis()
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
        public void ContructorTest_FontWithOrigin()
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
        public void ContructorTest_FontWithSingleDpiWithOrigin()
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
        public void ContructorTest_FontWithXandYDpisWithOrigin()
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
        public void ContructorTest_FontOnly_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(Vector2.Zero, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ContructorTest_FontWithSingleDpi_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            float dpi = 123;
            var options = new RendererOptions(font, dpi)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(Vector2.Zero, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ContructorTest_FontWithXandYDpis_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(Vector2.Zero, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ContructorTest_FontWithOrigin_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
            var options = new RendererOptions(font, origin)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(72, options.DpiX);
            Assert.Equal(72, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(origin, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ContructorTest_FontWithSingleDpiWithOrigin_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
            float dpi = 123;
            var options = new RendererOptions(font, dpi, origin)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(dpi, options.DpiX);
            Assert.Equal(dpi, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(origin, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void ContructorTest_FontWithXandYDpisWithOrigin_WithFallbackFonts()
        {
            Font font = FakeFont.CreateFont("ABC");
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFont("DEF").Family,
                FakeFont.CreateFont("GHI").Family
            };

            var origin = new Vector2(123, 345);
            float dpix = 123;
            float dpiy = 456;
            var options = new RendererOptions(font, dpix, dpiy, origin)
            {
                FallbackFontFamilies = fontFamilys
            };

            Assert.Equal(dpix, options.DpiX);
            Assert.Equal(dpiy, options.DpiY);
            Assert.Equal(fontFamilys, options.FallbackFontFamilies);
            Assert.Equal(font, options.Font);
            Assert.Equal(origin, options.Origin);
            VerifyPropertyDefault(options);
        }

        [Fact]
        public void GetStylePassesCorrectValues()
        {
            Font font = FakeFont.CreateFontWithInstance("ABC", out Fakes.FakeFontInstance abcFontInstance);
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFontWithInstance("DEF", out Fakes.FakeFontInstance defFontInstance).Family,
                FakeFont.CreateFontWithInstance("GHI", out Fakes.FakeFontInstance ghiFontInstance).Family
            };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            AppliedFontStyle style = options.GetStyle(4, 10);

            Assert.Equal(0, style.Start);
            Assert.Equal(9, style.End);
            Assert.Equal(font.Size, style.PointSize);
            Assert.Equal(4, style.TabWidth);
            Assert.True(style.ApplyKerning);

            Assert.Equal(abcFontInstance, style.MainFont);
            Assert.Equal(2, style.FallbackFonts.Count());
            Assert.Contains(defFontInstance, style.FallbackFonts);
            Assert.Contains(ghiFontInstance, style.FallbackFonts);
        }

        [Fact]
        public void GetMissingGlyphFromMainFont()
        {
            Font font = FakeFont.CreateFontWithInstance("ABC", out Fakes.FakeFontInstance abcFontInstance);
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFontWithInstance("DEF", out Fakes.FakeFontInstance defFontInstance).Family,
                FakeFont.CreateFontWithInstance("GHI", out Fakes.FakeFontInstance ghiFontInstance).Family
            };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            AppliedFontStyle style = options.GetStyle(4, 10);

            GlyphMetrics glyph = Assert.Single(style.GetGlyphLayers(new CodePoint('Z'), ColorFontSupport.None));
            Assert.Equal(GlyphType.Fallback, glyph.GlyphType);
            Assert.Equal(abcFontInstance, glyph.Metrics);
        }

        [Theory]
        [InlineData('A', "abcFontInstance")]
        [InlineData('F', "defFontInstance")]
        [InlineData('H', "efghiFontInstance")]
        public void GetGlyphFromFirstAvailableInstance(char character, string instance)
        {
            Font font = FakeFont.CreateFontWithInstance("ABC", out Fakes.FakeFontInstance abcFontInstance);
            FontFamily[] fontFamilys = new[]
            {
                FakeFont.CreateFontWithInstance("DEF", out Fakes.FakeFontInstance defFontInstance).Family,
                FakeFont.CreateFontWithInstance("EFGHI", out Fakes.FakeFontInstance efghiFontInstance).Family
            };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            AppliedFontStyle style = options.GetStyle(4, 10);

            GlyphMetrics glyph = Assert.Single(style.GetGlyphLayers(new CodePoint(character), ColorFontSupport.None));
            Assert.Equal(GlyphType.Standard, glyph.GlyphType);
            Fakes.FakeFontInstance expectedInstance = instance switch
            {
                "abcFontInstance" => abcFontInstance,
                "defFontInstance" => defFontInstance,
                "efghiFontInstance" => efghiFontInstance,
                _ => throw new Exception("does not match")
            };

            Assert.Equal(expectedInstance, glyph.Metrics);
        }
    }
}

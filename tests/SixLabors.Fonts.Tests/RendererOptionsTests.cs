using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SixLabors.Fonts.Tables.General;
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
        }

        [Fact]
        public void ContructorTest_FontOnly()
        {
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFont("ABC");
            var fontFamilys = new[]{
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
            var font = FakeFont.CreateFontWithInstance("ABC", out var abcFontInstance);
            var fontFamilys = new[]{
                    FakeFont.CreateFontWithInstance("DEF", out var defFontInstance).Family,
                    FakeFont.CreateFontWithInstance("GHI", out var ghiFontInstance).Family
                    };
            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            var style = options.GetStyle(4, 10);

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
            var font = FakeFont.CreateFontWithInstance("ABC", out var abcFontInstance);
            var fontFamilys = new[] {
                    FakeFont.CreateFontWithInstance("DEF", out var defFontInstance).Family,
                    FakeFont.CreateFontWithInstance("GHI", out var ghiFontInstance).Family
             };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            var style = options.GetStyle(4, 10);

            var glyph = Assert.Single(style.GetGlyphLayers('Z', ColorFontSupport.None));
            Assert.Equal(GlyphType.Fallback, glyph.GlyphType);
            Assert.Equal(abcFontInstance, glyph.Font);
        }

        [Theory]
        [InlineData('A', "abcFontInstance")]
        [InlineData('F', "defFontInstance")]
        [InlineData('H', "efghiFontInstance")]
        public void GetGlyphFromFirstAvailableInstance(char character, string instance)
        {
            var font = FakeFont.CreateFontWithInstance("ABC", out var abcFontInstance);
            var fontFamilys = new[] {
                    FakeFont.CreateFontWithInstance("DEF", out var defFontInstance).Family,
                    FakeFont.CreateFontWithInstance("EFGHI", out var efghiFontInstance).Family
             };

            var options = new RendererOptions(font)
            {
                FallbackFontFamilies = fontFamilys
            };

            var style = options.GetStyle(4, 10);

            var glyph = Assert.Single(style.GetGlyphLayers(character, ColorFontSupport.None));
            Assert.Equal(GlyphType.Standard, glyph.GlyphType);
            var expectedInstance = instance switch
            {
                "abcFontInstance" => abcFontInstance,
                "defFontInstance" => defFontInstance,
                "efghiFontInstance" => efghiFontInstance,
                _ => throw new Exception("does not match")
            };

            Assert.Equal(expectedInstance, glyph.Font);
        }

    }
}

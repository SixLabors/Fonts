using Moq;
using SixLabors.Fonts.Tables.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Tables.General.Glyphs;
using Xunit;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Fact]
        public void FakeFontGetGlyph()
        {
            var font = CreateFont("hello world");
            var glyph = font.GetGlyph('h');
            Assert.NotNull(glyph);
        }

        [Theory]
        [InlineData("hello world", 20, 330)]
        [InlineData("hello world\nhello world",
            50, //30 actaul line height + 20 actual height
            330)]
        [InlineData("hello\nworld",
            50, //30 actaul line height + 20 actual height
            150)]
        public void MeasureText(string text, float height, float width)
        {
            var font = CreateFont(text);

            var scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var size = new TextMeasurer().MeasureText(text, font, 72 * font.EmSize);

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }



        [Theory]
        [InlineData("hello world", 20, 330)]
        [InlineData("hello world hello world",
            50, //30 actaul line height + 20 actual height
            330)]
        public void MeasureTextWordWrapping(string text, float height, float width)
        {
            var font = CreateFont(text);

            var scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
            {
                WrappingWidth = 340
            });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("ab", 939, 1148, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 939, 1148, true)]
        [InlineData("AB", 885, 1148, false)] // width changes between kerning enabled or not
        [InlineData("AB", 885, 769, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool enableKerning)
        {
            FontCollection c = new FontCollection();
            var font = c.Install(TestFonts.SimpleFontFileData());

            var scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var size = new TextMeasurer().MeasureText(text, new FontSpan(new Font(font, 1), 72 * font.EmSize) { ApplyKerning = enableKerning });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            var d = fc.Install(new FakeFontInstance(text));
            return new Font(d, 1);
        }
    }
}
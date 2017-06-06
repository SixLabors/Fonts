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
using System.Collections.Immutable;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Fact]
        public void FakeFontGetGlyph()
        {
            Font font = CreateFont("hello world");
            Glyph glyph = font.GetGlyph('h');
            Assert.NotNull(glyph);
        }

        [Theory]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Left,
            "hello world\nhello",
            0,
            0)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            "hello world\nhello",
            0,
            -330)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            "hello world\nhello",
            0,
            -165)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Left,
            "hello world\nhello",
            -50,
            0)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            "hello world\nhello",
            -50,
            -330)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            "hello world\nhello",
            -50,
            -165)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            "hello world\nhello",
            -25,
            0)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            "hello world\nhello",
            -25,
            -330)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            "hello world\nhello",
            -25,
            -165)]
        public void VerticalAlignmentTests(
            VerticalAlignment vertical,
            HorizontalAlignment horizental,
            string text, float top, float left)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            FontSpan span = new FontSpan(font, scaleFactor)
            {
                HorizontalAlignment = horizental,
                VerticalAlignment = vertical
            };

            ImmutableArray<GlyphLayout> glyphsToRender = new TextLayout().GenerateLayout(text, span);
            var fontInst = span.Font.FontInstance;
            float lineHeight = (fontInst.LineHeight * span.Font.Size) / (fontInst.EmSize * 72);
            lineHeight *= scaleFactor;
            Bounds bound = TextMeasurer.GetBounds(glyphsToRender, span.DPI);

            Assert.Equal(left, bound.Min.X, 3);
            Assert.Equal(top, bound.Min.Y - lineHeight, 3);
        }


        [Theory]
        [InlineData("hello", 20, 150)]
        [InlineData("hello world", 20, 330)]
        [InlineData("hello world\nhello world",
            50, //30 actaul line height + 20 actual height
            330)]
        [InlineData("hello\nworld",
            50, //30 actaul line height + 20 actual height
            150)]
        public void MeasureText(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            Size size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
            {

            });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("hello world", 20, 330)]
        [InlineData("hello world hello world",
            80, //30 actaul line height + 20 actual height
            330)]
        public void MeasureTextWordWrapping(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            Size size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
            {
                WrappingWidth = 350
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("ab", 939, 1148, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 939, 1148, true)]
        [InlineData("AB", 885, 1148, false)] // width changes between kerning enabled or not
        [InlineData("AB", 885, 769, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool enableKerning)
        {
            FontCollection c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData());

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            Size size = new TextMeasurer().MeasureText(text, new FontSpan(new Font(font, 1), 72 * font.EmSize) { ApplyKerning = enableKerning });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text));
            return new Font(d, 1);
        }
    }
}
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
using SixLabors.Primitives;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        private float expectedY;

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
            0,
            0)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            0,
            -330)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            0,
            -165)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Left,
            -60,
            0)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            -60,
            -330)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            -60,
            -165)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            -30,
            0)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -30,
            -330)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -30,
            -165)]
        public void VerticalAlignmentTests(
            VerticalAlignment vertical,
            HorizontalAlignment horizental,
            float top, float left)
        {
            var text = "hello world\nhello";
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
            RectangleF bound = TextMeasurer.GetBounds(glyphsToRender, new Vector2(span.DpiX, span.DpiY));

            Assert.Equal(330, bound.Width, 3);
            Assert.Equal(60, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
        }


        [Theory]
        [InlineData("hello", 30, 150)]
        [InlineData("hello world", 30, 330)]
        [InlineData("hello world\nhello world", 60, 330)]
        [InlineData("hello\nworld", 60, 150)]
        public void MeasureText(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            SizeF size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
            {

            });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("hello world", 30, 330)]
        [InlineData("hello world hello world",
            90, //30 actaul line height + 20 actual height
            330)]
        public void MeasureTextWordWrapping(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            SizeF size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
            {
                WrappingWidth = 350
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("ab", 1236, 1148, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 1236, 1148, true)]
        [InlineData("AB", 1236, 1148, false)] // width changes between kerning enabled or not
        [InlineData("AB", 1236, 769, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool enableKerning)
        {
            FontCollection c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            SizeF size = new TextMeasurer().MeasureText(text, new FontSpan(new Font(font, 1), 72 * font.EmSize) { ApplyKerning = enableKerning });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("a", 100, 100, 100, 100)] 
        public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
        {
            FontCollection c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var glyphRenderer = new GlyphRenderer();
            var renderer = new TextRenderer(glyphRenderer);
            renderer.RenderText(text, new FontSpan(new Font(font, 1), 72 * font.EmSize), new PointF(x, y));

            Assert.Equal(new PointF(expectedX, expectedY), glyphRenderer.GlyphRects[0].Location);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
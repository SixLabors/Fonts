using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Fact]
        public void FakeFontGetGlyph()
        {
            Font font = CreateFont("hello world");
            Glyph glyph = font.GetGlyph('h');
            Assert.NotEqual(default(Glyph), glyph);
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
            -50,
            10)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            -50,
            -320)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            -50,
            -155)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            -25,
            10)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -25,
            -320)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -25,
            -155)]
        public void VerticalAlignmentTests(
            VerticalAlignment vertical,
            HorizontalAlignment horizental,
            float top, float left)
        {
            string text = "hello world\nhello";
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var span = new RendererOptions(font, scaleFactor)
            {
                HorizontalAlignment = horizental,
                VerticalAlignment = vertical
            };

            IReadOnlyList<GlyphLayout> glyphsToRender = new TextLayout().GenerateLayout(text.AsSpan(), span);
            IFontInstance fontInst = span.Font.Instance;
            float lineHeight = (fontInst.LineHeight * span.Font.Size) / (fontInst.EmSize * 72);
            lineHeight *= scaleFactor;
            FontRectangle bound = TextMeasurer.GetBounds(glyphsToRender, new Vector2(span.DpiX, span.DpiY));

            Assert.Equal(310, bound.Width, 3);
            Assert.Equal(40, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
        }


        [Fact]
        public unsafe void MeasureTextWithSpan()
        {
            Font font = CreateFont("hello");

            Span<char> text = stackalloc char[] { 'h', 'e', 'l', 'l', 'o' };

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 

            FontRectangle size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, 72 * font.EmSize));

            Assert.Equal(10, size.Height, 4);
            Assert.Equal(130, size.Width, 4);
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

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            FontRectangle size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, 72 * font.EmSize));

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Fact]
        public void TryMeasureCharacterBounds()
        {
            string text = "a b\nc";
            var expectedGlyphMetrics = new GlyphMetric[] {
                new GlyphMetric('a', new FontRectangle(10, 0, 10, 10), false),
                new GlyphMetric(' ', new FontRectangle(40, 0, 30, 10), false),
                new GlyphMetric('b', new FontRectangle(70, 0, 10, 10), false),
                new GlyphMetric('\n', new FontRectangle(100, 0, 0, 10), true),
                new GlyphMetric('c', new FontRectangle(10, 30, 10, 10), false),
            };
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            Assert.True(TextMeasurer.TryMeasureCharacterBounds(text.AsSpan(), new RendererOptions(font, 72 * font.EmSize), out GlyphMetric[] glyphMetrics));

            Assert.Equal(text.Length, glyphMetrics.Length);
            for (int i = 0; i < glyphMetrics.Length; i++)
            {
                GlyphMetric expected = expectedGlyphMetrics[i];
                GlyphMetric actual = glyphMetrics[i];
                Assert.Equal(expected.Character, actual.Character);
                Assert.Equal(expected.IsControlCharacter, actual.IsControlCharacter);
                // 4 dp as there is minor offset difference in the float values
                Assert.Equal(expected.Bounds.X, actual.Bounds.X, 4);
                Assert.Equal(expected.Bounds.Y, actual.Bounds.Y, 4);
                Assert.Equal(expected.Bounds.Height, actual.Bounds.Height, 4);
                Assert.Equal(expected.Bounds.Width, actual.Bounds.Width, 4);
            }
        }

        [Theory]
        [InlineData("hello world", 10, 310)]
        [InlineData("hello world hello world hello world",
            70, // 30 actual line height * 2 + 10 actual height
            310)]
        public void MeasureTextWordWrapping(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            FontRectangle size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, 72 * font.EmSize)
            {
                WrappingWidth = 350
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("ab", 477, 1081, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 477, 1081, true)]
        [InlineData("AB", 465, 1033, false)] // width changes between kerning enabled or not
        [InlineData("AB", 465, 654, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool enableKerning)
        {
            var c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            FontRectangle size = TextMeasurer.MeasureBounds(text, new RendererOptions(new Font(font, 1), 72 * font.EmSize) { ApplyKerning = enableKerning });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("a", 100, 100, 125, 452)]
        public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
        {
            var c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var glyphRenderer = new GlyphRenderer();
            var renderer = new TextRenderer(glyphRenderer);
            renderer.RenderText(text, new RendererOptions(new Font(font, 1), 72 * font.EmSize, new Vector2(x, y)));

            Assert.Equal(expectedX, glyphRenderer.GlyphRects[0].Location.X, 2);
            Assert.Equal(expectedY, glyphRenderer.GlyphRects[0].Location.Y, 2);
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}

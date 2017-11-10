using System.Numerics;
using Xunit;
using SixLabors.Fonts.Tests.Fakes;
using System.Collections.Immutable;
using SixLabors.Primitives;

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
            10,
            10)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            10,
            -320)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            10,
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
            -20,
            10)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -20,
            -320)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -20,
            -155)]
        public void VerticalAlignmentTests(
            VerticalAlignment vertical,
            HorizontalAlignment horizental,
            float top, float left)
        {
            var text = "hello world\nhello";
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            RendererOptions span = new RendererOptions(font, scaleFactor)
            {
                HorizontalAlignment = horizental,
                VerticalAlignment = vertical
            };

            ImmutableArray<GlyphLayout> glyphsToRender = new TextLayout().GenerateLayout(text, span);
            var fontInst = span.Font.FontInstance;
            float lineHeight = (fontInst.LineHeight * span.Font.Size) / (fontInst.EmSize * 72);
            lineHeight *= scaleFactor;
            RectangleF bound = TextMeasurer.GetBounds(glyphsToRender, new Vector2(span.DpiX, span.DpiY));

            Assert.Equal(310, bound.Width, 3);
            Assert.Equal(40, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
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
            SizeF size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, 72 * font.EmSize)
            {

            }).Size;

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
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
            SizeF size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, 72 * font.EmSize)
            {
                WrappingWidth = 350
            }).Size;

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
            FontCollection c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            SizeF size = TextMeasurer.MeasureBounds(text, new RendererOptions(new Font(font, 1), 72 * font.EmSize) { ApplyKerning = enableKerning }).Size;

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("a", 100, 100, 125, 828)] 
        public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
        {
            FontCollection c = new FontCollection();
            Font font = c.Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var glyphRenderer = new GlyphRenderer();
            var renderer = new TextRenderer(glyphRenderer);
            renderer.RenderText(text, new RendererOptions(new Font(font, 1), 72 * font.EmSize, new PointF(x, y)));

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

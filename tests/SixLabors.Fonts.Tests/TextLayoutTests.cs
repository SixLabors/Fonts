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
        [InlineData(TextAlignment.Left,
            "hello world\nhello",
            1,
            20,
            330,
            0,
            330)]
        [InlineData(TextAlignment.Left,
            "hello world\nhello",
            2,
            20,
            150,
            0,
            150)]
        [InlineData(TextAlignment.Right,
            "hello world\nhello",
            1,
            20,
            330,
            -330,
            0)]
        [InlineData(TextAlignment.Right,
            "hello world\nhello",
            2,
            20,
            150,
            -150,
            0)]
        [InlineData(TextAlignment.Center,
            "hello world\nhello",
            1,
            20,
            330,
            -330 /2f,
            330/2f)]
        [InlineData(TextAlignment.Center,
            "hello world\nhello",
            2,
            20,
            150,
            -150 / 2f,
            150 /2f)]
        public void AlignLines_NoWidth(TextAlignment alignment, string text, int line, float height, float width, float left, float right)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            ImmutableArray<GlyphLayout> glyphsToRender = new TextLayout().GenerateLayout(text, new FontSpan(font, scaleFactor)
            {
                Alignment = alignment
            });

            List<int> startOfLines = glyphsToRender.Select((x, i) => new { x, i }).Where(x => x.x.StartOfLine).Select(x => x.i).ToList();
            startOfLines.Add(glyphsToRender.Length);

            line--; // zero based indexes
            int startOfLine = startOfLines[line];
            int endOfLine = startOfLines[line + 1];
            int count = endOfLine - startOfLine;

            float actualLeft = glyphsToRender.Skip(startOfLine).Take(count).Min(x => x.Location.X) * scaleFactor;
            float actualRight = glyphsToRender.Skip(startOfLine).Take(count).Max(x => x.Location.X + x.Width) * scaleFactor;

            float actualTop = glyphsToRender.Skip(startOfLine).Take(count).Min(x => x.Location.Y) * scaleFactor;
            float actualBottom = glyphsToRender.Skip(startOfLine).Take(count).Max(x => x.Location.Y + x.Height) * scaleFactor;

            Vector2 topLeft = new Vector2(actualLeft, actualTop);
            Vector2 bottomRight = new Vector2(actualRight, actualBottom);

            Size size = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
            Assert.Equal(left, actualLeft, 4);
            Assert.Equal(right, actualRight, 4);
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
            50, //30 actaul line height + 20 actual height
            330)]
        public void MeasureTextWordWrapping(string text, float height, float width)
        {
            Font font = CreateFont(text);

            int scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            Size size = new TextMeasurer().MeasureText(text, new FontSpan(font, 72 * font.EmSize)
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
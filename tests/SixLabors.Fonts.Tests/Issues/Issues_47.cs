using System.Collections.Generic;
using System.Collections.Immutable;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_47
    {
        [Theory]
        [InlineData("hello world hello world hello world hello world")]
        public void LeftAlignedTextNewLineShouldNotStartWithWhiteSpace(string text)
        {
            Font font = CreateFont("\t x");

            GlyphRenderer r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            float lineYPos = layout[0].Location.Y;
            foreach (GlyphLayout glyph in layout)
            {
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.False(glyph.IsWhiteSpace);
                    lineYPos = glyph.Location.Y;
                }
            }
        }

        [Theory]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Left)]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Right)]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Center)]
        [InlineData("hello   world   hello   world   hello   hello   world", HorizontalAlignment.Left)]
        public void NewWrappedLinesShouldNotStartOrEndWithWhiteSpace(string text, HorizontalAlignment horiAlignment)
        {
            Font font = CreateFont("\t x");

            GlyphRenderer r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350,
                HorizontalAlignment = horiAlignment
            });

            float lineYPos = layout[0].Location.Y;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyph = layout[i];
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.False(glyph.IsWhiteSpace);
                    Assert.False(layout[i - 1].IsWhiteSpace);
                    lineYPos = glyph.Location.Y;
                }
            }
        }

        [Fact]
        public void WhiteSpaceAtStartOfTextShouldNotBeTrimmed()
        {
            Font font = CreateFont("\t x");
            string text = "   hello world hello world hello world";

            GlyphRenderer r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350
            });

            Assert.True(layout[0].IsWhiteSpace);
            Assert.True(layout[1].IsWhiteSpace);
            Assert.True(layout[2].IsWhiteSpace);
        }

        [Fact]
        public void WhiteSpaceAtTheEndOfTextShouldBeTrimmed()
        {
            Font font = CreateFont("\t x");
            string text = "hello world hello world hello world   ";

            GlyphRenderer r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350
            });

            Assert.False(layout[layout.Count - 1].IsWhiteSpace);
            Assert.False(layout[layout.Count - 2].IsWhiteSpace);
            Assert.False(layout[layout.Count - 3].IsWhiteSpace);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}

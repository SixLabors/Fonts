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
            var font = CreateFont("\t x");

            GlyphRenderer r = new GlyphRenderer();

            ImmutableArray<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            float lineYPos = layout[0].Location.Y;
            foreach (GlyphLayout glyph in layout)
            {
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.Equal(false, glyph.IsWhiteSpace);
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
            var font = CreateFont("\t x");

            GlyphRenderer r = new GlyphRenderer();

            ImmutableArray<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350,
                HorizontalAlignment = horiAlignment
            });

            float lineYPos = layout[0].Location.Y;
            for (int i = 0; i < layout.Length; i++)
            {
                GlyphLayout glyph = layout[i];
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.Equal(false, glyph.IsWhiteSpace);
                    Assert.Equal(false, layout[i - 1].IsWhiteSpace);
                    lineYPos = glyph.Location.Y;
                }
            }
        }

        [Fact]
        public void WhiteSpaceAtStartOfTextShouldNotBeTrimmed()
        {
            var font = CreateFont("\t x");
            var text = "   hello world hello world hello world";

            GlyphRenderer r = new GlyphRenderer();

            ImmutableArray<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350
            });

            Assert.Equal(true, layout[0].IsWhiteSpace);
            Assert.Equal(true, layout[1].IsWhiteSpace);
            Assert.Equal(true, layout[2].IsWhiteSpace);
        }

        [Fact]
        public void WhiteSpaceAtTheEndOfTextShouldBeTrimmed()
        {
            var font = CreateFont("\t x");
            var text = "hello world hello world hello world   ";

            GlyphRenderer r = new GlyphRenderer();

            ImmutableArray<GlyphLayout> layout = new TextLayout().GenerateLayout(text, new RendererOptions(new Font(font, 30), 72)
            {
                WrappingWidth = 350
            });

            Assert.Equal(false, layout[layout.Length - 1].IsWhiteSpace);
            Assert.Equal(false, layout[layout.Length - 2].IsWhiteSpace);
            Assert.Equal(false, layout[layout.Length - 3].IsWhiteSpace);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}

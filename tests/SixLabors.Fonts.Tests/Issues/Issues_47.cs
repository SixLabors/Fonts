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

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}

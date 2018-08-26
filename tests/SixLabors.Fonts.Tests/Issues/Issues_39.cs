using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_39
    {
        [Fact]
        public void RenderingEmptyString_DoesNotThrow()
        {
            Font font = CreateFont("\t x");

            var r = new GlyphRenderer();

            new TextRenderer(r).RenderText("", new RendererOptions(new Font(font, 30), 72));
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
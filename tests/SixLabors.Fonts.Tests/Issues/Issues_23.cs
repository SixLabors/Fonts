using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_23
    {
        [Fact]
        public void BleadingFonts()
        {
            // wendy one returns wrong points for 'o'
            Font font = new FontCollection().Install(TestFonts.WendyOneFile).CreateFont(12);

            var r = new GlyphRenderer();

            new TextRenderer(r).RenderText("o", new RendererOptions(new Font(font, 30), 72));

            Assert.DoesNotContain(System.Numerics.Vector2.Zero, r.ControlPoints);
        }
    }
}

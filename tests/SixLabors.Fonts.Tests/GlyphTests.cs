using System.Linq;

namespace SixLabors.Fonts.Tests
{
    using Moq;
    using SixLabors.Fonts.Tests.Fakes;
    using SixLabors.Primitives;
    using System.Numerics;
    using Xunit;

    public class GlyphTests
    {
        GlyphRenderer renderer = new GlyphRenderer();
       
        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            Glyph glyph = new Glyph(new GlyphInstance((FontInstance)CreateFont("A").FontInstance, new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1), 0, 0, 1, 0), 10);

            var locationInFontSpace = new PointF(99, 99) / 72; // glyp ends up 10px over due to offiset in fake glyph
            glyph.RenderTo(renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new RectangleF(99, 89, 0, 0), this.renderer.GlyphRects.Single());
        }

        [Fact]
        public void IdenticalGlyphsInDiferentPalcesCreateIdenticalKeys()
        {
            Font fakeFont = CreateFont("AB");
            TextRenderer textRenderer = new TextRenderer(this.renderer);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));

            Assert.Equal(this.renderer.GlyphKeys[0], this.renderer.GlyphKeys[2]);
            Assert.NotEqual(this.renderer.GlyphKeys[1], this.renderer.GlyphKeys[2]);
        }

        [Fact]
        public void BeginGLyph_returnsfalse_skiprenderingfigures()
        {
            Mock<IGlyphRenderer> renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<GlyphRendererParameters>())).Returns(false);
            Font fakeFont = CreateFont("A");
            TextRenderer textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Never);
        }

        [Fact]
        public void BeginGLyph_returnstrue_rendersfigures()
        {
            Mock<IGlyphRenderer> renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<GlyphRendererParameters>())).Returns(true);
            Font fakeFont = CreateFont("A");
            TextRenderer textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Exactly(3));
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}

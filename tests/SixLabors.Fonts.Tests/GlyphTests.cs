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
        Glyph glyph = new Glyph(new GlyphInstance(new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1), 0, 0, 1, 0), 10);
        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            var locationInFontSpace = new PointF(99, 99) / 72; // glyp ends up 10px over due to offiset in fake glyph
            glyph.RenderTo(renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new RectangleF(99, 89, 0, 0), renderer.GlyphRects.Single());
        }

        [Fact]
        public void IdenticalGlyphsInDiferentPalcesCreateIdenticalKeys()
        {
            var fakeFont = CreateFont("AB");
            var textRenderer = new TextRenderer(renderer);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));

            Assert.Equal(renderer.GlyphKeys[0], renderer.GlyphKeys[2]);
            Assert.NotEqual(renderer.GlyphKeys[1], renderer.GlyphKeys[2]);
        }

        [Fact]
        public void BeginGLyph_returnsfalse_skiprenderingfigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<int>())).Returns(false);
            var fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Never);
        }

        [Fact]
        public void BeginGLyph_returnstrue_rendersfigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<int>())).Returns(true);
            var fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

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

using System.Linq;
using System.Numerics;
using Moq;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class GlyphTests
    {
        private readonly GlyphRenderer renderer = new GlyphRenderer();

        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            var glyph = new Glyph(new GlyphInstance((FontInstance)CreateFont("A").Instance, new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1), 0, 0, 1, 0), 10);

            var locationInFontSpace = new PointF(99, 99) / 72; // glyp ends up 10px over due to offiset in fake glyph
            glyph.RenderTo(renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new RectangleF(99, 89, 0, 0), this.renderer.GlyphRects.Single());
        }

        [Fact]
        public void IdenticalGlyphsInDiferentPalcesCreateIdenticalKeys()
        {
            Font fakeFont = CreateFont("AB");
            var textRenderer = new TextRenderer(this.renderer);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));

            Assert.Equal(this.renderer.GlyphKeys[0], this.renderer.GlyphKeys[2]);
            Assert.NotEqual(this.renderer.GlyphKeys[1], this.renderer.GlyphKeys[2]);
        }

        [Fact]
        public void BeginGLyph_returnsfalse_skiprenderingfigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<GlyphRendererParameters>())).Returns(false);
            Font fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Never);
        }

        [Fact]
        public void BeginGLyph_returnstrue_rendersfigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<RectangleF>(), It.IsAny<GlyphRendererParameters>())).Returns(true);
            Font fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Exactly(3));
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }

        [Fact]
        public void LoadGlyph()
        {
            Font font = new FontCollection().Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            // Get letter A
            Glyph g = font.GetGlyph(41);
            var instance = g.Instance;

            Assert.Equal(20, instance.ControlPoints.Length);
            Assert.Equal(20, instance.OnCurves.Length);
        }
    }
}

using System.Globalization;
using System.Linq;
using System.Numerics;
using Moq;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class GlyphTests
    {
        private readonly GlyphRenderer renderer = new GlyphRenderer();

        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            var glyph = new Glyph(new GlyphInstance((FontInstance)CreateFont("A").Instance, new Fonts.Tables.General.Glyphs.GlyphVector(new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1)), 0, 0, 1, 0), 10);

            Vector2 locationInFontSpace = new Vector2(99, 99) / 72; // glyp ends up 10px over due to offiset in fake glyph
            glyph.RenderTo(this.renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new FontRectangle(99, 89, 0, 0), this.renderer.GlyphRects.Single());
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
            renderer.Setup(x => x.BeginGlyph(It.IsAny<FontRectangle>(), It.IsAny<GlyphRendererParameters>())).Returns(false);
            Font fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Never);
        }

        [Fact]
        public void BeginGLyph_returnstrue_rendersfigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<FontRectangle>(), It.IsAny<GlyphRendererParameters>())).Returns(true);
            Font fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Exactly(3));
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }

        [Fact]
        public void LoadGlyph()
        {
            Font font = new FontCollection().Install(TestFonts.SimpleFontFileData()).CreateFont(12);

            // Get letter A
            Glyph g = font.GetGlyph(41);
            GlyphInstance instance = g.Instance;

            Assert.Equal(20, instance.ControlPoints.Length);
            Assert.Equal(20, instance.OnCurves.Length);
        }

        [Fact]
        public void RenderColrGlyph()
        {
            Font font = new FontCollection().Install(TestFonts.TwemojiMozillaData()).CreateFont(12);

            // Get letter Grinning Face emoji
            var instance = font.Instance as FontInstance;
            Assert.True(instance.TryGetGlyphIndex(this.AsCodePoint("😀"), out var idx));
            Assert.True(instance.TryGetColoredVectors(idx, out var vectors));

            Assert.Equal(3, vectors.Length);
        }

        [Fact]
        public void RenderColrGlyphTextRenderer()
        {
            Font font = new FontCollection().Install(TestFonts.TwemojiMozillaData()).CreateFont(12);

            var renderer = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(renderer, "😀", new RendererOptions(font)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.Equal(3, renderer.Colors.Count);
        }

        private int AsCodePoint(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLowSurrogate(text[i]))
                {
                    continue;
                }

                bool hasFourBytes = char.IsHighSurrogate(text[i]);
                int codePoint = hasFourBytes ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
                return codePoint;
            }
            return 0;
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Moq;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class GlyphTests
    {
        private readonly GlyphRenderer renderer = new GlyphRenderer();

        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            const string text = "A";
            CodePoint codePoint = this.AsCodePoint(text);
            var font = (FontMetrics)CreateFont(text).FontMetrics;
            var glyph = new Glyph(
                new GlyphMetrics(
                font,
                codePoint,
                new GlyphVector(new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1)),
                0,
                0,
                0,
                0,
                1,
                0),
                10);

            Vector2 locationInFontSpace = new Vector2(99, 99) / 72; // glyph ends up 10px over due to offset in fake glyph
            glyph.RenderTo(this.renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new FontRectangle(99, 89, 0, 0), this.renderer.GlyphRects.Single());
        }

        [Fact]
        public void IdenticalGlyphsInDifferentPlacesCreateIdenticalKeys()
        {
            Font fakeFont = CreateFont("AB");
            var textRenderer = new TextRenderer(this.renderer);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));

            Assert.Equal(this.renderer.GlyphKeys[0], this.renderer.GlyphKeys[2]);
            Assert.NotEqual(this.renderer.GlyphKeys[1], this.renderer.GlyphKeys[2]);
        }

        [Fact]
        public void BeginGlyph_ReturnsFalse_SkipRenderingFigures()
        {
            var renderer = new Mock<IGlyphRenderer>();
            renderer.Setup(x => x.BeginGlyph(It.IsAny<FontRectangle>(), It.IsAny<GlyphRendererParameters>())).Returns(false);
            Font fakeFont = CreateFont("A");
            var textRenderer = new TextRenderer(renderer.Object);

            textRenderer.RenderText("ABA", new RendererOptions(fakeFont));
            renderer.Verify(x => x.BeginFigure(), Times.Never);
        }

        [Fact]
        public void BeginGlyph_ReturnsTrue_RendersFigures()
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
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }

        [Fact]
        public void LoadGlyph()
        {
            Font font = new FontCollection().Add(TestFonts.SimpleFontFileData()).CreateFont(12);

            // Get letter A
            Glyph g = font.GetGlyph(new CodePoint(41));
            GlyphMetrics instance = g.GlyphMetrics;

            Assert.Equal(20, instance.ControlPoints.Length);
            Assert.Equal(20, instance.OnCurves.Length);
        }

        [Fact]
        public void RenderColrGlyph()
        {
            Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

            // Get letter Grinning Face emoji
            var instance = font.FontMetrics as FontMetrics;
            CodePoint codePoint = this.AsCodePoint("😀");
            Assert.True(instance.TryGetGlyphId(codePoint, out ushort idx));
            Assert.True(instance.TryGetColoredVectors(codePoint, idx, out GlyphMetrics[] vectors));

            Assert.Equal(3, vectors.Length);
        }

        [Fact]
        public void RenderColrGlyphTextRenderer()
        {
            Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

            var renderer = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(renderer, "😀", new RendererOptions(font)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.Equal(3, renderer.Colors.Count);
        }

#if NETCOREAPP3_0_OR_GREATER
        [Theory]
        [InlineData(false, 843)]
        [InlineData(true, 851)]
        public void RenderWoff2Glyphs_EqualToTtfGlyphs(bool applyKerning, int expectedControlPoints)
        {
            Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
            Font fontWoff2 = new FontCollection().Add(TestFonts.OpenSansFileWoff2).CreateFont(12);
            string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var rendererTtf = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(fontTtf)
            {
                ApplyKerning = applyKerning,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });
            var rendererWoff2 = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererWoff2, testStr, new RendererOptions(fontWoff2)
            {
                ApplyKerning = applyKerning,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.Equal(expectedControlPoints, rendererTtf.ControlPoints.Count);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff2.ControlPoints));
        }
#endif

        private CodePoint AsCodePoint(string text) => CodePoint.DecodeFromUtf16At(text.AsSpan(), 0);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
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
        private readonly GlyphRenderer renderer = new();

        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            const string text = "A";
            CodePoint codePoint = this.AsCodePoint(text);
            var font = (StreamFontMetrics)CreateFont(text).FontMetrics;
            var glyph = new Glyph(
                new GlyphMetrics(
                    font,
                    codePoint,
                    new GlyphVector(new Vector2[0], new bool[0], new ushort[0], new Bounds(0, font.UnitsPerEm, 0, font.UnitsPerEm), Array.Empty<byte>()),
                    0,
                    0,
                    0,
                    0,
                    0),
                10);

            Vector2 locationInFontSpace = new Vector2(99, 99) / 72; // glyph ends up 10px over due to offset in fake glyph
            glyph.RenderTo(this.renderer, locationInFontSpace, new RendererOptions(null, 72));

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
            Glyph g = font.GetGlyphs(new CodePoint(41), ColorFontSupport.None).First();
            GlyphOutline instance = g.GlyphMetrics.GetOutline();

            Assert.Equal(20, instance.ControlPoints.Length);
            Assert.Equal(20, instance.OnCurves.Length);
        }

        [Fact]
        public void RenderColrGlyph()
        {
            Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

            // Get letter Grinning Face emoji
            var instance = font.FontMetrics as StreamFontMetrics;
            CodePoint codePoint = this.AsCodePoint("😀");
            Assert.True(instance.TryGetGlyphId(codePoint, out ushort idx));
            IEnumerable<GlyphMetrics> vectors = instance.GetGlyphMetrics(codePoint, idx, ColorFontSupport.MicrosoftColrFormat);
            Assert.Equal(3, vectors.Count());
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

        [Fact]
        public void RenderWoffGlyphs_IsEqualToTtfGlyphs()
        {
            Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
            Font fontWoff = new FontCollection().Add(TestFonts.OpenSansFileWoff1).CreateFont(12);
            string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int expectedControlPointsCount = 1238;

            var rendererTtf = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(fontTtf)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });
            var rendererWoff = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererWoff, testStr, new RendererOptions(fontWoff)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.Equal(expectedControlPointsCount, rendererWoff.ControlPoints.Count);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff.ControlPoints));
        }

        [Theory]
        [InlineData("\uFB00")]
        [InlineData("\uFB01")]
        [InlineData("\uFB02")]
        [InlineData("\uFB03")]
        [InlineData("\uFB04")]
        public void RenderWoff_CompositeGlyphs_IsEqualToTtfGlyphs(string testStr)
        {
            Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
            Font fontWoff = new FontCollection().Add(TestFonts.OpenSansFileWoff1).CreateFont(12);

            var rendererTtf = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(fontTtf)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });
            var rendererWoff = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererWoff, testStr, new RendererOptions(fontWoff)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.True(rendererTtf.ControlPoints.Count > 0);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff.ControlPoints));
        }

#if NETCOREAPP3_0_OR_GREATER
        [Theory]
        [InlineData(false, 1238)]
        [InlineData(true, 1238)]
        public void RenderWoff2Glyphs_IsEqualToTtfGlyphs(bool applyKerning, int expectedControlPoints)
        {
            Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
            Font fontWoff2 = new FontCollection().Add(TestFonts.OpenSansFileWoff2).CreateFont(12);
            string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var rendererTtf = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(fontTtf)
            {
                KerningMode = applyKerning ? KerningMode.Normal : KerningMode.None,
                ApplyHinting = false,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });
            var rendererWoff2 = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererWoff2, testStr, new RendererOptions(fontWoff2)
            {
                KerningMode = applyKerning ? KerningMode.Normal : KerningMode.None,
                ApplyHinting = false,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.Equal(expectedControlPoints, rendererWoff2.ControlPoints.Count);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff2.ControlPoints));
        }

        [Theory]
        [InlineData("\uFB00")]
        [InlineData("\uFB01")]
        [InlineData("\uFB02")]
        [InlineData("\uFB03")]
        [InlineData("\uFB04")]
        public void RenderWoff2_CompositeGlyphs_IsEqualToTtfGlyphs(string testStr)
        {
            Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
            Font fontWoff2 = new FontCollection().Add(TestFonts.OpenSansFileWoff2).CreateFont(12);

            var rendererTtf = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(fontTtf)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });
            var rendererWoff2 = new ColorGlyphRenderer();
            TextRenderer.RenderTextTo(rendererWoff2, testStr, new RendererOptions(fontWoff2)
            {
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            Assert.True(rendererTtf.ControlPoints.Count > 0);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff2.ControlPoints));
        }
#endif

        private CodePoint AsCodePoint(string text) => CodePoint.DecodeFromUtf16At(text.AsSpan(), 0);
    }
}

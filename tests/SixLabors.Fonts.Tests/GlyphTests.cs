// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using Moq;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class GlyphTests
{
    private readonly GlyphRenderer renderer = new();

    [Fact]
    public void RenderToPointAndSingleDPI()
    {
        const string text = "A";
        CodePoint codePoint = this.AsCodePoint(text);
        Font font = CreateFont(text, 10);
        TextRun textRun = new() { Start = 0, End = 1, Font = font };

        FontMetrics metrics = font.FontMetrics;
        TrueTypeGlyphMetrics glyphMetrics = new(
            (StreamFontMetrics)metrics,
            0,
            codePoint,
            new GlyphVector(
                Array.Empty<ControlPoint>(),
                Array.Empty<ushort>(),
                new Bounds(0, metrics.UnitsPerEm, 0, metrics.UnitsPerEm),
                Array.Empty<byte>(),
                false),
            0,
            0,
            0,
            0,
            metrics.UnitsPerEm,
            textRun.TextAttributes,
            textRun.TextDecorations);

        Glyph glyph = new(glyphMetrics.CloneForRendering(textRun), font.Size);

        Vector2 locationInFontSpace = new Vector2(99, 99) / 72; // glyph ends up 10px over due to offset in fake glyph
        glyph.RenderTo(this.renderer, locationInFontSpace, Vector2.Zero, GlyphLayoutMode.Horizontal, new TextOptions(font));

        Assert.Equal(new FontRectangle(99, 89, 0, 0), this.renderer.GlyphRects.Single());
    }

    [Fact]
    public void IdenticalGlyphsInDifferentPlacesCreateIdenticalKeys()
    {
        Font fakeFont = CreateFont("AB");
        var textRenderer = new TextRenderer(this.renderer);

        textRenderer.RenderText("ABA", new TextOptions(fakeFont));

        Assert.Equal(this.renderer.GlyphKeys[0], this.renderer.GlyphKeys[2]);
        Assert.NotEqual(this.renderer.GlyphKeys[1], this.renderer.GlyphKeys[2]);
    }

    [Fact]
    public void BeginGlyph_ReturnsFalse_SkipRenderingFigures()
    {
        var renderer = new Mock<IGlyphRenderer>();
        renderer.Setup(x => x.BeginGlyph(It.Ref<FontRectangle>.IsAny, It.Ref<GlyphRendererParameters>.IsAny)).Returns(false);
        Font fakeFont = CreateFont("A");
        var textRenderer = new TextRenderer(renderer.Object);

        textRenderer.RenderText("ABA", new TextOptions(fakeFont));
        renderer.Verify(x => x.BeginFigure(), Times.Never);
    }

    [Fact]
    public void BeginGlyph_ReturnsTrue_RendersFigures()
    {
        var renderer = new Mock<IGlyphRenderer>();
        renderer.Setup(x => x.BeginGlyph(It.Ref<FontRectangle>.IsAny, It.Ref<GlyphRendererParameters>.IsAny)).Returns(true);
        Font fakeFont = CreateFont("A");
        var textRenderer = new TextRenderer(renderer.Object);

        textRenderer.RenderText("ABA", new TextOptions(fakeFont));
        renderer.Verify(x => x.BeginFigure(), Times.Exactly(3));
    }

    public static Font CreateFont(string text, float pointSize = 1)
    {
        var fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, pointSize);
    }

    [Fact]
    public void LoadGlyph()
    {
        Font font = new FontCollection().Add(TestFonts.SimpleFontFileData()).CreateFont(12);

        // Get letter A
        Assert.True(font.TryGetGlyphs(new CodePoint(41), ColorFontSupport.None, out IReadOnlyList<Glyph> glyphs));
        Glyph g = glyphs[0];
        GlyphVector instance = ((TrueTypeGlyphMetrics)g.GlyphMetrics).GetOutline();

        Assert.Equal(20, instance.ControlPoints.Count);
    }

    [Fact]
    public void RenderColrGlyph()
    {
        Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

        // Get letter Grinning Face emoji
        var instance = font.FontMetrics as StreamFontMetrics;
        CodePoint codePoint = this.AsCodePoint("😀");
        Assert.True(instance.TryGetGlyphId(codePoint, out ushort idx));
        IEnumerable<GlyphMetrics> vectors = instance.GetGlyphMetrics(
            codePoint,
            idx,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.MicrosoftColrFormat);

        Assert.Equal(3, vectors.Count());
    }

    [Fact]
    public void RenderColrGlyphTextRenderer()
    {
        Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

        var renderer = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(renderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });

        Assert.Equal(3, renderer.Colors.Count);
    }

    [Fact]
    public void RenderColrGlyphWithVariationSelector()
    {
        Font font = new FontCollection().Add(TestFonts.TwemojiMozillaData()).CreateFont(12);

        string text = "\u263A\uFE0F"; // Fully-qualified sequence for emoji 'smiling face'
        IReadOnlyList<GlyphLayout> layout = TextLayout.GenerateLayout(text.AsSpan(), new TextOptions(font));

        // Check that no glyphs were generated by the variation selector
        Assert.True(layout.All(v => v.Glyph.GlyphMetrics.CodePoint.Value == 0x263A));
        Assert.Equal(4, layout.Count);
    }

    [Fact]
    public void EmojiWidthIsComputedCorrectlyWithSubstitutionOnZwj()
    {
        Font font = new FontCollection().Add(TestFonts.SegoeuiEmojiData()).CreateFont(72);

        string text = "\U0001F469\U0001F3FB\u200D\U0001F91D\u200D\U0001F469\U0001F3FC"; // women holding hands: light skin tone, medium-light skin tone
        string text2 = "\U0001F46D\U0001F3FB"; // women holding hands: light skin tone

        FontRectangle size = TextMeasurer.MeasureSize(text, new TextOptions(font));
        FontRectangle size2 = TextMeasurer.MeasureSize(text2, new TextOptions(font));

        Assert.Equal(75F, size.Width);
        Assert.Equal(75F, size2.Width);
    }

    [Theory]
    [InlineData(false, false, 1238)]
    [InlineData(false, true, 1238)]
    [InlineData(true, false, 1238)]
    [InlineData(true, true, 1238)]
    public void RenderWoffGlyphs_IsEqualToTtfGlyphs(bool applyKerning, bool applyHinting, int expectedControlPoint)
    {
        Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
        Font fontWoff = new FontCollection().Add(TestFonts.OpenSansFileWoff1).CreateFont(12);
        string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var rendererTtf = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });
        var rendererWoff = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererWoff, testStr, new TextOptions(fontWoff)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });

        Assert.Equal(expectedControlPoint, rendererWoff.ControlPoints.Count);
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
        TextRenderer.RenderTextTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });
        var rendererWoff = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererWoff, testStr, new TextOptions(fontWoff)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });

        Assert.True(rendererTtf.ControlPoints.Count > 0);
        Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff.ControlPoints));
    }

#if NETCOREAPP3_0_OR_GREATER
    [Theory]
    [InlineData(false, false, 1238)]
    [InlineData(false, true, 1238)]
    [InlineData(true, false, 1238)]
    [InlineData(true, true, 1238)]
    public void RenderWoff2Glyphs_IsEqualToTtfGlyphs(bool applyKerning, bool applyHinting, int expectedControlPoints)
    {
        Font fontTtf = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);
        Font fontWoff2 = new FontCollection().Add(TestFonts.OpenSansFileWoff2).CreateFont(12);
        string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var rendererTtf = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });
        var rendererWoff2 = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererWoff2, testStr, new TextOptions(fontWoff2)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
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
        TextRenderer.RenderTextTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });
        var rendererWoff2 = new ColorGlyphRenderer();
        TextRenderer.RenderTextTo(rendererWoff2, testStr, new TextOptions(fontWoff2)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
        });

        Assert.True(rendererTtf.ControlPoints.Count > 0);
        Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff2.ControlPoints));
    }
#endif

#if OS_WINDOWS
    [Theory]
    [InlineData("Arial")]
    [InlineData("Segoe UI Emoji")]
    public void RendererIsThreadsafe(string fontName)
    {
        const int threadCount = 10;
        Parallel.For(0, threadCount, _ =>
        {
            ColorGlyphRenderer renderer1 = new();
            TextRenderer.RenderTextTo(renderer1, "A 🙂 ", new TextOptions(SystemFonts.CreateFont(fontName, 15)));

            ColorGlyphRenderer renderer2 = new();
            TextRenderer.RenderTextTo(renderer2, "A 🙂 ", new TextOptions(SystemFonts.CreateFont(fontName, 15)));

            Assert.True(renderer1.ControlPoints.Count > 0);
            Assert.True(renderer2.ControlPoints.Count > 0);
            Assert.True(renderer1.ControlPoints.SequenceEqual(renderer2.ControlPoints));
        });
    }

#endif
    private CodePoint AsCodePoint(string text) => CodePoint.DecodeFromUtf16At(text.AsSpan(), 0);
}

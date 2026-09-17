// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using Moq;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class GlyphTests
{
    private readonly GlyphRenderer renderer = new();
    private static readonly ApproximateFloatComparer Comparer = new(.1F);

    [Fact]
    public void RenderToPointAndSingleDPI()
    {
        const string text = "A";
        CodePoint codePoint = AsCodePoint(text);
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
            textRun.TextDecorations,
            GlyphType.Standard);

        Glyph glyph = new(glyphMetrics, font.Size, textRun, Vector2.Zero, new Vector2(glyphMetrics.AdvanceWidth, glyphMetrics.AdvanceHeight));

        Vector2 locationInFontSpace = new Vector2(99, 99) / 72; // glyph ends up 10px over due to offset in fake glyph
        glyph.RenderTo(this.renderer, 0, locationInFontSpace, Vector2.Zero, new Vector2(-1F), GlyphLayoutMode.Horizontal, new TextOptions(font));

        Assert.Equal(new FontRectangle(99, 89, 0, 0), this.renderer.GlyphRects.Single());
    }

    [Fact]
    public void IdenticalGlyphsInDifferentPlacesCreateDifferentKeys()
    {
        Font fakeFont = CreateFont("AB");
        TextRenderer textRenderer = new(this.renderer);

        textRenderer.Render("ABA", new TextOptions(fakeFont));

        Assert.NotEqual(this.renderer.GlyphKeys[0], this.renderer.GlyphKeys[2]);
        Assert.NotEqual(this.renderer.GlyphKeys[1], this.renderer.GlyphKeys[2]);
    }

    [Fact]
    public void BeginGlyph_ReturnsFalse_SkipRenderingFigures()
    {
        Mock<IGlyphRenderer> renderer = new();
        renderer.Setup(x => x.BeginGlyph(It.Ref<FontRectangle>.IsAny, It.Ref<GlyphRendererParameters>.IsAny)).Returns(false);
        Font fakeFont = CreateFont("A");
        TextRenderer textRenderer = new(renderer.Object);

        textRenderer.Render("ABA", new TextOptions(fakeFont));
        renderer.Verify(x => x.BeginFigure(), Times.Never);
    }

    [Fact]
    public void BeginGlyph_ReturnsTrue_RendersFigures()
    {
        Mock<IGlyphRenderer> renderer = new();
        renderer.Setup(x => x.BeginGlyph(It.Ref<FontRectangle>.IsAny, It.Ref<GlyphRendererParameters>.IsAny)).Returns(true);
        Font fakeFont = CreateFont("A");
        TextRenderer textRenderer = new(renderer.Object);

        textRenderer.Render("ABA", new TextOptions(fakeFont));
        renderer.Verify(x => x.BeginFigure(), Times.Exactly(3));
    }

    public static Font CreateFont(string text, float pointSize = 1)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, pointSize);
    }

    [Fact]
    public void LoadGlyph()
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);

        // Get letter A
        Assert.True(font.TryGetGlyphs(new CodePoint(0x41), ColorFontSupport.None, out Glyph? glyph));
        GlyphVector instance = ((TrueTypeGlyphMetrics)glyph.Value.GlyphMetrics).GetOutline();

        Assert.Equal(6, instance.ControlPoints.Count);
    }

    [Fact]
    public void RenderColrGlyphTextRenderer()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);

        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        Assert.Equal(3, renderer.Colors.Count);
    }

    [Fact]
    public void RenderColrGlyphTextRunColorFontSupportOverridesOptions()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);

        // The run disables color for the first emoji only; the second keeps the
        // options value and contributes the three COLR v0 layer colors.
        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "😀😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0,
            TextRuns = [new TextRun { Start = 0, End = 1, ColorFontSupport = ColorFontSupport.None }]
        });

        Assert.Equal(2, renderer.GlyphKeys.Count);
        Assert.Equal(3, renderer.Colors.Count);
    }

    [Fact]
    public void RenderColrGlyphWithVariationSelector()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 72);

        const string text = "\u263A\uFE0F"; // Fully-qualified sequence for emoji 'smiling face'

        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, text, new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        // Check that no glyphs were generated by the variation selector
        Assert.Single(renderer.GlyphKeys);
        Assert.Equal(0x263A, renderer.GlyphKeys[0].CodePoint.Value);
        Assert.Equal(4, renderer.Colors.Count);
    }

    [Fact]
    public void EmojiWidthIsComputedCorrectlyWithSubstitutionOnZwj()
    {
        Font font = TestFonts.GetFont(TestFonts.SegoeuiEmojiFile, 72);

        const string text = "\U0001F469\U0001F3FB\u200D\U0001F91D\u200D\U0001F469\U0001F3FC"; // women holding hands: light skin tone, medium-light skin tone
        const string text2 = "\U0001F46D\U0001F3FB"; // women holding hands: light skin tone

        FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font));
        FontRectangle size2 = TextMeasurer.MeasureBounds(text2, new TextOptions(font));

        Assert.Equal(50.625F, size.Width, Comparer);
        Assert.Equal(50.555F, size2.Width, Comparer);
    }

    [Theory]
    [InlineData(false, false, 719)]
    [InlineData(false, true, 719)]
    [InlineData(true, false, 719)]
    [InlineData(true, true, 719)]
    public void RenderWoffGlyphs_IsEqualToTtfGlyphs(bool applyKerning, bool applyHinting, int expectedControlPoint)
    {
        Font fontTtf = TestFonts.GetFont(TestFonts.OpenSansFile, 12);
        Font fontWoff = TestFonts.GetFont(TestFonts.OpenSansFileWoff1, 12);
        string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        ColorGlyphRenderer rendererTtf = new();
        TextRenderer.RenderTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
        });
        ColorGlyphRenderer rendererWoff = new();
        TextRenderer.RenderTo(rendererWoff, testStr, new TextOptions(fontWoff)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
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
        Font fontTtf = TestFonts.GetFont(TestFonts.OpenSansFile, 12);
        Font fontWoff = TestFonts.GetFont(TestFonts.OpenSansFileWoff1, 12);

        ColorGlyphRenderer rendererTtf = new();
        TextRenderer.RenderTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.ColrV0
        });
        ColorGlyphRenderer rendererWoff = new();
        TextRenderer.RenderTo(rendererWoff, testStr, new TextOptions(fontWoff)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        Assert.True(rendererTtf.ControlPoints.Count > 0);
        Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff.ControlPoints));
    }

    [Theory]
    [InlineData(false, false, 719)]
    [InlineData(false, true, 719)]
    [InlineData(true, false, 719)]
    [InlineData(true, true, 719)]
    public void RenderWoff2Glyphs_IsEqualToTtfGlyphs(bool applyKerning, bool applyHinting, int expectedControlPoints)
    {
        Font fontTtf = TestFonts.GetFont(TestFonts.OpenSansFile, 12);
        Font fontWoff2 = TestFonts.GetFont(TestFonts.OpenSansFileWoff2, 12);
        string testStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        ColorGlyphRenderer rendererTtf = new();
        TextRenderer.RenderTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
        });
        ColorGlyphRenderer rendererWoff2 = new();
        TextRenderer.RenderTo(rendererWoff2, testStr, new TextOptions(fontWoff2)
        {
            KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
            HintingMode = applyHinting ? HintingMode.Standard : HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
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
        Font fontTtf = TestFonts.GetFont(TestFonts.OpenSansFile, 12);
        Font fontWoff2 = TestFonts.GetFont(TestFonts.OpenSansFileWoff2, 12);

        ColorGlyphRenderer rendererTtf = new();
        TextRenderer.RenderTo(rendererTtf, testStr, new TextOptions(fontTtf)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.ColrV0
        });
        ColorGlyphRenderer rendererWoff2 = new();
        TextRenderer.RenderTo(rendererWoff2, testStr, new TextOptions(fontWoff2)
        {
            HintingMode = HintingMode.Standard,
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        Assert.True(rendererTtf.ControlPoints.Count > 0);
        Assert.True(rendererTtf.ControlPoints.SequenceEqual(rendererWoff2.ControlPoints));
    }

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
            TextRenderer.RenderTo(renderer1, "A 🙂 ", new TextOptions(SystemFonts.CreateFont(fontName, 15)));

            ColorGlyphRenderer renderer2 = new();
            TextRenderer.RenderTo(renderer2, "A 🙂 ", new TextOptions(SystemFonts.CreateFont(fontName, 15)));

            Assert.True(renderer1.ControlPoints.Count > 0);
            Assert.True(renderer2.ControlPoints.Count > 0);
            Assert.True(renderer1.ControlPoints.SequenceEqual(renderer2.ControlPoints));
        });
    }

#endif
    [Fact]
    public void RenderFamilySequence_SegoeUIEmoji170_ColrV0AndColrV1ShareGlyphs()
    {
        // Segoe UI Emoji 1.70 carries COLR v1 paint graphs and COLR v0 layer records in one
        // table and builds the family from four positioned glyphs. HarfBuzz shapes the
        // sequence to these glyph ids; the differential test checks their positions.
        Font font = TestFonts.GetFont(TestFonts.SegoeuiEmoji170File, 72);
        const string family = "👨‍👩‍👧‍👦";
        ushort[] expectedGlyphIds = [1283, 1464, 1271, 1259];

        PaintCaptureRenderer outlines = RenderFamily(font, family, ColorFontSupport.None);
        PaintCaptureRenderer colrV0 = RenderFamily(font, family, ColorFontSupport.ColrV0);
        PaintCaptureRenderer colrV1 = RenderFamily(font, family, ColorFontSupport.ColrV1);

        foreach (PaintCaptureRenderer renderer in new[] { outlines, colrV0, colrV1 })
        {
            Assert.Equal(expectedGlyphIds, renderer.VisibleKeys.Select(k => k.GlyphId));
        }

        Assert.All(outlines.VisibleKeys, k => Assert.Equal(GlyphType.Standard, k.GlyphType));
        Assert.Equal(0, outlines.SolidLayers + outlines.GradientLayers);

        Assert.All(colrV0.VisibleKeys, k => Assert.Equal(GlyphType.Painted, k.GlyphType));
        Assert.True(colrV0.SolidLayers > 0);
        Assert.Equal(0, colrV0.GradientLayers);

        Assert.All(colrV1.VisibleKeys, k => Assert.Equal(GlyphType.Painted, k.GlyphType));
        Assert.True(colrV1.GradientLayers > 0);
    }

    [Fact]
    public void RenderThumbsUp_SegoeUIEmoji170_RadialGradientsKeepTheirWholeTransform()
    {
        // The shading gradients of the toned thumbs-up sit under PaintTransform and PaintScale
        // records that squash, skew and reflect them. The circles stay in the paint's own space
        // and the whole transform reaches the renderer, so none of that is lost.
        Font font = TestFonts.GetFont(TestFonts.SegoeuiEmoji170File, 72);
        PaintCaptureRenderer renderer = RenderFamily(font, "👍🏽", ColorFontSupport.ColrV1);

        RadialGradientPaint[] radials = renderer.Paints.OfType<RadialGradientPaint>().ToArray();
        Assert.NotEmpty(radials);
        Assert.All(radials, r => Assert.True(Matrix3x2.Invert(r.Transform, out _)));

        // A similarity maps the axes to perpendicular vectors of equal length; the squashed
        // shading gradients do not.
        Assert.Contains(radials, r => MathF.Abs((r.Transform.M11 * r.Transform.M21) + (r.Transform.M12 * r.Transform.M22)) > 0.01F
            || MathF.Abs(new Vector2(r.Transform.M11, r.Transform.M12).Length() - new Vector2(r.Transform.M21, r.Transform.M22).Length()) > 0.01F);

        // PaintScale with a negative x scale reflects the gradient.
        Assert.Contains(radials, r => r.Transform.GetDeterminant() < 0F);
    }

    private static PaintCaptureRenderer RenderFamily(Font font, string text, ColorFontSupport colorFontSupport)
    {
        PaintCaptureRenderer renderer = new();
        TextRenderer.RenderTo(renderer, text, new TextOptions(font)
        {
            ColorFontSupport = colorFontSupport
        });

        return renderer;
    }

    private static CodePoint AsCodePoint(string text) => CodePoint.DecodeFromUtf16At(text.AsSpan(), 0);

    /// <summary>
    /// Counts solid and gradient paint layers and exposes the glyph keys of the glyphs that
    /// draw something, leaving out the zero width joiners.
    /// </summary>
    private sealed class PaintCaptureRenderer : GlyphRenderer
    {
        public int SolidLayers { get; private set; }

        public int GradientLayers { get; private set; }

        public List<Paint> Paints { get; } = [];

        public GlyphRendererParameters[] VisibleKeys
            => this.GlyphKeys.Where(k => !CodePoint.IsZeroWidthJoiner(k.CodePoint)).ToArray();

        public override void BeginLayer(Paint paint, FillRule fillRule)
        {
            this.Paints.Add(paint);
            if (paint is SolidPaint)
            {
                this.SolidLayers++;
            }
            else
            {
                this.GradientLayers++;
            }

            base.BeginLayer(paint, fillRule);
        }
    }
}

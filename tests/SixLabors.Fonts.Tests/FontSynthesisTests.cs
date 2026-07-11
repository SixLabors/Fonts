// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class FontSynthesisTests
{
    // Chromium and SkParagraph pass an exact quarter shear to SkFont for synthesized italics.
    private const float ExpectedSkew = 1F / 4F;

    // A divisor of 31 matches Chromium's DirectWrite weight while retaining FreeType dilation.
    private const float ExpectedBoldEmScale = 1F / 31F;

    private static readonly ApproximateFloatComparer SkewComparer = new(0.001F);

    private static readonly ApproximateFloatComparer GeometryComparer = new(0.1F);

    private static FontFamily RegularOnlyFamily(string file)
        => new FontCollection().Add(file);

    private static TextOptions PaintedEmojiOptions(FontStyle style)
    {
        FontCollection collection = new();
        FontFamily emoji = collection.Add(TestFonts.TwemojiMozillaFile);
        FontFamily fallback = collection.Add(TestFonts.OpenSansFile);

        // Twemoji does not contain U+0020, so use the same explicit text fallback as the browser
        // comparison instead of measuring the full-em missing-glyph advance between emoji.
        return new TextOptions(new Font(emoji, 54, style))
        {
            Dpi = 96F,
            LineSpacing = 1.4F,
            ColorFontSupport = ColorFontSupport.ColrV0,
            FallbackFontFamilies = [fallback]
        };
    }

    [Fact]
    public void FamilyWithoutItalic_FallsBackToRegularMetrics()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.OpenSansFile);
        Font italic = new(family, 24, FontStyle.Italic);

        // Italic was requested but the resolved face is still the regular one.
        Assert.Equal(FontStyle.Italic, italic.RequestedStyle);
        Assert.False(italic.IsItalic);
    }

    [Fact]
    public void GetObliqueSkew_TrueType_NonZeroOnlyWhenItalicSynthesized()
        => AssertObliqueSkew(TestFonts.OpenSansFile);

    [Fact]
    public void GetObliqueSkew_Cff_NonZeroOnlyWhenItalicSynthesized()
        => AssertObliqueSkew(TestFonts.PlantinStdRegularFile);

    [Fact]
    public void SyntheticItalic_ShearsGlyphOutline_TrueType()
        => AssertSyntheticItalicShear(TestFonts.OpenSansFile, "Hg", ColorFontSupport.None);

    [Fact]
    public void SyntheticItalic_ShearsGlyphOutline_Cff()
        => AssertSyntheticItalicShear(TestFonts.PlantinStdRegularFile, "Hg", ColorFontSupport.None);

    [Fact]
    public void SyntheticItalic_ShearsPaintedEmoji()
        => AssertSyntheticItalicShear(TestFonts.TwemojiMozillaFile, "😀", ColorFontSupport.ColrV0);

    [Fact]
    public void SyntheticItalic_PaintedEmojiBoundsContainRenderedGeometry()
    {
        const string text = "😀 ☺️ ❤️ ✌️ ⭐";
        const float tolerance = .1F;
        TextOptions options = PaintedEmojiOptions(FontStyle.Italic);
        options.HintingMode = HintingMode.None;

        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, text, options);

        Assert.NotEmpty(renderer.ControlPoints);
        foreach (Vector2 point in renderer.ControlPoints)
        {
            Assert.InRange(point.X, bounds.Left - tolerance, bounds.Right + tolerance);
            Assert.InRange(point.Y, bounds.Top - tolerance, bounds.Bottom + tolerance);
        }
    }

    [Fact]
    public void BoundsTransform_UsesAllFourCorners()
    {
        Bounds bounds = new(0F, 0F, 10F, 20F);
        Bounds transformed = Bounds.Transform(bounds, Matrix3x2.CreateRotation(MathF.PI / 4F));

        Assert.Equal(-14.142136F, transformed.Min.X, GeometryComparer);
        Assert.Equal(0F, transformed.Min.Y, GeometryComparer);
        Assert.Equal(7.071068F, transformed.Max.X, GeometryComparer);
        Assert.Equal(21.213203F, transformed.Max.Y, GeometryComparer);
    }

    [Fact]
    public void VisualTest_SyntheticItalicPaintedEmoji()
    {
        TextOptions options = PaintedEmojiOptions(FontStyle.Italic);

        // Painted emoji retain their authored layers while the complete layered outline receives
        // the same synthetic italic transform as monochrome glyphs.
        TextLayoutTestUtilities.TestLayout("😀 ☺️ ❤️ ✌️ ⭐", options, properties: "COLRv0");
    }

    [Theory]
    [InlineData("OpenSans-Regular.ttf", "TrueType")]
    [InlineData("PlantinStdRegular.otf", "CFF")]
    public void VisualTest_SyntheticItalic(string fontFile, string fontFormat)
    {
        const string text = "Sphinx of black quartz, judge my vow.\nPack my box with five dozen liquor jugs.";
        string path = Path.Combine(TestEnvironment.FontTestDataFullPath, fontFile);
        Font font = new(RegularOnlyFamily(path), 36, FontStyle.Italic);
        TextOptions options = new(font)
        {
            Dpi = 96F,
            LineSpacing = 1.4F
        };

        // The collection contains only the regular face, so requesting italic exercises the
        // synthesized outline while using the standard rendering-test output and comparison path.
        TextLayoutTestUtilities.TestLayout(text, options, properties: fontFormat);
    }

    [Fact]
    public void SyntheticItalic_DoesNotChangeAdvance_ButWidensBounds()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.OpenSansFile);
        Font regular = new(family, 48, FontStyle.Regular);
        Font italic = new(family, 48, FontStyle.Italic);

        const string text = "Hg";

        // The advance width is a layout metric and must be unchanged by synthesis so that
        // spacing continues to match a browser rendering the same regular face.
        FontRectangle regularAdvance = TextMeasurer.MeasureAdvance(text, new TextOptions(regular));
        FontRectangle italicAdvance = TextMeasurer.MeasureAdvance(text, new TextOptions(italic));
        Assert.Equal(regularAdvance.Width, italicAdvance.Width, SkewComparer);

        // The rendered ink bounds however widen because the outline is sheared.
        FontRectangle regularBounds = TextMeasurer.MeasureBounds(text, new TextOptions(regular));
        FontRectangle italicBounds = TextMeasurer.MeasureBounds(text, new TextOptions(italic));
        Assert.True(italicBounds.Width > regularBounds.Width);
    }

    [Fact]
    public void GetObliqueSkew_ReturnsZero_WhenGlyphHasNoTextRun()
    {
        // The metrics returned directly from the font (i.e. not cloned for a specific run)
        // carry no text run, so synthesis cannot be determined and must be disabled.
        Font italic = new(RegularOnlyFamily(TestFonts.OpenSansFile), 24, FontStyle.Italic);

        Assert.True(italic.FontMetrics.TryGetGlyphMetrics(
            new CodePoint('H'),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics metrics));

        Assert.Equal(0F, metrics.GetObliqueSkew());
    }

    private static void AssertObliqueSkew(string file)
    {
        FontFamily family = RegularOnlyFamily(file);
        CodePoint codePoint = new('H');

        Font regular = new(family, 24, FontStyle.Regular);
        Font italic = new(family, 24, FontStyle.Italic);

        Assert.True(regular.TryGetGlyphs(codePoint, out Glyph? regularGlyph));
        Assert.True(italic.TryGetGlyphs(codePoint, out Glyph? italicGlyph));

        Assert.Equal(0F, regularGlyph.Value.GlyphMetrics.GetObliqueSkew());
        Assert.Equal(ExpectedSkew, italicGlyph.Value.GlyphMetrics.GetObliqueSkew(), SkewComparer);
    }

    private static void AssertSyntheticItalicShear(string file, string text, ColorFontSupport colorFontSupport)
    {
        FontFamily family = RegularOnlyFamily(file);
        Font regular = new(family, 48, FontStyle.Regular);
        Font italic = new(family, 48, FontStyle.Italic);

        GlyphRenderer regularRenderer = new();
        TextRenderer.RenderTo(regularRenderer, text, new TextOptions(regular)
        {
            HintingMode = HintingMode.None,
            ColorFontSupport = colorFontSupport
        });

        GlyphRenderer italicRenderer = new();
        TextRenderer.RenderTo(italicRenderer, text, new TextOptions(italic)
        {
            HintingMode = HintingMode.None,
            ColorFontSupport = colorFontSupport
        });

        List<Vector2> r = regularRenderer.ControlPoints;
        List<Vector2> i = italicRenderer.ControlPoints;

        Assert.NotEmpty(r);
        Assert.Equal(r.Count, i.Count);

        // The synthesized slant is a pure horizontal shear applied in the glyph's Y-up space.
        // In device space (Y-down) this means the Y coordinate of every point is preserved and
        // the horizontal displacement satisfies xi - xr = skew * (originY - yr), so the quantity
        // (xi - xr) + skew * yr is constant across every emitted control point.
        float invariant = (i[0].X - r[0].X) + (ExpectedSkew * r[0].Y);
        float maxHorizontalShift = 0F;
        for (int p = 0; p < r.Count; p++)
        {
            Assert.Equal(r[p].Y, i[p].Y, GeometryComparer);
            Assert.Equal(invariant, (i[p].X - r[p].X) + (ExpectedSkew * r[p].Y), GeometryComparer);
            maxHorizontalShift = MathF.Max(maxHorizontalShift, MathF.Abs(i[p].X - r[p].X));
        }

        // Ensure the glyph was actually slanted and not left upright.
        Assert.True(maxHorizontalShift > 1F);
    }

    [Fact]
    public void FamilyWithoutBold_FallsBackToRegularMetrics()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.OpenSansFile);
        Font bold = new(family, 24, FontStyle.Bold);

        // Bold was requested but the resolved face is still the regular one.
        Assert.Equal(FontStyle.Bold, bold.RequestedStyle);
        Assert.False(bold.IsBold);
    }

    [Fact]
    public void ShouldSynthesizeBold_TrueType_TrueOnlyWhenBoldSynthesized()
        => AssertShouldSynthesizeBold(TestFonts.OpenSansFile);

    [Fact]
    public void ShouldSynthesizeBold_Cff_TrueOnlyWhenBoldSynthesized()
        => AssertShouldSynthesizeBold(TestFonts.PlantinStdRegularFile);

    [Fact]
    public void SyntheticBoldStrength_MatchesBrowserWeight()
    {
        const float pointSize = 48F;
        Font bold = new(RegularOnlyFamily(TestFonts.OpenSansFile), pointSize, FontStyle.Bold);

        Assert.True(bold.TryGetGlyphs(new CodePoint('H'), out Glyph? glyph));

        // GetSyntheticBoldStrength receives point size multiplied by DPI. At the default 72 DPI,
        // the scale factors cancel to leave the browser-matched emboldening fraction times point size.
        float strength = glyph.Value.GlyphMetrics.GetSyntheticBoldStrength(pointSize * 72F);
        Assert.Equal(pointSize * ExpectedBoldEmScale, strength, SkewComparer);
    }

    [Theory]
    [InlineData("OpenSans-Regular.ttf", "TrueType")]
    [InlineData("PlantinStdRegular.otf", "CFF")]
    public void VisualTest_SyntheticBold(string fontFile, string fontFormat)
    {
        const string text = "Sphinx of black quartz, judge my vow.\nPack my box with five dozen liquor jugs.";
        string path = Path.Combine(TestEnvironment.FontTestDataFullPath, fontFile);
        Font font = new(RegularOnlyFamily(path), 36, FontStyle.Bold);
        TextOptions options = new(font)
        {
            Dpi = 96F,
            LineSpacing = 1.4F
        };

        // The collection contains only the regular face, so requesting bold exercises the
        // synthesized outline while using the standard rendering-test output and comparison path.
        TextLayoutTestUtilities.TestLayout(text, options, properties: fontFormat);
    }

    [Fact]
    public void SyntheticBold_DilatesGlyphOutline_TrueType()
        => AssertSyntheticBoldGrows(TestFonts.OpenSansFile, "H");

    [Fact]
    public void SyntheticBold_DilatesGlyphOutline_Cff()
        => AssertSyntheticBoldGrows(TestFonts.PlantinStdRegularFile, "H");

    [Fact]
    public void SyntheticBold_PreservesOutlineSegments_TrueType()
        => AssertSyntheticBoldPreservesSegments(TestFonts.OpenSansFile, "Hg");

    [Fact]
    public void SyntheticBold_PreservesOutlineSegments_Cff()
        => AssertSyntheticBoldPreservesSegments(TestFonts.PlantinStdRegularFile, "Hg");

    [Fact]
    public void SyntheticBold_DoesNotChangeAdvance_ButWidensBounds()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.OpenSansFile);
        Font regular = new(family, 48, FontStyle.Regular);
        Font bold = new(family, 48, FontStyle.Bold);

        const string text = "Hg";

        // Browsers (verified against Chrome/Blink) keep the faux-bold advance identical to the
        // regular advance - they thicken the stems in place rather than widening the advance - so
        // our advance must stay unchanged to match a browser rendering the same regular face.
        FontRectangle regularAdvance = TextMeasurer.MeasureAdvance(text, new TextOptions(regular));
        FontRectangle boldAdvance = TextMeasurer.MeasureAdvance(text, new TextOptions(bold));
        Assert.Equal(regularAdvance.Width, boldAdvance.Width, SkewComparer);

        // The rendered ink bounds however grow because the outline is dilated.
        FontRectangle regularBounds = TextMeasurer.MeasureBounds(text, new TextOptions(regular));
        FontRectangle boldBounds = TextMeasurer.MeasureBounds(text, new TextOptions(bold));
        Assert.True(boldBounds.Width > regularBounds.Width);
        Assert.True(boldBounds.Height > regularBounds.Height);
    }

    [Fact]
    public void SyntheticBoldItalic_CombinesBothSyntheses()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.OpenSansFile);
        CodePoint codePoint = new('H');
        Font boldItalic = new(family, 24, FontStyle.BoldItalic);

        Assert.True(boldItalic.TryGetGlyphs(codePoint, out Glyph? glyph));

        // Both syntheses are driven independently, so requesting bold italic on a regular-only
        // family must enable both the shear and the outline dilation.
        Assert.Equal(ExpectedSkew, glyph.Value.GlyphMetrics.GetObliqueSkew(), SkewComparer);
        Assert.True(glyph.Value.GlyphMetrics.ShouldSynthesizeBold());
    }

    [Fact]
    public void SyntheticBold_DoesNotEmboldenPaintedEmoji()
    {
        FontFamily family = RegularOnlyFamily(TestFonts.TwemojiMozillaFile);
        Font regular = new(family, 48, FontStyle.Regular);
        Font bold = new(family, 48, FontStyle.Bold);
        CodePoint codePoint = new(0x1F600);

        Assert.True(bold.TryGetGlyphs(codePoint, ColorFontSupport.ColrV0, out Glyph? glyph));
        Assert.Equal(GlyphType.Painted, glyph.Value.GlyphMetrics.GlyphType);
        Assert.False(glyph.Value.GlyphMetrics.ShouldSynthesizeBold());

        TextOptions regularOptions = new(regular)
        {
            HintingMode = HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
        };
        TextOptions boldOptions = new(bold)
        {
            HintingMode = HintingMode.None,
            ColorFontSupport = ColorFontSupport.ColrV0
        };

        GlyphRenderer regularRenderer = new();
        TextRenderer.RenderTo(regularRenderer, "😀", regularOptions);

        GlyphRenderer boldRenderer = new();
        TextRenderer.RenderTo(boldRenderer, "😀", boldOptions);

        // Browsers leave authored color layers unchanged for bold text.
        Assert.Equal(regularRenderer.ControlPoints, boldRenderer.ControlPoints);
        Assert.Equal(regularRenderer.GlyphRects, boldRenderer.GlyphRects);
    }

    [Fact]
    public void VisualTest_SyntheticBoldPaintedEmoji()
    {
        TextOptions options = PaintedEmojiOptions(FontStyle.Bold);

        // A bold request must preserve the authored color geometry because browsers do not apply
        // synthetic emboldening to painted emoji.
        TextLayoutTestUtilities.TestLayout("😀 ☺️ ❤️ ✌️ ⭐", options, properties: "COLRv0");
    }

    [Fact]
    public void ShouldSynthesizeBold_ReturnsFalse_WhenGlyphHasNoTextRun()
    {
        // The metrics returned directly from the font (i.e. not cloned for a specific run)
        // carry no text run, so synthesis cannot be determined and must be disabled.
        Font bold = new(RegularOnlyFamily(TestFonts.OpenSansFile), 24, FontStyle.Bold);

        Assert.True(bold.FontMetrics.TryGetGlyphMetrics(
            new CodePoint('H'),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics metrics));

        Assert.False(metrics.ShouldSynthesizeBold());
    }

    private static void AssertShouldSynthesizeBold(string file)
    {
        FontFamily family = RegularOnlyFamily(file);
        CodePoint codePoint = new('H');

        Font regular = new(family, 24, FontStyle.Regular);
        Font bold = new(family, 24, FontStyle.Bold);

        Assert.True(regular.TryGetGlyphs(codePoint, out Glyph? regularGlyph));
        Assert.True(bold.TryGetGlyphs(codePoint, out Glyph? boldGlyph));

        Assert.False(regularGlyph.Value.GlyphMetrics.ShouldSynthesizeBold());
        Assert.True(boldGlyph.Value.GlyphMetrics.ShouldSynthesizeBold());
    }

    private static void AssertSyntheticBoldGrows(string file, string text)
    {
        FontFamily family = RegularOnlyFamily(file);
        Font regular = new(family, 48, FontStyle.Regular);
        Font bold = new(family, 48, FontStyle.Bold);

        // The dilated outline must enclose the original one, so the rendered ink extents must
        // grow outward. Note the top edge is pinned to the layout ascender, so growth is asserted
        // via the overall width and height together with the left, right and bottom ink extents.
        FontRectangle regularBounds = TextMeasurer.MeasureBounds(text, new TextOptions(regular));
        FontRectangle boldBounds = TextMeasurer.MeasureBounds(text, new TextOptions(bold));

        Assert.True(boldBounds.Width > regularBounds.Width);
        Assert.True(boldBounds.Height > regularBounds.Height);
        Assert.True(boldBounds.Left < regularBounds.Left);
        Assert.True(boldBounds.Right > regularBounds.Right);
        Assert.True(boldBounds.Bottom > regularBounds.Bottom);
    }

    private static void AssertSyntheticBoldPreservesSegments(string file, string text)
    {
        FontFamily family = RegularOnlyFamily(file);
        Font regular = new(family, 48, FontStyle.Regular);
        Font bold = new(family, 48, FontStyle.Bold);

        GlyphRenderer regularRenderer = new();
        TextRenderer.RenderTo(regularRenderer, text, new TextOptions(regular) { HintingMode = HintingMode.None });

        GlyphRenderer boldRenderer = new();
        TextRenderer.RenderTo(boldRenderer, text, new TextOptions(bold) { HintingMode = HintingMode.None });

        List<Vector2> r = regularRenderer.ControlPoints;
        List<Vector2> b = boldRenderer.ControlPoints;

        // Emboldening only offsets existing points; it never adds or removes outline segments.
        Assert.NotEmpty(r);
        Assert.Equal(r.Count, b.Count);

        // Every point moves by at most a small fraction of the em, confirming a dilation rather
        // than an arbitrary distortion of the outline.
        float maxShift = 0F;
        for (int p = 0; p < r.Count; p++)
        {
            maxShift = MathF.Max(maxShift, Vector2.Distance(r[p], b[p]));
        }

        Assert.True(maxShift > 0.1F);
        Assert.True(maxShift < 48F * 0.1F);
    }

    [Fact]
    public void EmboldeningRenderer_ForwardsCallsAndDilatesOutline()
    {
        RecordingGlyphRenderer inner = new();
        EmboldeningGlyphRenderer sut = new(inner, 2F);

        // Exercise the non-outline pass-through surface.
        sut.BeginText(default);
        Assert.True(sut.BeginGlyph(default, default));
        sut.BeginLayer(null, default, null);

        // A triangular contour covering every segment kind plus an arc (treated as a line).
        sut.BeginFigure();
        sut.MoveTo(new Vector2(0, 0));
        sut.LineTo(new Vector2(10, 0));
        sut.QuadraticBezierTo(new Vector2(12, 5), new Vector2(10, 10));
        sut.CubicBezierTo(new Vector2(8, 12), new Vector2(4, 12), new Vector2(0, 10));
        sut.ArcTo(1, 1, 0, false, false, new Vector2(0, 0));
        sut.EndFigure();

        // An empty figure must be dropped rather than replayed.
        sut.BeginFigure();
        sut.EndFigure();

        _ = sut.EnabledDecorations();
        sut.SetDecoration(TextDecorations.Underline, default, default, 1F, ReadOnlyMemory<float>.Empty);
        sut.EndLayer();
        sut.EndGlyph();
        sut.EndText();

        Assert.True(inner.BeganText);
        Assert.True(inner.BeganGlyph);
        Assert.True(inner.BeganLayer);
        Assert.True(inner.EndedLayer);
        Assert.True(inner.EndedGlyph);
        Assert.True(inner.EndedText);
        Assert.True(inner.QueriedDecorations);
        Assert.True(inner.SetDecorationCalled);

        // Exactly one non-empty contour is replayed, preserving the emitted point count
        // (Move=1, Line=1, Quadratic=2, Cubic=3, Arc-as-Line=1).
        Assert.Single(inner.Figures);
        Assert.Equal(8, inner.Figures[0].Count);
    }

    [Fact]
    public void EmboldeningRenderer_MatchesFreeTypeForOuterContourAndCounter()
    {
        RecordingGlyphRenderer inner = new();
        EmboldeningGlyphRenderer sut = new(inner, 2F);

        // The outer contour is clockwise in device coordinates. FreeType leaves its left and top
        // edges fixed while increasing the right and bottom extents by the requested strength.
        sut.BeginFigure();
        sut.MoveTo(new Vector2(0, 0));
        sut.LineTo(new Vector2(20, 0));
        sut.LineTo(new Vector2(20, 20));
        sut.LineTo(new Vector2(0, 20));
        sut.EndFigure();

        // The counter uses the opposite winding, so the same outline orientation shrinks it and
        // increases the surrounding stem thickness instead of expanding the hole.
        sut.BeginFigure();
        sut.MoveTo(new Vector2(5, 5));
        sut.LineTo(new Vector2(5, 15));
        sut.LineTo(new Vector2(15, 15));
        sut.LineTo(new Vector2(15, 5));
        sut.EndFigure();

        sut.CompleteOutline();

        Vector2[] expectedOuter = [new(0, 0), new(22, 0), new(22, 22), new(0, 22)];
        Vector2[] expectedCounter = [new(7, 7), new(7, 15), new(15, 15), new(15, 7)];

        Assert.Equal(2, inner.Figures.Count);
        Assert.Equal(expectedOuter, inner.Figures[0]);
        Assert.Equal(expectedCounter, inner.Figures[1]);
    }

    private sealed class RecordingGlyphRenderer : IGlyphRenderer
    {
        public bool BeganText { get; private set; }

        public bool EndedText { get; private set; }

        public bool BeganGlyph { get; private set; }

        public bool EndedGlyph { get; private set; }

        public bool BeganLayer { get; private set; }

        public bool EndedLayer { get; private set; }

        public bool QueriedDecorations { get; private set; }

        public bool SetDecorationCalled { get; private set; }

        public List<List<Vector2>> Figures { get; } = [];

        private List<Vector2> current;

        public void BeginText(in FontRectangle bounds) => this.BeganText = true;

        public void EndText() => this.EndedText = true;

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.BeganGlyph = true;
            return true;
        }

        public void EndGlyph() => this.EndedGlyph = true;

        public void BeginLayer(Paint paint, FillRule fillRule, ClipQuad? clipBounds) => this.BeganLayer = true;

        public void EndLayer() => this.EndedLayer = true;

        public void BeginFigure() => this.current = [];

        public void MoveTo(Vector2 point) => this.current.Add(point);

        public void LineTo(Vector2 point) => this.current.Add(point);

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            this.current.Add(secondControlPoint);
            this.current.Add(point);
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            this.current.Add(secondControlPoint);
            this.current.Add(thirdControlPoint);
            this.current.Add(point);
        }

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
            => this.current.Add(point);

        public void EndFigure() => this.Figures.Add(this.current);

        public TextDecorations EnabledDecorations()
        {
            this.QueriedDecorations = true;
            return TextDecorations.None;
        }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
            => this.SetDecorationCalled = true;
    }
}

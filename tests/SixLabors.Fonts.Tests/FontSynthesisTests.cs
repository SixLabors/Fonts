// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class FontSynthesisTests
{
    // tan(14 degrees) - the default oblique slant browsers apply for synthesized italics.
    private static readonly float ExpectedSkew = MathF.Tan(14F * MathF.PI / 180F);

    private static readonly ApproximateFloatComparer SkewComparer = new(0.001F);

    private static readonly ApproximateFloatComparer GeometryComparer = new(0.1F);

    private static FontFamily RegularOnlyFamily(string file)
        => new FontCollection().Add(file);

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
        => AssertSyntheticItalicShear(TestFonts.OpenSansFile, "Hg");

    [Fact]
    public void SyntheticItalic_ShearsGlyphOutline_Cff()
        => AssertSyntheticItalicShear(TestFonts.PlantinStdRegularFile, "Hg");

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

    private static void AssertSyntheticItalicShear(string file, string text)
    {
        FontFamily family = RegularOnlyFamily(file);
        Font regular = new(family, 48, FontStyle.Regular);
        Font italic = new(family, 48, FontStyle.Italic);

        GlyphRenderer regularRenderer = new();
        TextRenderer.RenderTextTo(regularRenderer, text, new TextOptions(regular) { HintingMode = HintingMode.None });

        GlyphRenderer italicRenderer = new();
        TextRenderer.RenderTextTo(italicRenderer, text, new TextOptions(italic) { HintingMode = HintingMode.None });

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
        TextRenderer.RenderTextTo(regularRenderer, text, new TextOptions(regular) { HintingMode = HintingMode.None });

        GlyphRenderer boldRenderer = new();
        TextRenderer.RenderTextTo(boldRenderer, text, new TextOptions(bold) { HintingMode = HintingMode.None });

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

        public List<List<Vector2>> Figures { get; } = new();

        private List<Vector2>? current;

        public void BeginText(in FontRectangle bounds) => this.BeganText = true;

        public void EndText() => this.EndedText = true;

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.BeganGlyph = true;
            return true;
        }

        public void EndGlyph() => this.EndedGlyph = true;

        public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds) => this.BeganLayer = true;

        public void EndLayer() => this.EndedLayer = true;

        public void BeginFigure() => this.current = new List<Vector2>();

        public void MoveTo(Vector2 point) => this.current!.Add(point);

        public void LineTo(Vector2 point) => this.current!.Add(point);

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            this.current!.Add(secondControlPoint);
            this.current!.Add(point);
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            this.current!.Add(secondControlPoint);
            this.current!.Add(thirdControlPoint);
            this.current!.Add(point);
        }

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
            => this.current!.Add(point);

        public void EndFigure() => this.Figures.Add(this.current!);

        public TextDecorations EnabledDecorations()
        {
            this.QueriedDecorations = true;
            return TextDecorations.None;
        }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
            => this.SetDecorationCalled = true;
    }
}

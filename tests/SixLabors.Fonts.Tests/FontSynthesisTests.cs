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
}

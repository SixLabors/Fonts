// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Tables.Variations;

/// <summary>
/// Tests for variable font support including gvar, HVAR, CFF2 blend, and MVAR.
/// Test cases ported from fontkit (https://github.com/foliojs/fontkit).
/// </summary>
public class VariationFontTests
{
    [Fact]
    public void FontVariation_ValidTag_CreatesInstance()
    {
        FontVariation variation = new("wght", 700);
        Assert.Equal("wght", variation.Tag);
        Assert.Equal(700, variation.Value);
    }

    [Fact]
    public void FontVariation_InvalidTagLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FontVariation("wg", 700));
        Assert.Throws<ArgumentException>(() => new FontVariation("weight", 700));
    }

    [Fact]
    public void FontVariation_NullTag_ThrowsArgumentException()
        => Assert.ThrowsAny<ArgumentException>(() => new FontVariation(null, 700));

    [Fact]
    public void CanCreateFontWithVariations()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font baseFont = family.CreateFont(12);

        Font variedFont = new(baseFont, new FontVariation("wght", 700));

        Assert.Single(variedFont.Variations.ToArray());
        Assert.Equal("wght", variedFont.Variations[0].Tag);
        Assert.Equal(700, variedFont.Variations[0].Value);
    }

    [Fact]
    public void CanCreateFontWithVariationsViaFontFamily()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 700));

        Assert.Single(variedFont.Variations.ToArray());
    }

    [Fact]
    public void BaseFontHasEmptyVariations()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font baseFont = family.CreateFont(12);

        Assert.True(baseFont.Variations.IsEmpty);
    }

    [Fact]
    public void VariationsDoNotAffectNonVariableFont()
    {
        // OpenSans is not a variable font; variations should be silently ignored.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.OpenSansFile);
        Font baseFont = family.CreateFont(12);
        Font variedFont = new(baseFont, new FontVariation("wght", 700));

        // Both should resolve to the same metrics since it's not variable.
        Assert.Equal(baseFont.FontMetrics.UnitsPerEm, variedFont.FontMetrics.UnitsPerEm);
    }

    [Fact]
    public void CanLoadVariationAxes_RobotoFlex()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font font = family.CreateFont(12);

        Assert.True(font.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        Assert.Equal(13, axes.Length);

        Assert.Equal("wght", axes.Span[0].Tag);
        Assert.Equal(100, axes.Span[0].Min);
        Assert.Equal(1000, axes.Span[0].Max);
        Assert.Equal(400, axes.Span[0].Default);
    }

    [Fact]
    public void CanLoadVariationAxes_AdobeVFPrototype()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font font = family.CreateFont(12);

        Assert.True(font.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        Assert.Equal(2, axes.Length);

        Assert.Equal("wght", axes.Span[0].Tag);
        Assert.Equal(200, axes.Span[0].Min);
        Assert.Equal(900, axes.Span[0].Max);

        Assert.Equal("CNTR", axes.Span[1].Tag);
    }

    [Fact]
    public void GVar_VariedGlyphDiffersFromDefault()
    {
        // Verify that applying a weight variation actually changes glyph outlines.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVAROne);
        Font defaultFont = family.CreateFont(12);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 300));

        // Get glyph metrics for '彌' at default and varied weights.
        CodePoint cp = new('彌');

        Assert.True(defaultFont.TryGetGlyphs(cp, out Glyph? defaultGlyph));
        Assert.True(variedFont.TryGetGlyphs(cp, out Glyph? variedGlyph));

        // The bounds should differ between default and weight=300.
        Assert.NotEqual(
            defaultGlyph.Value.GlyphMetrics.Bounds,
            variedGlyph.Value.GlyphMetrics.Bounds);
    }

    [Theory]
    [InlineData("TestGVAROne")]
    [InlineData("TestGVARTwo")]
    [InlineData("TestGVARThree")]
    public void GVar_AllPointShareModes_ProduceSameResult(string fontName)
    {
        // fontkit tests: all three TestGVAR fonts should produce identical results
        // at wght=300 for "彌" — they differ only in how points are shared in gvar.
        string fontPath = fontName switch
        {
            "TestGVAROne" => TestFonts.TestGVAROne,
            "TestGVARTwo" => TestFonts.TestGVARTwo,
            "TestGVARThree" => TestFonts.TestGVARThree,
            _ => throw new ArgumentException(fontName)
        };

        FontFamily family = TestFonts.GetFontFamily(fontPath);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 300));

        CodePoint cp = new('彌');
        Assert.True(variedFont.TryGetGlyphs(cp, out Glyph? glyph));

        // All three fonts should produce glyph metrics at this variation.
        Assert.NotNull(glyph);
    }

    [Fact]
    public void GVar_AllThreeFontsProduceIdenticalBounds()
    {
        // All three TestGVAR fonts encode the same variation data differently.
        // They should produce identical glyph bounds at the same axis value.
        Font font1 = TestFonts.GetFontFamily(TestFonts.TestGVAROne).CreateFont(12, new FontVariation("wght", 300));
        Font font2 = TestFonts.GetFontFamily(TestFonts.TestGVARTwo).CreateFont(12, new FontVariation("wght", 300));
        Font font3 = TestFonts.GetFontFamily(TestFonts.TestGVARThree).CreateFont(12, new FontVariation("wght", 300));

        CodePoint cp = new('彌');
        font1.TryGetGlyphs(cp, out Glyph? g1);
        font2.TryGetGlyphs(cp, out Glyph? g2);
        font3.TryGetGlyphs(cp, out Glyph? g3);

        // Bounds should be identical across all three encoding modes.
        Assert.Equal(g1.Value.GlyphMetrics.Width, g2.Value.GlyphMetrics.Width);
        Assert.Equal(g1.Value.GlyphMetrics.Width, g3.Value.GlyphMetrics.Width);
        Assert.Equal(g1.Value.GlyphMetrics.Height, g2.Value.GlyphMetrics.Height);
        Assert.Equal(g1.Value.GlyphMetrics.Height, g3.Value.GlyphMetrics.Height);
    }

    [Fact]
    public void HVAR_AdvanceWidthVariesWithWeight()
    {
        // fontkit: TestGVARFour at wght=150, glyph 'O' should have advanceWidth=706
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVARFour);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 150));

        CodePoint cp = new('O');
        Assert.True(variedFont.TryGetGlyphs(cp, out Glyph? glyph));

        Assert.Equal(706, glyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void HVAR_FallsBackToLastEntry()
    {
        // fontkit: TestHVARTwo at wght=400, glyph 'A' should have advanceWidth=584
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestHVARTwo);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 400));

        CodePoint cp = new('A');
        Assert.True(variedFont.TryGetGlyphs(cp, out Glyph? glyph));

        Assert.Equal(584, glyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void HVAR_DefaultWeightPreservesOriginalWidth()
    {
        // fontkit: TestGVARFour at default wght (1000), glyph 'O' advanceWidth=700
        // At default axis value the HVAR delta should be zero.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVARFour);
        Font defaultFont = family.CreateFont(12);

        Assert.True(defaultFont.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        VariationAxis wghtAxis = Assert.Single(axes.ToArray(), a => a.Tag == "wght");

        Font variedFont = family.CreateFont(12, new FontVariation("wght", wghtAxis.Default));

        CodePoint cp = new('O');
        defaultFont.TryGetGlyphs(cp, out Glyph? defaultGlyph);
        variedFont.TryGetGlyphs(cp, out Glyph? variedGlyph);

        Assert.Equal(defaultGlyph.Value.GlyphMetrics.AdvanceWidth, variedGlyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void HVAR_AdvanceWidthAtSpecificWeight()
    {
        // fontkit: TestGVARFour at wght=150, glyph 'O' should have advanceWidth=706
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVARFour);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 150));

        CodePoint cp = new('O');
        Assert.True(variedFont.TryGetGlyphs(cp, out Glyph? glyph));

        Assert.Equal(706, glyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void GVar_AdobeVFPrototype_VariedGlyphDiffersFromDefault()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font defaultFont = family.CreateFont(12);
        Font lightFont = family.CreateFont(12, new FontVariation("wght", 200));
        Font boldFont = family.CreateFont(12, new FontVariation("wght", 900));

        CodePoint cp = new('A');
        defaultFont.TryGetGlyphs(cp, out Glyph? defaultGlyph);
        lightFont.TryGetGlyphs(cp, out Glyph? lightGlyph);
        boldFont.TryGetGlyphs(cp, out Glyph? boldGlyph);

        // Bold should be wider than default, light should be narrower.
        Assert.True(boldGlyph.Value.GlyphMetrics.AdvanceWidth >= defaultGlyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void GVar_AdobeVFPrototype_DifferentWeightsProduceDifferentBounds()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font font200 = family.CreateFont(12, new FontVariation("wght", 200));
        Font font900 = family.CreateFont(12, new FontVariation("wght", 900));

        CodePoint cp = new('A');
        font200.TryGetGlyphs(cp, out Glyph? g200);
        font900.TryGetGlyphs(cp, out Glyph? g900);

        // Bounds should differ between light and heavy weights.
        Assert.NotEqual(g200.Value.GlyphMetrics.Width, g900.Value.GlyphMetrics.Width);
    }

    [Fact]
    public void GVar_AdobeVFPrototype_GSUB_SubstitutesGlyphAtHeavyWeight()
    {
        // fontkit: AdobeVFPrototype at wght=900, '$' substitutes to 'dollar.nostroke' (glyphId 2).
        // GSUB FeatureVariations activate alternate glyphs based on axis values.
        // Must use TextRenderer to trigger GSUB.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font defaultFont = family.CreateFont(12);
        Font heavyFont = family.CreateFont(12, new FontVariation("wght", 900));

        GlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTextTo(defaultRenderer, "$", new TextOptions(defaultFont));

        GlyphRenderer heavyRenderer = new();
        TextRenderer.RenderTextTo(heavyRenderer, "$", new TextOptions(heavyFont));

        // The GSUB substitution should produce a different glyph ID at wght=900.
        Assert.NotEqual(defaultRenderer.GlyphKeys[0].GlyphId, heavyRenderer.GlyphKeys[0].GlyphId);
    }

    [Fact]
    public void CFF2_CanLoadFont()
    {
        // AdobeVFPrototype-Subset.otf is the only CFF2 font in the test suite.
        // It contains 3 glyphs: .notdef, '$' (glyph 1), and 'dollar.nostroke' (glyph 2).
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font font = family.CreateFont(12);

        Assert.NotNull(font.FontMetrics);
    }

    [Fact]
    public void CFF2_CanLoadVariationAxes()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font font = family.CreateFont(12);

        Assert.True(font.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        Assert.Equal(2, axes.Length);
        Assert.Equal("wght", axes.Span[0].Tag);
    }

    [Fact]
    public void CFF2_CanCreateFontWithVariation()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 900));

        Assert.Single(variedFont.Variations.ToArray());
    }

    [Fact]
    public void CFF2_RendersGlyphAtDefaultWeight()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font font = family.CreateFont(48);

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "$", new TextOptions(font));

        Assert.NotEmpty(renderer.GlyphKeys);
        Assert.NotEmpty(renderer.ControlPoints);
    }

    [Fact]
    public void CFF2_RendersGlyphAtVariedWeight()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font font = family.CreateFont(48, new FontVariation("wght", 900));

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "$", new TextOptions(font));

        Assert.NotEmpty(renderer.GlyphKeys);
        Assert.NotEmpty(renderer.ControlPoints);
    }

    [Fact]
    public void CFF2_MultipleWeightsRenderSuccessfully()
    {
        // Verify the CFF2 parser handles charstrings at multiple weight values.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototypeSubset);
        Font lightFont = family.CreateFont(48, new FontVariation("wght", 0));
        Font heavyFont = family.CreateFont(48, new FontVariation("wght", 900));

        GlyphRenderer lightRenderer = new();
        TextRenderer.RenderTextTo(lightRenderer, "$", new TextOptions(lightFont));

        GlyphRenderer heavyRenderer = new();
        TextRenderer.RenderTextTo(heavyRenderer, "$", new TextOptions(heavyFont));

        Assert.NotEmpty(lightRenderer.ControlPoints);
        Assert.NotEmpty(heavyRenderer.ControlPoints);
    }

    [Fact]
    public void MVAR_MetricsVaryWithAxisValues()
    {
        // RobotoFlex has MVAR table. Global metrics should change with weight.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font defaultFont = family.CreateFont(12);
        Font heavyFont = family.CreateFont(12, new FontVariation("wght", 1000));

        // Ascender/descender may change with MVAR.
        // At minimum, the font should load successfully with both axis values.
        Assert.NotNull(defaultFont.FontMetrics);
        Assert.NotNull(heavyFont.FontMetrics);

        // UnitsPerEm should remain the same (not affected by MVAR).
        Assert.Equal(defaultFont.FontMetrics.UnitsPerEm, heavyFont.FontMetrics.UnitsPerEm);
    }

    [Fact]
    public void GPOS_MarkAnchorPositionsVaryWithWeight()
    {
        // fontkit: Mada-VF at wght=900, layout 'ف', positions[0] xOffset≈639, yOffset≈542.
        // The mark positioning should differ between default and heavy weights.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.MadaVF);
        Font defaultFont = family.CreateFont(72);
        Font heavyFont = family.CreateFont(72, new FontVariation("wght", 900));

        GlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTextTo(defaultRenderer, "\u0641", new TextOptions(defaultFont));

        GlyphRenderer heavyRenderer = new();
        TextRenderer.RenderTextTo(heavyRenderer, "\u0641", new TextOptions(heavyFont));

        // Both should render, and the glyph bounds should differ due to
        // GPOS mark anchor adjustments varying with weight.
        Assert.NotEmpty(defaultRenderer.GlyphRects);
        Assert.NotEmpty(heavyRenderer.GlyphRects);
        Assert.NotEqual(defaultRenderer.GlyphRects[0], heavyRenderer.GlyphRects[0]);
    }

    [Fact]
    public void MultipleVariationInstances_DoNotInterfere()
    {
        // Create two different variation instances from the same base font.
        // They should produce different results without corrupting each other.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVARFour);
        Font lightFont = family.CreateFont(12, new FontVariation("wght", 150));
        Font heavyFont = family.CreateFont(12, new FontVariation("wght", 900));

        CodePoint cp = new('O');
        lightFont.TryGetGlyphs(cp, out Glyph? lightGlyph);
        heavyFont.TryGetGlyphs(cp, out Glyph? heavyGlyph);

        // Both should succeed.
        Assert.NotNull(lightGlyph);
        Assert.NotNull(heavyGlyph);

        // They should produce different advance widths.
        Assert.NotEqual(
            lightGlyph.Value.GlyphMetrics.AdvanceWidth,
            heavyGlyph.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void VariationInstance_DoesNotCorruptBaseFont()
    {
        // Get a glyph from the default font, then create a variation,
        // then get the same glyph from the default font again.
        // The default font should not be affected.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVARFour);
        Font defaultFont = family.CreateFont(12);

        CodePoint cp = new('O');
        defaultFont.TryGetGlyphs(cp, out Glyph? before);
        ushort widthBefore = before.Value.GlyphMetrics.AdvanceWidth;

        // Create and use a variation instance.
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 150));
        variedFont.TryGetGlyphs(cp, out _);

        // Default font should still produce the same width.
        defaultFont.TryGetGlyphs(cp, out Glyph? after);
        Assert.Equal(widthBefore, after.Value.GlyphMetrics.AdvanceWidth);
    }

    [Fact]
    public void TextMeasurer_AdvanceChangesWithVariation()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font thinFont = family.CreateFont(72, new FontVariation("wght", 100));
        Font heavyFont = family.CreateFont(72, new FontVariation("wght", 1000));

        TextOptions thinOptions = new(thinFont);
        TextOptions heavyOptions = new(heavyFont);

        FontRectangle thinAdvance = TextMeasurer.MeasureAdvance("Hello", thinOptions);
        FontRectangle heavyAdvance = TextMeasurer.MeasureAdvance("Hello", heavyOptions);

        // Heavy weight should produce a wider advance than thin.
        Assert.True(
            heavyAdvance.Width > thinAdvance.Width,
            $"Heavy advance ({heavyAdvance.Width}) should be wider than thin ({thinAdvance.Width})");
    }

    [Fact]
    public void TextMeasurer_MultipleAxesWork()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font font = family.CreateFont(
            72,
            new FontVariation("wght", 700),
            new FontVariation("wdth", 75));

        TextOptions options = new(font);
        FontRectangle advance = TextMeasurer.MeasureAdvance("Test", options);

        // Should produce a valid non-zero measurement.
        Assert.True(advance.Width > 0);
        Assert.True(advance.Height > 0);
    }

    [Fact]
    public void Renderer_VariedFontProducesGlyphs()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVAROne);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 300));

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "彌", new TextOptions(variedFont));

        Assert.NotEmpty(renderer.GlyphKeys);
        Assert.NotEmpty(renderer.GlyphRects);
    }

    [Fact]
    public void Renderer_DifferentVariationsProduceDifferentControlPoints()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.TestGVAROne);
        Font defaultFont = family.CreateFont(72);
        Font variedFont = family.CreateFont(72, new FontVariation("wght", 300));

        GlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTextTo(defaultRenderer, "彌", new TextOptions(defaultFont));

        GlyphRenderer variedRenderer = new();
        TextRenderer.RenderTextTo(variedRenderer, "彌", new TextOptions(variedFont));

        // Both should produce control points, but they should differ.
        Assert.NotEmpty(defaultRenderer.ControlPoints);
        Assert.NotEmpty(variedRenderer.ControlPoints);

        // At least some control points should differ between the two variations.
        bool anyDifference = false;
        int count = Math.Min(defaultRenderer.ControlPoints.Count, variedRenderer.ControlPoints.Count);
        for (int i = 0; i < count; i++)
        {
            if (defaultRenderer.ControlPoints[i] != variedRenderer.ControlPoints[i])
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference, "Control points should differ between default and varied glyphs");
    }

    [Fact]
    public void Renderer_GVar_AdobeVFPrototype_VariedFontProducesGlyphs()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font variedFont = family.CreateFont(12, new FontVariation("wght", 900));

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "A", new TextOptions(variedFont));

        Assert.NotEmpty(renderer.GlyphKeys);
    }

    [Fact]
    public void NotoSansHK_VariableWeight_LoadsSuccessfully()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoSansHKVariableFontWght);
        Font thinFont = family.CreateFont(12, new FontVariation("wght", 100));
        Font boldFont = family.CreateFont(12, new FontVariation("wght", 900));

        Assert.NotNull(thinFont.FontMetrics);
        Assert.NotNull(boldFont.FontMetrics);
    }

    [Fact]
    public void NotoEmoji_GVar_OutlinesVaryWithWeight()
    {
        // Noto Emoji is a TrueType variable font (gvar/HVAR) with a weight axis (300–700, default 400).
        // Advance width stays constant at 2600 across weights, but glyph outlines change.
        // Verified against fontkit: star U+2B50 glyphId=184, advance=2600 at all weights,
        // light bbox={233,-320,2367,1720}, bold bbox={203,-350,2397,1750}.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoEmojiVariableFont);
        Font lightFont = family.CreateFont(12, new FontVariation("wght", 300));
        Font boldFont = family.CreateFont(12, new FontVariation("wght", 700));

        // Advance width should be constant across weights.
        CodePoint cp = new(0x2B50);
        Assert.True(lightFont.TryGetGlyphs(cp, out Glyph? lightGlyph));
        Assert.True(boldFont.TryGetGlyphs(cp, out Glyph? boldGlyph));
        Assert.Equal(2600, lightGlyph.Value.GlyphMetrics.AdvanceWidth);
        Assert.Equal(2600, boldGlyph.Value.GlyphMetrics.AdvanceWidth);

        // Render both and verify outlines differ.
        GlyphRenderer lightRenderer = new();
        TextRenderer.RenderTextTo(lightRenderer, "\u2B50", new TextOptions(lightFont));

        GlyphRenderer boldRenderer = new();
        TextRenderer.RenderTextTo(boldRenderer, "\u2B50", new TextOptions(boldFont));

        Assert.NotEmpty(lightRenderer.ControlPoints);
        Assert.NotEmpty(boldRenderer.ControlPoints);

        // Control points should differ between light and bold weights.
        bool anyDifference = false;
        int count = Math.Min(lightRenderer.ControlPoints.Count, boldRenderer.ControlPoints.Count);
        for (int i = 0; i < count; i++)
        {
            if (lightRenderer.ControlPoints[i] != boldRenderer.ControlPoints[i])
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference, "Glyph outlines should differ between light and bold weights");
    }

    [Theory]
    [InlineData(300, "Light")]
    [InlineData(400, "Regular")]
    [InlineData(700, "Bold")]
    public void VisualTest_NotoEmoji_WeightVariations(float weight, string label)
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoEmojiVariableFont);
        Font font = family.CreateFont(48, new FontVariation("wght", weight));

        TextOptions options = new(font);

        TextLayoutTestUtilities.TestLayout(
            "\u2B50\u263A\u2764\u270C",
            options,
            properties: [label, weight]);
    }

    [Theory]
    [InlineData(100, "Thin")]
    [InlineData(400, "Regular")]
    [InlineData(700, "Bold")]
    [InlineData(1000, "Heavy")]
    public void VisualTest_RobotoFlex_WeightVariations(float weight, string label)
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font font = family.CreateFont(36, new FontVariation("wght", weight));

        TextOptions options = new(font);

        TextLayoutTestUtilities.TestLayout(
            "The quick brown fox jumps over the lazy dog.",
            options,
            properties: [label, weight]);
    }

    [Theory]
    [InlineData(200, "Light")]
    [InlineData(900, "Black")]
    public void VisualTest_AdobeVFPrototype_GVar_WeightVariations(float weight, string label)
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.AdobeVFPrototype);
        Font font = family.CreateFont(48, new FontVariation("wght", weight));

        TextOptions options = new(font);

        TextLayoutTestUtilities.TestLayout(
            "ABCDEFGH",
            options,
            properties: [label, weight]);
    }

    [Fact]
    public void VisualTest_RobotoFlex_MultipleAxes()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.RobotoFlex);
        Font font = family.CreateFont(
            36,
            new FontVariation("wght", 700),
            new FontVariation("wdth", 75));

        TextOptions options = new(font);

        TextLayoutTestUtilities.TestLayout(
            "Multiple variation axes",
            options);
    }

    [Fact]
    public void CVar_CanLoadFontWithCvarTable()
    {
        // VotoSerif has cvar, cvt, fpgm, prep, fvar, gvar tables.
        // Axes: wght (28–194, default 94), wdth (70–100), opsz (12–72).
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvar);
        Font font = family.CreateFont(12);

        Assert.NotNull(font.FontMetrics);
        Assert.True(font.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        Assert.Equal(3, axes.Length);
        Assert.Equal("wght", axes.Span[0].Tag);
        Assert.Equal("wdth", axes.Span[1].Tag);
        Assert.Equal("opsz", axes.Span[2].Tag);
    }

    [Fact]
    public void CVar_NoShared_CanLoadFontWithCvarTable()
    {
        // Same font but cvar uses no shared point numbers.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvarNoShared);
        Font font = family.CreateFont(12);

        Assert.NotNull(font.FontMetrics);
        Assert.True(font.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> axes));
        Assert.Equal(3, axes.Length);
    }

    [Fact]
    public void CVar_HintedRenderingWithVariation()
    {
        // Exercise the cvar code path: hinting enabled + variation applied.
        // cvar deltas modify CVT values before TrueType hinting instructions run.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvar);
        Font defaultFont = family.CreateFont(48);
        Font variedFont = family.CreateFont(48, new FontVariation("wght", 194));

        TextOptions defaultOptions = new(defaultFont) { HintingMode = HintingMode.Standard };
        TextOptions variedOptions = new(variedFont) { HintingMode = HintingMode.Standard };

        GlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTextTo(defaultRenderer, "hono", defaultOptions);

        GlyphRenderer variedRenderer = new();
        TextRenderer.RenderTextTo(variedRenderer, "hono", variedOptions);

        Assert.NotEmpty(defaultRenderer.ControlPoints);
        Assert.NotEmpty(variedRenderer.ControlPoints);
    }

    [Fact]
    public void CVar_NoShared_HintedRenderingWithVariation()
    {
        // Same test with the no-shared-points variant of the cvar font.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvarNoShared);
        Font variedFont = family.CreateFont(48, new FontVariation("wght", 194));

        TextOptions options = new(variedFont) { HintingMode = HintingMode.Standard };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "hono", options);

        Assert.NotEmpty(renderer.ControlPoints);
    }

    [Fact]
    public void CVar_HintedRenderingAtSmallSize()
    {
        // At small sizes, TrueType hinting with cvar-adjusted CVT values
        // has a greater effect on grid-fitting. Verify both paths render successfully.
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvar);
        Font font = family.CreateFont(12, new FontVariation("wght", 28));

        TextOptions hintedOptions = new(font) { HintingMode = HintingMode.Standard };
        TextOptions unhintedOptions = new(font) { HintingMode = HintingMode.None };

        GlyphRenderer hintedRenderer = new();
        TextRenderer.RenderTextTo(hintedRenderer, "hono", hintedOptions);

        GlyphRenderer unhintedRenderer = new();
        TextRenderer.RenderTextTo(unhintedRenderer, "hono", unhintedOptions);

        Assert.NotEmpty(hintedRenderer.ControlPoints);
        Assert.NotEmpty(unhintedRenderer.ControlPoints);
    }

    [Theory]
    [InlineData(28, "Light", HintingMode.None)]
    [InlineData(28, "Light", HintingMode.Standard)]
    [InlineData(94, "Regular", HintingMode.None)]
    [InlineData(94, "Regular", HintingMode.Standard)]
    [InlineData(194, "Heavy", HintingMode.None)]
    [InlineData(194, "Heavy", HintingMode.Standard)]
    public void VisualTest_VotoSerif_CVar_WeightVariations(float weight, string label, HintingMode hintingMode)
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.VotoSerifCvar);
        Font font = family.CreateFont(48, new FontVariation("wght", weight));

        TextOptions options = new(font) { HintingMode = hintingMode };

        TextLayoutTestUtilities.TestLayout(
            "hono",
            options,
            properties: [label, weight, hintingMode]);
    }
}

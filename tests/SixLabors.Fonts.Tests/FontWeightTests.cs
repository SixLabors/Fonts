// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class FontWeightTests
{
    private const string BrowserComparisonText = "The quick brown fox jumps over the lazy dog.";

    // The browser page places live browser text and rendered PNGs side by side. These sizes keep
    // both columns visible on a typical laptop display while retaining enough detail to compare
    // the synthetic and variable weight outlines.
    private const float BrowserComparisonPointSize = 18F;

    private const float BrowserComparisonEmojiPointSize = 27F;

    [Fact]
    public void TextOptions_DefaultWeightIsNullAndCopyPreservesWeight()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 12);
        TextOptions defaultOptions = new(font);
        TextOptions weightedOptions = new(font) { FontWeight = FontWeight.ExtraBold };

        Assert.Null(defaultOptions.FontWeight);
        Assert.Equal(FontWeight.ExtraBold, new TextOptions(weightedOptions).FontWeight);
    }

    [Fact]
    public void FontDescription_ExposesOpenTypeWeight()
    {
        FontDescription description = FontDescription.LoadDescription(TestFonts.OpenSansFile);

        Assert.Equal(FontWeight.Normal, description.Weight);
    }

    [Fact]
    public void TextRunWeight_OverridesTextOptionsWeight()
    {
        Font font = TestFonts.GetFont(TestFonts.RobotoFlex, 12);
        TextRun textRun = new()
        {
            Start = 0,
            End = 1,
            FontWeight = FontWeight.Black
        };

        TextOptions options = new(font)
        {
            FontWeight = FontWeight.Light,
            TextRuns = [textRun]
        };

        TextRun resolvedRun = Assert.Single(TextLayout.BuildTextRuns("A", options));
        FontVariation weight = Assert.Single(
            resolvedRun.ResolvedFont.Variations.ToArray(),
            variation => variation.Tag == KnownVariationAxes.Weight);

        Assert.Equal((int)FontWeight.Black, weight.Value);
    }

    [Fact]
    public void VariableWeight_MatchesExplicitVariation()
    {
        Font font = TestFonts.GetFont(TestFonts.RobotoFlex, 48);
        Font explicitFont = new(font, new FontVariation(KnownVariationAxes.Weight, (int)FontWeight.Black));

        GlyphRenderer weightedRenderer = new();
        TextRenderer.RenderTo(
            weightedRenderer,
            "Weight",
            new TextOptions(font) { FontWeight = FontWeight.Black });

        GlyphRenderer explicitRenderer = new();
        TextRenderer.RenderTo(explicitRenderer, "Weight", new TextOptions(explicitFont));

        Assert.Equal(explicitRenderer.ControlPoints, weightedRenderer.ControlPoints);
        Assert.Equal(explicitRenderer.GlyphRects, weightedRenderer.GlyphRects);
    }

    [Theory]
    [InlineData(FontWeight.Normal, 0F)]
    [InlineData(FontWeight.Medium, 0F)]
    [InlineData(FontWeight.SemiBold, 1F)]
    [InlineData(FontWeight.Bold, 1F)]
    [InlineData(FontWeight.Black, 1F)]
    public void StaticWeight_UsesBrowserBoldThreshold(FontWeight weight, float expectedFactor)
    {
        const float pointSize = 48F;
        Font font = new(RegularOnlyFamily(TestFonts.OpenSansFile), pointSize);
        TextRun textRun = new()
        {
            Start = 0,
            End = 1,
            Font = font,
            FontWeight = weight
        };

        textRun.ResolveFontWeight(defaultWeight: null);
        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            new CodePoint('H'),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics metrics));

        FontGlyphMetrics renderMetrics = metrics.CloneForRendering(textRun);
        float strength = renderMetrics.GetSyntheticBoldStrength(pointSize * 72F);

        Assert.Equal((pointSize / 31F) * expectedFactor, strength, new ApproximateFloatComparer(.0001F));
    }

    [Theory]
    [InlineData(FontWeight.Medium)]
    [InlineData(FontWeight.Bold)]
    [InlineData(FontWeight.Black)]
    public void StaticWeight_DoesNotChangeAdvance(FontWeight weight)
    {
        Font font = new(RegularOnlyFamily(TestFonts.OpenSansFile), 48);
        FontRectangle regularAdvance = TextMeasurer.MeasureAdvance("Weight", new TextOptions(font));
        FontRectangle weightedAdvance = TextMeasurer.MeasureAdvance(
            "Weight",
            new TextOptions(font) { FontWeight = weight });

        Assert.Equal(regularAdvance.Width, weightedAdvance.Width);
    }

    [Fact]
    public void PaintedEmoji_DoesNotSynthesizeWeight()
    {
        Font font = new(RegularOnlyFamily(TestFonts.TwemojiMozillaFile), 48);
        TextRun textRun = new()
        {
            Start = 0,
            End = 1,
            Font = font,
            FontWeight = FontWeight.Black
        };

        textRun.ResolveFontWeight(defaultWeight: null);
        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            new CodePoint(0x1F600),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.ColrV0,
            out FontGlyphMetrics metrics));

        FontGlyphMetrics renderMetrics = metrics.CloneForRendering(textRun);

        Assert.Equal(GlyphType.Painted, renderMetrics.GlyphType);
        Assert.False(renderMetrics.ShouldSynthesizeBold());
    }

    [Theory]
    [InlineData(FontWeight.Medium)]
    [InlineData(FontWeight.SemiBold)]
    public void SystemWeight_UsesInstalledFaceWhenAvailable(FontWeight requestedWeight)
    {
        if (!TestEnvironment.IsWindows || !SystemFonts.TryGet("Segoe UI", out FontFamily family))
        {
            return;
        }

        Font font = family.CreateFont(18);
        TextOptions options = new(font) { FontWeight = requestedWeight };
        TextRun textRun = Assert.Single(TextLayout.BuildTextRuns("Weight", options));

        // DirectWrite resolves both 500 and 600 to Segoe UI Semibold. This specifically protects
        // the equal-distance 500 request from incorrectly selecting the lighter Regular 400 face.
        Assert.Equal(FontWeight.SemiBold, textRun.ResolvedFont.FontMetrics.Description.Weight);
        Assert.False(textRun.UsesVariableWeight);

        Assert.True(textRun.ResolvedFont.FontMetrics.TryGetGlyphMetrics(
            new CodePoint('W'),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics metrics));

        Assert.False(metrics.CloneForRendering(textRun).ShouldSynthesizeBold());
    }

    [Theory]
    [InlineData(FontWeight.Thin, FontWeight.Normal)]
    [InlineData(FontWeight.ExtraLight, FontWeight.Normal)]
    [InlineData(FontWeight.Light, FontWeight.Normal)]
    [InlineData(FontWeight.Normal, FontWeight.Normal)]
    [InlineData(FontWeight.Medium, FontWeight.Normal)]
    [InlineData(FontWeight.SemiBold, FontWeight.Bold)]
    [InlineData(FontWeight.Bold, FontWeight.Bold)]
    [InlineData(FontWeight.ExtraBold, FontWeight.Black)]
    [InlineData(FontWeight.Black, FontWeight.Black)]
    public void SystemWeight_UsesBrowserFaceMatching(FontWeight requestedWeight, FontWeight expectedWeight)
    {
        if (!TestEnvironment.IsWindows || !SystemFonts.TryGet("Arial", out FontFamily family))
        {
            return;
        }

        Font font = family.CreateFont(18);
        TextOptions options = new(font) { FontWeight = requestedWeight };
        TextRun textRun = Assert.Single(TextLayout.BuildTextRuns("Weight", options));

        // CSS Fonts Level 4 section 5 maps missing weights to the nearest face in a specified
        // search direction. For the installed Arial family, 100-500 select Regular, 600-700
        // select Bold, and 800-900 select Arial Black.
        Assert.Equal(expectedWeight, textRun.ResolvedFont.FontMetrics.Description.Weight);
        Assert.False(textRun.UsesVariableWeight);

        Assert.True(textRun.ResolvedFont.FontMetrics.TryGetGlyphMetrics(
            new CodePoint('W'),
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics metrics));

        Assert.False(metrics.CloneForRendering(textRun).ShouldSynthesizeBold());
    }

    [Theory]
    [InlineData(FontWeight.Thin, "Thin")]
    [InlineData(FontWeight.ExtraLight, "ExtraLight")]
    [InlineData(FontWeight.Light, "Light")]
    [InlineData(FontWeight.Normal, "Normal")]
    [InlineData(FontWeight.Medium, "Medium")]
    [InlineData(FontWeight.SemiBold, "SemiBold")]
    [InlineData(FontWeight.Bold, "Bold")]
    [InlineData(FontWeight.ExtraBold, "ExtraBold")]
    [InlineData(FontWeight.Black, "Black")]
    public void VisualTest_StaticWeight(FontWeight weight, string label)
    {
        Font font = new(RegularOnlyFamily(TestFonts.OpenSansFile), BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, weight);

        // Open Sans exposes only its regular face here, matching the browser's single-face
        // declaration and exercising synthetic weight above the authored 400 weight.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            properties: [label, (int)weight]);
    }

    [Theory]
    [InlineData(FontWeight.Thin, "Thin")]
    [InlineData(FontWeight.ExtraLight, "ExtraLight")]
    [InlineData(FontWeight.Light, "Light")]
    [InlineData(FontWeight.Normal, "Normal")]
    [InlineData(FontWeight.Medium, "Medium")]
    [InlineData(FontWeight.SemiBold, "SemiBold")]
    [InlineData(FontWeight.Bold, "Bold")]
    [InlineData(FontWeight.ExtraBold, "ExtraBold")]
    [InlineData(FontWeight.Black, "Black")]
    public void VisualTest_VariableWeight(FontWeight weight, string label)
    {
        Font font = TestFonts.GetFont(TestFonts.RobotoFlex, BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, weight);

        // Roboto Flex exposes a registered wght axis, matching the browser's variable @font-face.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            properties: [label, (int)weight]);
    }

    [Theory]
    [InlineData(FontWeight.Thin, "Thin")]
    [InlineData(FontWeight.ExtraLight, "ExtraLight")]
    [InlineData(FontWeight.Light, "Light")]
    [InlineData(FontWeight.Normal, "Normal")]
    [InlineData(FontWeight.Medium, "Medium")]
    [InlineData(FontWeight.SemiBold, "SemiBold")]
    [InlineData(FontWeight.Bold, "Bold")]
    [InlineData(FontWeight.ExtraBold, "ExtraBold")]
    [InlineData(FontWeight.Black, "Black")]
    public void VisualTest_SystemSegoeUIWeight(FontWeight weight, string label)
    {
        if (!TestEnvironment.IsWindows || !SystemFonts.TryGet("Segoe UI", out FontFamily family))
        {
            return;
        }

        TextOptions options = BrowserComparisonOptions(family.CreateFont(BrowserComparisonPointSize), weight);

        // The browser and Fonts both resolve this family through the Windows system collection,
        // allowing authored numeric faces and synthesized missing weights to be compared directly.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            properties: [label, (int)weight]);
    }

    [Theory]
    [InlineData(FontWeight.Thin, "Thin")]
    [InlineData(FontWeight.ExtraLight, "ExtraLight")]
    [InlineData(FontWeight.Light, "Light")]
    [InlineData(FontWeight.Normal, "Normal")]
    [InlineData(FontWeight.Medium, "Medium")]
    [InlineData(FontWeight.SemiBold, "SemiBold")]
    [InlineData(FontWeight.Bold, "Bold")]
    [InlineData(FontWeight.ExtraBold, "ExtraBold")]
    [InlineData(FontWeight.Black, "Black")]
    public void VisualTest_SystemArialWeight(FontWeight weight, string label)
    {
        if (!TestEnvironment.IsWindows || !SystemFonts.TryGet("Arial", out FontFamily family))
        {
            return;
        }

        TextOptions options = BrowserComparisonOptions(family.CreateFont(BrowserComparisonPointSize), weight);

        // Arial provides the common regular and bold faces but not every numeric weight, exposing
        // how the browser and Fonts each handle weights missing from an installed static family.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            properties: [label, (int)weight]);
    }

    [Theory]
    [InlineData(FontWeight.Normal, "Normal")]
    [InlineData(FontWeight.Black, "Black")]
    public void VisualTest_PaintedEmojiWeight(FontWeight weight, string label)
    {
        FontCollection collection = new();
        FontFamily emoji = collection.Add(TestFonts.TwemojiMozillaFile);
        FontFamily fallback = collection.Add(TestFonts.OpenSansFile);
        TextOptions options = BrowserComparisonOptions(new Font(emoji, BrowserComparisonEmojiPointSize), weight);
        options.ColorFontSupport = ColorFontSupport.ColrV0;
        options.FallbackFontFamilies = [fallback];

        // The 900 sample must retain the same authored color geometry as the 400 sample.
        TextLayoutTestUtilities.TestLayout(
            "😀 ☺️ ❤️ ✌️ ⭐",
            options,
            properties: [label, (int)weight]);
    }

    private static TextOptions BrowserComparisonOptions(Font font, FontWeight weight)
        => new(font)
        {
            FontWeight = weight,
            Dpi = 96F,
            LineSpacing = 1.4F
        };

    private static FontFamily RegularOnlyFamily(string path)
    {
        const string familyName = "Weight Test";
        FontCollection collection = new();
        IFontMetricsCollection metricsCollection = collection;
        metricsCollection.AddMetrics(new FileFontMetrics(path), familyName, FontStyle.Regular);
        return collection.GetByCulture(familyName, CultureInfo.InvariantCulture);
    }
}

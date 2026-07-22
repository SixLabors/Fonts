// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.Fonts.Tests;

public class TextBaselineTests
{
    // Shared with tests/Browser/TextBaseline.html, which renders the identical text with the
    // equivalent browser anchors; the letters cover an ascender, the x-height, and descenders.
    private const string BrowserComparisonText = "Hxplq";

    // CJK companion string for the same page. The Noto Sans SC subset in tests/Fonts carries
    // a real BASE table on both axes plus vertical metrics, so these rows exercise the
    // table-driven baselines; its glyph set is pinned to exactly these characters plus
    // BrowserComparisonText, so extending either string requires regenerating the subset.
    private const string BrowserComparisonCjkText = "永国Hxq、";

    // Rendered large so the differences between reference lines span many pixels; at small
    // sizes adjacent anchors differ by well under a pixel.
    private const float BrowserComparisonPointSize = 72;

    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    private static Font Font => TextLayoutTests.CreateRenderingFont();

    [Theory]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RenderAndMetrics_AnchorReferenceLineAtOrigin(TextBaseline baseline)
    {
        const string text = "Hxp";
        const float originY = 100;
        Font font = Font;
        float referenceOffset = GetReferenceOffsetPx(font, baseline);

        // Alphabetic places the baseline exactly on the origin; every other value renders the
        // same glyphs shifted by the selected reference line's offset from that baseline.
        GlyphRenderer alphabetic = new();
        TextRenderer.RenderTo(alphabetic, text, Options(font, TextBaseline.Alphabetic, originY));

        GlyphRenderer anchored = new();
        TextRenderer.RenderTo(anchored, text, Options(font, baseline, originY));

        Assert.Equal(alphabetic.GlyphRects.Count, anchored.GlyphRects.Count);
        for (int i = 0; i < alphabetic.GlyphRects.Count; i++)
        {
            Assert.Equal(alphabetic.GlyphRects[i].Y - referenceOffset, anchored.GlyphRects[i].Y, Comparer);
            Assert.Equal(alphabetic.GlyphRects[i].X, anchored.GlyphRects[i].X, Comparer);
        }

        // Metrics agree with rendering: the absolute baseline of the first line sits at the
        // origin minus the reference offset.
        TextBlock block = new(text, Options(font, baseline, originY));
        LineMetrics line = block.GetLineMetrics(-1).Span[0];
        Assert.Equal(originY - referenceOffset, line.Start.Y + line.Baseline, Comparer);
    }

    [Fact]
    public void LineBox_IsTheDefault_AndMatchesLegacyLayout()
    {
        const string text = "Hxp";
        Font font = Font;

        TextOptions defaultOptions = new(font) { Origin = new Vector2(0, 100) };
        Assert.Equal(TextBaseline.LineBox, defaultOptions.TextBaseline);

        GlyphRenderer legacy = new();
        TextRenderer.RenderTo(legacy, text, defaultOptions);

        GlyphRenderer explicitLineBox = new();
        TextRenderer.RenderTo(explicitLineBox, text, Options(font, TextBaseline.LineBox, 100));

        Assert.Equal(legacy.GlyphRects.Count, explicitLineBox.GlyphRects.Count);
        for (int i = 0; i < legacy.GlyphRects.Count; i++)
        {
            Assert.Equal(legacy.GlyphRects[i], explicitLineBox.GlyphRects[i], Comparer);
        }
    }

    [Fact]
    public void GlyphId_LineBoxAnchorsEmBoxTop_AlphabeticRestoresBaseline()
    {
        Font font = Font;
        Assert.True(font.TryGetGlyphs(new CodePoint('A'), out Glyph? glyph));
        ushort glyphId = glyph.Value.GlyphMetrics.GlyphId;

        FontMetrics metrics = font.FontMetrics;
        float scalePx = font.Size / metrics.ScaleFactor * 72F;
        float ascenderPx = metrics.HorizontalMetrics.Ascender * scalePx;
        float deltaPx = (metrics.HorizontalMetrics.LineHeight - metrics.UnitsPerEm) * scalePx * .5F;

        GlyphRenderer lineBox = new();
        TextRenderer.RenderTo(lineBox, glyphId, GlyphIdOptions(font, TextBaseline.LineBox));

        GlyphRenderer alphabetic = new();
        TextRenderer.RenderTo(alphabetic, glyphId, GlyphIdOptions(font, TextBaseline.Alphabetic));

        // LineBox anchors the line-box top at the origin: the baseline sits one delta-adjusted
        // ascender below it, the delta centering the em box within the declared line height.
        Assert.Equal(alphabetic.GlyphRects[0].Y + ascenderPx - deltaPx, lineBox.GlyphRects[0].Y, Comparer);
        Assert.Equal(alphabetic.GlyphRects[0].X, lineBox.GlyphRects[0].X, Comparer);
    }

    [Fact]
    public void Text_MeasureAdvance_IsZeroBased_AndUnmovedByBaseline()
    {
        const string text = "Hxp";
        Font font = Font;
        FontRectangle reference = TextMeasurer.MeasureAdvance(text, Options(font, TextBaseline.LineBox, 0));

        Assert.Equal(0, reference.X);
        Assert.Equal(0, reference.Y);
        Assert.True(reference.Width > 0);
        Assert.True(reference.Height > 0);

        // The advance is a logical measure: no origin and no baseline anchor may move it.
        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            Assert.Equal(reference, TextMeasurer.MeasureAdvance(text, Options(font, baseline, 100)));
        }
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void Text_MeasureBounds_MatchesRenderedInk(TextBaseline baseline)
    {
        const string text = "Hxp";
        TextOptions options = Options(Font, baseline, 100);

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, text, options);

        // Measured ink bounds must equal the union of the boxes the renderer reports.
        FontRectangle expected = default;
        bool hasInk = false;
        foreach (FontRectangle rect in renderer.GlyphRects)
        {
            if (rect.Width <= 0 && rect.Height <= 0)
            {
                continue;
            }

            expected = hasInk ? FontRectangle.Union(expected, rect) : rect;
            hasInk = true;
        }

        Assert.True(hasInk);
        Assert.Equal(expected, TextMeasurer.MeasureBounds(text, options), Comparer);
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void Text_MeasureRenderableBounds_ComposesAdvanceAtOrigin(TextBaseline baseline)
    {
        const string text = "Hxp";
        const float originY = 100;
        TextOptions options = Options(Font, baseline, originY);

        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle expected = FontRectangle.Union(
            new FontRectangle(0, originY, advance.Width, advance.Height),
            bounds);

        Assert.Equal(expected, TextMeasurer.MeasureRenderableBounds(text, options), Comparer);
    }

    [Fact]
    public void GlyphId_Alphabetic_MatchesTextRenderedPosition()
    {
        Font font = Font;
        const float originY = 100;

        GlyphRenderer text = new();
        TextRenderer.RenderTo(text, "A", Options(font, TextBaseline.Alphabetic, originY));

        Assert.True(font.TryGetGlyphs(new CodePoint('A'), out Glyph? glyph));
        GlyphRenderer glyphId = new();
        TextRenderer.RenderTo(glyphId, glyph.Value.GlyphMetrics.GlyphId, GlyphIdOptions(font, TextBaseline.Alphabetic));

        // Both APIs anchor the alphabetic baseline at the origin, so a lone glyph must land
        // at the same position through either pipeline.
        Assert.Equal(text.GlyphRects[0], glyphId.GlyphRects[0], Comparer);
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Central)]
    public void BaselineOffset_ShiftsInkTowardOverSide_AdvanceUnchanged(TextBaseline baseline)
    {
        const string text = "Hxp";
        const float offset = 12.5F;
        Font font = Font;

        TextOptions reference = Options(font, baseline, 100);
        TextOptions shifted = Options(font, baseline, 100);
        shifted.BaselineOffset = offset;

        // A positive shift moves the whole block toward the over side, up for horizontal
        // layouts, without touching the inline axis. LineBox exercises the block-aligned
        // branch and the anchored baselines the reference-line branch.
        GlyphRenderer referenceRenderer = new();
        TextRenderer.RenderTo(referenceRenderer, text, reference);

        GlyphRenderer shiftedRenderer = new();
        TextRenderer.RenderTo(shiftedRenderer, text, shifted);

        Assert.Equal(referenceRenderer.GlyphRects.Count, shiftedRenderer.GlyphRects.Count);
        for (int i = 0; i < referenceRenderer.GlyphRects.Count; i++)
        {
            Assert.Equal(referenceRenderer.GlyphRects[i].Y - offset, shiftedRenderer.GlyphRects[i].Y, Comparer);
            Assert.Equal(referenceRenderer.GlyphRects[i].X, shiftedRenderer.GlyphRects[i].X, Comparer);
        }

        // The logical advance is unaffected, matching the CSS baseline-shift model.
        Assert.Equal(TextMeasurer.MeasureAdvance(text, reference), TextMeasurer.MeasureAdvance(text, shifted));

        // Measured ink carries the identical shift so measurement agrees with rendering.
        FontRectangle referenceBounds = TextMeasurer.MeasureBounds(text, reference);
        FontRectangle shiftedBounds = TextMeasurer.MeasureBounds(text, shifted);

        Assert.Equal(referenceBounds.Y - offset, shiftedBounds.Y, Comparer);
        Assert.Equal(referenceBounds.X, shiftedBounds.X, Comparer);

        // Line metrics report the same shifted baseline position.
        TextBlock referenceBlock = new(text, reference);
        TextBlock shiftedBlock = new(text, shifted);
        LineMetrics referenceLine = referenceBlock.GetLineMetrics(-1).Span[0];
        LineMetrics shiftedLine = shiftedBlock.GetLineMetrics(-1).Span[0];

        Assert.Equal(
            referenceLine.Start.Y + referenceLine.Baseline - offset,
            shiftedLine.Start.Y + shiftedLine.Baseline,
            Comparer);
    }

    [Theory]
    [InlineData(LayoutMode.VerticalLeftRight, TextBaseline.LineBox)]
    [InlineData(LayoutMode.VerticalLeftRight, TextBaseline.Central)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, TextBaseline.LineBox)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, TextBaseline.Central)]
    public void BaselineOffset_ShiftsInkTowardOverSide_Vertical(LayoutMode layoutMode, TextBaseline baseline)
    {
        const string text = "Hxp";
        const float offset = 12.5F;
        Font font = Font;

        TextOptions reference = Options(font, baseline, 100);
        reference.LayoutMode = layoutMode;

        TextOptions shifted = Options(font, baseline, 100);
        shifted.LayoutMode = layoutMode;
        shifted.BaselineOffset = offset;

        // In vertical layouts the over side is +X, so a positive shift moves columns right
        // without touching the block flow axis.
        GlyphRenderer referenceRenderer = new();
        TextRenderer.RenderTo(referenceRenderer, text, reference);

        GlyphRenderer shiftedRenderer = new();
        TextRenderer.RenderTo(shiftedRenderer, text, shifted);

        Assert.Equal(referenceRenderer.GlyphRects.Count, shiftedRenderer.GlyphRects.Count);
        for (int i = 0; i < referenceRenderer.GlyphRects.Count; i++)
        {
            Assert.Equal(referenceRenderer.GlyphRects[i].X + offset, shiftedRenderer.GlyphRects[i].X, Comparer);
            Assert.Equal(referenceRenderer.GlyphRects[i].Y, shiftedRenderer.GlyphRects[i].Y, Comparer);
        }

        // The logical advance is unaffected, matching the CSS baseline-shift model.
        Assert.Equal(TextMeasurer.MeasureAdvance(text, reference), TextMeasurer.MeasureAdvance(text, shifted));

        // Line metrics report the columns' shifted flow-axis position.
        TextBlock referenceBlock = new(text, reference);
        TextBlock shiftedBlock = new(text, shifted);
        LineMetrics referenceLine = referenceBlock.GetLineMetrics(-1).Span[0];
        LineMetrics shiftedLine = shiftedBlock.GetLineMetrics(-1).Span[0];

        Assert.Equal(referenceLine.Start.X + offset, shiftedLine.Start.X, Comparer);
    }

    [Fact]
    public void BaselineOffset_IsPixelUnits_IndependentOfDpi()
    {
        const string text = "Hxp";
        const float offset = 10F;
        Font font = Font;

        TextOptions reference = BrowserComparisonOptions(font, TextBaseline.Alphabetic, 100);
        TextOptions shifted = BrowserComparisonOptions(font, TextBaseline.Alphabetic, 100);
        shifted.BaselineOffset = offset;

        // The shift is specified in pixel units like the origin, so at 96 dpi the rendered
        // displacement is still exactly the requested pixel amount.
        FontRectangle referenceBounds = TextMeasurer.MeasureBounds(text, reference);
        FontRectangle shiftedBounds = TextMeasurer.MeasureBounds(text, shifted);

        Assert.Equal(referenceBounds.Y - offset, shiftedBounds.Y, Comparer);
        Assert.Equal(referenceBounds.X, shiftedBounds.X, Comparer);
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    public void GlyphId_BaselineOffset_ShiftsRenderAndMeasureTogether(LayoutMode layoutMode)
    {
        const float offset = 12.5F;
        Font font = Font;
        Assert.True(font.TryGetGlyphs(new CodePoint('A'), out Glyph? glyph));
        ushort glyphId = glyph.Value.GlyphMetrics.GlyphId;

        GlyphOptions reference = GlyphIdOptions(font, TextBaseline.Alphabetic);
        reference.LayoutMode = layoutMode;

        GlyphOptions shifted = GlyphIdOptions(font, TextBaseline.Alphabetic);
        shifted.LayoutMode = layoutMode;
        shifted.BaselineOffset = offset;

        GlyphRenderer referenceRenderer = new();
        TextRenderer.RenderTo(referenceRenderer, glyphId, reference);

        GlyphRenderer shiftedRenderer = new();
        TextRenderer.RenderTo(shiftedRenderer, glyphId, shifted);

        // The single-glyph pipeline shares the text shift model: toward the over side on
        // the axis the layout mode selects.
        if (layoutMode == LayoutMode.HorizontalTopBottom)
        {
            Assert.Equal(referenceRenderer.GlyphRects[0].Y - offset, shiftedRenderer.GlyphRects[0].Y, Comparer);
            Assert.Equal(referenceRenderer.GlyphRects[0].X, shiftedRenderer.GlyphRects[0].X, Comparer);
        }
        else
        {
            Assert.Equal(referenceRenderer.GlyphRects[0].X + offset, shiftedRenderer.GlyphRects[0].X, Comparer);
            Assert.Equal(referenceRenderer.GlyphRects[0].Y, shiftedRenderer.GlyphRects[0].Y, Comparer);
        }

        // Measurement mirrors the renderer exactly for the same options.
        Assert.Equal(shiftedRenderer.GlyphRects[0], TextMeasurer.MeasureBounds(glyphId, shifted), Comparer);

        // The glyph advance is a logical measure the shift may not move.
        Assert.Equal(TextMeasurer.MeasureAdvance(glyphId, reference), TextMeasurer.MeasureAdvance(glyphId, shifted));
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference(TextBaseline baseline)
    {
        const float originY = 120;
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, baseline, originY);

        // The red rule marks the origin so each reference image shows the selected reference
        // line of the text sitting exactly on it.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            beforeAction: static image => DrawOriginRule(image, (int)originY),
            properties: baseline);
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference_VerticalLeftRight(TextBaseline baseline)
        => TestVerticalLayout(LayoutMode.VerticalLeftRight, baseline, TestFonts.OpenSansFile, BrowserComparisonText);

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference_VerticalMixedLeftRight(TextBaseline baseline)
        => TestVerticalLayout(LayoutMode.VerticalMixedLeftRight, baseline, TestFonts.OpenSansFile, BrowserComparisonText);

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference_Cjk(TextBaseline baseline)
    {
        const float originY = 120;
        Font font = TestFonts.GetFont(TestFonts.NotoSansSCBaselineSubsetFile, BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, baseline, originY);

        // The red rule marks the origin so each reference image shows the selected reference
        // line of the text sitting exactly on it. Hanging exercises the metric fallback: the
        // font's baseline table defines the ideographic set but no hanging baseline.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonCjkText,
            options,
            beforeAction: static image => DrawOriginRule(image, (int)originY),
            properties: baseline);
    }

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference_CjkVerticalLeftRight(TextBaseline baseline)
        => TestVerticalLayout(LayoutMode.VerticalLeftRight, baseline, TestFonts.NotoSansSCBaselineSubsetFile, BrowserComparisonCjkText);

    [Theory]
    [InlineData(TextBaseline.LineBox)]
    [InlineData(TextBaseline.TextTop)]
    [InlineData(TextBaseline.Hanging)]
    [InlineData(TextBaseline.Middle)]
    [InlineData(TextBaseline.Central)]
    [InlineData(TextBaseline.Alphabetic)]
    [InlineData(TextBaseline.Ideographic)]
    [InlineData(TextBaseline.TextBottom)]
    public void RendersAnchoredToReference_CjkVerticalMixedLeftRight(TextBaseline baseline)
        => TestVerticalLayout(LayoutMode.VerticalMixedLeftRight, baseline, TestFonts.NotoSansSCBaselineSubsetFile, BrowserComparisonCjkText);

    [Theory]
    [InlineData(-40F)]
    [InlineData(-20F)]
    [InlineData(0F)]
    [InlineData(20F)]
    [InlineData(40F)]
    public void RendersBaselineShiftToReference(float baselineOffset)
    {
        const float originY = 120;
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, TextBaseline.Alphabetic, originY);
        options.BaselineOffset = baselineOffset;

        // The red rule marks the origin: the alphabetic baseline sits on it at zero shift
        // and moves toward the over side, up, as the shift increases.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            beforeAction: static image => DrawOriginRule(image, (int)originY),
            properties: baselineOffset);
    }

    [Theory]
    [InlineData(-40F)]
    [InlineData(-20F)]
    [InlineData(0F)]
    [InlineData(20F)]
    [InlineData(40F)]
    public void RendersBaselineShiftToReference_Hanging(float baselineOffset)
    {
        const float originY = 120;
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, BrowserComparisonPointSize);
        TextOptions options = BrowserComparisonOptions(font, TextBaseline.Hanging, originY);
        options.BaselineOffset = baselineOffset;

        // The shift composes with the anchor: the hanging baseline sits on the rule at zero
        // shift and the whole block moves from that anchored position as the shift changes.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            beforeAction: static image => DrawOriginRule(image, (int)originY),
            properties: baselineOffset);
    }

    [Theory]
    [InlineData(-40F)]
    [InlineData(-20F)]
    [InlineData(0F)]
    [InlineData(20F)]
    [InlineData(40F)]
    public void RendersBaselineShiftToReference_VerticalLeftRight(float baselineOffset)
        => TestVerticalShiftLayout(LayoutMode.VerticalLeftRight, baselineOffset);

    [Theory]
    [InlineData(-40F)]
    [InlineData(-20F)]
    [InlineData(0F)]
    [InlineData(20F)]
    [InlineData(40F)]
    public void RendersBaselineShiftToReference_VerticalMixedLeftRight(float baselineOffset)
        => TestVerticalShiftLayout(LayoutMode.VerticalMixedLeftRight, baselineOffset);

    private static void TestVerticalShiftLayout(
        LayoutMode layoutMode,
        float baselineOffset,
        [System.Runtime.CompilerServices.CallerMemberName] string test = "")
    {
        const float originX = 120;
        TextOptions options = new(TestFonts.GetFont(TestFonts.OpenSansFile, BrowserComparisonPointSize))
        {
            Origin = new Vector2(originX, 10),
            TextBaseline = TextBaseline.Central,
            BaselineOffset = baselineOffset,
            LayoutMode = layoutMode,
            Dpi = 96F
        };

        // Columns anchor the central axis on the origin rule at zero shift and move toward
        // the over side, right, as the shift increases.
        TextLayoutTestUtilities.TestLayout(
            BrowserComparisonText,
            options,
            test: test,
            beforeAction: static image => DrawOriginColumnRule(image, (int)originX),
            properties: baselineOffset);
    }

    private static void TestVerticalLayout(
        LayoutMode layoutMode,
        TextBaseline baseline,
        string fontFile,
        string text,
        [System.Runtime.CompilerServices.CallerMemberName] string test = "")
    {
        const float originX = 120;
        TextOptions options = new(TestFonts.GetFont(fontFile, BrowserComparisonPointSize))
        {
            Origin = new Vector2(originX, 10),
            TextBaseline = baseline,
            LayoutMode = layoutMode,
            Dpi = 96F
        };

        // Columns anchor along X from the central column axis; the red rule marks the origin
        // column so each reference image shows the selected reference line sitting on it.
        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            test: test,
            beforeAction: static image => DrawOriginColumnRule(image, (int)originX),
            properties: baseline);
    }

    private static void DrawOriginRule(Image<Rgba32> image, int y)
    {
        Rgba32 red = Color.Red.ToPixel<Rgba32>();
        for (int x = 0; x < image.Width; x++)
        {
            image[x, y] = red;
        }
    }

    private static void DrawOriginColumnRule(Image<Rgba32> image, int x)
    {
        Rgba32 red = Color.Red.ToPixel<Rgba32>();
        for (int y = 0; y < image.Height; y++)
        {
            image[x, y] = red;
        }
    }

    private static TextOptions Options(Font font, TextBaseline baseline, float originY)
        => new(font)
        {
            Origin = new Vector2(0, originY),
            TextBaseline = baseline
        };

    // 96 dpi maps the font's point size one to one onto CSS pixels so the browser page
    // mirrors the rendered geometry exactly, matching the other browser comparison tests.
    private static TextOptions BrowserComparisonOptions(Font font, TextBaseline baseline, float originY)
        => new(font)
        {
            Origin = new Vector2(0, originY),
            TextBaseline = baseline,
            Dpi = 96F
        };

    private static GlyphOptions GlyphIdOptions(Font font, TextBaseline baseline)
        => new()
        {
            Font = font,
            Origin = new Vector2(0, 100),
            TextBaseline = baseline
        };

    private static float GetReferenceOffsetPx(Font font, TextBaseline baseline)
    {
        // Expectations derive from the public font metrics at the default 72 DPI, mirroring
        // the documented definition of each reference line rather than the implementation.
        FontMetrics metrics = font.FontMetrics;
        float scale = font.Size / metrics.ScaleFactor * 72F;
        float ascender = metrics.HorizontalMetrics.Ascender * scale;
        float descender = metrics.HorizontalMetrics.Descender * scale;
        float xHeight = metrics.XHeight * scale;
        if (xHeight <= 0)
        {
            xHeight = ascender * .5F;
        }

        return baseline switch
        {
            TextBaseline.TextTop => -ascender,
            TextBaseline.Hanging => -0.8F * ascender,
            TextBaseline.Middle => -xHeight * .5F,
            TextBaseline.Central => -(ascender + descender) * .5F,
            TextBaseline.Ideographic or TextBaseline.TextBottom => -descender,
            _ => 0F,
        };
    }
}

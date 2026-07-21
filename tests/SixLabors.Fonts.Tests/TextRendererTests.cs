// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

public class TextRendererTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    private static Font Font => TextLayoutTests.CreateRenderingFont();

    [Fact]
    public void RenderTo_VisibleBounds_CoveringBandMatchesFullRender()
    {
        const string text = "Alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu.";
        TextOptions options = Options(90);

        GlyphRenderer full = new();
        TextRenderer.RenderTo(full, text, options);

        // The banded one-shot path takes the region from the options, unlike TextBlock which
        // takes it per call.
        TextOptions bandedOptions = Options(90);
        bandedOptions.VisibleBounds = new TextBlock(text, Options(-1)).MeasureRenderableBounds(90);

        GlyphRenderer culled = new();
        TextRenderer.RenderTo(culled, text, bandedOptions);

        Assert.Equal(full.GlyphRects.Count, culled.GlyphRects.Count);
        for (int i = 0; i < full.GlyphRects.Count; i++)
        {
            Assert.Equal(full.GlyphRects[i], culled.GlyphRects[i], Comparer);
        }
    }

    [Fact]
    public void RenderTo_VisibleBounds_CullsLinesAndStopsBreakingOutsideBand()
    {
        const string text = "Alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi.";
        const float wrappingLength = 90;
        TextOptions options = Options(wrappingLength);

        GlyphRenderer full = new();
        TextRenderer.RenderTo(full, text, options);

        TextBlock block = new(text, Options(-1));
        ReadOnlySpan<LineMetrics> lines = block.GetLineMetrics(wrappingLength).Span;
        Assert.True(lines.Length >= 7);
        LineMetrics middleLine = lines[lines.Length / 2];

        // Default options are eligible for early-terminated line breaking, so this exercises
        // the truncated-box path end to end.
        TextOptions bandedOptions = Options(wrappingLength);
        bandedOptions.VisibleBounds = new FontRectangle(0, middleLine.Start.Y, 10000, middleLine.Extent.Y);

        GlyphRenderer culled = new();
        TextRenderer.RenderTo(culled, text, bandedOptions);

        int offset = AssertRendersContiguousSliceOfFull(full, culled);
        Assert.True(offset > 0);
        Assert.True(offset + culled.GlyphRects.Count < full.GlyphRects.Count);
    }

    [Fact]
    public void RenderTo_VisibleBounds_AlignmentRequiringFullLineSet_MatchesFullRender()
    {
        const string text = "Alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi.";
        const float wrappingLength = 90;
        TextOptions options = Options(wrappingLength);
        options.HorizontalAlignment = HorizontalAlignment.Center;

        GlyphRenderer full = new();
        TextRenderer.RenderTo(full, text, options);

        TextOptions measureOptions = Options(-1);
        measureOptions.HorizontalAlignment = HorizontalAlignment.Center;
        TextBlock block = new(text, measureOptions);
        ReadOnlySpan<LineMetrics> lines = block.GetLineMetrics(wrappingLength).Span;
        Assert.True(lines.Length >= 7);
        LineMetrics middleLine = lines[lines.Length / 2];

        // Centered block alignment depends on the widest line, so breaking must not terminate
        // early; the banded walk over the full line set still has to place the visible slice
        // exactly where the full render does.
        TextOptions bandedOptions = Options(wrappingLength);
        bandedOptions.HorizontalAlignment = HorizontalAlignment.Center;
        bandedOptions.VisibleBounds = new FontRectangle(-10000, middleLine.Start.Y, 20000, middleLine.Extent.Y);

        GlyphRenderer culled = new();
        TextRenderer.RenderTo(culled, text, bandedOptions);

        int offset = AssertRendersContiguousSliceOfFull(full, culled);
        Assert.True(offset > 0);
        Assert.True(offset + culled.GlyphRects.Count < full.GlyphRects.Count);
    }

    private static TextOptions Options(float wrappingLength)
        => new(Font) { WrappingLength = wrappingLength };

    private static int AssertRendersContiguousSliceOfFull(GlyphRenderer full, GlyphRenderer culled)
    {
        Assert.True(culled.GlyphRects.Count > 0);
        Assert.True(culled.GlyphRects.Count < full.GlyphRects.Count);

        int offset = full.GlyphRects.FindIndex(rect => rect.Equals(culled.GlyphRects[0]));
        Assert.True(offset >= 0);
        Assert.True(offset + culled.GlyphRects.Count <= full.GlyphRects.Count);
        for (int i = 0; i < culled.GlyphRects.Count; i++)
        {
            Assert.Equal(full.GlyphRects[i + offset], culled.GlyphRects[i], Comparer);
        }

        return offset;
    }
}

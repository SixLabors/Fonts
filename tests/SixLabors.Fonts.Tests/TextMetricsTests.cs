// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class TextMetricsTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    [Fact]
    public void Measure_EmptyString_ReturnsEmptyMetrics()
    {
        Font font = TextLayoutTests.CreateFont("hello world");
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics metrics = TextMeasurer.Measure(string.Empty, options);

        Assert.Equal(FontRectangle.Empty, metrics.Advance, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Bounds, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.RenderableBounds, Comparer);
        Assert.Equal(0, metrics.LineCount);
        Assert.True(metrics.MeasureGlyphAdvances().IsEmpty);
        Assert.True(metrics.MeasureGlyphBounds().IsEmpty);
        Assert.True(metrics.MeasureGlyphRenderableBounds().IsEmpty);
        Assert.True(metrics.GraphemeMetrics.IsEmpty);
        Assert.True(metrics.LineMetrics.IsEmpty);
    }

    [Fact]
    public void Measure_StringAndSpanOverloads_Match()
    {
        const string text = "hello world\nhello world";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics fromString = TextMeasurer.Measure(text, options);
        TextMetrics fromSpan = TextMeasurer.Measure(text.AsSpan(), options);

        Assert.Equal(fromString.Advance, fromSpan.Advance, Comparer);
        Assert.Equal(fromString.Bounds, fromSpan.Bounds, Comparer);
        Assert.Equal(fromString.RenderableBounds, fromSpan.RenderableBounds, Comparer);
        Assert.Equal(fromString.LineCount, fromSpan.LineCount);
        Assert.Equal(fromString.MeasureGlyphAdvances().Length, fromSpan.MeasureGlyphAdvances().Length);
        Assert.Equal(fromString.LineMetrics.Length, fromSpan.LineMetrics.Length);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("hello\nworld")]
    [InlineData("hello world\nhello world")]
    [InlineData("a b\nc")]
    public void Measure_MatchesGranularRectangles(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(TextMeasurer.MeasureAdvance(text, options), metrics.Advance, Comparer);
        Assert.Equal(TextMeasurer.MeasureBounds(text, options), metrics.Bounds, Comparer);
        Assert.Equal(TextMeasurer.MeasureRenderableBounds(text, options), metrics.RenderableBounds, Comparer);
        Assert.Equal(TextMeasurer.CountLines(text, options), metrics.LineCount);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_MeasureGlyphAdvances_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        ReadOnlySpan<GlyphBounds> expected = TextMeasurer.MeasureGlyphAdvances(text, options).Span;
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.MeasureGlyphAdvances().Span);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_MeasureGlyphBounds_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        ReadOnlySpan<GlyphBounds> expected = TextMeasurer.MeasureGlyphBounds(text, options).Span;
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.MeasureGlyphBounds().Span);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_MeasureGlyphRenderableBounds_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        ReadOnlySpan<GlyphBounds> expected = TextMeasurer.MeasureGlyphRenderableBounds(text, options).Span;
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.MeasureGlyphRenderableBounds().Span);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("hello\nworld")]
    [InlineData("hello world\nhello world\nhello")]
    public void Measure_LineMetrics_MatchGetLineMetrics(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        ReadOnlySpan<LineMetrics> expected = TextMeasurer.GetLineMetrics(text, options).Span;
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(expected.Length, metrics.LineMetrics.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            LineMetrics e = expected[i];
            LineMetrics a = metrics.LineMetrics[i];
            Assert.Equal(e.Ascender, a.Ascender, Comparer);
            Assert.Equal(e.Baseline, a.Baseline, Comparer);
            Assert.Equal(e.Descender, a.Descender, Comparer);
            Assert.Equal(e.LineHeight, a.LineHeight, Comparer);
            Assert.Equal(e.Start, a.Start, Comparer);
            Assert.Equal(e.Extent, a.Extent, Comparer);
            Assert.Equal(e.StringIndex, a.StringIndex);
            Assert.Equal(e.GraphemeIndex, a.GraphemeIndex);
            Assert.Equal(e.GraphemeCount, a.GraphemeCount);
        }
    }

    [Fact]
    public void Measure_PerCharacterArrays_HaveMatchingLengthAndCodepoints()
    {
        const string text = "hello world\nhello";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        int count = metrics.MeasureGlyphAdvances().Length;
        Assert.Equal(count, metrics.MeasureGlyphBounds().Length);
        Assert.Equal(count, metrics.MeasureGlyphRenderableBounds().Length);

        for (int i = 0; i < count; i++)
        {
            CodePoint cp = metrics.MeasureGlyphAdvances().Span[i].Codepoint;
            Assert.Equal(cp, metrics.MeasureGlyphBounds().Span[i].Codepoint);
            Assert.Equal(cp, metrics.MeasureGlyphRenderableBounds().Span[i].Codepoint);
        }
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    public void Measure_MatchesGranularRectangles_AcrossLayoutModes(LayoutMode layoutMode)
    {
        const string text = "hello world";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            LayoutMode = layoutMode
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(TextMeasurer.MeasureAdvance(text, options), metrics.Advance, Comparer);
        Assert.Equal(TextMeasurer.MeasureBounds(text, options), metrics.Bounds, Comparer);
        Assert.Equal(TextMeasurer.MeasureRenderableBounds(text, options), metrics.RenderableBounds, Comparer);
    }

    private static void AssertGlyphBoundsEqual(ReadOnlySpan<GlyphBounds> expected, ReadOnlySpan<GlyphBounds> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            GlyphBounds e = expected[i];
            GlyphBounds a = actual[i];
            Assert.Equal(e.Codepoint, a.Codepoint);
            Assert.Equal(e.GraphemeIndex, a.GraphemeIndex);
            Assert.Equal(e.StringIndex, a.StringIndex);
            Assert.Equal(e.Bounds, a.Bounds, Comparer);
        }
    }
}

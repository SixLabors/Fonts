// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class TextMetricsTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    [Fact]
    public void Empty_HasZeroedRectanglesAndEmptyCollections()
    {
        TextMetrics metrics = TextMetrics.Empty;

        Assert.Equal(FontRectangle.Empty, metrics.Advance, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Bounds, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Size, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.RenderableBounds, Comparer);
        Assert.Equal(0, metrics.LineCount);
        Assert.Empty(metrics.CharacterAdvances);
        Assert.Empty(metrics.CharacterSizes);
        Assert.Empty(metrics.CharacterBounds);
        Assert.Empty(metrics.CharacterRenderableBounds);
        Assert.Empty(metrics.Lines);
    }

    [Fact]
    public void Measure_EmptyString_ReturnsEmptyMetrics()
    {
        Font font = TextLayoutTests.CreateFont("hello world");
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics metrics = TextMeasurer.Measure(string.Empty, options);

        Assert.Equal(FontRectangle.Empty, metrics.Advance, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Bounds, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Size, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.RenderableBounds, Comparer);
        Assert.Equal(0, metrics.LineCount);
        Assert.Empty(metrics.CharacterAdvances);
        Assert.Empty(metrics.CharacterSizes);
        Assert.Empty(metrics.CharacterBounds);
        Assert.Empty(metrics.CharacterRenderableBounds);
        Assert.Empty(metrics.Lines);
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
        Assert.Equal(fromString.Size, fromSpan.Size, Comparer);
        Assert.Equal(fromString.RenderableBounds, fromSpan.RenderableBounds, Comparer);
        Assert.Equal(fromString.LineCount, fromSpan.LineCount);
        Assert.Equal(fromString.CharacterAdvances.Count, fromSpan.CharacterAdvances.Count);
        Assert.Equal(fromString.Lines.Count, fromSpan.Lines.Count);
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
        Assert.Equal(TextMeasurer.MeasureSize(text, options), metrics.Size, Comparer);
        Assert.Equal(TextMeasurer.MeasureRenderableBounds(text, options), metrics.RenderableBounds, Comparer);
        Assert.Equal(TextMeasurer.CountLines(text, options), metrics.LineCount);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_CharacterAdvances_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        bool hasAdvances = TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> expected);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(hasAdvances, metrics.CharacterAdvances.Any(g => g.Bounds.Width > 0 || g.Bounds.Height > 0));
        AssertGlyphBoundsEqual(expected, metrics.CharacterAdvances);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_CharacterBounds_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> expected);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.CharacterBounds);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_CharacterSizes_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> expected);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.CharacterSizes);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b\nc")]
    public void Measure_CharacterRenderableBounds_MatchGranularOverload(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMeasurer.TryMeasureCharacterRenderableBounds(text, options, out ReadOnlySpan<GlyphBounds> expected);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        AssertGlyphBoundsEqual(expected, metrics.CharacterRenderableBounds);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("hello\nworld")]
    [InlineData("hello world\nhello world\nhello")]
    public void Measure_Lines_MatchGetLineMetrics(string text)
    {
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        LineMetrics[] expected = TextMeasurer.GetLineMetrics(text, options);
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        Assert.Equal(expected.Length, metrics.Lines.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            LineMetrics e = expected[i];
            LineMetrics a = metrics.Lines[i];
            Assert.Equal(e.Ascender, a.Ascender, Comparer);
            Assert.Equal(e.Baseline, a.Baseline, Comparer);
            Assert.Equal(e.Descender, a.Descender, Comparer);
            Assert.Equal(e.LineHeight, a.LineHeight, Comparer);
            Assert.Equal(e.Start, a.Start, Comparer);
            Assert.Equal(e.Extent, a.Extent, Comparer);
            Assert.Equal(e.StringIndex, a.StringIndex);
            Assert.Equal(e.GraphemeIndex, a.GraphemeIndex);
            Assert.Equal(e.GraphemeCount, a.GraphemeCount);
            Assert.Equal(e.GlyphIndex, a.GlyphIndex);
            Assert.Equal(e.GlyphCount, a.GlyphCount);
        }
    }

    [Fact]
    public void Measure_PerCharacterArrays_HaveMatchingLengthAndCodepoints()
    {
        const string text = "hello world\nhello";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        int count = metrics.CharacterAdvances.Count;
        Assert.Equal(count, metrics.CharacterSizes.Count);
        Assert.Equal(count, metrics.CharacterBounds.Count);
        Assert.Equal(count, metrics.CharacterRenderableBounds.Count);

        for (int i = 0; i < count; i++)
        {
            CodePoint cp = metrics.CharacterAdvances[i].Codepoint;
            Assert.Equal(cp, metrics.CharacterSizes[i].Codepoint);
            Assert.Equal(cp, metrics.CharacterBounds[i].Codepoint);
            Assert.Equal(cp, metrics.CharacterRenderableBounds[i].Codepoint);
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

    private static void AssertGlyphBoundsEqual(ReadOnlySpan<GlyphBounds> expected, IReadOnlyList<GlyphBounds> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
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

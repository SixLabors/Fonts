// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests;

public class TextBlockTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    private static Font Font => TextLayoutTests.CreateRenderingFont();

    [Fact]
    public void Constructor_IgnoresWrappingLength()
    {
        const string text = "The quick brown fox jumps over the lazy dog.";
        TextOptions preparedOptions = Options(35);
        TextBlock block = new(text, preparedOptions);

        TextMetrics expected = TextMeasurer.Measure(text, Options(-1));
        TextMetrics actual = block.Measure(-1);

        AssertTextMetricsEqual(expected, actual);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(160)]
    [InlineData(-1)]
    public void Measure_ReusesPreparedText_ForDifferentWrappingLengths(float wrappingLength)
    {
        const string text = "The paragraph begins in English, then says שלום and مرحبا before returning to Latin text.";
        TextBlock block = new(text, Options(-1));

        TextMetrics expected = TextMeasurer.Measure(text, Options(wrappingLength));
        TextMetrics actual = block.Measure(wrappingLength);

        AssertTextMetricsEqual(expected, actual);
    }

    [Fact]
    public void GranularMeasurements_MatchTextMeasurer()
    {
        const string text = "Hello world\nSecond line";
        const float wrappingLength = 95;
        TextBlock block = new(text, Options(-1));

        Assert.Equal(
            TextMeasurer.MeasureAdvance(text, Options(wrappingLength)),
            block.MeasureAdvance(wrappingLength),
            Comparer);

        Assert.Equal(
            TextMeasurer.MeasureBounds(text, Options(wrappingLength)),
            block.MeasureBounds(wrappingLength),
            Comparer);

        Assert.Equal(
            TextMeasurer.MeasureRenderableBounds(text, Options(wrappingLength)),
            block.MeasureRenderableBounds(wrappingLength),
            Comparer);

        Assert.Equal(TextMeasurer.CountLines(text, Options(wrappingLength)), block.CountLines(wrappingLength));
        AssertLineMetricsEqual(TextMeasurer.GetLineMetrics(text, Options(wrappingLength)), block.GetLineMetrics(wrappingLength));
    }

    [Fact]
    public void GetLineMetrics_IncludesSourceMapping()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineMetrics> metrics = block.Measure(-1).LineMetrics;

        Assert.Equal(2, metrics.Length);
        Assert.Equal(0, metrics[0].StringIndex);
        Assert.Equal(firstLine.Length, metrics[1].StringIndex);
        Assert.Equal(0, metrics[0].GraphemeIndex);
        Assert.Equal(metrics[0].GraphemeCount, metrics[1].GraphemeIndex);
    }

    [Fact]
    public void LayoutLines_ReturnsMetricsAndGraphemeMetricsPerLine()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1);
        ReadOnlySpan<LineMetrics> metrics = block.GetLineMetrics(-1);

        Assert.Equal(2, lines.Length);
        AssertLineMetricsEqual(metrics, lines);

        Assert.Equal(firstLine.Length, lines[0].GraphemeMetrics.Length);
        Assert.Equal("Second line".Length, lines[1].GraphemeMetrics.Length);
        Assert.Equal(0, lines[0].GraphemeMetrics[0].StringIndex);
        Assert.Equal(firstLine.Length - 1, lines[0].GraphemeMetrics[^1].StringIndex);
        Assert.Equal(firstLine.Length, lines[1].GraphemeMetrics[0].StringIndex);
    }

    [Fact]
    public void LayoutLines_LeadingHardBreak_IncludesHardBreakGrapheme()
    {
        const string text = "\n\tHelloworld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1);

        Assert.Equal(2, lines.Length);
        Assert.Equal(1, lines[0].GraphemeMetrics.Length);
        Assert.Equal(0, lines[0].GraphemeMetrics[0].StringIndex);
        Assert.False(lines[1].GraphemeMetrics.IsEmpty);
        Assert.Equal(1, lines[1].GraphemeMetrics[0].StringIndex);
    }

    [Fact]
    public void CharacterMeasurements_MatchTextMeasurer()
    {
        const string text = "A quick test.";
        const float wrappingLength = 70;
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<GlyphBounds> expectedAdvances = TextMeasurer.MeasureGlyphAdvances(text, Options(wrappingLength));
        ReadOnlySpan<GlyphBounds> actualAdvances = block.MeasureGlyphAdvances(wrappingLength);

        AssertGlyphBoundsEqual(expectedAdvances, actualAdvances);

        ReadOnlySpan<GlyphBounds> expectedBounds = TextMeasurer.MeasureGlyphBounds(text, Options(wrappingLength));
        ReadOnlySpan<GlyphBounds> actualBounds = block.MeasureGlyphBounds(wrappingLength);

        AssertGlyphBoundsEqual(expectedBounds, actualBounds);

        ReadOnlySpan<GlyphBounds> expectedRenderableBounds = TextMeasurer.MeasureGlyphRenderableBounds(text, Options(wrappingLength));
        ReadOnlySpan<GlyphBounds> actualRenderableBounds = block.MeasureGlyphRenderableBounds(wrappingLength);

        AssertGlyphBoundsEqual(expectedRenderableBounds, actualRenderableBounds);

        ReadOnlySpan<GraphemeMetrics> expectedGraphemeMetrics = TextMeasurer.GetGraphemeMetrics(text, Options(wrappingLength));
        ReadOnlySpan<GraphemeMetrics> actualGraphemeMetrics = block.GetGraphemeMetrics(wrappingLength);

        AssertGraphemeMetricsEqual(expectedGraphemeMetrics, actualGraphemeMetrics);
    }

    [Fact]
    public void LayoutLines_MeasureGlyphBounds_MatchBlockSlices()
    {
        const string text = "Hello\nWorld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1);
        ReadOnlySpan<GlyphBounds> expectedAdvances = block.MeasureGlyphAdvances(-1);
        ReadOnlySpan<GlyphBounds> expectedBounds = block.MeasureGlyphBounds(-1);
        ReadOnlySpan<GlyphBounds> expectedRenderableBounds = block.MeasureGlyphRenderableBounds(-1);

        int glyphIndex = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            ReadOnlySpan<GlyphBounds> lineAdvances = lines[i].MeasureGlyphAdvances();
            ReadOnlySpan<GlyphBounds> lineBounds = lines[i].MeasureGlyphBounds();
            ReadOnlySpan<GlyphBounds> lineRenderableBounds = lines[i].MeasureGlyphRenderableBounds();

            AssertGlyphBoundsEqual(expectedAdvances.Slice(glyphIndex, lineAdvances.Length), lineAdvances);
            AssertGlyphBoundsEqual(expectedBounds.Slice(glyphIndex, lineBounds.Length), lineBounds);
            AssertGlyphBoundsEqual(expectedRenderableBounds.Slice(glyphIndex, lineRenderableBounds.Length), lineRenderableBounds);

            glyphIndex += lineAdvances.Length;
        }

        Assert.Equal(expectedAdvances.Length, glyphIndex);
    }

    [Fact]
    public void EmptyText_ReturnsEmptyMeasurements()
    {
        TextBlock block = new(string.Empty, Options(-1));

        TextMetrics metrics = block.Measure(100);

        Assert.Equal(FontRectangle.Empty, metrics.Advance, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Bounds, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.RenderableBounds, Comparer);
        Assert.Equal(0, metrics.LineCount);
        Assert.True(metrics.MeasureGlyphAdvances().IsEmpty);
        Assert.True(metrics.MeasureGlyphBounds().IsEmpty);
        Assert.True(metrics.MeasureGlyphRenderableBounds().IsEmpty);
        Assert.True(metrics.GraphemeMetrics.IsEmpty);
        Assert.True(metrics.LineMetrics.IsEmpty);
        Assert.Equal(FontRectangle.Empty, block.MeasureAdvance(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureBounds(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureRenderableBounds(100), Comparer);
        Assert.Equal(0, block.CountLines(100));
        Assert.True(block.GetLineMetrics(100).IsEmpty);

        Assert.True(block.MeasureGlyphAdvances(100).IsEmpty);

        Assert.True(block.MeasureGlyphBounds(100).IsEmpty);

        Assert.True(block.MeasureGlyphRenderableBounds(100).IsEmpty);

        Assert.True(block.GetGraphemeMetrics(100).IsEmpty);
    }

    private static TextOptions Options(float wrappingLength)
        => new(Font) { WrappingLength = wrappingLength };

    private static void AssertTextMetricsEqual(TextMetrics expected, TextMetrics actual)
    {
        Assert.Equal(expected.Advance, actual.Advance, Comparer);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.RenderableBounds, actual.RenderableBounds, Comparer);
        Assert.Equal(expected.LineCount, actual.LineCount);
        AssertGlyphBoundsEqual(expected.MeasureGlyphAdvances(), actual.MeasureGlyphAdvances());
        AssertGlyphBoundsEqual(expected.MeasureGlyphBounds(), actual.MeasureGlyphBounds());
        AssertGlyphBoundsEqual(expected.MeasureGlyphRenderableBounds(), actual.MeasureGlyphRenderableBounds());
        AssertGraphemeMetricsEqual(expected.GraphemeMetrics, actual.GraphemeMetrics);
        AssertLineMetricsEqual(expected.LineMetrics, actual.LineMetrics);
    }

    private static void AssertGlyphBoundsEqual(ReadOnlySpan<GlyphBounds> expected, ReadOnlySpan<GlyphBounds> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertGlyphBoundsEqual(expected[i], actual[i]);
        }
    }

    private static void AssertGlyphBoundsEqual(GlyphBounds expected, GlyphBounds actual)
    {
        Assert.Equal(expected.Codepoint, actual.Codepoint);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.GraphemeIndex, actual.GraphemeIndex);
        Assert.Equal(expected.StringIndex, actual.StringIndex);
    }

    private static void AssertGraphemeMetricsEqual(ReadOnlySpan<GraphemeMetrics> expected, ReadOnlySpan<GraphemeMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Advance, actual[i].Advance, Comparer);
            Assert.Equal(expected[i].Bounds, actual[i].Bounds, Comparer);
            Assert.Equal(expected[i].RenderableBounds, actual[i].RenderableBounds, Comparer);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].GraphemeIndex);
            Assert.Equal(expected[i].StringIndex, actual[i].StringIndex);
        }
    }

    private static void AssertLineMetricsEqual(ReadOnlySpan<LineMetrics> expected, ReadOnlySpan<LineMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Ascender, actual[i].Ascender, Comparer);
            Assert.Equal(expected[i].Baseline, actual[i].Baseline, Comparer);
            Assert.Equal(expected[i].Descender, actual[i].Descender, Comparer);
            Assert.Equal(expected[i].LineHeight, actual[i].LineHeight, Comparer);
            Assert.Equal(expected[i].Start, actual[i].Start, Comparer);
            Assert.Equal(expected[i].Extent, actual[i].Extent, Comparer);
            Assert.Equal(expected[i].StringIndex, actual[i].StringIndex);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].GraphemeIndex);
            Assert.Equal(expected[i].GraphemeCount, actual[i].GraphemeCount);
        }
    }

    private static void AssertLineMetricsEqual(ReadOnlySpan<LineMetrics> expected, ReadOnlySpan<LineLayout> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Ascender, actual[i].LineMetrics.Ascender, Comparer);
            Assert.Equal(expected[i].Baseline, actual[i].LineMetrics.Baseline, Comparer);
            Assert.Equal(expected[i].Descender, actual[i].LineMetrics.Descender, Comparer);
            Assert.Equal(expected[i].LineHeight, actual[i].LineMetrics.LineHeight, Comparer);
            Assert.Equal(expected[i].Start, actual[i].LineMetrics.Start, Comparer);
            Assert.Equal(expected[i].Extent, actual[i].LineMetrics.Extent, Comparer);
            Assert.Equal(expected[i].StringIndex, actual[i].LineMetrics.StringIndex);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].LineMetrics.GraphemeIndex);
            Assert.Equal(expected[i].GraphemeCount, actual[i].LineMetrics.GraphemeCount);
        }
    }
}

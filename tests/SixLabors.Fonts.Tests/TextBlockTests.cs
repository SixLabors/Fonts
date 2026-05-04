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
            TextMeasurer.MeasureSize(text, Options(wrappingLength)),
            block.MeasureSize(wrappingLength),
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
    public void GetLineMetrics_IncludesSourceAndGlyphMapping()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        TextMetrics textMetrics = block.Measure(-1);
        IReadOnlyList<LineMetrics> metrics = textMetrics.Lines;

        Assert.Equal(2, metrics.Count);
        Assert.Equal(0, metrics[0].StringIndex);
        Assert.Equal(firstLine.Length, metrics[1].StringIndex);
        Assert.Equal(0, metrics[0].GraphemeIndex);
        Assert.Equal(metrics[0].GraphemeCount, metrics[1].GraphemeIndex);
        Assert.Equal(0, metrics[0].GlyphIndex);
        Assert.Equal(metrics[0].GlyphCount, metrics[1].GlyphIndex);
        Assert.Equal(textMetrics.CharacterBounds.Count, metrics[0].GlyphCount + metrics[1].GlyphCount);
    }

    [Fact]
    public void CharacterMeasurements_MatchTextMeasurer()
    {
        const string text = "A quick test.";
        const float wrappingLength = 70;
        TextBlock block = new(text, Options(-1));

        Assert.Equal(
            TextMeasurer.TryMeasureCharacterAdvances(text, Options(wrappingLength), out ReadOnlySpan<GlyphBounds> expectedAdvances),
            block.TryMeasureCharacterAdvances(wrappingLength, out ReadOnlySpan<GlyphBounds> actualAdvances));

        AssertGlyphBoundsEqual(expectedAdvances, actualAdvances);

        Assert.Equal(
            TextMeasurer.TryMeasureCharacterSizes(text, Options(wrappingLength), out ReadOnlySpan<GlyphBounds> expectedSizes),
            block.TryMeasureCharacterSizes(wrappingLength, out ReadOnlySpan<GlyphBounds> actualSizes));

        AssertGlyphBoundsEqual(expectedSizes, actualSizes);

        Assert.Equal(
            TextMeasurer.TryMeasureCharacterBounds(text, Options(wrappingLength), out ReadOnlySpan<GlyphBounds> expectedBounds),
            block.TryMeasureCharacterBounds(wrappingLength, out ReadOnlySpan<GlyphBounds> actualBounds));

        AssertGlyphBoundsEqual(expectedBounds, actualBounds);

        Assert.Equal(
            TextMeasurer.TryMeasureCharacterRenderableBounds(text, Options(wrappingLength), out ReadOnlySpan<GlyphBounds> expectedRenderableBounds),
            block.TryMeasureCharacterRenderableBounds(wrappingLength, out ReadOnlySpan<GlyphBounds> actualRenderableBounds));

        AssertGlyphBoundsEqual(expectedRenderableBounds, actualRenderableBounds);
    }

    [Fact]
    public void EmptyText_ReturnsEmptyMeasurements()
    {
        TextBlock block = new(string.Empty, Options(-1));

        TextMetrics metrics = block.Measure(100);

        AssertTextMetricsEqual(TextMetrics.Empty, metrics);
        Assert.Equal(FontRectangle.Empty, block.MeasureAdvance(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureSize(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureBounds(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureRenderableBounds(100), Comparer);
        Assert.Equal(0, block.CountLines(100));
        Assert.Empty(block.GetLineMetrics(100));

        Assert.False(block.TryMeasureCharacterAdvances(100, out ReadOnlySpan<GlyphBounds> advances));
        Assert.True(advances.IsEmpty);

        Assert.False(block.TryMeasureCharacterSizes(100, out ReadOnlySpan<GlyphBounds> sizes));
        Assert.True(sizes.IsEmpty);

        Assert.False(block.TryMeasureCharacterBounds(100, out ReadOnlySpan<GlyphBounds> bounds));
        Assert.True(bounds.IsEmpty);

        Assert.False(block.TryMeasureCharacterRenderableBounds(100, out ReadOnlySpan<GlyphBounds> renderableBounds));
        Assert.True(renderableBounds.IsEmpty);
    }

    private static TextOptions Options(float wrappingLength)
        => new(Font) { WrappingLength = wrappingLength };

    private static void AssertTextMetricsEqual(TextMetrics expected, TextMetrics actual)
    {
        Assert.Equal(expected.Advance, actual.Advance, Comparer);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.Size, actual.Size, Comparer);
        Assert.Equal(expected.RenderableBounds, actual.RenderableBounds, Comparer);
        Assert.Equal(expected.LineCount, actual.LineCount);
        AssertGlyphBoundsEqual(expected.CharacterAdvances, actual.CharacterAdvances);
        AssertGlyphBoundsEqual(expected.CharacterSizes, actual.CharacterSizes);
        AssertGlyphBoundsEqual(expected.CharacterBounds, actual.CharacterBounds);
        AssertGlyphBoundsEqual(expected.CharacterRenderableBounds, actual.CharacterRenderableBounds);
        AssertLineMetricsEqual(expected.Lines, actual.Lines);
    }

    private static void AssertGlyphBoundsEqual(IReadOnlyList<GlyphBounds> expected, IReadOnlyList<GlyphBounds> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            AssertGlyphBoundsEqual(expected[i], actual[i]);
        }
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

    private static void AssertLineMetricsEqual(IReadOnlyList<LineMetrics> expected, IReadOnlyList<LineMetrics> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
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
            Assert.Equal(expected[i].GlyphIndex, actual[i].GlyphIndex);
            Assert.Equal(expected[i].GlyphCount, actual[i].GlyphCount);
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Characterization tests that pin the current output of <see cref="TextMeasurer"/>'s
/// granular overloads using the real OpenSans-Regular font at 12pt / 72 DPI.
/// </summary>
/// <remarks>
/// <para>
/// The numeric values baked into the <c>_Pinned</c> theories are a snapshot of the current
/// implementation. They are not derived from a spec — they guard against accidental drift
/// in the measurement pipeline during refactors. If an intentional change alters the output,
/// update the expected values to the new snapshot and document why in the commit message.
/// </para>
/// <para>
/// The <c>Invariant_</c> tests verify relationships that must hold regardless of font —
/// cross-method consistency, origin handling, string/span parity, empty-text behaviour.
/// </para>
/// </remarks>
public class TextMeasurerReferenceTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    private static Font Font => TextLayoutTests.CreateRenderingFont();

    private static TextOptions Options(float originX = 0, float originY = 0, LayoutMode layoutMode = LayoutMode.HorizontalTopBottom)
        => new(Font)
        {
            Origin = new Vector2(originX, originY),
            LayoutMode = layoutMode
        };

    [Theory]
    [InlineData("A", 0f, 0f, 7.5879f, 12f)]
    [InlineData("Hello", 0f, 0f, 28.8633f, 12f)]
    [InlineData("Hello, World!", 0f, 0f, 71.8301f, 12f)]
    [InlineData("The quick brown fox", 0f, 0f, 113.502f, 12f)]
    [InlineData("Hello\nWorld", 0f, 0f, 33.5742f, 24f)]
    [InlineData("A\nB\nC", 0f, 0f, 7.752f, 36f)]
    public void MeasureAdvance_Pinned(string text, float ex, float ey, float ew, float eh)
    {
        FontRectangle actual = TextMeasurer.MeasureAdvance(text, Options());
        Assert.Equal(new FontRectangle(ex, ey, ew, eh), actual, Comparer);
    }

    [Theory]
    [InlineData("A", 0f, 2.0537f, 7.5762f, 8.6016f)]
    [InlineData("Hello", 1.1719f, 1.5381f, 27.0352f, 9.2344f)]
    [InlineData("Hello, World!", 1.1719f, 1.5381f, 69.7617f, 10.6641f)]
    [InlineData("The quick brown fox", 0.1055f, 1.4736f, 113.168f, 12.0527f)]
    [InlineData("Hello\nWorld", 0.1758f, 1.5381f, 32.3672f, 21.2344f)]
    [InlineData("A\nB\nC", 0f, 2.0537f, 7.5762f, 32.7188f)]
    public void MeasureBounds_Pinned(string text, float ex, float ey, float ew, float eh)
    {
        FontRectangle actual = TextMeasurer.MeasureBounds(text, Options());
        Assert.Equal(new FontRectangle(ex, ey, ew, eh), actual, Comparer);
    }

    [Theory]
    [InlineData("A", 0f, 0f, 7.5879f, 12f)]
    [InlineData("Hello", 0f, 0f, 28.8633f, 12f)]

    // "Hello, World!" has comma descender — renderable height (12.2022) exceeds advance height (12).
    [InlineData("Hello, World!", 0f, 0f, 71.8301f, 12.2022f)]

    // "The quick brown fox" has q, p descenders — renderable height (13.5264) exceeds advance (12).
    [InlineData("The quick brown fox", 0f, 0f, 113.502f, 13.5264f)]
    [InlineData("Hello\nWorld", 0f, 0f, 33.5742f, 24f)]
    [InlineData("A\nB\nC", 0f, 0f, 7.752f, 36f)]
    public void MeasureRenderableBounds_Pinned(string text, float ex, float ey, float ew, float eh)
    {
        FontRectangle actual = TextMeasurer.MeasureRenderableBounds(text, Options());
        Assert.Equal(new FontRectangle(ex, ey, ew, eh), actual, Comparer);
    }

    [Fact]
    public void MeasureAdvance_OffsetOrigin_MatchesZeroOrigin()
    {
        const string text = "Hello, World!";
        FontRectangle atZero = TextMeasurer.MeasureAdvance(text, Options());
        FontRectangle atOffset = TextMeasurer.MeasureAdvance(text, Options(100, 50));
        Assert.Equal(atZero, atOffset, Comparer);
    }

    [Fact]
    public void MeasureBounds_OffsetOrigin_ShiftsByOrigin()
    {
        const string text = "Hello, World!";
        FontRectangle atZero = TextMeasurer.MeasureBounds(text, Options());
        FontRectangle atOffset = TextMeasurer.MeasureBounds(text, Options(100, 50));

        Assert.Equal(atZero.X + 100, atOffset.X, Comparer);
        Assert.Equal(atZero.Y + 50, atOffset.Y, Comparer);
        Assert.Equal(atZero.Width, atOffset.Width, Comparer);
        Assert.Equal(atZero.Height, atOffset.Height, Comparer);
    }

    [Fact]
    public void MeasureRenderableBounds_OffsetOrigin_ShiftsByOrigin()
    {
        const string text = "Hello, World!";
        FontRectangle atZero = TextMeasurer.MeasureRenderableBounds(text, Options());
        FontRectangle atOffset = TextMeasurer.MeasureRenderableBounds(text, Options(100, 50));

        Assert.Equal(atZero.X + 100, atOffset.X, Comparer);
        Assert.Equal(atZero.Y + 50, atOffset.Y, Comparer);
        Assert.Equal(atZero.Width, atOffset.Width, Comparer);
        Assert.Equal(atZero.Height, atOffset.Height, Comparer);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("Hello", 1)]
    [InlineData("Hello, World!", 1)]
    [InlineData("Hello\nWorld", 2)]
    [InlineData("A\nB\nC", 3)]
    [InlineData("A\nB\nC\nD", 4)]
    public void CountLines_Pinned(string text, int expected)
    {
        Assert.Equal(expected, TextMeasurer.CountLines(text, Options()));
    }

    [Theory]
    [InlineData("Hello", 1)]
    [InlineData("Hello\nWorld", 2)]
    [InlineData("A\nB\nC\nD", 4)]
    public void GetLineMetrics_Count_MatchesCountLines(string text, int expectedLineMetrics)
    {
        ReadOnlySpan<LineMetrics> metrics = TextMeasurer.GetLineMetrics(text, Options()).Span;
        Assert.Equal(expectedLineMetrics, metrics.Length);
        Assert.Equal(expectedLineMetrics, TextMeasurer.CountLines(text, Options()));
    }

    [Fact]
    public void GetLineMetrics_PerLineExtent_MatchesLineAdvance()
    {
        const string text = "Hello\nWorld world";
        ReadOnlySpan<LineMetrics> metrics = TextMeasurer.GetLineMetrics(text, Options()).Span;
        Assert.Equal(2, metrics.Length);

        FontRectangle line1Advance = TextMeasurer.MeasureAdvance("Hello", Options());
        FontRectangle line2Advance = TextMeasurer.MeasureAdvance("World world", Options());
        Assert.Equal(line1Advance.Width, metrics[0].Extent.X, Comparer);
        Assert.Equal(line2Advance.Width, metrics[1].Extent.X, Comparer);
    }

    [Fact]
    public void GetLineMetrics_AscenderBaselineDescender_AreOrdered()
    {
        ReadOnlySpan<LineMetrics> metrics = TextMeasurer.GetLineMetrics("Hello", Options()).Span;
        Assert.Equal(1, metrics.Length);
        LineMetrics m = metrics[0];
        Assert.True(m.Ascender < m.Baseline, $"Ascender ({m.Ascender}) should be < Baseline ({m.Baseline})");
        Assert.True(m.Baseline < m.Descender, $"Baseline ({m.Baseline}) should be < Descender ({m.Descender})");
        Assert.True(m.LineHeight > 0);
    }

    [Fact]
    public void GetLineMetrics_EmptyText_ReturnsEmpty()
        => Assert.True(TextMeasurer.GetLineMetrics(string.Empty, Options()).IsEmpty);

    [Fact]
    public void GetGlyphMetrics_Hi_Pinned()
    {
        const string text = "Hi!";
        ReadOnlySpan<GlyphMetrics> metrics = TextMeasurer.GetGlyphMetrics(text, Options()).Span;

        GlyphMetrics[] expected =
        [
            new(
                new CodePoint('H'),
                new FontRectangle(0, 0, 8.8477f, 12f),
                new FontRectangle(1.1719f, 2.0889f, 6.4922f, 8.5664f),
                new FontRectangle(0f, 0f, 8.8477f, 12f),
                0,
                0),
            new(
                new CodePoint('i'),
                new FontRectangle(8.8477f, 0, 3.0293f, 12f),
                new FontRectangle(9.7852f, 1.8311f, 1.1719f, 8.8242f),
                new FontRectangle(8.8477f, 0f, 3.0293f, 12f),
                1,
                1),
            new(
                new CodePoint('!'),
                new FontRectangle(11.877f, 0, 3.1699f, 12f),
                new FontRectangle(12.7559f, 2.0889f, 1.3945f, 8.7305f),
                new FontRectangle(11.877f, 0f, 3.1699f, 12f),
                2,
                2),
        ];

        AssertGlyphMetricsEqual(expected, metrics);
    }

    [Fact]
    public void GetGraphemeMetrics_Hi_Pinned()
    {
        const string text = "Hi!";
        ReadOnlySpan<GraphemeMetrics> graphemes = TextMeasurer.GetGraphemeMetrics(text, Options()).Span;

        GraphemeMetrics[] expected =
        [
            new(
                new FontRectangle(0, 0, 8.8477f, 12f),
                new FontRectangle(1.1719f, 2.0889f, 6.4922f, 8.5664f),
                new FontRectangle(0, 0, 8.8477f, 12f),
                0,
                0,
                0,
                false),
            new(
                new FontRectangle(8.8477f, 0, 3.0293f, 12f),
                new FontRectangle(9.7852f, 1.8311f, 1.1719f, 8.8242f),
                new FontRectangle(8.8477f, 0, 3.0293f, 12f),
                1,
                1,
                0,
                false),
            new(
                new FontRectangle(11.877f, 0, 3.1699f, 12f),
                new FontRectangle(12.7559f, 2.0889f, 1.3945f, 8.7305f),
                new FontRectangle(11.877f, 0, 3.1699f, 12f),
                2,
                2,
                0,
                false),
        ];
        AssertGraphemeMetricsEqual(expected, graphemes);
    }

    [Fact]
    public void GetGlyphMetrics_Codepoints_PreserveSourceOrder()
    {
        const string text = "Hi!";
        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, Options()).Span;

        Assert.Equal(3, glyphs.Length);
        Assert.Equal(new CodePoint('H'), glyphs[0].CodePoint);
        Assert.Equal(new CodePoint('i'), glyphs[1].CodePoint);
        Assert.Equal(new CodePoint('!'), glyphs[2].CodePoint);
        Assert.Equal(0, glyphs[0].StringIndex);
        Assert.Equal(1, glyphs[1].StringIndex);
        Assert.Equal(2, glyphs[2].StringIndex);
    }

    [Fact]
    public void GetGlyphMetrics_Newline_IsTrimmedAndAdvancesIndices()
    {
        const string text = "A\nB";
        TextOptions options = Options();
        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;

        Assert.Equal(2, glyphs.Length);
        Assert.Equal(new CodePoint('A'), glyphs[0].CodePoint);
        Assert.Equal(0, glyphs[0].StringIndex);

        Assert.Equal(new CodePoint('B'), glyphs[1].CodePoint);
        Assert.Equal(2, glyphs[1].StringIndex);

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The newline ends a non-empty line and is trimmed from visual metrics.
        // The following glyph keeps its source string index, so callers still see
        // the original source gap while measurements only reflect visible content.
        FontRectangle expectedBounds = FontRectangle.Union(glyphs[0].Bounds, glyphs[1].Bounds);
        Assert.Equal(expectedBounds, metrics.Bounds, Comparer);
    }

    [Fact]
    public void GetGlyphMetrics_EmptyText_ReturnsEmpty()
    {
        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(string.Empty, Options()).Span;
        Assert.Equal(0, glyphs.Length);
    }

    [Fact]
    public void GetGlyphMetrics_ShiftsWithOrigin()
    {
        const string text = "Hi!";
        ReadOnlySpan<GlyphMetrics> atZero = TextMeasurer.GetGlyphMetrics(text, Options()).Span;
        GlyphMetrics[] zeroCopy = atZero.ToArray();
        ReadOnlySpan<GlyphMetrics> atOffset = TextMeasurer.GetGlyphMetrics(text, Options(100, 50)).Span;

        Assert.Equal(zeroCopy.Length, atOffset.Length);
        for (int i = 0; i < zeroCopy.Length; i++)
        {
            Assert.Equal(zeroCopy[i].Bounds.X + 100, atOffset[i].Bounds.X, Comparer);
            Assert.Equal(zeroCopy[i].Bounds.Y + 50, atOffset[i].Bounds.Y, Comparer);
            Assert.Equal(zeroCopy[i].Bounds.Width, atOffset[i].Bounds.Width, Comparer);
            Assert.Equal(zeroCopy[i].Bounds.Height, atOffset[i].Bounds.Height, Comparer);
        }
    }

    [Theory]
    [InlineData("Hello", 0f, 0f)]
    [InlineData("Hello, World!", 100, 50)]
    [InlineData("Hello\nWorld", 10, 20)]
    [InlineData("A\nB\nC", -20, 30)]
    public void Invariant_RenderableBoundsEqualsUnionOfAdvanceAndBounds(string text, float ox, float oy)
    {
        TextOptions options = Options(ox, oy);
        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        FontRectangle absAdvance = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle expected = FontRectangle.Union(absAdvance, bounds);

        FontRectangle renderable = TextMeasurer.MeasureRenderableBounds(text, options);

        Assert.Equal(expected, renderable, Comparer);
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Hi!")]
    [InlineData("A\nB\nC")]
    [InlineData("Hello world")]
    public void Invariant_GlyphMetricsRenderableBoundsEqualsUnion(string text)
    {
        TextOptions options = Options();
        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics(text, options).Span;

        for (int i = 0; i < glyphs.Length; i++)
        {
            Assert.Equal(FontRectangle.Union(glyphs[i].Advance, glyphs[i].Bounds), glyphs[i].RenderableBounds, Comparer);
        }
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Hi!")]
    [InlineData("A\nB\nC")]
    public void Invariant_StringAndSpanGlyphMetricsOverloads_Match(string text)
    {
        TextOptions options = Options();
        ReadOnlySpan<GlyphMetrics> fromString = TextMeasurer.GetGlyphMetrics(text, options).Span;
        ReadOnlySpan<GlyphMetrics> fromSpan = TextMeasurer.GetGlyphMetrics(text.AsSpan(), options).Span;

        AssertGlyphMetricsEqual(fromString, fromSpan);
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Hello, World!")]
    [InlineData("Hello\nWorld")]
    public void Invariant_StringAndSpanOverloads_Match(string text)
    {
        TextOptions options = Options();

        Assert.Equal(
            TextMeasurer.MeasureAdvance(text, options),
            TextMeasurer.MeasureAdvance(text.AsSpan(), options),
            Comparer);
        Assert.Equal(
            TextMeasurer.MeasureBounds(text, options),
            TextMeasurer.MeasureBounds(text.AsSpan(), options),
            Comparer);
        Assert.Equal(
            TextMeasurer.MeasureRenderableBounds(text, options),
            TextMeasurer.MeasureRenderableBounds(text.AsSpan(), options),
            Comparer);
        Assert.Equal(
            TextMeasurer.CountLines(text, options),
            TextMeasurer.CountLines(text.AsSpan(), options));
    }

    [Fact]
    public void Invariant_EmptyText_AllMethodsReturnEmpty()
    {
        TextOptions options = Options();

        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureAdvance(string.Empty, options), Comparer);
        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureRenderableBounds(string.Empty, options), Comparer);
        Assert.Equal(0, TextMeasurer.CountLines(string.Empty, options));
        Assert.True(TextMeasurer.GetLineMetrics(string.Empty, options).IsEmpty);
        Assert.True(TextMeasurer.GetGlyphMetrics(string.Empty, options).IsEmpty);
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void Invariant_RenderableBoundsEqualsUnion_AcrossLayoutModes(LayoutMode layoutMode)
    {
        const string text = "Hello world";
        TextOptions options = Options(15, 25, layoutMode);

        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        FontRectangle absAdvance = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle expected = FontRectangle.Union(absAdvance, bounds);

        FontRectangle renderable = TextMeasurer.MeasureRenderableBounds(text, options);

        Assert.Equal(expected, renderable, Comparer);
    }

    private static void AssertGlyphMetricsEqual(ReadOnlySpan<GlyphMetrics> expected, ReadOnlySpan<GlyphMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            GlyphMetrics e = expected[i];
            GlyphMetrics a = actual[i];
            Assert.Equal(e.CodePoint, a.CodePoint);
            Assert.Equal(e.GraphemeIndex, a.GraphemeIndex);
            Assert.Equal(e.StringIndex, a.StringIndex);
            Assert.Equal(e.Advance, a.Advance, Comparer);
            Assert.Equal(e.Bounds, a.Bounds, Comparer);
            Assert.Equal(e.RenderableBounds, a.RenderableBounds, Comparer);
        }
    }

    private static void AssertGraphemeMetricsEqual(GraphemeMetrics[] expected, ReadOnlySpan<GraphemeMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            GraphemeMetrics e = expected[i];
            GraphemeMetrics a = actual[i];
            Assert.Equal(e.Advance, a.Advance, Comparer);
            Assert.Equal(e.Bounds, a.Bounds, Comparer);
            Assert.Equal(e.RenderableBounds, a.RenderableBounds, Comparer);
            Assert.Equal(e.GraphemeIndex, a.GraphemeIndex);
            Assert.Equal(e.StringIndex, a.StringIndex);
            Assert.Equal(e.BidiLevel, a.BidiLevel);
            Assert.Equal(e.IsLineBreak, a.IsLineBreak);
        }
    }
}

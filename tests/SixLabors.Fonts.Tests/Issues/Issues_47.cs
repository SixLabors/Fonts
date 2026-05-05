// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_47
{
    [Theory]
    [InlineData("hello world hello world hello world hello world")]
    public void LeftAlignedTextNewLineShouldNotStartWithWhiteSpace(string text)
    {
        Font font = CreateFont("\t x");

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350,
            HorizontalAlignment = HorizontalAlignment.Left
        });

        for (int lineIndex = 0; lineIndex < metrics.LineMetrics.Length; lineIndex++)
        {
            if (lineIndex > 0)
            {
                CodePoint lineStart = CodePoint.DecodeFromUtf16At(text.AsSpan(), metrics.LineMetrics[lineIndex].StringIndex);
                Assert.False(CodePoint.IsWhiteSpace(lineStart));
            }
        }
    }

    [Theory]
    [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Left)]
    [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Right)]
    [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Center)]
    [InlineData("hello   world   hello   world   hello   hello   world", HorizontalAlignment.Left)]
    public void NewWrappedLineMetricsShouldNotStartWithWhiteSpace(string text, HorizontalAlignment horizontalAlignment)
    {
        Font font = CreateFont("\t x");

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350,
            HorizontalAlignment = horizontalAlignment
        });

        for (int lineIndex = 0; lineIndex < metrics.LineMetrics.Length; lineIndex++)
        {
            if (lineIndex > 0)
            {
                CodePoint lineStart = CodePoint.DecodeFromUtf16At(text.AsSpan(), metrics.LineMetrics[lineIndex].StringIndex);
                Assert.False(CodePoint.IsWhiteSpace(lineStart));
            }
        }
    }

    [Fact]
    public void WhiteSpaceAtStartOfTextShouldNotBeTrimmed()
    {
        Font font = CreateFont("\t x");
        string text = "   hello world hello world hello world";

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350
        });

        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[0].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[1].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[2].Codepoint));
    }

    [Fact]
    public void WhiteSpaceAtTheEndOfTextShouldNotContributeToMeasurement()
    {
        Font font = CreateFont("\t x");
        string text = "hello world hello world hello world   ";
        string trimmed = "hello world hello world hello world";

        TextOptions options = new(new Font(font, 30))
        {
            WrappingLength = 350
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        TextMetrics trimmedMetrics = TextMeasurer.Measure(trimmed, options);

        Assert.Equal(trimmedMetrics.Advance, metrics.Advance);
        Assert.Equal(trimmedMetrics.Bounds, metrics.Bounds);

        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[^1].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[^2].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.MeasureGlyphAdvances()[^3].Codepoint));
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

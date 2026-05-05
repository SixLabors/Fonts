// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_434
{
    [Theory]
    [InlineData("- Lorem ipsullll\n\ndolor sit amet\n-consectetur elit", 5)]
    public void ShouldInsertExtraLineBreaksA(string text, int expectedLineCount)
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: expectedLineCount);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(expectedLineCount, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);

            // The collection contains laid-out glyph entries. This fixture shapes one glyph per source
            // code point, so preserved hard breaks raise the count from the old trimmed 47.
            Assert.Equal(50, metrics.MeasureGlyphAdvances().Length);
            AssertPreservedLineBreakAdvances(metrics.MeasureGlyphAdvances(), 16, 17, 32);
        }
    }

    [Theory]
    [InlineData("- Lorem ipsullll\n\n\ndolor sit amet\n-consectetur elit", 6)]
    public void ShouldInsertExtraLineBreaksB(string text, int expectedLineCount)
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options, properties: expectedLineCount);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(expectedLineCount, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);

            // The collection contains laid-out glyph entries. This fixture shapes one glyph per source
            // code point, so preserved hard breaks raise the count from the old trimmed 48.
            Assert.Equal(51, metrics.MeasureGlyphAdvances().Length);
            AssertPreservedLineBreakAdvances(metrics.MeasureGlyphAdvances(), 16, 17, 18, 33);
        }
    }

    private static void AssertPreservedLineBreakAdvances(ReadOnlySpan<GlyphBounds> advances, params int[] expectedStringIndices)
    {
        int lineBreakIndex = 0;
        for (int i = 0; i < advances.Length; i++)
        {
            GlyphBounds advance = advances[i];
            if (!CodePoint.IsNewLine(advance.Codepoint))
            {
                continue;
            }

            Assert.Equal(expectedStringIndices[lineBreakIndex], advance.StringIndex);
            Assert.True(advance.Bounds.Width > 0 || advance.Bounds.Height > 0);
            lineBreakIndex++;
        }

        Assert.Equal(expectedStringIndices.Length, lineBreakIndex);
    }
}

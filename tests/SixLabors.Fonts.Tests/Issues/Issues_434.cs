// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

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

            // Hard breaks that terminate visual lines are trimmed from the glyph stream.
            // The line count assertion above owns the regression; this count verifies
            // that trimming no longer exposes those break glyphs as measured advances.
            Assert.Equal(47, metrics.MeasureGlyphAdvances().Length);
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

            // Hard breaks that terminate visual lines are trimmed from the glyph stream.
            // The line count assertion above owns the regression; this count verifies
            // that trimming no longer exposes those break glyphs as measured advances.
            Assert.Equal(48, metrics.MeasureGlyphAdvances().Length);
        }
    }
}

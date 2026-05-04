// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_431
{
    [Fact]
    public void ShouldNotInsertExtraLineBreaks()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            const string text = "- Lorem ipsullll\ndolor sit amet\n-consectetur elit";

            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(4, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);
            Assert.Equal(46, metrics.CharacterAdvances.Count);
        }
    }

    [Fact]
    public void ShouldNotInsertExtraLineBreaks_2()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            const string text = "- Lorem ipsullll dolor sit amet\n-consectetur elit";

            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(4, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);
            Assert.Equal(46, metrics.CharacterAdvances.Count);
        }
    }
}

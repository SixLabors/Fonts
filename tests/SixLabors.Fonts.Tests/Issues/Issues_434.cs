// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_434
{
    [Theory]
    [InlineData("- Lorem ipsullll\n\ndolor sit amet\n-consectetur elit", 3)]
    [InlineData("- Lorem ipsullll\n\n\ndolor sit amet\n-consectetur elit", 3)]
    public void ShouldNotInsertExtraLineBreaks(string text, int expectedLineCount)
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(expectedLineCount, lineCount);

            IReadOnlyList<GlyphLayout> layout = TextLayout.GenerateLayout(text, options);
            Assert.Equal(47, layout.Count);
        }
    }
}

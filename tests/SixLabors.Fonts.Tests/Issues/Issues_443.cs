// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_443
{
    [Fact]
    public void ShouldBreakIntoTwoLinesA()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(100);

            TextLayoutTestUtilities.TestLayout(
                "This text should break     into two lines",
                new TextOptions(font)
                {
                    LineSpacing = 1.1499023f,
                    WrappingLength = 1000,
                    WordBreaking = WordBreaking.BreakWord
                });
        }
    }

    [Fact]
    public void ShouldBreakIntoTwoLinesB()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(100);

            TextLayoutTestUtilities.TestLayout(
                "ABCDEF",
                new TextOptions(font)
                {
                    LineSpacing = 1.1499023f,
                    WrappingLength = 100,
                    WordBreaking = WordBreaking.BreakWord
                });
        }
    }
}

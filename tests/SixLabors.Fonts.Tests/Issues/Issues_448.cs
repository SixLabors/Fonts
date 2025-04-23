// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_448
{
    [Fact]
    public void Issue_448()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(20);

            TextLayoutTestUtilities.TestLayout(
                "aaaaa bbbbb/ccccc ddddd",
                new TextOptions(font)
                {
                    WrappingLength = 150
                });
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_450
{
    [Fact]
    public void Issue_450()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(92);

            TextLayoutTestUtilities.TestLayout(
                "Super, Smash Bros (1999)",
                new TextOptions(font)
                {
                    Origin = new Vector2(50, 20),
                    WrappingLength = 960,
                });
        }
    }
}

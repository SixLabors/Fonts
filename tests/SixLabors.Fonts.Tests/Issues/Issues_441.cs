// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_441
{
    [Fact]
    public void LineWrappingWithExplicitNewLine()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(80);

            TextLayoutTestUtilities.TestLayout(
                "What connects the following:\nx",
                new TextOptions(font)
                {
                    Origin = new Vector2(50, 20),
                    WrappingLength = 800
                });
        }
    }

    [Fact]
    public void LineWrappingWithImplicitNewLine()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(80);

            TextLayoutTestUtilities.TestLayout(
                "What connects the following: x",
                new TextOptions(font)
                {
                    Origin = new Vector2(50, 20),
                    WrappingLength = 800
                });
        }
    }
}

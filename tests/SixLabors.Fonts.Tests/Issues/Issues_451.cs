// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_451
{
    private readonly FontFamily berry = new FontCollection().Add(TestFonts.VeryBerryProRegular);

    [Fact]
    public void Issue_451_A()
    {
        Font font = this.berry.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "The",
            new TextOptions(font));
    }

    [Fact]
    public void Issue_451_B()
    {
        Font font = this.berry.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "Th",
            new TextOptions(font));
    }

    [Fact]
    public void Issue_451_C()
    {
        Font font = this.berry.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "The quick brown fox jumps over the lazy dog",
            new TextOptions(font));
    }
}

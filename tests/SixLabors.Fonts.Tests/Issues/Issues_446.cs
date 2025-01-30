// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_446
{
    private readonly FontFamily charisSIL = new FontCollection().Add(TestFonts.CharisSILRegular);

    [Fact]
    public void Issue_446_A()
    {
        Font font = this.charisSIL.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "⇒ Tim Cook\n⇒ Jef Bezos\n⇒ Steve Jobs\n⇒ Mark Zuckerberg",
            new TextOptions(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 860,
            });
    }

    [Fact]
    public void Issue_446_B()
    {
        Font font = this.charisSIL.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "⇒ Tim Cook\n⇒ Jeff Bezos\n⇒ Steve Jobs\n⇒ Mark Zuckerberg",
            new TextOptions(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 860,
            });
    }
}

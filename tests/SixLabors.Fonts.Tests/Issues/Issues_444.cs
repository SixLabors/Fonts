// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_444
{
    private FontFamily charisSL = new FontCollection().Add(TestFonts.CharisSILRegular);

    [Fact]
    public void Issue_444_A()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(92);

            TextLayoutTestUtilities.TestLayout(
                "- Bill Clinton\r\n- Richard Nixon\r\n- Lyndon B. Johnson\r\n- John F. Kennedy",
                new TextOptions(font)
                {
                    Origin = new Vector2(50, 20),
                    WrappingLength = 860,
                });
        }
    }

    [Fact]
    public void Issue_444_B()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(92);

            TextLayoutTestUtilities.TestLayout(
                "- Bill Clinton\r\n- John F. Kennedy\r\n- Richard Nixon\r\n- Lyndon B. Johnson",
                new TextOptions(font)
                {
                    Origin = new Vector2(50, 20),
                    WrappingLength = 860,
                });
        }
    }

    [Fact]
    public void Issue_444_C()
    {
        Font font = this.charisSL.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "⇒ Bill Clinton\n⇒ Richard Nixon\n⇒ Lyndon B. Johnson\n⇒ John F. Kennedy",
            new TextOptions(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 860,
            });
    }

    [Fact]
    public void Issue_444_D()
    {
        Font font = this.charisSL.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "⇒ Bill Clinton\r\n⇒ Richard Nixon\r\n⇒ Lyndon B. Johnson\r\n⇒ John F. Kennedy",
            new TextOptions(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 860,
            });
    }

    [Fact]
    public void Issue_444_E()
    {
        Font font = this.charisSL.CreateFont(85);
        TextLayoutTestUtilities.TestLayout(
            "⇒ Bill Clinton\n⇒ Richard Nixon\n⇒ John F. Kennedy\n⇒ Lyndon B. Johnson",
            new TextOptions(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 860,
            });
    }
}

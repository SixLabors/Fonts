// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_484
{
    [Fact]
    public void Test_Issue_484()
        => Parallel.For(0, 10, static _ => Test_Issue_484_Core());

    private static void Test_Issue_484_Core()
    {
        FontCollection fontCollection = new();
        string arial = fontCollection.Add(TestFonts.Arial).Name;

        FontFamily arialFamily = fontCollection.Get(arial);
        Font arialFont = arialFamily.CreateFont(12, FontStyle.Regular);

        TextOptions textOptions = new(arialFont)
        {
            HintingMode = HintingMode.Standard
        };

        FontRectangle advance = TextMeasurer.MeasureAdvance("Hello, World!", textOptions);
        Assert.NotEqual(FontRectangle.Empty, advance);
    }

    [Fact]
    public void Test_Issue_484_B()
    {
        FontCollection fontCollection = new();
        string arial = fontCollection.Add(TestFonts.Arial).Name;

        FontFamily arialFamily = fontCollection.Get(arial);
        Font arialFont = arialFamily.CreateFont(12, FontStyle.Regular);

        Parallel.For(0, 10, _ => Test_Issue_484_Core_B(arialFont));
    }

    private static void Test_Issue_484_Core_B(Font font)
    {
        TextOptions textOptions = new(font)
        {
            HintingMode = HintingMode.Standard
        };

        FontRectangle advance = TextMeasurer.MeasureAdvance("Hello, World!", textOptions);
        Assert.NotEqual(FontRectangle.Empty, advance);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_363
{
    [Fact]
    public void GSubFormat2NUllReferenceException()
    {
        Font font = new FontCollection().Add(TestFonts.BNazaninFile).CreateFont(12);

        TextOptions textOptions = new(font);
        string text = "تست فونت 1234";
        FontRectangle rect = TextMeasurer.MeasureAdvance(text, textOptions);
        Assert.NotEqual(default, rect);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_367
{
    [Fact]
    public void ShouldMatchBrowserBreak()
    {
        Font font = new FontCollection().Add(TestFonts.CourierPrimeFile).CreateFont(12);

        TextOptions options = new(font)
        {
            Dpi = 96f // 1in = 96px
        };

        const float wrappingLengthInInches = 3.875f;
        options.WrappingLength = wrappingLengthInInches * options.Dpi;

        const string text = "Leer, but lonesome has fussin' change a faith. Themself seen and four trample.";
        int lineCount = TextMeasurer.CountLines(text, options);

        Assert.Equal(3, lineCount);

        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        Assert.Equal(365, advance.Width);
        Assert.Equal(48, advance.Height);
    }
}

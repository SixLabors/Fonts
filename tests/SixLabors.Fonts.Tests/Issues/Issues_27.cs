// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_27
{
    [Fact]
    public void ThrowsMeasuringWhitespace()
    {
        // wendy one returns wrong points for 'o'
        Font font = new FontCollection().Add(TestFonts.WendyOneFile).CreateFont(12);
        FontRectangle size = TextMeasurer.MeasureBounds("          ", new TextOptions(new Font(font, 30)));

        Assert.Equal(6, size.Width, 1F);
        Assert.Equal(0, size.Height, 1F);
    }
}

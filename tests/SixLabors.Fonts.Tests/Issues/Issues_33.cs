// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_33
{
    [Theory]
    [InlineData("\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs", 580, 70)] // newlines aren't directly measured but it is used for offsetting
    [InlineData("\n\tHelloworld", 310, 10)]
    [InlineData("\tHelloworld", 310, 10)]
    [InlineData("  Helloworld", 340, 10)]
    [InlineData("Hell owor ld\t", 390, 10)]
    [InlineData("Helloworld  ", 360, 10)]
    public void WhiteSpaceAtStartOfLineNotMeasured(string text, float width, float height)
    {
        Font font = CreateFont(text);
        FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(height, size.Height, 2);
        Assert.Equal(width, size.Width, 2);
    }

    public static Font CreateFont(string text)
    {
        var fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1);
    }
}

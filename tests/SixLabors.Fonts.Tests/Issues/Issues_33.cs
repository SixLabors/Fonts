// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_33
{
    [Theory]
    [InlineData("\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs", 580, 120)]
    [InlineData("\n\tHelloworld", 310, 60)]
    [InlineData("\tHelloworld", 310, 10)]
    [InlineData("  Helloworld", 340, 10)]
    [InlineData("Hell owor ld\t", 340, 10)]
    [InlineData("Helloworld  ", 280, 10)]
    public void WhiteSpaceAtStartOfLineNotMeasured(string text, float width, float height)
    {
        Font font = CreateFont(text);
        FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(height, size.Height, 2F);
        Assert.Equal(width, size.Width, 2F);
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_33
{
    [Theory]
    [InlineData("\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs", 580, 100)]
    [InlineData("\n\tHelloworld", 310, 40)]
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

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom, 310, 40)]
    [InlineData(LayoutMode.VerticalLeftRight, 40, 310)]
    [InlineData(LayoutMode.VerticalMixedLeftRight, 50, 310)]
    public void LeadingLineBreakContributesToWhitespaceBounds(LayoutMode layoutMode, float width, float height)
    {
        const string text = "\n\tHelloworld";
        Font font = CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            LayoutMode = layoutMode
        };

        FontRectangle size = TextMeasurer.MeasureBounds(text, options);

        // Whitespace has measurable bounds even though it does not render ink.
        // Hard breaks still cannot preserve the fake font's old negative top.
        Assert.Equal(height, size.Height, 2F);
        Assert.Equal(width, size.Width, 2F);
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

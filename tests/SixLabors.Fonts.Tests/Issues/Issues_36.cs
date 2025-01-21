// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_36
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void TextWidthForTabOnlyTextShouldBeSingleTabWidth(int tabCount)
    {
        Font font = CreateFont("\t");
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };

        FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t", options);
        string tabString = string.Empty.PadRight(tabCount, '\t');
        FontRectangle tabCountWidth = TextMeasurer.MeasureBounds(tabString, options);

        Assert.Equal(tabWidth.Width, tabCountWidth.Width, 2F);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void TextWidthForTabOnlyTextShouldBeSingleTabWidthMultipliedByTabCountMinusX(int tabCount)
    {
        Font font = CreateFont("\t x");

        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        FontRectangle xWidth = TextMeasurer.MeasureBounds("x", options);
        FontRectangle tabWidth = TextMeasurer.MeasureBounds("\tx", options);
        string tabString = "x".PadLeft(tabCount + 1, '\t');
        FontRectangle tabCountWidth = TextMeasurer.MeasureBounds(tabString, options);

        float singleTabWidth = tabWidth.Width - xWidth.Width;
        float finalTabWidth = tabCountWidth.Width - xWidth.Width;
        Assert.Equal(singleTabWidth * tabCount, finalTabWidth, 2F);
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

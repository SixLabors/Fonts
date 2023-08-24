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
    public void TextWidthForTabOnlyTextShouldBeSingleTabWidthMultipliedByTabCount(int tabCount)
    {
        Font font = CreateFont("\t x");

        FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
        string tabString = string.Empty.PadRight(tabCount, '\t');
        FontRectangle tabCountWidth = TextMeasurer.MeasureBounds(tabString, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        Assert.Equal(tabWidth.Width * tabCount, tabCountWidth.Width, 2);
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

        FontRectangle xWidth = TextMeasurer.MeasureBounds("x", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
        FontRectangle tabWidth = TextMeasurer.MeasureBounds("\tx", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
        string tabString = "x".PadLeft(tabCount + 1, '\t');
        FontRectangle tabCountWidth = TextMeasurer.MeasureBounds(tabString, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

        float singleTabWidth = tabWidth.Width - xWidth.Width;
        float finalTabWidth = tabCountWidth.Width - xWidth.Width;
        Assert.Equal(singleTabWidth * tabCount, finalTabWidth, 2);
    }

    public static Font CreateFont(string text)
    {
        var fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1);
    }
}

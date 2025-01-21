// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_35
{
    [Fact]
    public void RenderingTabAtStartOrLineTooShort()
    {
        Font font = CreateFont("\t x");
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        FontRectangle xWidth = TextMeasurer.MeasureBounds("x", options);
        FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t", options);
        FontRectangle tabWithXWidth = TextMeasurer.MeasureBounds("\tx", options);

        Assert.Equal(tabWidth.Width + xWidth.Width, tabWithXWidth.Width, 2F);
    }

    [Fact]
    public void TwoTabsAreDoubleWidthOfOneTab()
    {
        Font font = CreateFont("\t x");
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        FontRectangle xWidth = TextMeasurer.MeasureBounds("x", options);
        FontRectangle tabWithXWidth = TextMeasurer.MeasureBounds("\tx", options);
        FontRectangle tabTabWithXWidth = TextMeasurer.MeasureBounds("\t\tx", options);

        Assert.Equal(tabTabWithXWidth.Width - xWidth.Width, 2 * (tabWithXWidth.Width - xWidth.Width), 2F);
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

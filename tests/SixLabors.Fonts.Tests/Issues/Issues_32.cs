// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_32
{
    [Fact]
    public void TabWidth0CausesBadTabRendering()
    {
        string text = "Hello\tworld";
        Font font = CreateFont(text);
        FontRectangle size = TextMeasurer.MeasureSize(text, new TextOptions(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            TabWidth = 0
        });

        // tab width of 0 should make tabs not render at all
        Assert.Equal(10, size.Height, 4F);
        Assert.Equal(320, size.Width, 4F);
    }

    public static Font CreateFont(string text)
    {
        var fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}

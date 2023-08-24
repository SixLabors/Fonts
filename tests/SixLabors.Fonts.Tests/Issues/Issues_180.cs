// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_180
{
    [Fact]
    public void CorrectlySetsHeightMetrics()
    {
        // Whitney-book has invalid hhea values.
        Font font = new FontCollection().Add(TestFonts.WhitneyBookFile).CreateFont(25);

        FontRectangle size = TextMeasurer.MeasureSize("H", new TextOptions(font));

        Assert.Equal(16, size.Width, 1);
        Assert.Equal(21, size.Height, 1);
    }
}

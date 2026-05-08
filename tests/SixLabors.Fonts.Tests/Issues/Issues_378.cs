// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_378
{
    [Fact]
    public void DoesNotBreakIncorrectly()
    {
        Font font = TestFonts.GetFont(TestFonts.PlantinStdRegularFile, 2048);

        TextOptions options = new(font) { WrappingLength = float.MaxValue };
        FontRectangle size = TextMeasurer.MeasureBounds("D\r\nD", options);

        FontRectangle size2 = TextMeasurer.MeasureBounds("D D", options);

        Assert.NotEqual(size, size2);
    }
}

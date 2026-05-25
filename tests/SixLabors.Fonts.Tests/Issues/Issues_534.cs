// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_534
{
    [Fact]
    public void ShouldLoadFontWithSvgTableHavingZeroEntries()
    {
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue534, 12);

        FontRectangle size = TextMeasurer.MeasureBounds("ABCabc123", new TextOptions(font));

        Assert.NotEqual(FontRectangle.Empty, size);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_514
{
    [Fact]
    public void LookupType2Format1SubTable()
    {
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue514, 12);

        FontRectangle size = TextMeasurer.MeasureSize("ABCabc123", new TextOptions(font));

        Assert.NotEqual(FontRectangle.Empty, size);
    }
}

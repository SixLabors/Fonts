// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_400
{
    [Fact]
    public void RenderingTextIncludesAllGlyphs()
    {
        TextOptions options = new(TestFonts.GetFont(TestFonts.Arial, 16 * 2))
        {
            WrappingLength = 1900
        };

        StringBuilder stringBuilder = new();
        stringBuilder
            .AppendLine()
            .AppendLine("                NEWS_CATEGORY=EWF&NEWS_HASH=4b298ff9277ef9fdf515356be95ea3caf57cd36&OFFSET=0&SEARCH_VALUE=CA88105E1088&ID_NEWS")
            .Append("          ");

        int lineCount = TextMeasurer.CountLines(stringBuilder.ToString(), options);
        Assert.Equal(4, lineCount);

        TextLayoutTestUtilities.TestLayout(stringBuilder.ToString(), options);
    }
}

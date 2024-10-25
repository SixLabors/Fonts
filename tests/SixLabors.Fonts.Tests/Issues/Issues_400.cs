// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_400
{
    [Fact]
    public void RenderingTextIncludesAllGlyphs()
    {
#if OS_WINDOWS

        TextOptions options = new(new Font(SystemFonts.Get("Arial"), 16 * 2))
        {
            WrappingLength = 1900
        };

        const string content = """
                          NEWS_CATEGORY=EWF&NEWS_HASH=4b298ff9277ef9fdf515356be95ea3caf57cd36&OFFSET=0&SEARCH_VALUE=CA88105E1088&ID_NEWS
          """;

        int lineCount = TextMeasurer.CountLines(content, options);
        Assert.Equal(2, lineCount);
#endif
    }
}

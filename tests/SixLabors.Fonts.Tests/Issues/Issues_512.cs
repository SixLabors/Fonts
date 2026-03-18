// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_512
{
    [Fact]
    public void MissingCffTableThrowsInvalidFontFileExceptionInsteadOfNullReferenceException()
    {
        const string text = "Hello";

        FontFamily family = TestFonts.GetFontFamily(TestFonts.Issues.Issue512_CreateCffGlyphMetrics);

        Font font = family.CreateFont(12);

        TextOptions options = new(font);

        InvalidFontFileException exception = Assert.Throws<InvalidFontFileException>(() => TextMeasurer.MeasureSize(text, options));
        Assert.Equal("Missing required CFF table.", exception.Message);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_534
{
    [Fact]
    public void ShouldLoadFontWithSvgTableHavingZeroEntries()
        => TestFonts.GetFont(TestFonts.Issues.Issue534, 12);
}

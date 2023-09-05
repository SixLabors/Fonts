// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_96
{
    [Fact]
    public void ShouldNotThrowArgumentExceptionWhenFontContainsDuplicateTables()
        => Assert.Throws<EndOfStreamException>(() => FontDescription.LoadDescription(TestFonts.Issues.Issue96File));
}

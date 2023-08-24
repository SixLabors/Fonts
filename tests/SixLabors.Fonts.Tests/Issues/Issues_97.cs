// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_97
{
    [Fact]
    public void ShouldNotThrowNullReferenceExceptionWhenReaderCannotBeCreatedForTable()
        => Assert.Throws<InvalidFontTableException>(() => FontDescription.LoadDescription(TestFonts.Issues.Issue97File));
}

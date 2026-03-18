// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_203
{
    [Fact]
    public void CanParseVersion1Font()
    {
        FontDescription font = FontDescription.LoadDescription(TestFonts.Version1Font);
        Assert.NotNull(font);
    }
}

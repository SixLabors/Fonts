// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tests.Tables.Variations;

public class VariationsTests
{
    private static readonly FontCollection TestFontCollection = new();
    private static readonly Font RobotoFlexTTF = CreateFont(TestFonts.RobotoFlex);

    private static Font CreateFont(string testFont)
    {
        FontFamily family = TestFontCollection.Add(testFont);
        return family.CreateFont(12);
    }

    [Fact]
    public void CanLoadVariationTables()
        => Assert.True(RobotoFlexTTF.FontMetrics.TryGetVariationAxes(out VariationAxis[] axes));
}

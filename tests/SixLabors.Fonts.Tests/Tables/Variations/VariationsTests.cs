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
    {
        Assert.True(RobotoFlexTTF.FontMetrics.TryGetVariationAxes(out VariationAxis[] variationAxes));
        Assert.Equal(13, variationAxes.Length);

        Assert.Equal("wght", variationAxes[0].Name);
        Assert.Equal("wght", variationAxes[0].Tag);
        Assert.Equal(100, variationAxes[0].Min);
        Assert.Equal(1000, variationAxes[0].Max);
        Assert.Equal(400, variationAxes[0].Default);

        Assert.Equal("wdth", variationAxes[1].Name);
        Assert.Equal("wdth", variationAxes[1].Tag);
        Assert.Equal(25, variationAxes[1].Min);
        Assert.Equal(151, variationAxes[1].Max);
        Assert.Equal(100, variationAxes[1].Default);

        Assert.Equal("opsz", variationAxes[2].Name);
        Assert.Equal("opsz", variationAxes[2].Tag);
        Assert.Equal(8, variationAxes[2].Min);
        Assert.Equal(144, variationAxes[2].Max);
        Assert.Equal(14, variationAxes[2].Default);

        Assert.Equal("GRAD", variationAxes[3].Name);
        Assert.Equal("GRAD", variationAxes[3].Tag);
        Assert.Equal(-200, variationAxes[3].Min);
        Assert.Equal(150, variationAxes[3].Max);
        Assert.Equal(0, variationAxes[3].Default);

        Assert.Equal("slnt", variationAxes[4].Name);
        Assert.Equal("slnt", variationAxes[4].Tag);
        Assert.Equal(-10, variationAxes[4].Min);
        Assert.Equal(0, variationAxes[4].Max);
        Assert.Equal(0, variationAxes[4].Default);

        Assert.Equal("XTRA", variationAxes[5].Name);
        Assert.Equal("XTRA", variationAxes[5].Tag);
        Assert.Equal(323, variationAxes[5].Min);
        Assert.Equal(603, variationAxes[5].Max);
        Assert.Equal(468, variationAxes[5].Default);

        Assert.Equal("XOPQ", variationAxes[6].Name);
        Assert.Equal("XOPQ", variationAxes[6].Tag);
        Assert.Equal(27, variationAxes[6].Min);
        Assert.Equal(175, variationAxes[6].Max);
        Assert.Equal(96, variationAxes[6].Default);

        Assert.Equal("YOPQ", variationAxes[7].Name);
        Assert.Equal("YOPQ", variationAxes[7].Tag);
        Assert.Equal(25, variationAxes[7].Min);
        Assert.Equal(135, variationAxes[7].Max);
        Assert.Equal(79, variationAxes[7].Default);

        Assert.Equal("YTLC", variationAxes[8].Name);
        Assert.Equal("YTLC", variationAxes[8].Tag);
        Assert.Equal(416, variationAxes[8].Min);
        Assert.Equal(570, variationAxes[8].Max);
        Assert.Equal(514, variationAxes[8].Default);

        Assert.Equal("YTUC", variationAxes[9].Name);
        Assert.Equal("YTUC", variationAxes[9].Tag);
        Assert.Equal(528, variationAxes[9].Min);
        Assert.Equal(760, variationAxes[9].Max);
        Assert.Equal(712, variationAxes[9].Default);

        Assert.Equal("YTAS", variationAxes[10].Name);
        Assert.Equal("YTAS", variationAxes[10].Tag);
        Assert.Equal(649, variationAxes[10].Min);
        Assert.Equal(854, variationAxes[10].Max);
        Assert.Equal(750, variationAxes[10].Default);

        Assert.Equal("YTDE", variationAxes[11].Name);
        Assert.Equal("YTDE", variationAxes[11].Tag);
        Assert.Equal(-305, variationAxes[11].Min);
        Assert.Equal(-98, variationAxes[11].Max);
        Assert.Equal(-203, variationAxes[11].Default);

        Assert.Equal("YTFI", variationAxes[12].Name);
        Assert.Equal("YTFI", variationAxes[12].Tag);
        Assert.Equal(560, variationAxes[12].Min);
        Assert.Equal(788, variationAxes[12].Max);
        Assert.Equal(738, variationAxes[12].Default);
    }
}

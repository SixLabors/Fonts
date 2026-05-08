// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tests.Tables.Variations;

public class VariationsTests
{
    private static readonly FontCollection TestFontCollection = new();
    private static readonly Font RobotoFlexTTF = CreateFont(TestFonts.RobotoFlex);
    private static readonly Font AdobeVFPrototype = CreateFont(TestFonts.AdobeVFPrototype);

    private static Font CreateFont(string testFont)
        => TestFonts.GetFont(TestFontCollection, testFont, 12);

    [Fact]
    public void CanLoadVariationTables_RobotoFlex()
    {
        Assert.True(RobotoFlexTTF.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> variationAxes));
        Assert.Equal(13, variationAxes.Length);

        Assert.Equal("wght", variationAxes.Span[0].Name);
        Assert.Equal("wght", variationAxes.Span[0].Tag);
        Assert.Equal(100, variationAxes.Span[0].Min);
        Assert.Equal(1000, variationAxes.Span[0].Max);
        Assert.Equal(400, variationAxes.Span[0].Default);

        Assert.Equal("wdth", variationAxes.Span[1].Name);
        Assert.Equal("wdth", variationAxes.Span[1].Tag);
        Assert.Equal(25, variationAxes.Span[1].Min);
        Assert.Equal(151, variationAxes.Span[1].Max);
        Assert.Equal(100, variationAxes.Span[1].Default);

        Assert.Equal("opsz", variationAxes.Span[2].Name);
        Assert.Equal("opsz", variationAxes.Span[2].Tag);
        Assert.Equal(8, variationAxes.Span[2].Min);
        Assert.Equal(144, variationAxes.Span[2].Max);
        Assert.Equal(14, variationAxes.Span[2].Default);

        Assert.Equal("GRAD", variationAxes.Span[3].Name);
        Assert.Equal("GRAD", variationAxes.Span[3].Tag);
        Assert.Equal(-200, variationAxes.Span[3].Min);
        Assert.Equal(150, variationAxes.Span[3].Max);
        Assert.Equal(0, variationAxes.Span[3].Default);

        Assert.Equal("slnt", variationAxes.Span[4].Name);
        Assert.Equal("slnt", variationAxes.Span[4].Tag);
        Assert.Equal(-10, variationAxes.Span[4].Min);
        Assert.Equal(0, variationAxes.Span[4].Max);
        Assert.Equal(0, variationAxes.Span[4].Default);

        Assert.Equal("XTRA", variationAxes.Span[5].Name);
        Assert.Equal("XTRA", variationAxes.Span[5].Tag);
        Assert.Equal(323, variationAxes.Span[5].Min);
        Assert.Equal(603, variationAxes.Span[5].Max);
        Assert.Equal(468, variationAxes.Span[5].Default);

        Assert.Equal("XOPQ", variationAxes.Span[6].Name);
        Assert.Equal("XOPQ", variationAxes.Span[6].Tag);
        Assert.Equal(27, variationAxes.Span[6].Min);
        Assert.Equal(175, variationAxes.Span[6].Max);
        Assert.Equal(96, variationAxes.Span[6].Default);

        Assert.Equal("YOPQ", variationAxes.Span[7].Name);
        Assert.Equal("YOPQ", variationAxes.Span[7].Tag);
        Assert.Equal(25, variationAxes.Span[7].Min);
        Assert.Equal(135, variationAxes.Span[7].Max);
        Assert.Equal(79, variationAxes.Span[7].Default);

        Assert.Equal("YTLC", variationAxes.Span[8].Name);
        Assert.Equal("YTLC", variationAxes.Span[8].Tag);
        Assert.Equal(416, variationAxes.Span[8].Min);
        Assert.Equal(570, variationAxes.Span[8].Max);
        Assert.Equal(514, variationAxes.Span[8].Default);

        Assert.Equal("YTUC", variationAxes.Span[9].Name);
        Assert.Equal("YTUC", variationAxes.Span[9].Tag);
        Assert.Equal(528, variationAxes.Span[9].Min);
        Assert.Equal(760, variationAxes.Span[9].Max);
        Assert.Equal(712, variationAxes.Span[9].Default);

        Assert.Equal("YTAS", variationAxes.Span[10].Name);
        Assert.Equal("YTAS", variationAxes.Span[10].Tag);
        Assert.Equal(649, variationAxes.Span[10].Min);
        Assert.Equal(854, variationAxes.Span[10].Max);
        Assert.Equal(750, variationAxes.Span[10].Default);

        Assert.Equal("YTDE", variationAxes.Span[11].Name);
        Assert.Equal("YTDE", variationAxes.Span[11].Tag);
        Assert.Equal(-305, variationAxes.Span[11].Min);
        Assert.Equal(-98, variationAxes.Span[11].Max);
        Assert.Equal(-203, variationAxes.Span[11].Default);

        Assert.Equal("YTFI", variationAxes.Span[12].Name);
        Assert.Equal("YTFI", variationAxes.Span[12].Tag);
        Assert.Equal(560, variationAxes.Span[12].Min);
        Assert.Equal(788, variationAxes.Span[12].Max);
        Assert.Equal(738, variationAxes.Span[12].Default);
    }

    [Fact]
    public void CanLoadVariationTables_AdobeVFPrototype()
    {
        Assert.True(AdobeVFPrototype.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> variationAxes));
        Assert.Equal(2, variationAxes.Length);

        Assert.Equal("Weight", variationAxes.Span[0].Name);
        Assert.Equal("wght", variationAxes.Span[0].Tag);
        Assert.Equal(200, variationAxes.Span[0].Min);
        Assert.Equal(900, variationAxes.Span[0].Max);
        Assert.Equal(389.344, Math.Round(variationAxes.Span[0].Default, 3));

        Assert.Equal("Contrast", variationAxes.Span[1].Name);
        Assert.Equal("CNTR", variationAxes.Span[1].Tag);
        Assert.Equal(0, variationAxes.Span[1].Min);
        Assert.Equal(100, variationAxes.Span[1].Max);
        Assert.Equal(0, variationAxes.Span[1].Default);
    }
}

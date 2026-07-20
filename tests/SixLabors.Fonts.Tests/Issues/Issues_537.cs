// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_537
{
    [Fact]
    public void ShouldMeasureFontWithSelfReferentialCompositeGlyph()
    {
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue537, 16);

        FontRectangle bounds = TextMeasurer.MeasureRenderableBounds("ABCabc123!@#", new TextOptions(font));

        Assert.NotEqual(FontRectangle.Empty, bounds);
    }

    [Fact]
    public void ShouldMeasureCffFontWithSelfReferentialSubroutine()
    {
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue537Cff, 16);

        FontRectangle bounds = TextMeasurer.MeasureRenderableBounds("A", new TextOptions(font));

        Assert.NotEqual(FontRectangle.Empty, bounds);
    }
}

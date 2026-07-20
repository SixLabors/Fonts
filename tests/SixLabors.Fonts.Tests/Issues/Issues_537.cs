// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.Cff;

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
    public void SelfReferentialSubroutineProducesEmptyBounds()
    {
        // With one subroutine the Type 2 bias is 107, so -107 (encoded as byte 32) selects subroutine
        // zero. That subroutine repeats the same call, reproducing a validly indexed cyclic program.
        byte[] selfReferentialSubroutine = [32, (byte)Type2Operator1.Callsubr];
        byte[] charString = [32, (byte)Type2Operator1.Callsubr];
        byte[][] localSubroutines = [selfReferentialSubroutine];

        using CffEvaluationEngine engine = new(charString, [], localSubroutines, 0, 1);
        Bounds bounds = engine.GetBounds();

        Assert.Equal(Bounds.Empty, bounds);
    }
}

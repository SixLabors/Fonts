// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_337
{
    [Fact]
    public void CanShapeCompositeGlyphs()
    {
        Font font = new FontCollection().Add(TestFonts.DFKaiSBFile).CreateFont(1024);
        ColorGlyphRenderer renderer = new();
        TextOptions options = new(font)
        {
            HintingMode = HintingMode.Standard
        };

        TextRenderer.RenderTextTo(renderer, "標楷體輸出", options);

        Assert.Equal(5, renderer.GlyphKeys.Count);
        Assert.Equal(5, renderer.GlyphRects.Count);

        var expected = new FontRectangle[]
        {
            new FontRectangle(0, -254.99994F, 1024, 1024),
            new FontRectangle(1024, -254.99994F, 1024, 1024),
            new FontRectangle(2048, -254.99994F, 1024, 1024),
            new FontRectangle(3072, -254.99994F, 1024, 1024),
            new FontRectangle(4096, -254.99994F, 1024, 1024)
        };

        for (int i = 0; i < expected.Length; i++)
        {
            CompareRectangleExact(expected[i], renderer.GlyphRects[i]);
        }
    }

    private static void CompareRectangle(FontRectangle expected, FontRectangle actual, float precision = 4F)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
        Assert.Equal(expected.Width, actual.Width, precision);
        Assert.Equal(expected.Height, actual.Height, precision);
    }

    private static void CompareRectangleExact(FontRectangle expected, FontRectangle actual)
    {
        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
    }
}

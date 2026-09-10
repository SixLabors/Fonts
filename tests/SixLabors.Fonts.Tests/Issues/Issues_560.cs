// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_560
{
    [Fact]
    public void ShouldMeasureFontWithCyclicColrPaintGraph()
    {
        // The base glyph for U+F0100 is a PaintColrGlyph that references itself, so the
        // color layers cannot resolve and the glyph falls back to its plain outline bounds.
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue560, 32);

        FontRectangle bounds = TextMeasurer.MeasureBounds("\U000F0100", new TextOptions(font));

        Assert.NotEqual(FontRectangle.Empty, bounds);
    }

    [Fact]
    public void ShouldRenderFontWithCyclicColrPaintGraph()
    {
        Font font = TestFonts.GetFont(TestFonts.Issues.Issue560, 32);
        ColorGlyphRenderer renderer = new();

        TextRenderer.RenderTo(renderer, "\U000F0100", new TextOptions(font) { ColorFontSupport = ColorFontSupport.ColrV1 });

        // The cycle is cut at its first repeated reference, so the glyph paints no layers.
        Assert.Empty(renderer.Colors);
    }
}

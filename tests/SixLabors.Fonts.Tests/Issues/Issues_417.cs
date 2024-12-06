// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_417
{
    [Fact]
    public void DoesNotThrow_InvalidAnchor()
    {
        FontFamily family = new FontCollection().Add(TestFonts.NotoSansRegular);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        Font font = family.CreateFont(metrics?.UnitsPerEm ?? 1000);

        TextOptions options = new(font);

        // References values generated using.
        // https://www.corvelsoftware.co.uk/crowbar/
        TextMeasurer.TryMeasureCharacterAdvances("Text", options, out GlyphBounds[] advances);

        Assert.Equal(4, advances.Length);
        Assert.Equal(486, advances[0].Bounds.Width);
        Assert.Equal(544, advances[1].Bounds.Width);
        Assert.Equal(529, advances[2].Bounds.Width);
        Assert.Equal(361, advances[3].Bounds.Width);

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "Text", new TextOptions(font));

        int[] expectedGlyphIndices = { 55, 72, 91, 87 };
        Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
        for (int i = 0; i < expectedGlyphIndices.Length; i++)
        {
            Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
        }
    }
}

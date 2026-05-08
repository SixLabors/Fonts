// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_417
{
    [Fact]
    public void DoesNotThrow_InvalidAnchor()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoSansRegular);
        family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

        Font font = family.CreateFont(metrics?.UnitsPerEm ?? 1000);

        TextOptions options = new(font);

        // Crowbar reports the positioned advance widths, not tight rendered bounds.
        // https://www.corvelsoftware.co.uk/crowbar/
        ReadOnlySpan<GlyphMetrics> glyphs = TextMeasurer.GetGlyphMetrics("Text", options).Span;

        Assert.Equal(4, glyphs.Length);
        Assert.Equal(486, glyphs[0].Advance.Width);
        Assert.Equal(544, glyphs[1].Advance.Width);
        Assert.Equal(529, glyphs[2].Advance.Width);
        Assert.Equal(361, glyphs[3].Advance.Width);

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "Text", new TextOptions(font));

        int[] expectedGlyphIndices = [55, 72, 91, 87];
        Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
        for (int i = 0; i < expectedGlyphIndices.Length; i++)
        {
            Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
        }
    }
}

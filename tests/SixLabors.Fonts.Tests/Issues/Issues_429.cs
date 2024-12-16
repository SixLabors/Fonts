// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_429
{
    private static readonly ApproximateFloatComparer Comparer = new(.1F);

    [Fact]
    public void VerticalMixedLayout_ExpectedRotation()
    {
        if (SystemFonts.TryGet("Yu Gothic", out FontFamily family))
        {
            const string text = "あいうえお、「こんにちはー」。もしもし。ABCDEFG 日本語";
            Font font = family.CreateFont(30.0F);

            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalMixedRightLeft
            };

            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);

            // Only the Latin glyph + space should be rotated.
            // Any other glyphs that appear rotated have actually been substituted by the font.
            int[] rotatedGlyphs = new int[] { 20, 21, 22, 23, 24, 25, 26, 27 };

            for (int i = 0; i < glyphs.Count; i++)
            {
                GlyphLayout glyph = glyphs[i];

                if (rotatedGlyphs.Contains(i))
                {
                    Assert.Equal(GlyphLayoutMode.VerticalRotated, glyph.LayoutMode);
                }
                else
                {
                    Assert.Equal(GlyphLayoutMode.Vertical, glyph.LayoutMode);
                }
            }
        }
    }

    [Fact]
    public void VerticalMixedLayout_ExpectedBounds()
    {
        if (SystemFonts.TryGet("Yu Gothic", out FontFamily family))
        {
            const string text = "あいうえお、「こんにちはー」。もしもし。ABCDEFG 日本語";
            Font font = family.CreateFont(30.0F);

            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalMixedRightLeft
            };

            FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
            FontRectangle size = TextMeasurer.MeasureSize(text, options);
            FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);

            Assert.Equal(new FontRectangle(0.83496094F, 2.8417969F, 28.31543F, 834.9464F), bounds, Comparer);
            Assert.Equal(new FontRectangle(0, 0, 28.31543F, 834.9464F), size, Comparer);
            Assert.Equal(new FontRectangle(0, 0, 32.98462F, 839.3556F), advance, Comparer);
        }
    }
}

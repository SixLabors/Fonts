// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_191
{
    [Fact]
    public void CanLoadMacintoshGlyphs()
    {
        ReadOnlyMemory<FontFamily> families = new FontCollection()
            .AddCollection(TestFonts.HelveticaTTCFile);

        FontFamily family = families.Span[0];
        foreach (FontFamily candidate in families.Span)
        {
            ReadOnlySpan<FontStyle> styles = candidate.GetAvailableStyles().Span;
            bool hasRegular = false;

            foreach (FontStyle style in styles)
            {
                if (style == FontStyle.Regular)
                {
                    hasRegular = true;
                    break;
                }
            }

            if (hasRegular)
            {
                family = candidate;
                break;
            }
        }

        Font font = family.CreateFont(12);

        const ColorFontSupport support = ColorFontSupport.None;

        Assert.True(font.TryGetGlyphs(new CodePoint('A'), support, out Glyph? ga));
        Assert.True(font.TryGetGlyphs(new CodePoint('x'), support, out Glyph? gx));

        Assert.NotEqual(ga, gx);

        Assert.Equal(1366, ga.Value.GlyphMetrics.AdvanceWidth);
        Assert.Equal(2048, ga.Value.GlyphMetrics.AdvanceHeight);

        Assert.Equal(1024, gx.Value.GlyphMetrics.AdvanceWidth);
        Assert.Equal(2048, gx.Value.GlyphMetrics.AdvanceHeight);
    }
}

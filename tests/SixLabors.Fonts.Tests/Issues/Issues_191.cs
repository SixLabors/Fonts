// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_191
{
    [Fact]
    public void CanLoadMacintoshGlyphs()
    {
        Font font = new FontCollection()
            .AddCollection(TestFonts.HelveticaTTCFile)
            .First(x => x.GetAvailableStyles().Contains(FontStyle.Regular)).CreateFont(12);

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

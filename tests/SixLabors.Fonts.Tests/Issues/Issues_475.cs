// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_475
{
    [Fact]
    public void Test_Issue_475()
    {
        const string text = "වේගවත් දුඹුරු හිවලා කම්මැලි බල්ලා උඩින් පනී";

        FontCollection fontCollection = new();
        FontFamily noto = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily sc = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansSCRegular);
        Font mainFont = noto.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                sc,
            ],
        };

        // None of the fonts here can actually render the real glyphs in the text, just squares
        // so just verify that we don't hit any exceptions and get the correct glyph count.
        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);

        Assert.Equal(43, renderer.GlyphRects.Count);
    }
}

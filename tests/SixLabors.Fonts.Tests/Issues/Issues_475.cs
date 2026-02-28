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
        string noto = fontCollection.Add(TestFonts.NotoSansRegular).Name;
        string sc = fontCollection.Add(TestFonts.NotoSansSCRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(noto);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                fontCollection.Get(sc),
            ],
        };

        // None of the fonts here can actually render the real glyphs in the text, just squares
        // so just verify that we don't hit any exceptions and get the correct glyph count.
        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);

        Assert.Equal(43, renderer.GlyphRects.Count);
    }
}

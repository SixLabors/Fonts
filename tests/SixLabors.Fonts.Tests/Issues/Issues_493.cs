// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_493
{
    [Fact]
    public void Test_Issue_493_Ogham()
    {
        const string text =
            """
            Ogham
            ᚛ᚐᚅᚋᚐᚇᚐᚅᚈᚐ᚜
            """;

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.NotoSansOghamRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont) { HintingMode = HintingMode.Standard };

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_493_Runic()
    {
        const string text =
            """
            Runic
            ᚦᛖᚱᛅᛈᛁᛑᛒᚱᚢᚾᚠᛅᚢᛋ
            """;

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.NotoSansRunicRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont) { HintingMode = HintingMode.Standard };

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_493_MgOpenCanonic()
    {
        const string text = "the quick brown fox jumps over the lazy dog";

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.NotoSansRunicRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont) { HintingMode = HintingMode.Standard };

        TextLayoutTestUtilities.TestLayout(text, options);
    }
}

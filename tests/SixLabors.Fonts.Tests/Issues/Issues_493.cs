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

        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansOghamRegular, 30);

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

        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansRunicRegular, 30);

        TextOptions options = new(mainFont) { HintingMode = HintingMode.Standard };

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_493_MgOpenCanonic()
    {
        const string text = "the quick brown fox jumps over the lazy dog";

        Font mainFont = TestFonts.GetFont(TestFonts.MgOpenCanonicRegular, 30);

        TextOptions hintOptions = new(mainFont) { HintingMode = HintingMode.Standard };

        TextLayoutTestUtilities.TestLayout(text, hintOptions);
    }
}

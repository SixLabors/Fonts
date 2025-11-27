// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_483
{
    [Fact]
    public void Test_Issue_483()
    {
        const string text = "Kannada: ವೇಗವುಳ್ಳ ಕಂದು ನರಿ ಸೋಮಾರಿ ನಾಯಿಯ ಮೇಲೆ ಹಾರುತ್ತದೆ";

        FontCollection fontCollection = new();
        string noto = fontCollection.Add(TestFonts.NotoSansRegular).Name;
        string kannada = fontCollection.Add(TestFonts.NotoSansKannadaRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(noto);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                fontCollection.Get(kannada),
            ],
        };

        // There are too many metrics to validate here so we just ensure no exceptions are thrown
        // and the rendering looks correct by inspecting the snapshot.
        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false);
    }
}

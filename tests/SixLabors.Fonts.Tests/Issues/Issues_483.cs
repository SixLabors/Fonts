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
        FontFamily noto = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansRegular);
        FontFamily kannada = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansKannadaRegular);
        Font mainFont = noto.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                kannada,
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

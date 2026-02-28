// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_468
{
    [Fact]
    public void TestIssue_468()
    {
        StringBuilder stringBuilder = new();
        stringBuilder
            .AppendLine("Latin: The quick brown fox jumps over the lazy dog.")
            .AppendLine("Arabic (RTL & Shaping): نص حكيم له سر قاطع وذو شأن عظيم");

        string text = stringBuilder.ToString();
        FontCollection fontCollection = new();

        string consola = fontCollection.Add(TestFonts.Consola).Name;
        string arabic = fontCollection.Add(TestFonts.NotoSansArabicRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(consola);
        Font mainFont = mainFontFamily.CreateFont(50, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                fontCollection.Get(arabic),
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

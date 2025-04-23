// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if OS_WINDOWS
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_388_390
{
    [Fact]
    public void UniversalShaper_NullReferenceException_388()
    {
        CodePoint taiLeCharacter = new(0x195C);     // ᥜ
        CodePoint sundaneseCharacter = new(0x1B9B); // ᮛ
        CodePoint tifinaghCharacter = new(0x2D43);  // ⵃ
        CodePoint chamCharacter = new(0xAA43);      // ꩃ

        CodePoint latainCharacter = new(0x0041);    // A
        CodePoint hiraganaCharacter = new(0x3042);  // あ

        FontFamily fontFamily = SystemFonts.Get("Yu Gothic");
        Font font = fontFamily.CreateFont(20.0F);
        TextOptions textOption = new(font);

        _ = TextMeasurer.MeasureBounds($"{latainCharacter}{taiLeCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{hiraganaCharacter}{taiLeCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{latainCharacter}{sundaneseCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{hiraganaCharacter}{sundaneseCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{latainCharacter}{tifinaghCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{hiraganaCharacter}{tifinaghCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{latainCharacter}{chamCharacter}", textOption);
        _ = TextMeasurer.MeasureBounds($"{hiraganaCharacter}{chamCharacter}", textOption);
    }

    [Fact]
    public void UniversalShaper_NullReferenceException_390()
    {
        const string s = " 꿹ꓴ/ꥀ냘";
        FontFamily fontFamily = SystemFonts.Get("Arial");
        Font font = new(fontFamily, 10f);
        _ = TextMeasurer.MeasureBounds(s, new TextOptions(font));
    }
}
#endif

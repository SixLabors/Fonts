// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_469
{
    [Fact]
    public void Test_Issue_469()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("Latin: The quick brown fox jumps over the lazy dog.")
            .AppendLine("Cyrillic: Съешь же ещё этих мягких французских булок.")
            .AppendLine("Greek: Ζαφείρι δέξου πάγκαλο, βαθῶν ψυχῆς τὸ σῆμα.")
            .AppendLine("Chinese: 敏捷的棕色狐狸跳过了懒狗")
            .AppendLine("Japanese: いろはにほへと ちりぬるを")
            .AppendLine("Korean: 다람쥐 헌 쳇바퀴에 타고파")
            .AppendLine("Arabic (RTL & Shaping): نص حكيم له سر قاطع وذو شأن عظيم")
            .AppendLine("Hebrew (RTL): דג סקרן שט בים מאוכזב ולפתע מצא חברה")
            .AppendLine("Thai (Complex): เป็นมนุษย์สุดประเสริฐเลิศคุณค่า")
            .AppendLine("Devanagari (Conjuncts): ऋषियों को सताने वाले राक्षसों का अंत हो गया");

        string text = stringBuilder.ToString();

        FontCollection fontCollection = new();
        FontFamily arial = TestFonts.GetFontFamily(fontCollection, TestFonts.Arial);
        FontFamily cousine = TestFonts.GetFontFamily(fontCollection, TestFonts.CousineRegular);
        FontFamily hind = TestFonts.GetFontFamily(fontCollection, TestFonts.HindRegular);
        FontFamily nanumGothicCoding = TestFonts.GetFontFamily(fontCollection, TestFonts.NanumGothicCodingRegular);
        FontFamily inconsolata = TestFonts.GetFontFamily(fontCollection, TestFonts.InconsolataRegular);
        FontFamily notoNaskhArabic = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoNaskhArabicRegular);
        FontFamily notoSansJpThin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansJPRegular);
        FontFamily notoSansScThin = TestFonts.GetFontFamily(fontCollection, TestFonts.NotoSansSCThin);
        FontFamily sarabun = TestFonts.GetFontFamily(fontCollection, TestFonts.SarabunRegular);

        Font mainFont = arial.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                inconsolata,
                nanumGothicCoding,
                cousine,
                notoSansScThin,
                notoSansJpThin,
                notoNaskhArabic,
                sarabun,
                hind,
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

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_469
{
    [Fact]
    public void Test_Issue_469()
    {
        const string arialFontName = "Arial";
        const string inconsolataFontName = "Inconsolata";
        const string nanumGothicCodingFontName = "NanumGothicCoding";
        const string cousineFontName = "Cousine";
        const string notoSansScThinFontName = "Noto Sans SC Thin";
        const string notoSansJpThinFontName = "Noto Sans JP Thin";
        const string notoNaskhArabicFontName = "Noto Naskh Arabic";
        const string sarabunFontName = "Sarabun";
        const string hindFontName = "Hind";

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
        fontCollection.Add(TestFonts.Arial);
        fontCollection.Add(TestFonts.CousineRegular);
        fontCollection.Add(TestFonts.HindRegular);
        fontCollection.Add(TestFonts.NanumGothicCodingRegular);
        fontCollection.Add(TestFonts.InconsolataRegular);
        fontCollection.Add(TestFonts.NotoNaskhArabicRegular);
        fontCollection.Add(TestFonts.NotoSansHKVariableFontWght);
        fontCollection.Add(TestFonts.NotoSansJPRegular);
        fontCollection.Add(TestFonts.NotoSansSCRegular);
        fontCollection.Add(TestFonts.SarabunRegular);

        FontFamily mainFontFamily = fontCollection.Get(arialFontName);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont)
        {
            FallbackFontFamilies =
            [
                fontCollection.Get(inconsolataFontName),
                fontCollection.Get(nanumGothicCodingFontName),
                fontCollection.Get(cousineFontName),
                fontCollection.Get(notoSansScThinFontName),
                fontCollection.Get(notoSansJpThinFontName),
                fontCollection.Get(notoNaskhArabicFontName),
                fontCollection.Get(sarabunFontName),
                fontCollection.Get(hindFontName),
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

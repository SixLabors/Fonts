// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_488
{
    [Fact]
    public void Test_Issue_488_Sinhala()
    {
        const string text = "වේගවත් දුඹුරු හිවලා කම්මැලි බල්ලා උඩින් පනිනවා। සංයුක්ත: ක්‍ෂ ඥ්‍ඤ";

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.SinhalaSansRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Bengali()
    {
        const string text = "দ্রুত বাদামী শিয়াল অলস কুকুরের ওপর লাফ দেয়। সংযুক্ত: ক্ক ত্ত";

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.BengaliSansRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Tibetan()
    {
        const string text = "བོད་ཡིག་གི་དཔེ་མཚོན། རྐ རྒ རྔ སྐ";

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.TibetanSerifRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Myanmar()
    {
        const string text = "မြန်မာစာ စမ်းသပ်မှု။ ဗျည်းပေါင်းစုံ က ခ ဂ ဃ င စ ဆ ည့် န့်";

        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.MyanmarSansRegular).Name;

        FontFamily mainFontFamily = fontCollection.Get(name);
        Font mainFont = mainFontFamily.CreateFont(30, FontStyle.Regular);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_488
{
    [Fact]
    public void Test_Issue_488_Sinhala()
    {
        const string text = "වේගවත් දුඹුරු හිවලා කම්මැලි බල්ලා උඩින් පනිනවා। සංයුක්ත: ක්‍ෂ ඥ්‍ඤ";

        Font mainFont = TestFonts.GetFont(TestFonts.SinhalaSansRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Bengali()
    {
        const string text = "দ্রুত বাদামী শিয়াল অলস কুকুরের ওপর লাফ দেয়। সংযুক্ত: ক্ক ত্ত";

        Font mainFont = TestFonts.GetFont(TestFonts.BengaliSansRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Tibetan()
    {
        const string text = "བོད་ཡིག་གི་དཔེ་མཚོན། རྐ རྒ རྔ སྐ";

        Font mainFont = TestFonts.GetFont(TestFonts.TibetanSerifRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Myanmar()
    {
        const string text = "မြန်မာစာ စမ်းသပ်မှု။ ဗျည်းပေါင်းစုံ က ခ ဂ ဃ င စ ဆ ည့် န့်";

        Font mainFont = TestFonts.GetFont(TestFonts.MyanmarSansRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Lao()
    {
        const string text = "ໝາຈິ້ງຈອກສີນ້ຳຕານທີ່ວ່ອງໄວກະໂດດຂ້າມໝາຂີ້ຄ້ານ ສະຫຼັບ: ກ່ ງ່ ຍ່";

        Font mainFont = TestFonts.GetFont(TestFonts.LaoSerifRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }

    [Fact]
    public void Test_Issue_488_Hebrew()
    {
        // Exercises: dagesh (בּ כּ), shin/sin dots (שׁ שׂ), stacked marks (בָּ),
        // hataf vowels (אֱ), holam (לֹ), hiriq (הִ), meteg with vowels (בָֽ).
        const string text = "בְּרֵאשִׁית בָּרָא אֱלֹהִים שָׁלוֹם שָׂרָה כָּבוֹד בָֽרְכוּ";

        Font mainFont = TestFonts.GetFont(TestFonts.NotoSansHebrewRegular, 30);

        TextOptions options = new(mainFont);

        TextLayoutTestUtilities.TestLayout(text, options);
    }
}

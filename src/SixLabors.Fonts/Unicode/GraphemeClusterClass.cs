// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

// Reordering these enum properties will require the regeneration of the Grapheme.trie.
namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Unicode Grapheme Cluster classes.
    /// <see href="https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values"/>
    /// </summary>
    public enum GraphemeClusterClass
    {
        /// <summary>
        /// This is not a property value; it is used in the rules to represent any code point.
        /// </summary>
        Any = 0,

        /// <summary>
        /// U+000D CARRIAGE RETURN (CR)
        /// </summary>
        CarriageReturn = 1,

        /// <summary>
        /// U+000A LINE FEED (LF)
        /// </summary>
        LineFeed = 2,

        /// <summary>
        /// General_Category = Line_Separator, or<br/>
        /// General_Category = Paragraph_Separator, or<br/>
        /// General_Category = Control, or<br/>
        /// General_Category = Unassigned and Default_Ignorable_Code_Point, or<br/>
        /// General_Category = Format<br/>
        /// and not U+000D CARRIAGE RETURN<br/>
        /// and not U+000A LINE FEED<br/>
        /// and not U+200C ZERO WIDTH NON-JOINER (ZWNJ)<br/>
        /// and not U+200D ZERO WIDTH JOINER (ZWJ)<br/>
        /// and not Prepended_Concatenation_Mark = Yes
        /// </summary>
        Control = 3,

        /// <summary>
        /// Grapheme_Extend = Yes, or<br/>
        /// Emoji_Modifier = Yes<br/>
        /// This includes:<br/>
        /// General_Category = Nonspacing_Mark<br/>
        /// General_Category = Enclosing_Mark<br/>
        /// U+200C ZERO WIDTH NON-JOINER<br/>
        /// plus a few General_Category = Spacing_Mark needed for canonical equivalence.
        /// </summary>
        Extend = 4,

        /// <summary>
        /// Regional_Indicator = Yes<br/>
        /// This consists of the range:<br/>
        /// U+1F1E6 REGIONAL INDICATOR SYMBOL LETTER A
        /// ..U+1F1FF REGIONAL INDICATOR SYMBOL LETTER Z
        /// </summary>
        RegionalIndicator = 5,

        /// <summary>
        /// Indic_Syllabic_Category = Consonant_Preceding_Repha, or<br/>
        /// Indic_Syllabic_Category = Consonant_Prefixed, or<br/>
        /// Prepended_Concatenation_Mark = Yes
        /// </summary>
        Prepend = 6,

        /// <summary>
        /// Grapheme_Cluster_Break ≠ Extend, and<br/>
        /// General_Category = Spacing_Mark, or<br/>
        /// any of the following (which have General_Category = Other_Letter):<br/>
        /// U+0E33 ( ำ ) THAI CHARACTER SARA AM<br/>
        /// U+0EB3 ( ຳ ) LAO VOWEL SIGN AM
        /// </summary>
        SpacingMark = 7,

        /// <summary>
        /// Hangul_Syllable_Type = L, such as:<br/>
        /// U+1100 ( ᄀ ) HANGUL CHOSEONG KIYEOK<br/>
        /// U+115F ( ᅟ ) HANGUL CHOSEONG FILLER<br/>
        /// U+A960 ( ꥠ ) HANGUL CHOSEONG TIKEUT-MIEUM<br/>
        /// U+A97C ( ꥼ ) HANGUL CHOSEONG SSANGYEORINHIEUH
        /// </summary>
        HangulLead = 8,

        /// <summary>
        /// Hangul_Syllable_Type=V, such as:<br/>
        /// U+1160 ( ᅠ ) HANGUL JUNGSEONG FILLER<br/>
        /// U+11A2 ( ᆢ ) HANGUL JUNGSEONG SSANGARAEA<br/>
        /// U+D7B0 ( ힰ ) HANGUL JUNGSEONG O-YEO<br/>
        /// U+D7C6 ( ퟆ ) HANGUL JUNGSEONG ARAEA-E
        /// </summary>
        HangulVowel = 9,

        /// <summary>
        /// Hangul_Syllable_Type = T, such as:<br/>
        /// U+11A8 ( ᆨ ) HANGUL JONGSEONG KIYEOK<br/>
        /// U+11F9 ( ᇹ ) HANGUL JONGSEONG YEORINHIEUH<br/>
        /// U+D7CB ( ퟋ ) HANGUL JONGSEONG NIEUN-RIEUL<br/>
        /// U+D7FB ( ퟻ ) HANGUL JONGSEONG PHIEUPH-THIEUTH
        /// </summary>
        HangulTail = 10,

        /// <summary>
        /// Hangul_Syllable_Type=LV, that is:<br/>
        /// U+AC00 ( 가 ) HANGUL SYLLABLE GA<br/>
        /// U+AC1C ( 개 ) HANGUL SYLLABLE GAE<br/>
        /// U+AC38 ( 갸 ) HANGUL SYLLABLE GYA
        /// </summary>
        HangulLeadVowel = 11,

        /// <summary>
        /// Hangul_Syllable_Type=LVT, that is:<br/>
        /// U+AC01 ( 각 ) HANGUL SYLLABLE GAG<br/>
        /// U+AC02 ( 갂 ) HANGUL SYLLABLE GAGG<br/>
        /// U+AC03 ( 갃 ) HANGUL SYLLABLE GAGS<br/>
        /// U+AC04 ( 간 ) HANGUL SYLLABLE GAN
        /// </summary>
        HangulLeadVowelTail = 12,

        /// <summary>
        /// Extended Pictographic
        /// </summary>
        ExtendedPictographic = 13,

        /// <summary>
        /// U+200D ZERO WIDTH JOINER
        /// </summary>
        ZeroWidthJoiner = 14
    }
}

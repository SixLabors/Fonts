// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Unicode;

public class UnicodeUtilityTests
{
    [Theory]
    [InlineData(0x3040u, 0x309Fu)] // Hiragana
    [InlineData(0x30A0u, 0x30FFu)] // Katakana
    [InlineData(0x31F0u, 0x31FFu)] // Katakana Phonetic Extensions
    [InlineData(0xAC00u, 0xD7A3u)] // Hangul Syllables
    [InlineData(0x1100u, 0x11FFu)] // Hangul Jamo
    [InlineData(0xA960u, 0xA97Fu)] // Hangul Jamo Extended-A
    [InlineData(0xD7B0u, 0xD7FFu)] // Hangul Jamo Extended-B
    [InlineData(0x3100u, 0x312Fu)] // Bopomofo
    [InlineData(0x31A0u, 0x31BFu)] // Bopomofo Extended
    [InlineData(0x4E00u, 0x9FFFu)] // CJK Unified Ideographs
    [InlineData(0x3400u, 0x4DBFu)] // CJK Unified Ideographs Extension A
    [InlineData(0x20000u, 0x2A6DFu)] // CJK Unified Ideographs Extension B
    [InlineData(0x2A700u, 0x2B73Fu)] // CJK Unified Ideographs Extension C
    [InlineData(0x2B740u, 0x2B81Fu)] // CJK Unified Ideographs Extension D
    [InlineData(0x2B820u, 0x2CEAFu)] // CJK Unified Ideographs Extension E
    [InlineData(0x2CEB0u, 0x2EBEFu)] // CJK Unified Ideographs Extension F
    [InlineData(0x30000u, 0x3134Fu)] // CJK Unified Ideographs Extension G
    [InlineData(0xF900u, 0xFAFFu)] // CJK Compatibility Ideographs
    [InlineData(0x2F800u, 0x2FA1Fu)] // CJK Compatibility Ideographs Supplement
    [InlineData(0x2E80u, 0x2EFFu)] // CJK Radicals Supplement
    [InlineData(0x2F00u, 0x2FDFu)] // Kangxi Radicals
    [InlineData(0x2FF0u, 0x2FFFu)] // Ideographic Description Characters
    [InlineData(0x31C0u, 0x31EFu)] // CJK Strokes
    [InlineData(0x3000u, 0x303Fu)] // CJK Symbols and Punctuation
    [InlineData(0x3200u, 0x32FFu)] // Enclosed CJK Letters and Months
    [InlineData(0x1F200u, 0x1F2FFu)] // Enclosed Ideographic Supplement
    [InlineData(0x3300u, 0x33FFu)] // CJK Compatibility
    [InlineData(0xFE10u, 0xFE1Fu)] // Vertical Forms
    [InlineData(0xFF00u, 0xFFEFu)] // Halfwidth and Fullwidth Forms
    public void CanDetectCJKCodePoints(uint min, uint max)
    {
        for (uint i = min; i <= max; i++)
        {
            Assert.True(UnicodeUtility.IsCJKCodePoint(i));
        }
    }

    [Theory]
    [InlineData(0x0u, 0x7Fu)] // ASCII
    public void NoFalsePositiveCJKCodePoints(uint min, uint max)
    {
        for (uint i = min; i <= max; i++)
        {
            Assert.False(UnicodeUtility.IsCJKCodePoint(i));
        }
    }

    [Theory]
    [InlineData(0x00AD, 0x00AD)]
    [InlineData(0x034F, 0x034F)]
    [InlineData(0x061C, 0x061C)]
    [InlineData(0x115F, 0x1160)]
    [InlineData(0x17B4, 0x17B5)]
    [InlineData(0x180B, 0x180D)]
    [InlineData(0x180E, 0x180E)]
    [InlineData(0x180F, 0x180F)]
    [InlineData(0x200B, 0x200F)]
    [InlineData(0x202A, 0x202E)]
    [InlineData(0x2060, 0x2064)]
    [InlineData(0x2065, 0x2065)]
    [InlineData(0x2066, 0x206F)]
    [InlineData(0x3164, 0x3164)]
    [InlineData(0xFE00, 0xFE0F)]
    [InlineData(0xFEFF, 0xFEFF)]
    [InlineData(0xFFA0, 0xFFA0)]
    [InlineData(0xFFF0, 0xFFF8)]
    [InlineData(0x1BCA0, 0x1BCA3)]
    [InlineData(0x1D173, 0x1D17A)]
    [InlineData(0xE0000, 0xE0000)]
    [InlineData(0xE0001, 0xE0001)]
    [InlineData(0xE0002, 0xE001F)]
    [InlineData(0xE0020, 0xE007F)]
    [InlineData(0xE0080, 0xE00FF)]
    [InlineData(0xE0100, 0xE01EF)]
    [InlineData(0xE01F0, 0xE0FFF)]
    public void CanDetectDefaultIgnorableCodePoint(uint min, uint max)
    {
        for (uint i = min; i <= max; i++)
        {
            Assert.True(UnicodeUtility.IsDefaultIgnorableCodePoint(i));
        }
    }

    [Theory]
    [InlineData(0x0u, 0x7Fu)] // ASCII
    public void NoFalsePositiveDefaultIgnorableCodePoint(uint min, uint max)
    {
        for (uint i = min; i <= max; i++)
        {
            Assert.False(UnicodeUtility.IsDefaultIgnorableCodePoint(i));
        }
    }
}

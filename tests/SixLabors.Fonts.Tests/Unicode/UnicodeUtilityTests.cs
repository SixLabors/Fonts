// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests.Unicode
{
    public class UnicodeUtilityTests
    {
        [Theory]
        [InlineData(0x3040u, 0x309Fu)] // Hiragana
        [InlineData(0x30A0u, 0x30FFu)] // Katakana
        [InlineData(0xAC00u, 0xD7A3u)] // Hangul Syllables
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
    }
}

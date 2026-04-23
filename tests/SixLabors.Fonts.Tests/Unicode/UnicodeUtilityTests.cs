// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Unicode;

public class UnicodeUtilityTests
{
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

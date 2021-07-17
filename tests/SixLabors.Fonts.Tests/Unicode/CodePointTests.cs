// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests.Unicode
{
    public partial class CodePointTests
    {
        [Fact]
        public void CodePointIsValid()
        {
            uint i;
            for (i = 0; i <= 0x10FFFFu; i++)
            {
                Assert.True(CodePoint.IsValid(i));
            }

            i++;
            Assert.False(CodePoint.IsValid(i));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsDigit(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsDigit, CodePoint.IsDigit(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsLetter(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsLetter, CodePoint.IsLetter(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsLetterOrDigit(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsLetterOrDigit, CodePoint.IsLetterOrDigit(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsLower(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsLower, CodePoint.IsLower(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsNumber(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsNumber, CodePoint.IsNumber(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsPunctuation(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsPunctuation, CodePoint.IsPunctuation(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsSeparator(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsSeparator, CodePoint.IsSeparator(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsSymbol(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsSymbol, CodePoint.IsSymbol(testData.ScalarValue));

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void CodePointIsUpper(UnicodeInfoTestData testData)
            => Assert.Equal(testData.IsUpper, CodePoint.IsUpper(testData.ScalarValue));

        [Fact]
        public void CodePointIsWhiteSpaceAscii()
        {
            for (uint i = 0; i <= 0x7Fu; i++)
            {
                Assert.True(UnicodeUtility.IsAsciiCodePoint(i));
                Assert.Equal(CodePoint.IsWhiteSpace(new CodePoint(i)), char.IsWhiteSpace((char)i));
            }
        }

        [Fact]
        public void CodePointIsWhiteSpaceBmp()
        {
            for (uint i = 0x7Fu + 1u; i <= 0xFFFFu; i++)
            {
                Assert.True(UnicodeUtility.IsBmpCodePoint(i));
                Assert.Equal(CodePoint.IsWhiteSpace(new CodePoint(i)), char.IsWhiteSpace((char)i));
            }
        }

        [Fact]
        public void CodePointIsControl()
        {
            for (uint i = 0; i <= 0x10FFFFu; i++)
            {
                var cp = new CodePoint(i);
                if (cp.IsBmp)
                {
                    Assert.Equal(CodePoint.IsControl(new CodePoint(i)), char.IsControl((char)i));
                }
                else
                {
                    // Per the Unicode stability policy, the set of control characters
                    // is forever fixed at [ U+0000..U+001F ], [ U+007F..U+009F ]. No
                    // characters will ever be added to or removed from the "control characters"
                    // group. See https://www.unicode.org/policies/stability_policy.html.
                    Assert.False(CodePoint.IsControl(cp));
                }
            }
        }

        [Fact]
        public void CodePointIsMandatoryBreak()
        {
            static bool IsLineBreakClassBreak(uint value)
            {
                // See https://www.unicode.org/standard/reports/tr13/tr13-5.html
                switch (value)
                {
                    case 0x000A: // LINE FEED (LF)
                    case 0x000B: // LINE TABULATION (VT)
                    case 0x000C: // FORM FEED (FF)
                    case 0x000D: // CARRIAGE RETURN (CR)
                    case 0x0085: // NEXT LINE (NEL)
                    case 0x2028: // LINE SEPARATOR (LS)
                    case 0x2029: // PARAGRAPH SEPARATOR (PS)
                        return true;
                    default:
                        return false;
                }
            }

            for (uint i = 0; i <= 0x10FFFFu; i++)
            {
                Assert.Equal(CodePoint.IsNewLine(new CodePoint(i)), IsLineBreakClassBreak(i));
            }
        }

        [Fact]
        public void Tab()
        {
            Assert.True(CodePoint.IsWhiteSpace(new CodePoint('\t')));
            Assert.False(CodePoint.IsNewLine(new CodePoint('\t')));
        }
    }
}

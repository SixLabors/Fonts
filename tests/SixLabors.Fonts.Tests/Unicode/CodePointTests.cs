// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests.Unicode
{
    public partial class CodePointTests
    {
        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        public static void Ctor_Cast_Char_Valid(GeneralTestData testData)
        {
            var codePoint = new CodePoint(checked((char)testData.ScalarValue));
            var codePointFromCast = (CodePoint)(char)testData.ScalarValue;

            Assert.Equal(codePoint, codePointFromCast);
            Assert.Equal(testData.ScalarValue, codePoint.Value);
            Assert.Equal(testData.IsAscii, codePoint.IsAscii);
            Assert.Equal(testData.IsBmp, codePoint.IsBmp);
            Assert.Equal(testData.Plane, codePoint.Plane);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        public static void Ctor_Cast_Char_Invalid_Throws(char value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new CodePoint(value));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => (CodePoint)value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Ctor_Cast_Int32_Valid(GeneralTestData testData)
        {
            var codePoint = new CodePoint(testData.ScalarValue);
            var codePointFromCast = (CodePoint)testData.ScalarValue;

            Assert.Equal(codePoint, codePointFromCast);
            Assert.Equal(testData.ScalarValue, codePoint.Value);
            Assert.Equal(testData.IsAscii, codePoint.IsAscii);
            Assert.Equal(testData.IsBmp, codePoint.IsBmp);
            Assert.Equal(testData.Plane, codePoint.Plane);
        }

        [Theory]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void Ctor_Cast_Int32_Invalid_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new CodePoint(value));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => (CodePoint)value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Ctor_Cast_UInt32_Valid(GeneralTestData testData)
        {
            var codePoint = new CodePoint((uint)testData.ScalarValue);
            var codePointFromCast = (CodePoint)(uint)testData.ScalarValue;

            Assert.Equal(codePoint, codePointFromCast);
            Assert.Equal(testData.ScalarValue, codePoint.Value);
            Assert.Equal(testData.IsAscii, codePoint.IsAscii);
            Assert.Equal(testData.IsBmp, codePoint.IsBmp);
            Assert.Equal(testData.Plane, codePoint.Plane);
        }

        [Theory]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void Ctor_Cast_UInt32_Invalid_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new CodePoint((uint)value));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => (CodePoint)(uint)value);
        }

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_ValidOnly))]
        public static void Ctor_SurrogatePair_Valid(char highSurrogate, char lowSurrogate, int expectedValue)
            => Assert.Equal(expectedValue, new CodePoint(highSurrogate, lowSurrogate).Value);

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_InvalidOnly))]
        public static void Ctor_SurrogatePair_Invalid(char highSurrogate, char lowSurrogate)
        {
            string expectedParamName = !char.IsHighSurrogate(highSurrogate) ? nameof(highSurrogate) : nameof(lowSurrogate);
            Assert.Throws<ArgumentOutOfRangeException>(expectedParamName, () => new CodePoint(highSurrogate, lowSurrogate));
        }

        [Fact]
        public void CanEnumerateSpan()
        {
            // Test example taken from.
            // https://docs.microsoft.com/en-us/dotnet/api/system.text.rune?view=net-5.0#when-to-use-the-rune-type
            const string text = "𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟";
            int letterCount = 0;

            Span<char> span = text.ToCharArray();
            foreach (CodePoint codePoint in span.EnumerateCodePoints())
            {
                if (CodePoint.IsLetter(codePoint))
                {
                    letterCount++;
                }
            }

            Assert.Equal(8, letterCount);
        }

        [Fact]
        public void CanEnumerateReadonlySpan()
        {
            // Test example taken from.
            // https://docs.microsoft.com/en-us/dotnet/api/system.text.rune?view=net-5.0#when-to-use-the-rune-type
            const string text = "𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟";
            int letterCount = 0;

            foreach (CodePoint codePoint in text.AsSpan().EnumerateCodePoints())
            {
                if (CodePoint.IsLetter(codePoint))
                {
                    letterCount++;
                }
            }

            Assert.Equal(8, letterCount);
        }

        [Fact]
        public void CanEnumerateInvalidReadonlySpan()
        {
            // The string below contains 2 combining characters then
            // a single high surrogate code unit, then 2 more sets or combining characters.
            // 'ā̈' 'b' '�' 'ç'
            const string text = "a\u0304\u0308b\ud800c\u0327";
            int letterCount = 0;
            int codePointCount = 0;
            foreach (CodePoint codePoint in text.AsSpan().EnumerateCodePoints())
            {
                if (CodePoint.IsLetter(codePoint))
                {
                    letterCount++;
                }

                codePointCount++;
            }

            Assert.Equal(3, letterCount);
            Assert.Equal(7, codePointCount);
        }

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
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Utf16SequenceLengthIsCorrect(GeneralTestData testData)
        {
            var codePoint = new CodePoint(testData.ScalarValue);
            Assert.Equal(testData.Utf16Sequence.Length, codePoint.Utf16SequenceLength);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Utf8SequenceLengthIsCorrect(GeneralTestData testData)
        {
            var codePoint = new CodePoint(testData.ScalarValue);
            Assert.Equal(testData.Utf8Sequence.Length, codePoint.Utf8SequenceLength);
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

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0x80, 0x80)]
        [InlineData(0x80, 0x100)]
        [InlineData(0x100, 0x80)]
        public static void Operators_And_CompareTo(uint scalarValueLeft, uint scalarValueRight)
        {
            var left = new CodePoint(scalarValueLeft);
            var right = new CodePoint(scalarValueRight);

            Assert.Equal(scalarValueLeft == scalarValueRight, left == right);
            Assert.Equal(scalarValueLeft != scalarValueRight, left != right);
            Assert.Equal(scalarValueLeft < scalarValueRight, left < right);
            Assert.Equal(scalarValueLeft <= scalarValueRight, left <= right);
            Assert.Equal(scalarValueLeft > scalarValueRight, left > right);
            Assert.Equal(scalarValueLeft >= scalarValueRight, left >= right);
            Assert.Equal(Math.Sign(scalarValueLeft.CompareTo(scalarValueRight)), Math.Sign(left.CompareTo(right)));
            Assert.Equal(Math.Sign(((IComparable)scalarValueLeft).CompareTo(scalarValueRight)), Math.Sign(((IComparable)left).CompareTo(right)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x10FFFF)]
        public static void NonGenericCompareTo_NonNullAlwaysGreaterThanNull(uint scalarValue)
            => Assert.Equal(1, Math.Sign(((IComparable)new CodePoint(scalarValue)).CompareTo(null)));

        [Fact]
        public static void NonGenericCompareTo_GivenNonCodePointArgument_ThrowsArgumentException()
        {
            IComparable codePoint = new CodePoint(0);

            Assert.Throws<ArgumentException>(() => codePoint.CompareTo(0 /* int32 */));
        }

        [Fact]
        public static void ReplacementChar()
            => Assert.Equal(0xFFFD, CodePoint.ReplacementChar.Value);

        [Fact]
        public void CorrectlyIdentifiesTab()
        {
            Assert.True(CodePoint.IsWhiteSpace(new CodePoint('\t')));
            Assert.False(CodePoint.IsNewLine(new CodePoint('\t')));
        }

        // TODO: Add more cases from Script.txt
        [Theory]
        [InlineData(0x0041, 0x005A, ScriptClass.Latin)]
        [InlineData(0xFF41, 0xFF5A, ScriptClass.Latin)]
        [InlineData(0x0370, 0x0373, ScriptClass.Greek)]
        [InlineData(0x1D242, 0x1D245, ScriptClass.Greek)]
        [InlineData(0x620, 0x063F, ScriptClass.Arabic)]
        [InlineData(0x1EEF0, 0x1EEF1, ScriptClass.Arabic)]
        public static void CodePointIsScript(uint min, uint max, ScriptClass script)
        {
            for (uint i = min; i <= max; i++)
            {
                Assert.Equal(script, CodePoint.GetScriptClass(new CodePoint(i)));
            }
        }

        [Theory]
        [InlineData(0x10ACD, JoiningType.LeftJoining, JoiningGroup.ManichaeanHeth)]
        [InlineData(0x0715, JoiningType.RightJoining, JoiningGroup.DalathRish)]
        [InlineData(0x1886, JoiningType.Transparent, JoiningGroup.NoJoiningGroup)]
        [InlineData(0x18A6, JoiningType.DualJoining, JoiningGroup.NoJoiningGroup)]
        [InlineData(0x200C, JoiningType.NonJoining, JoiningGroup.NoJoiningGroup)]
        public static void CodePointIsJoiningClass(uint codePoint, JoiningType type, JoiningGroup group)
        {
            JoiningClass join = CodePoint.GetJoiningClass(new CodePoint(codePoint));

            Assert.Equal(type, join.JoiningType);
            Assert.Equal(group, join.JoiningGroup);
        }

        [Theory]
        [InlineData(0xFE0F, true)]
        [InlineData(0xFE0E, true)]
        [InlineData(0xFE01, true)]
        [InlineData(0xFE00, true)]
        [InlineData(0xFF00, false)]
        [InlineData(0xFE10, false)]
        public void CodePointIsVariationSelector(uint codePoint, bool expectedResult)
        {
            bool result = CodePoint.IsVariationSelector(new CodePoint(codePoint));
            Assert.Equal(expectedResult, result);
        }
    }
}

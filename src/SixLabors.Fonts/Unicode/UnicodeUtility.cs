// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SixLabors.Fonts.Unicode
{
    internal static class UnicodeUtility
    {
        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is an ASCII
        /// character ([ U+0000..U+007F ]).
        /// </summary>
        /// <remarks>
        /// Per http://www.unicode.org/glossary/#ASCII, ASCII is only U+0000..U+007F.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiCodePoint(uint value) => value <= 0x7Fu;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is in the
        /// Basic Multilingual Plane (BMP).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBmpCodePoint(uint value) => value <= 0xFFFFu;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a
        /// Chinese/Japanese/Korean (CJK) character.
        /// </summary>
        /// <remarks>
        /// <see href="https://blog.ceshine.net/post/cjk-unicode/"/>
        /// <see href="https://en.wikipedia.org/wiki/Hiragana_%28Unicode_block%29"/>
        /// <see href="https://en.wikipedia.org/wiki/Katakana_(Unicode_block)"/>
        /// <see href="https://en.wikipedia.org/wiki/Hangul_Syllables"/>
        /// <see href="https://en.wikipedia.org/wiki/CJK_Unified_Ideographs"/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCJKCodePoint(uint value)
        {
            // Hiragana
            if (IsInRangeInclusive(value, 0x3040u, 0x309Fu))
            {
                return true;
            }

            // Katakana
            if (IsInRangeInclusive(value, 0x30A0u, 0x30FFu))
            {
                return true;
            }

            // Hangul Syllables
            if (IsInRangeInclusive(value, 0xAC00u, 0xD7A3u))
            {
                return true;
            }

            // CJK Unified Ideographs
            if (IsInRangeInclusive(value, 0x4E00u, 0x9FFFu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension A
            if (IsInRangeInclusive(value, 0x3400u, 0x4DBFu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension B
            if (IsInRangeInclusive(value, 0x20000u, 0x2A6DFu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension C
            if (IsInRangeInclusive(value, 0x2A700u, 0x2B73Fu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension D
            if (IsInRangeInclusive(value, 0x2B740u, 0x2B81Fu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension E
            if (IsInRangeInclusive(value, 0x2B820u, 0x2CEAFu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension F
            if (IsInRangeInclusive(value, 0x2CEB0u, 0x2EBEFu))
            {
                return true;
            }

            // CJK Unified Ideographs Extension G
            if (IsInRangeInclusive(value, 0x30000u, 0x3134Fu))
            {
                return true;
            }

            // CJK Compatibility Ideographs
            if (IsInRangeInclusive(value, 0xF900u, 0xFAFFu))
            {
                return true;
            }

            // CJK Compatibility Ideographs Supplement
            if (IsInRangeInclusive(value, 0x2F800u, 0x2FA1Fu))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the Unicode plane (0 through 16, inclusive) which contains this code point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPlane(uint codePoint)
        {
            DebugAssertIsValidCodePoint(codePoint);

            return (int)(codePoint >> 16);
        }

        /// <summary>
        /// Given a Unicode scalar value, gets the number of UTF-16 code units required to represent this value.
        /// </summary>
        public static int GetUtf16SequenceLength(uint codePoint)
        {
            DebugAssertIsValidCodePoint(codePoint);

            codePoint -= 0x10000;  // if value < 0x10000, high byte = 0xFF; else high byte = 0x00
            codePoint += 2 << 24;  // if value < 0x10000, high byte = 0x01; else high byte = 0x02
            codePoint >>= 24;      // shift high byte down
            return (int)codePoint; // and return it
        }

        /// <summary>
        /// Given a Unicode scalar value, gets the number of UTF-8 code units required to represent this value.
        /// </summary>
        public static int GetUtf8SequenceLength(uint codePoint)
        {
            DebugAssertIsValidCodePoint(codePoint);

            // The logic below can handle all valid scalar values branchlessly.
            // It gives generally good performance across all inputs, and on x86
            // it's only six instructions: lea, sar, xor, add, shr, lea.

            // 'a' will be -1 if input is < 0x800; else 'a' will be 0
            // => 'a' will be -1 if input is 1 or 2 UTF-8 code units; else 'a' will be 0
            int a = ((int)codePoint - 0x0800) >> 31;

            // The number of UTF-8 code units for a given scalar is as follows:
            // - U+0000..U+007F => 1 code unit
            // - U+0080..U+07FF => 2 code units
            // - U+0800..U+FFFF => 3 code units
            // - U+10000+       => 4 code units
            //
            // If we XOR the incoming scalar with 0xF800, the chart mutates:
            // - U+0000..U+F7FF => 3 code units
            // - U+F800..U+F87F => 1 code unit
            // - U+F880..U+FFFF => 2 code units
            // - U+10000+       => 4 code units
            //
            // Since the 1- and 3-code unit cases are now clustered, they can
            // both be checked together very cheaply.
            codePoint ^= 0xF800u;
            codePoint -= 0xF880u;   // if scalar is 1 or 3 code units, high byte = 0xFF; else high byte = 0x00
            codePoint += 4 << 24;   // if scalar is 1 or 3 code units, high byte = 0x03; else high byte = 0x04
            codePoint >>= 24;       // shift high byte down

            // Final return value:
            // - U+0000..U+007F => 3 + (-1) * 2 = 1
            // - U+0080..U+07FF => 4 + (-1) * 2 = 2
            // - U+0800..U+FFFF => 3 + ( 0) * 2 = 3
            // - U+10000+       => 4 + ( 0) * 2 = 4
            return (int)codePoint + (a * 2);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="codePoint"/> is a valid Unicode code
        /// point, i.e., is in [ U+0000..U+10FFFF ], inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidCodePoint(uint codePoint) => codePoint <= 0x10FFFFu;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 high surrogate code point,
        /// i.e., is in [ U+D800..U+DBFF ], inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHighSurrogateCodePoint(uint value)
            => IsInRangeInclusive(value, 0xD800u, 0xDBFFu);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 low surrogate code point,
        /// i.e., is in [ U+DC00..U+DFFF ], inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLowSurrogateCodePoint(uint value)
            => IsInRangeInclusive(value, 0xDC00u, 0xDFFFu);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 surrogate code point,
        /// i.e., is in [ U+D800..U+DFFF ], inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSurrogateCodePoint(uint value)
            => IsInRangeInclusive(value, 0xD800u, 0xDFFFu);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
            => (value - lowerBound) <= (upperBound - lowerBound);

        /// <summary>
        /// Returns a Unicode scalar value from two code points representing a UTF-16 surrogate pair.
        /// </summary>
        public static uint GetScalarFromUtf16SurrogatePair(uint highSurrogateCodePoint, uint lowSurrogateCodePoint)
        {
            DebugAssertIsHighSurrogateCodePoint(highSurrogateCodePoint);
            DebugAssertIsLowSurrogateCodePoint(lowSurrogateCodePoint);

            // This calculation comes from the Unicode specification, Table 3-5.
            // Need to remove the D800 marker from the high surrogate and the DC00 marker from the low surrogate,
            // then fix up the "wwww = uuuuu - 1" section of the bit distribution. The code is written as below
            // to become just two instructions: shl, lea.
            return (highSurrogateCodePoint << 10) + lowSurrogateCodePoint - ((0xD800U << 10) + 0xDC00U - (1 << 16));
        }

        /// <summary>
        /// Decomposes an astral Unicode code point into UTF-16 high and low surrogate code units.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUtf16SurrogatesFromSupplementaryPlaneCodePoint(uint value, out char highSurrogateCodePoint, out char lowSurrogateCodePoint)
        {
            DebugAssertIsValidSupplementaryPlaneCodePoint(value);

            // This calculation comes from the Unicode specification, Table 3-5.
            highSurrogateCodePoint = (char)((value + ((0xD800u - 0x40u) << 10)) >> 10);
            lowSurrogateCodePoint = (char)((value & 0x3FFu) + 0xDC00u);
        }

        [Conditional("DEBUG")]
        internal static void DebugAssertIsHighSurrogateCodePoint(uint codePoint)
        {
            if (!IsHighSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 high surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void DebugAssertIsLowSurrogateCodePoint(uint codePoint)
        {
            if (!IsLowSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 low surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void DebugAssertIsValidCodePoint(uint codePoint)
        {
            if (!IsValidCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid Unicode code point value.");
            }
        }

        [Conditional("DEBUG")]
        internal static void DebugAssertIsValidSupplementaryPlaneCodePoint(uint codePoint)
        {
            if (!IsValidCodePoint(codePoint) || IsBmpCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid supplementary plane Unicode code point value.");
            }
        }

        /// <summary>
        /// Formats a code point as the hex string "U+XXXX".
        /// </summary>
        /// <remarks>
        /// The input value doesn't have to be a real code point in the Unicode codespace. It can be any integer.
        /// </remarks>
        internal static string ToHexString(uint codePoint) => FormattableString.Invariant($"U+{codePoint:X4}");
    }
}

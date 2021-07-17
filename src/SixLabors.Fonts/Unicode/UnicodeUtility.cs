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
        /// Returns a UTF-32 buffer from the provided source buffer.
        /// </summary>
        /// <param name="source">The buffer to read from.</param>
        /// <returns>The <see cref="Memory{Int32}"/>.</returns>
        public static Memory<int> ToUtf32(ReadOnlySpan<char> source)
        {
            unsafe
            {
                fixed (char* pstr = source)
                {
                    // Get required byte count
                    int byteCount = Encoding.UTF32.GetByteCount(pstr, source.Length);

                    // Allocate buffer
                    int[] utf32 = new int[byteCount / sizeof(int)];
                    fixed (int* putf32 = utf32)
                    {
                        // Convert
                        Encoding.UTF32.GetBytes(pstr, source.Length, (byte*)putf32, byteCount);

                        // Done
                        return utf32;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a Unicode scalar value from two code points representing a UTF-16 surrogate pair.
        /// </summary>
        public static uint GetScalarFromUtf16SurrogatePair(uint highSurrogateCodePoint, uint lowSurrogateCodePoint)
        {
            AssertIsHighSurrogateCodePoint(highSurrogateCodePoint);
            AssertIsLowSurrogateCodePoint(lowSurrogateCodePoint);

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
            AssertIsValidSupplementaryPlaneCodePoint(value);

            // This calculation comes from the Unicode specification, Table 3-5.
            highSurrogateCodePoint = (char)((value + ((0xD800u - 0x40u) << 10)) >> 10);
            lowSurrogateCodePoint = (char)((value & 0x3FFu) + 0xDC00u);
        }

        [Conditional("DEBUG")]
        internal static void AssertIsHighSurrogateCodePoint(uint codePoint)
        {
            if (!IsHighSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 high surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsLowSurrogateCodePoint(uint codePoint)
        {
            if (!IsLowSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 low surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidCodePoint(uint codePoint)
        {
            if (!IsValidCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid Unicode code point value.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidSupplementaryPlaneCodePoint(uint codePoint)
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
        private static string ToHexString(uint codePoint) => FormattableString.Invariant($"U+{codePoint:X4}");
    }
}

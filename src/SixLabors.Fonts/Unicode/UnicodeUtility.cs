// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode;

internal static class UnicodeUtility
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is an ASCII
    /// character ([ U+0000..U+007F ]).
    /// </summary>
    /// <remarks>
    /// Per http://www.unicode.org/glossary/#ASCII, ASCII is only U+0000..U+007F.
    /// </remarks>
    /// <param name="value">The codepoint to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiCodePoint(uint value) => value <= 0x7Fu;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is in the
    /// Basic Multilingual Plane (BMP).
    /// </summary>
    /// <param name="value">The codepoint to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBmpCodePoint(uint value) => value <= 0xFFFFu;

    /// <summary>
    /// Gets the codepoint value representing the vertical mirror for this instance.
    /// <br/>
    /// <see href="https://www.unicode.org/reports/tr50/#vertical_alternates"/>
    /// <br/>
    /// <see href="https://github.com/harfbuzz/harfbuzz/blob/a52c6df38a38c4e36ff991dfb4b7d92e48a44553/src/hb-ot-shape.cc#L652-L701"/>
    /// </summary>
    /// <param name="value">The codepoint to test.</param>
    /// <returns>
    /// The <see cref="uint"/> representing the mirror or <c>0u</c> if not found.
    /// </returns>
    public static uint GetVerticalMirror(uint value)
    {
        switch (value >> 8)
        {
            case 0x20:
                switch (value)
                {
                    case 0x2013u:
                        return 0xfe32u; // EN DASH
                    case 0x2014u:
                        return 0xfe31u; // EM DASH
                    case 0x2025u:
                        return 0xfe30u; // TWO DOT LEADER
                    case 0x2026u:
                        return 0xfe19u; // HORIZONTAL ELLIPSIS
                }

                break;
            case 0x30:
                switch (value)
                {
                    case 0x3001u:
                        return 0xfe11u; // IDEOGRAPHIC COMMA
                    case 0x3002u:
                        return 0xfe12u; // IDEOGRAPHIC FULL STOP
                    case 0x3008u:
                        return 0xfe3fu; // LEFT ANGLE BRACKET
                    case 0x3009u:
                        return 0xfe40u; // RIGHT ANGLE BRACKET
                    case 0x300au:
                        return 0xfe3du; // LEFT DOUBLE ANGLE BRACKET
                    case 0x300bu:
                        return 0xfe3eu; // RIGHT DOUBLE ANGLE BRACKET
                    case 0x300cu:
                        return 0xfe41u; // LEFT CORNER BRACKET
                    case 0x300du:
                        return 0xfe42u; // RIGHT CORNER BRACKET
                    case 0x300eu:
                        return 0xfe43u; // LEFT WHITE CORNER BRACKET
                    case 0x300fu:
                        return 0xfe44u; // RIGHT WHITE CORNER BRACKET
                    case 0x3010u:
                        return 0xfe3bu; // LEFT BLACK LENTICULAR BRACKET
                    case 0x3011u:
                        return 0xfe3cu; // RIGHT BLACK LENTICULAR BRACKET
                    case 0x3014u:
                        return 0xfe39u; // LEFT TORTOISE SHELL BRACKET
                    case 0x3015u:
                        return 0xfe3au; // RIGHT TORTOISE SHELL BRACKET
                    case 0x3016u:
                        return 0xfe17u; // LEFT WHITE LENTICULAR BRACKET
                    case 0x3017u:
                        return 0xfe18u; // RIGHT WHITE LENTICULAR BRACKET
                }

                break;
            case 0xfe:
                switch (value)
                {
                    case 0xfe4fu:
                        return 0xfe34u; // WAVY LOW LINE
                }

                break;
            case 0xff:
                switch (value)
                {
                    case 0xff01u:
                        return 0xfe15u; // FULLWIDTH EXCLAMATION MARK
                    case 0xff08u:
                        return 0xfe35u; // FULLWIDTH LEFT PARENTHESIS
                    case 0xff09u:
                        return 0xfe36u; // FULLWIDTH RIGHT PARENTHESIS
                    case 0xff0cu:
                        return 0xfe10u; // FULLWIDTH COMMA
                    case 0xff1au:
                        return 0xfe13u; // FULLWIDTH COLON
                    case 0xff1bu:
                        return 0xfe14u; // FULLWIDTH SEMICOLON
                    case 0xff1fu:
                        return 0xfe16u; // FULLWIDTH QUESTION MARK
                    case 0xff3bu:
                        return 0xfe47u; // FULLWIDTH LEFT SQUARE BRACKET
                    case 0xff3du:
                        return 0xfe48u; // FULLWIDTH RIGHT SQUARE BRACKET
                    case 0xff3fu:
                        return 0xfe33u; // FULLWIDTH LOW LINE
                    case 0xff5bu:
                        return 0xfe37u; // FULLWIDTH LEFT CURLY BRACKET
                    case 0xff5du:
                        return 0xfe38u; // FULLWIDTH RIGHT CURLY BRACKET
                }

                break;
        }

        return 0u;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a
    /// Chinese/Japanese/Korean (CJK) character.
    /// </summary>
    /// <remarks>
    /// <see href="https://blog.ceshine.net/post/cjk-unicode/"/>
    /// <see href="https://en.wikipedia.org/wiki/Unicode_block"/>
    /// </remarks>
    /// <param name="value">The codepoint to test.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> is a CJK code point.
    /// </returns>
    public static bool IsCJKCodePoint(uint value)
    {
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

        // CJK Unified Ideographs Extension H
        if (IsInRangeInclusive(value, 0x31350u, 0x323AFu))
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

        // CJK Compatibility
        if (IsInRangeInclusive(value, 0x3300u, 0x33FFu))
        {
            return true;
        }

        // CJK Radicals Supplement
        if (IsInRangeInclusive(value, 0x2E80u, 0x2EFFu))
        {
            return true;
        }

        // Kangxi Radicals
        if (IsInRangeInclusive(value, 0x2F00u, 0x2FDFu))
        {
            return true;
        }

        // Ideographic Description Characters
        if (IsInRangeInclusive(value, 0x2FF0u, 0x2FFFu))
        {
            return true;
        }

        // CJK Strokes
        if (IsInRangeInclusive(value, 0x31C0u, 0x31EFu))
        {
            return true;
        }

        // CJK Symbols and Punctuation
        if (IsInRangeInclusive(value, 0x3000u, 0x303Fu))
        {
            return true;
        }

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

        // Katakana Phonetic Extensions
        if (IsInRangeInclusive(value, 0x31F0u, 0x31FFu))
        {
            return true;
        }

        // Hangul Syllables
        if (IsInRangeInclusive(value, 0xAC00u, 0xD7AFu))
        {
            return true;
        }

        // Hangul Jamo
        if (IsInRangeInclusive(value, 0x1100u, 0x11FFu))
        {
            return true;
        }

        // Hangul Jamo Extended-A
        if (IsInRangeInclusive(value, 0xA960u, 0xA97Fu))
        {
            return true;
        }

        // Hangul Jamo Extended-B
        if (IsInRangeInclusive(value, 0xD7B0u, 0xD7FFu))
        {
            return true;
        }

        // Bopomofo
        if (IsInRangeInclusive(value, 0x3100u, 0x312Fu))
        {
            return true;
        }

        // Bopomofo Extended
        if (IsInRangeInclusive(value, 0x31A0u, 0x31BFu))
        {
            return true;
        }

        // Enclosed CJK Letters and Months
        if (IsInRangeInclusive(value, 0x3200u, 0x32FFu))
        {
            return true;
        }

        // Enclosed Ideographic Supplement
        if (IsInRangeInclusive(value, 0x1F200u, 0x1F2FFu))
        {
            return true;
        }

        // Vertical Forms
        if (IsInRangeInclusive(value, 0xFE10u, 0xFE1Fu))
        {
            return true;
        }

        // Halfwidth and Fullwidth Forms
        if (IsInRangeInclusive(value, 0xFF00u, 0xFFEFu))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a Default Ignorable Code Point.
    /// </summary>
    /// <param name="value">The codepoint value.</param>
    /// <remarks>
    /// <see href="http://www.unicode.org/reports/tr44/#Default_Ignorable_Code_Point"/>
    /// <see href="https://www.unicode.org/Public/14.0.0/ucd/DerivedCoreProperties.txt"/>
    /// </remarks>
    public static bool IsDefaultIgnorableCodePoint(uint value)
    {
        // SOFT HYPHEN
        if (value == 0x00AD)
        {
            return true;
        }

        // COMBINING GRAPHEME JOINER
        if (value == 0x034F)
        {
            return true;
        }

        // COMBINING GRAPHEME JOINER
        if (value == 0x061C)
        {
            return true;
        }

        // HANGUL CHOSEONG FILLER..HANGUL JUNGSEONG FILLER
        if (IsInRangeInclusive(value, 0x115F, 0x1160))
        {
            return true;
        }

        // KHMER VOWEL INHERENT AQ..KHMER VOWEL INHERENT AA
        if (IsInRangeInclusive(value, 0x17B4, 0x17B5))
        {
            return true;
        }

        // MONGOLIAN FREE VARIATION SELECTOR ONE..MONGOLIAN FREE VARIATION SELECTOR THREE
        if (IsInRangeInclusive(value, 0x180B, 0x180D))
        {
            return true;
        }

        // MONGOLIAN VOWEL SEPARATOR
        if (value == 0x180E)
        {
            return true;
        }

        // MONGOLIAN FREE VARIATION SELECTOR FOUR
        if (value == 0x180F)
        {
            return true;
        }

        // ZERO WIDTH SPACE..RIGHT-TO-LEFT MARK
        if (IsInRangeInclusive(value, 0x200B, 0x200F))
        {
            return true;
        }

        // LEFT-TO-RIGHT EMBEDDING..RIGHT-TO-LEFT OVERRIDE
        if (IsInRangeInclusive(value, 0x202A, 0x202E))
        {
            return true;
        }

        // WORD JOINER..INVISIBLE PLUS
        if (IsInRangeInclusive(value, 0x2060, 0x2064))
        {
            return true;
        }

        // <reserved-2065>
        if (value == 0x2065)
        {
            return true;
        }

        // LEFT-TO-RIGHT ISOLATE..NOMINAL DIGIT SHAPES
        if (IsInRangeInclusive(value, 0x2066, 0x206F))
        {
            return true;
        }

        // HANGUL FILLER
        if (value == 0x3164)
        {
            return true;
        }

        // VARIATION SELECTOR-1..VARIATION SELECTOR-16
        if (IsInRangeInclusive(value, 0xFE00, 0xFE0F))
        {
            return true;
        }

        // ZERO WIDTH NO-BREAK SPACE
        if (value == 0xFEFF)
        {
            return true;
        }

        // HALFWIDTH HANGUL FILLER
        if (value == 0xFFA0)
        {
            return true;
        }

        // <reserved-FFF0>..<reserved-FFF8>
        if (IsInRangeInclusive(value, 0xFFF0, 0xFFF8))
        {
            return true;
        }

        // SHORTHAND FORMAT LETTER OVERLAP..SHORTHAND FORMAT UP STEP
        if (IsInRangeInclusive(value, 0x1BCA0, 0x1BCA3))
        {
            return true;
        }

        // MUSICAL SYMBOL BEGIN BEAM..MUSICAL SYMBOL END PHRASE
        if (IsInRangeInclusive(value, 0x1D173, 0x1D17A))
        {
            return true;
        }

        // <reserved-E0000>
        if (value == 0xE0000)
        {
            return true;
        }

        // LANGUAGE TAG
        if (value == 0xE0001)
        {
            return true;
        }

        // <reserved-E0002>..<reserved-E001F>
        if (IsInRangeInclusive(value, 0xE0002, 0xE001F))
        {
            return true;
        }

        // TAG SPACE..CANCEL TAG
        if (IsInRangeInclusive(value, 0xE0020, 0xE007F))
        {
            return true;
        }

        // <reserved-E0080>..<reserved-E00FF>
        if (IsInRangeInclusive(value, 0xE0080, 0xE00FF))
        {
            return true;
        }

        // VARIATION SELECTOR-17..VARIATION SELECTOR-256
        if (IsInRangeInclusive(value, 0xE0100, 0xE01EF))
        {
            return true;
        }

        // <reserved-E01F0>..<reserved-E0FFF>
        if (IsInRangeInclusive(value, 0xE01F0, 0xE0FFF))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the specified code point should be rendered as a white space only.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldRenderWhiteSpaceOnly(in CodePoint codePoint)
    {
        if (CodePoint.IsWhiteSpace(codePoint))
        {
            return true;
        }

        // Note: While U+115F, U+1160, U+3164 and U+FFA0 are Default_Ignorable,
        // we do NOT want to hide them, as the way Uniscribe has implemented them
        // is with regular spacing glyphs, and that's the way fonts are made to work.
        // As such, we make exceptions for those four.
        // Also ignoring U+1BCA0..1BCA3. https://github.com/harfbuzz/harfbuzz/issues/503
        uint value = (uint)codePoint.Value;
        if (value is 0x115F or 0x1160 or 0x3164 or 0xFFA0)
        {
            return true;
        }

        if (IsInRangeInclusive(value, 0x1BCA0, 0x1BCA3))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the specified code point should not be rendered.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldNotBeRendered(in CodePoint codePoint)
        => CodePoint.IsNewLine(codePoint) || (IsDefaultIgnorableCodePoint((uint)codePoint.Value) && !ShouldRenderWhiteSpaceOnly(codePoint));

    /// <summary>
    /// Returns the Unicode plane (0 through 16, inclusive) which contains this code point.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPlane(uint codePoint)
    {
        DebugAssertIsValidCodePoint(codePoint);

        return (int)(codePoint >> 16);
    }

    /// <summary>
    /// Given a Unicode scalar value, gets the number of UTF-16 code units required to represent this value.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
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
    /// <param name="codePoint">The code point.</param>
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
    /// <param name="codePoint">The code point.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidCodePoint(uint codePoint) => codePoint <= 0x10FFFFu;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 high surrogate code point,
    /// i.e., is in [ U+D800..U+DBFF ], inclusive.
    /// </summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHighSurrogateCodePoint(uint value)
        => IsInRangeInclusive(value, 0xD800u, 0xDBFFu);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 low surrogate code point,
    /// i.e., is in [ U+DC00..U+DFFF ], inclusive.
    /// </summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLowSurrogateCodePoint(uint value)
        => IsInRangeInclusive(value, 0xDC00u, 0xDFFFu);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-16 surrogate code point,
    /// i.e., is in [ U+D800..U+DFFF ], inclusive.
    /// </summary>
    /// <param name="value">The value to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSurrogateCodePoint(uint value)
        => IsInRangeInclusive(value, 0xD800u, 0xDFFFu);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="lowerBound">The lower bound.</param>
    /// <param name="upperBound">The upper bound.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
        => (value - lowerBound) <= (upperBound - lowerBound);

    /// <summary>
    /// Returns a Unicode scalar value from two code points representing a UTF-16 surrogate pair.
    /// </summary>
    /// <param name="highSurrogateCodePoint">The high surrogate code point.</param>
    /// <param name="lowSurrogateCodePoint">The low surrogate code point.</param>
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
    /// <param name="value">The Unicode code point.</param>
    /// <param name="highSurrogateCodePoint">The high surrogate code point.</param>
    /// <param name="lowSurrogateCodePoint">The low surrogate code point.</param>
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
    /// <param name="codePoint">The code point.</param>
    internal static string ToHexString(uint codePoint) => FormattableString.Invariant($"U+{codePoint:X4}");
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Word_Break property values.
/// <see href="https://www.unicode.org/reports/tr29/#Word_Boundaries"/>
/// </summary>
public enum WordBreakClass : uint
{
    /// <summary>
    /// U+000D CARRIAGE RETURN (CR).
    /// </summary>
    CarriageReturn = 0,

    /// <summary>
    /// U+000A LINE FEED (LF).
    /// </summary>
    LineFeed = 1,

    /// <summary>
    /// Newline characters other than CR and LF.
    /// </summary>
    Newline = 2,

    /// <summary>
    /// Extending code points that are ignored by most word boundary rules.
    /// </summary>
    Extend = 3,

    /// <summary>
    /// U+200D ZERO WIDTH JOINER.
    /// </summary>
    ZeroWidthJoiner = 4,

    /// <summary>
    /// Regional indicator symbols used to build flag emoji pairs.
    /// </summary>
    RegionalIndicator = 5,

    /// <summary>
    /// Format characters that are ignored by most word boundary rules.
    /// </summary>
    Format = 6,

    /// <summary>
    /// Katakana characters.
    /// </summary>
    Katakana = 7,

    /// <summary>
    /// Hebrew letters.
    /// </summary>
    HebrewLetter = 8,

    /// <summary>
    /// Alphabetic letters.
    /// </summary>
    ALetter = 9,

    /// <summary>
    /// Single quote.
    /// </summary>
    SingleQuote = 10,

    /// <summary>
    /// Double quote.
    /// </summary>
    DoubleQuote = 11,

    /// <summary>
    /// Mid-letter and mid-number punctuation.
    /// </summary>
    MidNumLet = 12,

    /// <summary>
    /// Mid-letter punctuation.
    /// </summary>
    MidLetter = 13,

    /// <summary>
    /// Mid-number punctuation.
    /// </summary>
    MidNum = 14,

    /// <summary>
    /// Numeric characters.
    /// </summary>
    Numeric = 15,

    /// <summary>
    /// Connector characters that extend letters, numbers, and Katakana.
    /// </summary>
    ExtendNumLet = 16,

    /// <summary>
    /// Horizontal whitespace segmented as word-segmentation space.
    /// </summary>
    WSegSpace = 17,

    /// <summary>
    /// Other.
    /// </summary>
    Other = 0xFF
}

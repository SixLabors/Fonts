// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

// Reordering these enum properties will require the regeneration of the LineBreak.trie.
namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode line break classes.
/// <see href="https://www.unicode.org/reports/tr14/tr14-37.html#Table1"/>
/// </summary>
public enum LineBreakClass : uint
{
    /// <summary>
    /// Open punctuation
    /// </summary>
    OP = 0,

    /// <summary>
    /// Closing punctuation
    /// </summary>
    CL = 1,

    /// <summary>
    /// Closing parenthesis
    /// </summary>
    CP = 2,

    /// <summary>
    /// Ambiguous quotation
    /// </summary>
    QU = 3,

    /// <summary>
    /// Glue
    /// </summary>
    GL = 4,

    /// <summary>
    /// Non-starters
    /// </summary>
    NS = 5,

    /// <summary>
    /// Exclamation/Interrogation
    /// </summary>
    EX = 6,

    /// <summary>
    /// Symbols allowing break after
    /// </summary>
    SY = 7,

    /// <summary>
    /// Infix separator
    /// </summary>
    IS = 8,

    /// <summary>
    /// Prefix
    /// </summary>
    PR = 9,

    /// <summary>
    /// Postfix
    /// </summary>
    PO = 10,

    /// <summary>
    /// Numeric
    /// </summary>
    NU = 11,

    /// <summary>
    /// Alphabetic
    /// </summary>
    AL = 12,

    /// <summary>
    /// Hebrew Letter
    /// </summary>
    HL = 13,

    /// <summary>
    /// Ideographic
    /// </summary>
    ID = 14,

    /// <summary>
    /// Inseparable characters
    /// </summary>
    IN = 15,

    /// <summary>
    /// Hyphen
    /// </summary>
    HY = 16,

    /// <summary>
    /// Break after
    /// </summary>
    BA = 17,

    /// <summary>
    /// Break before
    /// </summary>
    BB = 18,

    /// <summary>
    /// Break on either side (but not pair)
    /// </summary>
    B2 = 19,

    /// <summary>
    /// Zero-width space
    /// </summary>
    ZW = 20,

    /// <summary>
    /// Combining marks
    /// </summary>
    CM = 21,

    /// <summary>
    /// Word joiner
    /// </summary>
    WJ = 22,

    /// <summary>
    /// Hangul LV
    /// </summary>
    H2 = 23,

    /// <summary>
    /// Hangul LVT
    /// </summary>
    H3 = 24,

    /// <summary>
    /// Hangul L Jamo
    /// </summary>
    JL = 25,

    /// <summary>
    /// Hangul V Jamo
    /// </summary>
    JV = 26,

    /// <summary>
    /// Hangul T Jamo
    /// </summary>
    JT = 27,

    /// <summary>
    /// Regional Indicator
    /// </summary>
    RI = 28,

    /// <summary>
    /// Emoji Base
    /// </summary>
    EB = 29,

    /// <summary>
    /// Emoji Modifier
    /// </summary>
    EM = 30,

    /// <summary>
    /// Zero Width Joiner
    /// </summary>
    ZWJ = 31,

    /// <summary>
    /// Contingent break
    /// </summary>
    CB = 32,

    /// <summary>
    /// Ambiguous (Alphabetic or Ideograph)
    /// </summary>
    AI = 33,

    /// <summary>
    /// Break (mandatory)
    /// </summary>
    BK = 34,

    /// <summary>
    /// Conditional Japanese Starter
    /// </summary>
    CJ = 35,

    /// <summary>
    /// Carriage return
    /// </summary>
    CR = 36,

    /// <summary>
    /// Line feed
    /// </summary>
    LF = 37,

    /// <summary>
    /// Next line
    /// </summary>
    NL = 38,

    /// <summary>
    /// South-East Asian
    /// </summary>
    SA = 39,

    /// <summary>
    /// Surrogates
    /// </summary>
    SG = 40,

    /// <summary>
    /// Space
    /// </summary>
    SP = 41,

    /// <summary>
    /// Unknown
    /// </summary>
    XX = 42,
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Line_Break property values.
/// <see href="https://www.unicode.org/reports/tr14/#Table1"/>
/// </summary>
public enum LineBreakClass : uint
{
    /// <summary>
    /// Open punctuation (OP): characters that prohibit a line break after them.
    /// </summary>
    OpenPunctuation = 0,

    /// <summary>
    /// Close punctuation (CL): characters that prohibit a line break before them.
    /// </summary>
    ClosePunctuation = 1,

    /// <summary>
    /// Close parenthesis (CP): closing bracket and parenthesis characters that prohibit a line break before them.
    /// </summary>
    CloseParenthesis = 2,

    /// <summary>
    /// Quotation (QU): quotation marks that can behave as opening punctuation, closing punctuation, or both.
    /// </summary>
    Quotation = 3,

    /// <summary>
    /// Non-breaking glue (GL): characters that prohibit line breaks before and after themselves.
    /// </summary>
    Glue = 4,

    /// <summary>
    /// Nonstarter (NS): characters that allow only indirect line breaks before themselves.
    /// </summary>
    Nonstarter = 5,

    /// <summary>
    /// Exclamation or interrogation (EX): sentence punctuation that prohibits a line break before itself.
    /// </summary>
    Exclamation = 6,

    /// <summary>
    /// Symbols allowing break after (SY): characters that prevent a line break before and allow one after.
    /// </summary>
    BreakSymbols = 7,

    /// <summary>
    /// Infix numeric separator (IS): characters that suppress breaks inside numeric expressions.
    /// </summary>
    InfixNumeric = 8,

    /// <summary>
    /// Prefix numeric (PR): characters that stay with a following numeric expression.
    /// </summary>
    PrefixNumeric = 9,

    /// <summary>
    /// Postfix numeric (PO): characters that stay with a preceding numeric expression.
    /// </summary>
    PostfixNumeric = 10,

    /// <summary>
    /// Numeric (NU): digits and related characters that form numeric expressions.
    /// </summary>
    Numeric = 11,

    /// <summary>
    /// Alphabetic (AL): letters and ordinary symbols that use alphabetic line breaking behavior.
    /// </summary>
    Alphabetic = 12,

    /// <summary>
    /// Hebrew letter (HL): Hebrew characters with special hyphen and solidus behavior.
    /// </summary>
    HebrewLetter = 13,

    /// <summary>
    /// Ideographic (ID): ideographic characters that generally allow breaks before or after.
    /// </summary>
    Ideographic = 14,

    /// <summary>
    /// Inseparable (IN): characters, such as leaders, that allow only indirect line breaks between pairs.
    /// </summary>
    Inseparable = 15,

    /// <summary>
    /// Hyphen (HY): hyphen-minus and similar characters that allow breaks after except in numeric context.
    /// </summary>
    Hyphen = 16,

    /// <summary>
    /// Break after (BA): characters that generally provide a line break opportunity after themselves.
    /// </summary>
    BreakAfter = 17,

    /// <summary>
    /// Break before (BB): characters that generally provide a line break opportunity before themselves.
    /// </summary>
    BreakBefore = 18,

    /// <summary>
    /// Break before and after (B2): characters that allow breaks on either side, but not between two B2 characters.
    /// </summary>
    BreakBeforeAndAfter = 19,

    /// <summary>
    /// Zero width space (ZW): an explicit opportunity for a line break.
    /// </summary>
    ZeroWidthSpace = 20,

    /// <summary>
    /// Combining mark (CM): combining marks and related controls that stay with the preceding character.
    /// </summary>
    CombiningMark = 21,

    /// <summary>
    /// Word joiner (WJ): characters that prohibit line breaks before and after themselves.
    /// </summary>
    WordJoiner = 22,

    /// <summary>
    /// Hangul LV syllable (H2): part of the Hangul sequence classes used to form Korean syllable blocks.
    /// </summary>
    HangulLeadVowelSyllable = 23,

    /// <summary>
    /// Hangul LVT syllable (H3): part of the Hangul sequence classes used to form Korean syllable blocks.
    /// </summary>
    HangulLeadVowelTailSyllable = 24,

    /// <summary>
    /// Hangul L Jamo (JL): leading consonant Jamo used to form Korean syllable blocks.
    /// </summary>
    HangulLeadJamo = 25,

    /// <summary>
    /// Hangul V Jamo (JV): vowel Jamo used to form Korean syllable blocks.
    /// </summary>
    HangulVowelJamo = 26,

    /// <summary>
    /// Hangul T Jamo (JT): trailing consonant Jamo used to form Korean syllable blocks.
    /// </summary>
    HangulTailJamo = 27,

    /// <summary>
    /// Regional indicator (RI): symbols paired for flag sequences.
    /// </summary>
    RegionalIndicator = 28,

    /// <summary>
    /// Emoji base (EB): emoji characters that must not break from a following emoji modifier.
    /// </summary>
    EmojiBase = 29,

    /// <summary>
    /// Emoji modifier (EM): emoji modifiers that must not break from a preceding emoji base.
    /// </summary>
    EmojiModifier = 30,

    /// <summary>
    /// Zero width joiner (ZWJ): joiner control that prohibits breaks inside joiner sequences.
    /// </summary>
    ZeroWidthJoiner = 31,

    /// <summary>
    /// Contingent break (CB): break opportunity determined by additional layout information.
    /// </summary>
    ContingentBreak = 32,

    /// <summary>
    /// Ambiguous alphabetic or ideographic (AI): resolved by LB1 before rule evaluation.
    /// </summary>
    Ambiguous = 33,

    /// <summary>
    /// Mandatory break (BK): characters that cause a line break after themselves.
    /// </summary>
    MandatoryBreak = 34,

    /// <summary>
    /// Conditional Japanese starter (CJ): small Kana and related characters resolved by tailoring.
    /// </summary>
    ConditionalJapaneseStarter = 35,

    /// <summary>
    /// Carriage return (CR): causes a line break after itself except in a CR LF sequence.
    /// </summary>
    CarriageReturn = 36,

    /// <summary>
    /// Line feed (LF): causes a line break after itself.
    /// </summary>
    LineFeed = 37,

    /// <summary>
    /// Next line (NL): causes a line break after itself.
    /// </summary>
    NextLine = 38,

    /// <summary>
    /// Complex context dependent (SA): requires language-specific analysis for line break opportunities.
    /// </summary>
    ComplexContext = 39,

    /// <summary>
    /// Surrogate (SG): surrogate code points, which do not occur in well-formed Unicode scalar text.
    /// </summary>
    Surrogate = 40,

    /// <summary>
    /// Space (SP): enables indirect line breaks.
    /// </summary>
    Space = 41,

    /// <summary>
    /// Aksara (AK): consonants that form orthographic syllables in Brahmic scripts.
    /// </summary>
    Aksara = 43,

    /// <summary>
    /// Aksara pre-base (AP): pre-base signs, such as repha, that form Brahmic orthographic syllables.
    /// </summary>
    AksaraPrebase = 44,

    /// <summary>
    /// Aksara start (AS): independent vowels and related starters for Brahmic orthographic syllables.
    /// </summary>
    AksaraStart = 45,

    /// <summary>
    /// Unambiguous hyphen (HH): hyphen characters with unambiguous break-after behavior except word-initially.
    /// </summary>
    UnambiguousHyphen = 46,

    /// <summary>
    /// Virama final (VF): final consonant viramas that form Brahmic orthographic syllables.
    /// </summary>
    ViramaFinal = 47,

    /// <summary>
    /// Virama (VI): conjoining viramas that form Brahmic orthographic syllables.
    /// </summary>
    Virama = 48,

    /// <summary>
    /// Unknown (XX): code points without an explicit line break class.
    /// </summary>
    Unknown = 0xFF,
}

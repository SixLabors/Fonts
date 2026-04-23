// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Grapheme_Cluster_Break property values and local rule sentinels.
/// <see href="https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values"/>
/// </summary>
/// <remarks>
/// UAX #29 uses these classes in ordered boundary rules to determine extended
/// grapheme clusters. Some members are rule sentinels rather than Unicode property
/// values exposed by the standard.
/// </remarks>
public enum GraphemeClusterClass
{
    /// <summary>
    /// Rule sentinel that matches any code point.
    /// </summary>
    /// <remarks>
    /// This is not a Unicode property value; it represents the "any" operand in
    /// UAX #29 boundary rules.
    /// </remarks>
    Any = 0,

    /// <summary>
    /// U+000D CARRIAGE RETURN (CR).
    /// </summary>
    CarriageReturn = 1,

    /// <summary>
    /// U+000A LINE FEED (LF).
    /// </summary>
    LineFeed = 2,

    /// <summary>
    /// Controls, separators, formats, and default-ignorable unassigned code points
    /// that form hard grapheme cluster boundaries.
    /// </summary>
    /// <remarks>
    /// This class excludes CR, LF, U+200C ZERO WIDTH NON-JOINER, U+200D ZERO
    /// WIDTH JOINER, and prepended concatenation marks because those participate
    /// in more specific UAX #29 rules.
    /// </remarks>
    Control = 3,

    /// <summary>
    /// Extending code points that remain in the same extended grapheme cluster as
    /// the preceding base.
    /// </summary>
    /// <remarks>
    /// This includes Grapheme_Extend code points, emoji modifiers, U+200C ZERO
    /// WIDTH NON-JOINER, and a small number of spacing marks needed for canonical
    /// equivalence.
    /// </remarks>
    Extend = 4,

    /// <summary>
    /// Regional indicator symbols used to build flag emoji pairs.
    /// </summary>
    RegionalIndicator = 5,

    /// <summary>
    /// Code points that prepend to the following grapheme cluster.
    /// </summary>
    /// <remarks>
    /// This includes Indic_Syllabic_Category values Consonant_Preceding_Repha and
    /// Consonant_Prefixed, plus Prepended_Concatenation_Mark code points.
    /// </remarks>
    Prepend = 6,

    /// <summary>
    /// Spacing marks that extend the previous grapheme cluster.
    /// </summary>
    /// <remarks>
    /// This includes spacing marks whose Grapheme_Cluster_Break value is not
    /// Extend, plus U+0E33 THAI CHARACTER SARA AM and U+0EB3 LAO VOWEL SIGN AM.
    /// </remarks>
    SpacingMark = 7,

    /// <summary>
    /// Hangul leading consonant Jamo (Hangul_Syllable_Type = L).
    /// </summary>
    HangulLead = 8,

    /// <summary>
    /// Hangul vowel Jamo (Hangul_Syllable_Type = V).
    /// </summary>
    HangulVowel = 9,

    /// <summary>
    /// Hangul trailing consonant Jamo (Hangul_Syllable_Type = T).
    /// </summary>
    HangulTail = 10,

    /// <summary>
    /// Hangul LV syllables.
    /// </summary>
    HangulLeadVowel = 11,

    /// <summary>
    /// Hangul LVT syllables.
    /// </summary>
    HangulLeadVowelTail = 12,

    /// <summary>
    /// Extended pictographic code points used by GB11 emoji ZWJ sequence handling.
    /// </summary>
    /// <remarks>
    /// This is not itself a Grapheme_Cluster_Break property value; UAX #29 uses
    /// it when matching emoji ZWJ sequences.
    /// </remarks>
    ExtendedPictographic = 13,

    /// <summary>
    /// U+200D ZERO WIDTH JOINER.
    /// </summary>
    ZeroWidthJoiner = 14,

    /// <summary>
    /// Other.
    /// </summary>
    /// <remarks>
    /// This is the Unicode <c>Other</c> / <c>XX</c> fallback for code points
    /// without an explicit grapheme cluster break class.
    /// </remarks>
    Other = 0xFF
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode East_Asian_Width property values.
/// <see href="https://www.unicode.org/reports/tr11/"/>
/// </summary>
/// <remarks>
/// East_Asian_Width is a Unicode character property used when interoperating with
/// East Asian legacy encodings and typography. The property has six default values,
/// but any operation that needs a display-cell width must still resolve those values
/// to a context-specific narrow or wide result.
/// </remarks>
public enum EastAsianWidthClass
{
    /// <summary>
    /// Neutral (N): characters that are not East Asian for this property.
    /// </summary>
    /// <remarks>
    /// Neutral characters are not normally found in legacy East Asian character sets or
    /// traditional East Asian typography. UAX #11 recommends treating them like narrow
    /// characters for practical resolved-width decisions, but the property value itself
    /// is distinct from <see cref="Narrow"/>.
    /// </remarks>
    Neutral = 0,

    /// <summary>
    /// Ambiguous (A): characters that can be either wide or narrow depending on context.
    /// </summary>
    /// <remarks>
    /// Ambiguous characters need extra information, such as language, script, font,
    /// source encoding, or explicit markup, before they can be resolved to a display
    /// width. In East Asian legacy contexts they may be treated as wide;
    /// otherwise UAX #11 recommends treating them as narrow by default.
    /// </remarks>
    Ambiguous = 1,

    /// <summary>
    /// Fullwidth (F): explicitly encoded fullwidth compatibility characters.
    /// </summary>
    /// <remarks>
    /// Fullwidth characters have a compatibility decomposition of type <c>&lt;wide&gt;</c>
    /// to another Unicode character that is implicitly narrow. They exist to preserve
    /// round-tripping with mixed-width East Asian legacy encodings.
    /// </remarks>
    Fullwidth = 2,

    /// <summary>
    /// Halfwidth (H): explicitly encoded halfwidth compatibility characters.
    /// </summary>
    /// <remarks>
    /// Halfwidth characters have a compatibility decomposition of type <c>&lt;narrow&gt;</c>
    /// to another Unicode character that is implicitly wide, with the special case of
    /// U+20A9 WON SIGN. They are distinct from ordinary narrow characters because they
    /// can still behave like East Asian compatibility forms for font selection and some
    /// punctuation behavior.
    /// </remarks>
    Halfwidth = 3,

    /// <summary>
    /// Narrow (Na): characters that are always narrow and have explicit wide or fullwidth counterparts.
    /// </summary>
    /// <remarks>
    /// Narrow characters are implicitly narrow in East Asian typography and legacy
    /// character sets. ASCII is the common example: the ordinary ASCII code points are
    /// Narrow, while their compatibility forms are Fullwidth.
    /// </remarks>
    Narrow = 4,

    /// <summary>
    /// Wide (W): characters that are always wide in East Asian typography.
    /// </summary>
    /// <remarks>
    /// Wide characters behave like ideographs for East Asian layout. This includes
    /// many Han, Kana, Hangul, and emoji-presentation characters that are not encoded
    /// as explicit Fullwidth compatibility forms.
    /// </remarks>
    Wide = 5
}

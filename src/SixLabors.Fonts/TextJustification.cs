// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Text justification modes.
/// </summary>
public enum TextJustification
{
    /// <summary>
    /// No justification
    /// </summary>
    None = 0,

    /// <summary>
    /// The text is justified by adding space between words (effectively varying word-spacing),
    /// which is most appropriate for languages that separate words using spaces, like English or Korean.
    /// </summary>
    InterWord,

    /// <summary>
    /// The text is justified by adding space between characters (effectively varying letter-spacing),
    /// which is most appropriate for languages like Japanese.
    /// </summary>
    InterCharacter
}

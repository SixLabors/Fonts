// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents a font family and style matched for a character.
/// </summary>
public readonly struct FontMatch
{
    internal FontMatch(FontFamily family, FontStyle style)
    {
        this.Family = family;
        this.Style = style;
    }

    /// <summary>
    /// Gets the matched font family.
    /// </summary>
    public FontFamily Family { get; }

    /// <summary>
    /// Gets the matched font style.
    /// </summary>
    public FontStyle Style { get; }
}

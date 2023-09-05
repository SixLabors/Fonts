// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Provides enumeration of various text decorations.
/// </summary>
[Flags]
public enum TextDecorations
{
    /// <summary>
    /// No attributes are applied
    /// </summary>
    None = 0,

    /// <summary>
    /// The text is underlined
    /// </summary>
    Underline = 1 << 0,

    /// <summary>
    /// The text contains a horizontal line through the center.
    /// </summary>
    Strikeout = 1 << 1,

    /// <summary>
    /// The text contains a horizontal line above it
    /// </summary>
    Overline = 1 << 2
}

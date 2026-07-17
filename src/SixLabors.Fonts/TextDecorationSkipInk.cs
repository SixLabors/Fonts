// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Identifies whether underline and overline decorations are interrupted where they would
/// otherwise cross glyph ink, mirroring the CSS <c>text-decoration-skip-ink</c> property.
/// Strikethrough decorations always cross the ink regardless of this setting.
/// </summary>
public enum TextDecorationSkipInk : byte
{
    /// <summary>
    /// Underlines and overlines skip over glyph ink, leaving gaps around descenders and
    /// ascenders that cross the decoration.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Decorations are drawn continuously, crossing any glyph ink in their path.
    /// </summary>
    None = 1
}

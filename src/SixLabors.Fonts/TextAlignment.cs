// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Text alignment modes.
/// </summary>
public enum TextAlignment
{
    /// <summary>
    /// Aligns text from the left or top when the text direction is <see cref="TextDirection.LeftToRight"/>
    /// and from the right or bottom when the text direction is <see cref="TextDirection.RightToLeft"/>.
    /// </summary>
    Start = 0,

    /// <summary>
    /// Aligns text from the right or bottom when the text direction is <see cref="TextDirection.LeftToRight"/>
    /// and from the left or top when the text direction is <see cref="TextDirection.RightToLeft"/>.
    /// </summary>
    End = 1,

    /// <summary>
    /// Aligns text from the center.
    /// </summary>
    Center = 2
}

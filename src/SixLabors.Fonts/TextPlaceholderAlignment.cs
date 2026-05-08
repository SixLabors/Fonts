// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies how an inline placeholder is aligned against surrounding text.
/// </summary>
public enum TextPlaceholderAlignment
{
    /// <summary>
    /// Align the placeholder baseline with the surrounding text baseline.
    /// </summary>
    Baseline,

    /// <summary>
    /// Align the placeholder above the surrounding text baseline.
    /// </summary>
    AboveBaseline,

    /// <summary>
    /// Align the placeholder below the surrounding text baseline.
    /// </summary>
    BelowBaseline,

    /// <summary>
    /// Align the placeholder with the top of the surrounding line box.
    /// </summary>
    Top,

    /// <summary>
    /// Align the placeholder with the bottom of the surrounding line box.
    /// </summary>
    Bottom,

    /// <summary>
    /// Align the placeholder with the middle of the surrounding line box.
    /// </summary>
    Middle
}

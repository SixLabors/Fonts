// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies hyphenation marker behavior when text breaks at hyphenation opportunities.
/// </summary>
public enum TextHyphenation
{
    /// <summary>
    /// Do not insert a hyphenation marker.
    /// </summary>
    None = 0,

    /// <summary>
    /// Insert the standard hyphenation marker.
    /// </summary>
    Standard,

    /// <summary>
    /// Insert the marker specified by <see cref="TextOptions.CustomHyphen"/>.
    /// </summary>
    Custom
}

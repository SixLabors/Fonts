// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies ellipsis behavior when laid-out text is limited to a maximum number of lines.
/// </summary>
public enum TextEllipsis
{
    /// <summary>
    /// Do not insert an ellipsis marker.
    /// </summary>
    None = 0,

    /// <summary>
    /// Insert the standard ellipsis marker.
    /// </summary>
    Standard,

    /// <summary>
    /// Insert the marker specified by <see cref="TextOptions.CustomEllipsis"/>.
    /// </summary>
    Custom
}

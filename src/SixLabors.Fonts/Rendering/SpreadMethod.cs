// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Specifies how a gradient should extend beyond the [0, 1] range.
/// </summary>
public enum SpreadMethod
{
    /// <summary>
    /// Clamp to the end colors (pad).
    /// </summary>
    Pad = 0,

    /// <summary>
    /// Mirror the gradient (reflect).
    /// </summary>
    Reflect = 1,

    /// <summary>
    /// Repeat the gradient (tile).
    /// </summary>
    Repeat = 2,
}

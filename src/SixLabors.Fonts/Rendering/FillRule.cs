// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Specifies the fill rule for path rasterization.
/// </summary>
public enum FillRule
{
    /// <summary>
    /// Non-zero winding rule.
    /// </summary>
    NonZero = 0,

    /// <summary>
    /// Even-odd rule.
    /// </summary>
    EvenOdd = 1,
}

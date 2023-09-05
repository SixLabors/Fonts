// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Vertical Orientation types.
/// <see href="https://www.unicode.org/reports/tr50/#vo"/>
/// </summary>
public enum VerticalOrientationType
{
    /// <summary>
    /// Characters which are displayed upright, with the same orientation that appears in the code charts.
    /// </summary>
    Upright,

    /// <summary>
    /// Characters which are displayed sideways, rotated 90 degrees clockwise compared to the code charts.
    /// </summary>
    Rotate,

    /// <summary>
    /// Characters which are not just upright or sideways, but generally require a different glyph than in the code
    /// charts when used in vertical texts. In addition, as a fallback, the character can be displayed with the code
    /// chart glyph upright.
    /// </summary>
    TransformUpright,

    /// <summary>
    /// Same as <see cref="TransformUpright"/> except that, as a fallback, the character can be displayed with the code chart glyph rotated 90 degrees clockwise.
    /// </summary>
    TransformRotate
}

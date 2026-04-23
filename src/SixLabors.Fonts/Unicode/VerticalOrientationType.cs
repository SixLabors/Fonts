// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Vertical_Orientation property values.
/// <see href="https://www.unicode.org/reports/tr50/#vo"/>
/// </summary>
/// <remarks>
/// These values are used when laying out text vertically. They describe the default
/// orientation of the character's code chart glyph and whether a vertical alternate
/// glyph should be used when the font supplies one.
/// </remarks>
public enum VerticalOrientationType
{
    /// <summary>
    /// Upright (U): displayed upright with the same orientation used in the code charts.
    /// </summary>
    Upright,

    /// <summary>
    /// Rotated (R): displayed sideways, rotated 90 degrees clockwise from the code charts.
    /// </summary>
    Rotate,

    /// <summary>
    /// Transformed upright (Tu): normally uses a vertical alternate glyph, falling back to upright.
    /// </summary>
    TransformUpright,

    /// <summary>
    /// Transformed rotated (Tr): normally uses a vertical alternate glyph, falling back to rotated.
    /// </summary>
    TransformRotate
}

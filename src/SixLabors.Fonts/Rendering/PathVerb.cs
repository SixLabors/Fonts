// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Path verb identifying the command type.
/// </summary>
internal enum PathVerb : byte
{
    /// <summary>
    /// Moves the current point without drawing.
    /// </summary>
    MoveTo = 0,

    /// <summary>
    /// Draws a straight line from the current point to the end point.
    /// </summary>
    LineTo = 1,

    /// <summary>
    /// Draws a quadratic Bézier from the current point to the end point using a single control point.
    /// </summary>
    QuadraticTo = 2,

    /// <summary>
    /// Draws a cubic Bézier from the current point to the end point using two control points.
    /// </summary>
    CubicTo = 3,

    /// <summary>
    /// Draws an elliptical arc from the current point to the end point.
    /// </summary>
    ArcTo = 4,

    /// <summary>
    /// Closes the current subpath.
    /// </summary>
    ClosePath = 5,
}

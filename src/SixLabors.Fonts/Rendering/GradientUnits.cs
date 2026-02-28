// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Coordinate system to interpret gradient geometry.
/// </summary>
public enum GradientUnits
{
    /// <summary>
    /// Coordinates are normalized to the painted geometry's bounds ([0, 1] in X and Y).
    /// The renderer will map these to the actual path bounds at paint time.
    /// </summary>
    ObjectBoundingBox = 0,

    /// <summary>
    /// Coordinates are absolute in the same space as the already-transformed geometry.
    /// Interpreters must pre-apply any gradient transforms before creating the paint.
    /// </summary>
    UserSpaceOnUse = 1,
}

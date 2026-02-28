// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a rectangular clipping region as a convex quadrilateral.
/// Allows for transformation by rotation, skew, or non-uniform scaling,
/// resulting in non-axis-aligned edges.
/// </summary>
public readonly struct ClipQuad
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipQuad"/> struct.
    /// </summary>
    /// <param name="topLeft">The top-left corner of the quadrilateral.</param>
    /// <param name="topRight">The top-right corner of the quadrilateral.</param>
    /// <param name="bottomRight">The bottom-right corner of the quadrilateral.</param>
    /// <param name="bottomLeft">The bottom-left corner of the quadrilateral.</param>
    public ClipQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
    {
        this.TopLeft = topLeft;
        this.TopRight = topRight;
        this.BottomRight = bottomRight;
        this.BottomLeft = bottomLeft;
    }

    /// <summary>
    /// Gets the top-left corner of the quadrilateral.
    /// </summary>
    public Vector2 TopLeft { get; }

    /// <summary>
    /// Gets the top-right corner of the quadrilateral.
    /// </summary>
    public Vector2 TopRight { get; }

    /// <summary>
    /// Gets the bottom-right corner of the quadrilateral.
    /// </summary>
    public Vector2 BottomRight { get; }

    /// <summary>
    /// Gets the bottom-left corner of the quadrilateral.
    /// </summary>
    public Vector2 BottomLeft { get; }

    /// <summary>
    /// Creates a <see cref="ClipQuad"/> from an axis-aligned <see cref="Bounds"/> and an optional transform.
    /// </summary>
    /// <param name="bounds">The bounds representing the untransformed rectangular area.</param>
    /// <param name="transform">An optional transform to apply. If omitted, no transform is applied.</param>
    /// <returns>A <see cref="ClipQuad"/> representing the transformed rectangle.</returns>
    internal static ClipQuad FromBounds(in Bounds bounds, in Matrix3x2 transform)
    {
        Vector2 tl = Vector2.Transform(bounds.Min, transform);
        Vector2 tr = Vector2.Transform(new Vector2(bounds.Max.X, bounds.Min.Y), transform);
        Vector2 br = Vector2.Transform(bounds.Max, transform);
        Vector2 bl = Vector2.Transform(new Vector2(bounds.Min.X, bounds.Max.Y), transform);
        return new ClipQuad(tl, tr, br, bl);
    }

    /// <summary>
    /// Determines whether the quadrilateral is axis-aligned within a small tolerance.
    /// </summary>
    /// <param name="tolerance">The tolerance for comparing parallel edges, typically a small epsilon.</param>
    /// <returns>
    /// <see langword="true"/> if opposite edges are parallel and of equal length; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsAxisAligned(float tolerance = 1E-4F)
    {
        Vector2 top = this.TopRight - this.TopLeft;
        Vector2 bottom = this.BottomRight - this.BottomLeft;
        Vector2 left = this.BottomLeft - this.TopLeft;
        Vector2 right = this.BottomRight - this.TopRight;

        bool horizontalParallel = MathF.Abs(Vector2.Dot(Vector2.Normalize(top), Vector2.Normalize(bottom)) - 1F) < tolerance;
        bool verticalParallel = MathF.Abs(Vector2.Dot(Vector2.Normalize(left), Vector2.Normalize(right)) - 1F) < tolerance;

        return horizontalParallel && verticalParallel;
    }
}

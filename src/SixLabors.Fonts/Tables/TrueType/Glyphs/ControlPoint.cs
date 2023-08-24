// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Represents a true type glyph control point.
/// </summary>
internal struct ControlPoint : IEquatable<ControlPoint>
{
    /// <summary>
    /// Gets or sets the position of the point.
    /// </summary>
    public Vector2 Point;

    /// <summary>
    /// Gets or sets a value indicating whether the point is on a curve.
    /// </summary>
    public bool OnCurve;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlPoint"/> struct.
    /// </summary>
    /// <param name="point">The position.</param>
    /// <param name="onCurve">Whether the point is on a curve.</param>
    public ControlPoint(Vector2 point, bool onCurve)
    {
        this.Point = point;
        this.OnCurve = onCurve;
    }

    public static bool operator ==(ControlPoint left, ControlPoint right)
        => left.Equals(right);

    public static bool operator !=(ControlPoint left, ControlPoint right)
        => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ControlPoint point && this.Equals(point);

    /// <inheritdoc/>
    public bool Equals(ControlPoint other)
        => this.Point.Equals(other.Point)
        && this.OnCurve == other.OnCurve;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.Point, this.OnCurve);

    /// <inheritdoc/>
    public override string ToString()
        => FormattableString.Invariant($"Point: {this.Point}, OnCurve: {this.OnCurve}");
}

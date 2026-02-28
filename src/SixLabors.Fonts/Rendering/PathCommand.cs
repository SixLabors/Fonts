// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A single path command with all coordinates already transformed into final render space (y-down).
/// For arc commands, radii and flags must be pre-adjusted by the interpreter.
/// </summary>
internal readonly struct PathCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathCommand"/> struct.
    /// </summary>
    /// <param name="verb">The command verb.</param>
    /// <param name="endPoint">The end point for the command.</param>
    /// <param name="controlPoint1">The first control point, if used by the verb.</param>
    /// <param name="controlPoint2">The second control point, if used by the verb.</param>
    /// <param name="radiusX">The x-radius for <see cref="PathVerb.ArcTo"/>.</param>
    /// <param name="radiusY">The y-radius for <see cref="PathVerb.ArcTo"/>.</param>
    /// <param name="rotationDegrees">The x-axis rotation in degrees for <see cref="PathVerb.ArcTo"/>.</param>
    /// <param name="largeArc">The large-arc flag for <see cref="PathVerb.ArcTo"/>.</param>
    /// <param name="sweep">The sweep flag for <see cref="PathVerb.ArcTo"/>.</param>
    public PathCommand(
        PathVerb verb,
        Vector2 endPoint,
        Vector2 controlPoint1,
        Vector2 controlPoint2,
        float radiusX,
        float radiusY,
        float rotationDegrees,
        bool largeArc,
        bool sweep)
    {
        this.Verb = verb;
        this.EndPoint = endPoint;
        this.ControlPoint1 = controlPoint1;
        this.ControlPoint2 = controlPoint2;
        this.RadiusX = radiusX;
        this.RadiusY = radiusY;
        this.RotationDegrees = rotationDegrees;
        this.LargeArc = largeArc;
        this.Sweep = sweep;
    }

    /// <summary>
    /// Gets the command verb.
    /// </summary>
    public PathVerb Verb { get; }

    /// <summary>
    /// Gets the end point for the command.
    /// For <see cref="PathVerb.MoveTo"/> and <see cref="PathVerb.LineTo"/> this is the target point.
    /// For curves and arcs it is the end point of the segment.
    /// </summary>
    public Vector2 EndPoint { get; }

    /// <summary>
    /// Gets the first control point (quadratic control or cubic control 1).
    /// Not used for <see cref="PathVerb.MoveTo"/>, <see cref="PathVerb.LineTo"/>, <see cref="PathVerb.ClosePath"/> or <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public Vector2 ControlPoint1 { get; }

    /// <summary>
    /// Gets the second control point (cubic control 2).
    /// Only used for <see cref="PathVerb.CubicTo"/>.
    /// </summary>
    public Vector2 ControlPoint2 { get; }

    /// <summary>
    /// Gets the x-radius for <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public float RadiusX { get; }

    /// <summary>
    /// Gets the y-radius for <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public float RadiusY { get; }

    /// <summary>
    /// Gets the rotation of the arc's x-axis in degrees for <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public float RotationDegrees { get; }

    /// <summary>
    /// Gets a value indicating whether the large-arc flag is set for <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public bool LargeArc { get; }

    /// <summary>
    /// Gets a value indicating whether the sweep flag is set for <see cref="PathVerb.ArcTo"/>.
    /// </summary>
    public bool Sweep { get; }

    /// <summary>
    /// Creates a <see cref="PathVerb.MoveTo"/> command.
    /// </summary>
    /// <param name="point">The destination point.</param>
    /// <returns>The command.</returns>
    public static PathCommand MoveTo(Vector2 point)
        => new(PathVerb.MoveTo, point, Vector2.Zero, Vector2.Zero, 0f, 0f, 0f, false, false);

    /// <summary>
    /// Creates a <see cref="PathVerb.LineTo"/> command.
    /// </summary>
    /// <param name="point">The destination point.</param>
    /// <returns>The command.</returns>
    public static PathCommand LineTo(Vector2 point)
        => new(PathVerb.LineTo, point, Vector2.Zero, Vector2.Zero, 0f, 0f, 0f, false, false);

    /// <summary>
    /// Creates a <see cref="PathVerb.QuadraticTo"/> command.
    /// </summary>
    /// <param name="control">The control point.</param>
    /// <param name="end">The end point.</param>
    /// <returns>The command.</returns>
    public static PathCommand QuadraticTo(Vector2 control, Vector2 end)
        => new(PathVerb.QuadraticTo, end, control, Vector2.Zero, 0f, 0f, 0f, false, false);

    /// <summary>
    /// Creates a <see cref="PathVerb.CubicTo"/> command.
    /// </summary>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The end point.</param>
    /// <returns>The command.</returns>
    public static PathCommand CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
        => new(PathVerb.CubicTo, end, control1, control2, 0f, 0f, 0f, false, false);

    /// <summary>
    /// Creates an <see cref="PathVerb.ArcTo"/> command.
    /// </summary>
    /// <param name="radiusX">The x-radius of the ellipse.</param>
    /// <param name="radiusY">The y-radius of the ellipse.</param>
    /// <param name="rotationDegrees">The rotation of the ellipse's x-axis in degrees.</param>
    /// <param name="largeArc">The large-arc flag.</param>
    /// <param name="sweep">The sweep flag.</param>
    /// <param name="end">The end point.</param>
    /// <returns>The command.</returns>
    public static PathCommand ArcTo(float radiusX, float radiusY, float rotationDegrees, bool largeArc, bool sweep, Vector2 end)
        => new(PathVerb.ArcTo, end, Vector2.Zero, Vector2.Zero, radiusX, radiusY, rotationDegrees, largeArc, sweep);

    /// <summary>
    /// Creates a <see cref="PathVerb.ClosePath"/> command.
    /// </summary>
    /// <returns>The command.</returns>
    public static PathCommand Close()
        => new(PathVerb.ClosePath, Vector2.Zero, Vector2.Zero, Vector2.Zero, 0f, 0f, 0f, false, false);
}

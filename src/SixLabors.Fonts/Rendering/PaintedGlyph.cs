// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A glyph fully decomposed into painted layers ready for rendering.
/// </summary>
internal readonly struct PaintedGlyph
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedGlyph"/> struct.
    /// </summary>
    /// <param name="layers">The painted layers.</param>
    public PaintedGlyph(List<PaintedLayer> layers)
    {
        this.Layers = layers;
        this.Bounds = CalculateBounds(layers);
    }

    /// <summary>
    /// Gets the layers for this glyph.
    /// </summary>
    public IReadOnlyList<PaintedLayer> Layers { get; }

    /// <summary>
    /// Gets the cached bounds of all painted geometry after applying each layer transform.
    /// </summary>
    public Bounds Bounds { get; }

    /// <summary>
    /// Gets a value indicating whether this glyph has no layers.
    /// </summary>
    public bool IsEmpty => this.Layers.Count == 0;

    /// <summary>
    /// Gets an empty glyph instance.
    /// </summary>
    public static PaintedGlyph Empty => new([]);

    private static Bounds CalculateBounds(List<PaintedLayer> layers)
    {
        Vector2 min = new(float.MaxValue);
        Vector2 max = new(float.MinValue);
        bool hasPoint = false;

        for (int i = 0; i < layers.Count; i++)
        {
            PaintedLayer layer = layers[i];
            IReadOnlyList<PathCommand> path = layer.Path;
            Vector2 current = default;

            for (int j = 0; j < path.Count; j++)
            {
                PathCommand command = path[j];
                switch (command.Verb)
                {
                    case PathVerb.MoveTo:
                    case PathVerb.LineTo:
                        Include(command.EndPoint, layer.Transform, ref min, ref max, ref hasPoint);
                        break;
                    case PathVerb.QuadraticTo:
                        Include(command.ControlPoint1, layer.Transform, ref min, ref max, ref hasPoint);
                        Include(command.EndPoint, layer.Transform, ref min, ref max, ref hasPoint);
                        break;
                    case PathVerb.CubicTo:
                        Include(command.ControlPoint1, layer.Transform, ref min, ref max, ref hasPoint);
                        Include(command.ControlPoint2, layer.Transform, ref min, ref max, ref hasPoint);
                        Include(command.EndPoint, layer.Transform, ref min, ref max, ref hasPoint);
                        break;
                    case PathVerb.ArcTo:
                        IncludeArc(current, command, layer.Transform, ref min, ref max, ref hasPoint);
                        break;
                }

                if (command.Verb != PathVerb.ClosePath)
                {
                    current = command.EndPoint;
                }
            }
        }

        return hasPoint ? new Bounds(min, max) : Bounds.Empty;
    }

    private static void Include(
        Vector2 point,
        Matrix3x2 transform,
        ref Vector2 min,
        ref Vector2 max,
        ref bool hasPoint)
    {
        point = Vector2.Transform(point, transform);
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
        hasPoint = true;
    }

    private static void IncludeArc(
        Vector2 start,
        PathCommand command,
        Matrix3x2 transform,
        ref Vector2 min,
        ref Vector2 max,
        ref bool hasPoint)
    {
        Include(start, transform, ref min, ref max, ref hasPoint);
        Include(command.EndPoint, transform, ref min, ref max, ref hasPoint);

        float radiusX = MathF.Abs(command.RadiusX);
        float radiusY = MathF.Abs(command.RadiusY);
        float angle = command.RotationDegrees * (MathF.PI / 180F);
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        float extentX = MathF.Sqrt((radiusX * radiusX * cos * cos) + (radiusY * radiusY * sin * sin));
        float extentY = MathF.Sqrt((radiusX * radiusX * sin * sin) + (radiusY * radiusY * cos * cos));

        // The endpoint lies on the ellipse, so twice its axis extents around that point enclose
        // the complete ellipse and therefore the selected arc without resolving its center.
        Vector2 extent = new(extentX * 2F, extentY * 2F);
        Bounds arcBounds = new(command.EndPoint - extent, command.EndPoint + extent);
        arcBounds = Bounds.Transform(in arcBounds, transform);
        min = Vector2.Min(min, arcBounds.Min);
        max = Vector2.Max(max, arcBounds.Max);
    }
}

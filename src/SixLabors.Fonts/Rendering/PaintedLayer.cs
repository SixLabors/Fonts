// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A single painted layer comprising a paint, a fill rule, and a path stream.
/// All coordinates must be pre-transformed in Fonts prior to construction.
/// </summary>
internal readonly struct PaintedLayer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedLayer"/> struct.
    /// </summary>
    /// <param name="paint">The paint definition.</param>
    /// <param name="fillRule">The fill rule.</param>
    /// <param name="transform">The transform applied to all path coordinates.</param>
    /// <param name="path">The path command stream for this layer.</param>
    public PaintedLayer(Paint? paint, FillRule fillRule, Matrix3x2 transform, IReadOnlyList<PathCommand> path)
    {
        this.Paint = paint;
        this.FillRule = fillRule;
        this.Transform = transform;
        this.Path = path;
    }

    /// <summary>
    /// Gets the paint definition for this layer.
    /// </summary>
    public Paint? Paint { get; }

    /// <summary>
    /// Gets the fill rule for rasterization.
    /// </summary>
    public FillRule FillRule { get; }

    /// <summary>
    /// Gets the transform applied to all path coordinates.
    /// </summary>
    public Matrix3x2 Transform { get; }

    /// <summary>
    /// Gets the path stream for this layer.
    /// </summary>
    public IReadOnlyList<PathCommand> Path { get; }
}

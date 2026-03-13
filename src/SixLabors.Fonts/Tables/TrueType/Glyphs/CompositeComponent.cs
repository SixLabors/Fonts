// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Stores the original component offset and point count for a single component
/// within a composite glyph. Used during gvar variation processing to apply
/// per-component offset deltas to the assembled outline.
/// </summary>
internal readonly struct CompositeComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeComponent"/> struct.
    /// </summary>
    /// <param name="dx">The original X offset of this component.</param>
    /// <param name="dy">The original Y offset of this component.</param>
    /// <param name="pointCount">The number of control points contributed by this component.</param>
    public CompositeComponent(float dx, float dy, int pointCount)
    {
        this.Dx = dx;
        this.Dy = dy;
        this.PointCount = pointCount;
    }

    /// <summary>
    /// Gets the original X offset of this component (before variation).
    /// </summary>
    public float Dx { get; }

    /// <summary>
    /// Gets the original Y offset of this component (before variation).
    /// </summary>
    public float Dy { get; }

    /// <summary>
    /// Gets the number of control points contributed by this component
    /// to the assembled composite glyph.
    /// </summary>
    public int PointCount { get; }
}

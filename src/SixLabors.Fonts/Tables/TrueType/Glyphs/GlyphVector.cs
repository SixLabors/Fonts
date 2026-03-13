// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Hinting;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Represents the raw glyph outlines for a given glyph comprised of a collection of glyph table entries.
/// The type is mutable by design to reduce copying during transformation.
/// </summary>
internal struct GlyphVector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphVector"/> struct.
    /// </summary>
    /// <param name="controlPoints">The control points defining the glyph outline.</param>
    /// <param name="endPoints">The indices of the last point of each contour.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="instructions">The TrueType hinting instructions.</param>
    /// <param name="isComposite">Whether this glyph is a composite glyph.</param>
    internal GlyphVector(
        IList<ControlPoint> controlPoints,
        IReadOnlyList<ushort> endPoints,
        Bounds bounds,
        ReadOnlyMemory<byte> instructions,
        bool isComposite)
    {
        this.ControlPoints = controlPoints;
        this.EndPoints = endPoints;
        this.Bounds = bounds;
        this.Instructions = instructions;
        this.IsComposite = isComposite;
    }

    /// <summary>
    /// Gets or sets the control points defining the glyph outline.
    /// </summary>
    public IList<ControlPoint> ControlPoints { get; set; }

    /// <summary>
    /// Gets or sets the indices of the last point of each contour.
    /// </summary>
    public IReadOnlyList<ushort> EndPoints { get; set; }

    /// <summary>
    /// Gets or sets the TrueType hinting instructions for this glyph.
    /// </summary>
    public ReadOnlyMemory<byte> Instructions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a composite glyph.
    /// </summary>
    public bool IsComposite { get; set; }

    /// <summary>
    /// Gets or sets the glyph bounding box.
    /// </summary>
    public Bounds Bounds { get; set; }

    /// <summary>
    /// Gets or sets the composite component information used for gvar variation processing.
    /// Each entry stores the original component offset and the number of control points
    /// contributed by that component, so that TransformPoints can apply per-component
    /// offset deltas to the assembled outline.
    /// Null for simple (non-composite) glyphs.
    /// </summary>
    public CompositeComponent[]? CompositeComponents { get; set; }

    /// <summary>
    /// Creates an empty glyph vector with no control points or contours.
    /// </summary>
    /// <param name="bounds">The optional bounds to assign to the empty glyph.</param>
    /// <returns>An empty <see cref="GlyphVector"/>.</returns>
    public static GlyphVector Empty(Bounds bounds = default)
        => new(Array.Empty<ControlPoint>(), Array.Empty<ushort>(), bounds, Array.Empty<byte>(), false);

    /// <summary>
    /// Transforms a glyph vector by a specified 3x2 matrix.
    /// </summary>
    /// <param name="src">The glyph vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    public static void TransformInPlace(ref GlyphVector src, Matrix3x2 matrix)
    {
        IList<ControlPoint> controlPoints = src.ControlPoints;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            point.Point = Vector2.Transform(point.Point, matrix);
            controlPoints[i] = point;
        }

        src.Bounds = Bounds.Transform(src.Bounds, matrix);
    }

    /// <summary>
    /// Applies True Type hinting to the specified glyph vector.
    /// </summary>
    /// <param name="hintingMode">The hinting mode.</param>
    /// <param name="glyph">The glyph vector to hint.</param>
    /// <param name="interpreter">The True Type interpreter.</param>
    /// <param name="pp1">The first phantom point.</param>
    /// <param name="pp2">The second phantom point.</param>
    /// <param name="pp3">The third phantom point.</param>
    /// <param name="pp4">The fourth phantom point.</param>
    public static void Hint(
        HintingMode hintingMode,
        ref GlyphVector glyph,
        TrueTypeInterpreter interpreter,
        Vector2 pp1,
        Vector2 pp2,
        Vector2 pp3,
        Vector2 pp4)
    {
        if (hintingMode == HintingMode.None)
        {
            return;
        }

        ControlPoint[] controlPoints = new ControlPoint[glyph.ControlPoints.Count + 4];
        controlPoints[^4].Point = pp1;
        controlPoints[^3].Point = pp2;
        controlPoints[^2].Point = pp3;
        controlPoints[^1].Point = pp4;

        for (int i = 0; i < glyph.ControlPoints.Count; i++)
        {
            controlPoints[i] = glyph.ControlPoints[i];
        }

        if (interpreter.TryHintGlyph(controlPoints, glyph.EndPoints, glyph.Instructions, glyph.IsComposite))
        {
            for (int i = 0; i < glyph.ControlPoints.Count; i++)
            {
                glyph.ControlPoints[i] = controlPoints[i];
            }
        }
    }

    /// <summary>
    /// Creates a new glyph vector that is a deep copy of the specified instance.
    /// </summary>
    /// <param name="src">The source glyph vector to copy.</param>
    /// <returns>The cloned <see cref="GlyphVector"/>.</returns>
    public static GlyphVector DeepClone(GlyphVector src)
    {
        List<ControlPoint> controlPoints = [.. src.ControlPoints];
        List<ushort> endPoints = [.. src.EndPoints];

        return new(controlPoints, endPoints, src.Bounds, src.Instructions, src.IsComposite)
        {
            CompositeComponents = src.CompositeComponents is not null
                ? [.. src.CompositeComponents]
                : null
        };
    }

    /// <summary>
    /// Returns a value indicating whether the current instance is empty.
    /// </summary>
    /// <returns>The <see cref="bool"/> indicating the result.</returns>
    public readonly bool HasValue() => this.ControlPoints?.Count > 0;
}

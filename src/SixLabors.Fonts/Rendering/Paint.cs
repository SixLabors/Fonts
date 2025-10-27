// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Base type for normalized paint definitions that can be used by any renderer.
/// Glyph sources must pre-apply all relevant transforms and resolve any palette
/// or format-specific constructs before creating a paint instance.
/// </summary>
public abstract class Paint
{
    /// <summary>
    /// Gets the per-layer opacity multiplier in the range [0, 1].
    /// Renderers should multiply this value into the alpha channel of the final brush.
    /// </summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>
    /// Gets or sets an optional transform to apply to the paint.
    /// Used to pre-apply gradientTransform in SVG or equivalent.
    /// </summary>
    internal Matrix3x2 Transform { get; set; }

    /// <summary>
    /// Gets the composite mode to use when applying this paint over existing content.
    /// </summary>
    public CompositeMode CompositeMode { get; init; } = CompositeMode.SrcOver;
}

/// <summary>
/// Solid color paint (direct RGBA). Interpreters must resolve palettes to RGBA.
/// Compatible with OT-SVG solid fills and COLR v1 PaintSolid after CPAL resolution.
/// </summary>
public sealed class SolidPaint : Paint
{
    /// <summary>
    /// Gets the color to use for the fill. Alpha is respected and further multiplied by <see cref="Paint.Opacity"/>.
    /// </summary>
    public GlyphColor Color { get; init; }
}

/// <summary>
/// Linear gradient paint.
/// </summary>
public sealed class LinearGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="P0"/> and <see cref="P1"/>.
    /// </summary>
    internal GradientUnits Units { get; init; }

    /// <summary>
    /// Gets the gradient start point. Normalized if <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>.
    /// </summary>
    public Vector2 P0 { get; init; }

    /// <summary>
    /// Gets the gradient end point. Normalized if <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>.
    /// </summary>
    public Vector2 P1 { get; init; }

    /// <summary>
    /// Gets the rotation point for the gradient. Normalized if <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>.
    /// </summary>
    public Vector2? P2 { get; init; }

    /// <summary>
    /// Gets the spread method applied when sampling outside the [0, 1] range.
    /// </summary>
    public SpreadMethod Spread { get; init; } = SpreadMethod.Pad;

    /// <summary>
    /// Gets the ordered gradient stops (ascending by <see cref="GradientStop.Offset"/>).
    /// </summary>
    public GradientStop[] Stops { get; init; } = [];
}

/// <summary>
/// Represents a radial gradient paint defined by two circles.
/// The first circle is centered at <see cref="Center0"/> with radius <see cref="Radius0"/>.
/// The second circle is centered at <see cref="Center1"/> with radius <see cref="Radius1"/>.
/// The color transition is computed between these two circles.
/// Compatible with two-circle radial gradients used by HTML Canvas and OpenType COLR v1.
/// </summary>
public sealed class RadialGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="Center0"/>, <see cref="Center1"/>,
    /// <see cref="Radius0"/>, and <see cref="Radius1"/>.
    /// </summary>
    internal GradientUnits Units { get; init; }

    /// <summary>
    /// Gets the center of the starting circle of the gradient.
    /// </summary>
    public Vector2 Center0 { get; init; }

    /// <summary>
    /// Gets the radius of the starting circle of the gradient.
    /// If <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>,
    /// the radius is normalized to the bounds.
    /// </summary>
    public float Radius0 { get; init; }

    /// <summary>
    /// Gets the center of the ending circle of the gradient.
    /// </summary>
    public Vector2 Center1 { get; init; }

    /// <summary>
    /// Gets the radius of the ending circle of the gradient.
    /// If <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>,
    /// the radius is normalized to the bounds.
    /// </summary>
    public float Radius1 { get; init; }

    /// <summary>
    /// Gets the spread method applied when sampling outside the [0, 1] range.
    /// </summary>
    public SpreadMethod Spread { get; init; } = SpreadMethod.Pad;

    /// <summary>
    /// Gets the ordered gradient stops, ascending by <see cref="GradientStop.Offset"/>.
    /// </summary>
    public GradientStop[] Stops { get; init; } = [];
}

/// <summary>
/// Sweep (conic) gradient paint. Angles are expressed in degrees in the renderer's y-down space.
/// </summary>
public sealed class SweepGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="Center"/>. Sweep gradients are typically user-space.
    /// </summary>
    internal GradientUnits Units { get; init; } = GradientUnits.UserSpaceOnUse;

    /// <summary>
    /// Gets the center of the sweep gradient.
    /// </summary>
    public Vector2 Center { get; init; }

    /// <summary>
    /// Gets the start angle in degrees.
    /// </summary>
    public float StartAngle { get; init; }

    /// <summary>
    /// Gets the end angle in degrees.
    /// </summary>
    public float EndAngle { get; init; }

    /// <summary>
    /// Gets the spread method applied when sampling outside the [0, 1] range.
    /// </summary>
    public SpreadMethod Spread { get; init; } = SpreadMethod.Pad;

    /// <summary>
    /// Gets the ordered gradient stops (ascending by <see cref="GradientStop.Offset"/>).
    /// </summary>
    public GradientStop[] Stops { get; init; } = [];
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Base type for normalized paint definitions that can be used by any renderer.
/// Interpreters must pre-apply all relevant transforms and resolve any palette
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
/// Linear gradient paint. Compatible with OT-SVG &lt;linearGradient&gt; and COLR v1 PaintLinearGradient
/// once normalized. Interpreters must:
/// <list type="bullet">
/// <item><description>Resolve all palette colors to RGBA in <see cref="Stops"/>.</description></item>
/// <item><description>Pre-apply <c>gradientTransform</c> and element/group transforms.</description></item>
/// <item><description>Provide <see cref="P0"/> and <see cref="P1"/> in the coordinate system defined by <see cref="Units"/>.</description></item>
/// </list>
/// </summary>
public sealed class LinearGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="P0"/> and <see cref="P1"/>.
    /// </summary>
    public GradientUnits Units { get; init; }

    /// <summary>
    /// Gets the gradient start point. Normalized if <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>.
    /// </summary>
    public Vector2 P0 { get; init; }

    /// <summary>
    /// Gets the gradient end point. Normalized if <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>.
    /// </summary>
    public Vector2 P1 { get; init; }

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
/// Radial gradient paint. Compatible with OT-SVG &lt;radialGradient&gt; and COLR v1 PaintRadialGradient
/// once normalized. Interpreters should represent elliptical cases by pre-applying transforms so the
/// renderer receives final coordinates in either user space or normalized bbox space.
/// </summary>
public sealed class RadialGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="Center"/>, <see cref="Focal"/>, and <see cref="Radius"/>.
    /// </summary>
    public GradientUnits Units { get; init; }

    /// <summary>
    /// Gets the center of the gradient.
    /// </summary>
    public Vector2 Center { get; init; }

    /// <summary>
    /// Gets the gradient radius. If <see cref="Units"/> is <see cref="GradientUnits.ObjectBoundingBox"/>,
    /// the radius is normalized to the bounds.
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// Gets the optional focal point. If <see langword="null"/>, the focal equals <see cref="Center"/>.
    /// </summary>
    public Vector2? Focal { get; init; }

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
/// Sweep (conic) gradient paint. There is no OT-SVG sweep gradient in 1.1, but this
/// form is useful to normalize COLR v1 PaintSweepGradient for a format-agnostic renderer.
/// Angles are expressed in degrees in the renderer's y-down space.
/// </summary>
public sealed class SweepGradientPaint : Paint
{
    /// <summary>
    /// Gets the coordinate system for <see cref="Center"/>. Sweep gradients are typically user-space.
    /// </summary>
    public GradientUnits Units { get; init; } = GradientUnits.UserSpaceOnUse;

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

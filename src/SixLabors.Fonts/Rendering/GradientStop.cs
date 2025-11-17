// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Defines a color stop for gradient paints.
/// Offsets must be clamped to the range [0, 1] by the interpreter.
/// Colors are direct RGBA and must not reference palettes.
/// </summary>
public readonly struct GradientStop
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> struct.
    /// </summary>
    /// <param name="offset">The stop position in the range [0, 1].</param>
    /// <param name="color">The color at the stop.</param>
    public GradientStop(float offset, GlyphColor color)
    {
        this.Offset = offset;
        this.Color = color;
    }

    /// <summary>
    /// Gets the stop position in the range [0, 1].
    /// </summary>
    public float Offset { get; }

    /// <summary>
    /// Gets the color at the stop (direct RGBA).
    /// </summary>
    public GlyphColor Color { get; }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Used to apply a transform against any glyphs rendered by the engine.
/// </summary>
internal struct TransformingGlyphRenderer : IGlyphRenderer
{
    private static readonly Vector2 YInverter = new(1, -1);
    private readonly IGlyphRenderer renderer;
    private Vector2 origin;
    private Vector2 scale;
    private Vector2 offset;
    private Matrix3x2 transform;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformingGlyphRenderer"/> struct.
    /// </summary>
    /// <param name="renderer">The underlying glyph renderer to delegate to.</param>
    /// <param name="origin">The origin point for rendering.</param>
    /// <param name="scale">The scale factor to apply.</param>
    /// <param name="offset">The offset to apply before transformation.</param>
    /// <param name="transform">The transformation matrix to apply.</param>
    public TransformingGlyphRenderer(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
    {
        this.renderer = renderer;
        this.origin = origin;
        this.scale = scale;
        this.offset = offset;
        this.transform = transform;
        this.IsOpen = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a figure is currently open.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <inheritdoc/>
    public void BeginFigure()
    {
        this.IsOpen = true;
        this.renderer.BeginFigure();
    }

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        this.IsOpen = false;
        return this.renderer.BeginGlyph(in bounds, in parameters);
    }

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds)
    {
        this.IsOpen = false;
        this.renderer.BeginText(in bounds);
    }

    /// <inheritdoc/>
    public void EndFigure()
    {
        this.IsOpen = false;
        this.renderer.EndFigure();
    }

    /// <inheritdoc/>
    public void EndGlyph()
    {
        this.IsOpen = false;
        this.renderer.EndGlyph();
    }

    /// <inheritdoc/>
    public void EndText()
    {
        this.IsOpen = false;
        this.renderer.EndText();
    }

    /// <inheritdoc/>
    public void LineTo(Vector2 point)
    {
        this.IsOpen = true;
        this.renderer.LineTo(this.Transform(point));
    }

    /// <inheritdoc/>
    public void MoveTo(Vector2 point)
    {
        if (this.IsOpen)
        {
            this.EndFigure();
        }

        this.BeginFigure();
        this.renderer.MoveTo(this.Transform(point));
        this.IsOpen = true;
    }

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotationDegrees, bool largeArc, bool sweep, Vector2 point)
    {
        this.IsOpen = true;
        this.renderer.ArcTo(radiusX * this.scale.X, radiusY * this.scale.Y, rotationDegrees, largeArc, sweep, this.Transform(point));
    }

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        this.IsOpen = true;
        this.renderer.CubicBezierTo(this.Transform(secondControlPoint), this.Transform(thirdControlPoint), this.Transform(point));
    }

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        this.IsOpen = true;
        this.renderer.QuadraticBezierTo(this.Transform(secondControlPoint), this.Transform(point));
    }

    /// <inheritdoc/>
    public readonly TextDecorations EnabledDecorations()
        => this.renderer.EnabledDecorations();

    /// <inheritdoc/>
    public readonly void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        => this.renderer.SetDecoration(textDecorations, this.Transform(start), this.Transform(end), thickness);

    /// <summary>
    /// Applies the scale, offset, transform matrix, and Y-axis inversion to the given point.
    /// </summary>
    /// <param name="point">The point to transform.</param>
    /// <returns>The transformed point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector2 Transform(Vector2 point)
        => (Vector2.Transform((point * this.scale) + this.offset, this.transform) * YInverter) + this.origin;

    /// <inheritdoc/>
    public readonly void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds)
        => this.renderer.BeginLayer(paint, fillRule, clipBounds);

    /// <inheritdoc/>
    public readonly void EndLayer() => this.renderer.EndLayer();
}

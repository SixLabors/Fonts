// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Adapts an existing <see cref="IGlyphRenderer"/> to the paint-aware <see cref="ILayeredGlyphRenderer"/> capability.
/// </summary>
internal sealed class PathOnlyPaintAdapter : ILayeredGlyphRenderer
{
    private readonly IGlyphRenderer inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathOnlyPaintAdapter"/> class.
    /// </summary>
    /// <param name="inner">The inner renderer.</param>
    public PathOnlyPaintAdapter(IGlyphRenderer inner) => this.inner = inner;

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule)
    {
        // Nothing to do for the adapter.
    }

    /// <inheritdoc/>
    public void EndLayer()
    {
        // Nothing to do for the adapter.
    }

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        => this.inner.BeginGlyph(in bounds, in parameters);

    /// <inheritdoc/>
    public void EndGlyph() => this.inner.EndGlyph();

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds) => this.inner.BeginText(in bounds);

    /// <inheritdoc/>
    public void EndText() => this.inner.EndText();

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations() => this.inner.EnabledDecorations();

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        => this.inner.SetDecoration(textDecorations, start, end, thickness);

    /// <inheritdoc/>
    public void BeginFigure() => this.inner.BeginFigure();

    /// <inheritdoc/>
    public void MoveTo(Vector2 point) => this.inner.MoveTo(point);

    /// <inheritdoc/>
    public void LineTo(Vector2 point) => this.inner.LineTo(point);

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        => this.inner.QuadraticBezierTo(secondControlPoint, point);

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        => this.inner.CubicBezierTo(secondControlPoint, thirdControlPoint, point);

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        => this.inner.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, point);

    /// <inheritdoc/>
    public void EndFigure() => this.inner.EndFigure();
}

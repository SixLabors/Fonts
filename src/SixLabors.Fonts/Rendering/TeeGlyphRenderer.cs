// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A glyph renderer that forwards every callback to a primary renderer while mirroring outline
/// geometry into up to two <see cref="GlyphIntersectionCollector"/> instances. This lets
/// decoration skip-ink measure a glyph's ink from the same emission the renderer receives,
/// including hinting, without decoding the outline a second time. Instances are reusable:
/// <see cref="Reset"/> rearms the tee for a new glyph without allocating, and
/// <see cref="Clear"/> drops the target references once the glyph completes so a pooled tee
/// does not root the renderer.
/// </summary>
internal sealed class TeeGlyphRenderer : IGlyphRenderer
{
    /// <summary>
    /// The renderer that receives every callback unchanged. Never null between
    /// <see cref="Reset"/> and <see cref="Clear"/>, the only window in which callbacks occur.
    /// </summary>
    private IGlyphRenderer primary = null!;

    /// <summary>
    /// The collector observing the underline band, if any.
    /// </summary>
    private GlyphIntersectionCollector? underlineInk;

    /// <summary>
    /// The collector observing the overline band, if any.
    /// </summary>
    private GlyphIntersectionCollector? overlineInk;

    /// <summary>
    /// Rearms the tee to forward to the given renderer and mirror geometry into the given
    /// collectors.
    /// </summary>
    /// <param name="primary">The renderer that receives every callback unchanged.</param>
    /// <param name="underlineInk">The collector observing the underline band, if any.</param>
    /// <param name="overlineInk">The collector observing the overline band, if any.</param>
    public void Reset(
        IGlyphRenderer primary,
        GlyphIntersectionCollector? underlineInk,
        GlyphIntersectionCollector? overlineInk)
    {
        this.primary = primary;
        this.underlineInk = underlineInk;
        this.overlineInk = overlineInk;
    }

    /// <summary>
    /// Drops the forwarding targets so a pooled tee does not keep the renderer or collectors
    /// reachable between glyphs.
    /// </summary>
    public void Clear()
    {
        this.primary = null!;
        this.underlineInk = null;
        this.overlineInk = null;
    }

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds) => this.primary.BeginText(in bounds);

    /// <inheritdoc/>
    public void EndText() => this.primary.EndText();

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        => this.primary.BeginGlyph(in bounds, in parameters);

    /// <inheritdoc/>
    public void EndGlyph() => this.primary.EndGlyph();

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds)
        => this.primary.BeginLayer(paint, fillRule, clipBounds);

    /// <inheritdoc/>
    public void EndLayer() => this.primary.EndLayer();

    /// <inheritdoc/>
    public void BeginFigure()
    {
        this.primary.BeginFigure();
        this.underlineInk?.BeginFigure();
        this.overlineInk?.BeginFigure();
    }

    /// <inheritdoc/>
    public void EndFigure()
    {
        this.primary.EndFigure();
        this.underlineInk?.EndFigure();
        this.overlineInk?.EndFigure();
    }

    /// <inheritdoc/>
    public void MoveTo(Vector2 point)
    {
        this.primary.MoveTo(point);
        this.underlineInk?.MoveTo(point);
        this.overlineInk?.MoveTo(point);
    }

    /// <inheritdoc/>
    public void LineTo(Vector2 point)
    {
        this.primary.LineTo(point);
        this.underlineInk?.LineTo(point);
        this.overlineInk?.LineTo(point);
    }

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        this.primary.QuadraticBezierTo(secondControlPoint, point);
        this.underlineInk?.QuadraticBezierTo(secondControlPoint, point);
        this.overlineInk?.QuadraticBezierTo(secondControlPoint, point);
    }

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        this.primary.CubicBezierTo(secondControlPoint, thirdControlPoint, point);
        this.underlineInk?.CubicBezierTo(secondControlPoint, thirdControlPoint, point);
        this.overlineInk?.CubicBezierTo(secondControlPoint, thirdControlPoint, point);
    }

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
    {
        this.primary.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, point);
        this.underlineInk?.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, point);
        this.overlineInk?.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, point);
    }

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations() => this.primary.EnabledDecorations();

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
        => this.primary.SetDecoration(textDecorations, start, end, thickness, intersections);
}

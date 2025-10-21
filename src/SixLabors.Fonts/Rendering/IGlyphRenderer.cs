// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A surface that can have a glyph rendered to it as a series of actions.
/// </summary>
public interface IGlyphRenderer
{
    /// <summary>
    /// Called before any glyphs have been rendered.
    /// </summary>
    /// <param name="bounds">The rectangle within the text will be rendered.</param>
    public void BeginText(in FontRectangle bounds);

    /// <summary>
    /// Called once all glyphs have completed rendering.
    /// </summary>
    public void EndText();

    /// <summary>
    /// Begins the glyph.
    /// </summary>
    /// <param name="bounds">The bounds the glyph will be rendered at and at what size.</param>
    /// <param name="parameters">
    /// The set of parameters that uniquely represents a version of a glyph at particular font size, font family, font style and DPI.
    /// </param>
    /// <returns>
    /// Returns <see langword="true"/> if the glyph should be rendered otherwise it returns <see langword="false"/>.
    /// </returns>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters);

    /// <summary>
    /// Ends the glyph.
    /// </summary>
    public void EndGlyph();

    /// <summary>
    /// Begins a new painted layer with the specified paint and fill rule.
    /// All geometry commands issued after this call belong to the layer until <see cref="EndLayer"/> is called.
    /// </summary>
    /// <param name="paint">The paint definition.</param>
    /// <param name="fillRule">The fill rule to use when rasterizing this layer.</param>
    /// <param name="clipBounds">The optional clip bounds to apply when rasterizing this layer.</param>
    public void BeginLayer(Paint? paint, FillRule fillRule, in FontRectangle? clipBounds);

    /// <summary>
    /// Ends the current painted layer.
    /// </summary>
    public void EndLayer();

    /// <summary>
    /// Begins the figure.
    /// </summary>
    public void BeginFigure();

    /// <summary>
    /// Sets a new start point to draw lines from.
    /// </summary>
    /// <param name="point">The point.</param>
    public void MoveTo(Vector2 point);

    /// <summary>
    /// Draw a straight line connecting the previous point to <paramref name="point"/>.
    /// </summary>
    /// <param name="point">The point.</param>
    public void LineTo(Vector2 point);

    /// <summary>
    /// Draw a quadratic bezier curve connecting the previous point to <paramref name="point"/>.
    /// </summary>
    /// <param name="secondControlPoint">The second control point.</param>
    /// <param name="point">The point.</param>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point);

    /// <summary>
    /// Draw a cubic bezier curve connecting the previous point to <paramref name="point"/>.
    /// </summary>
    /// <param name="secondControlPoint">The second control point.</param>
    /// <param name="thirdControlPoint">The third control point.</param>
    /// <param name="point">The point.</param>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point);

    /// <summary>
    /// <para>
    /// Adds an elliptical arc to the current figure. The arc curves from the last point to <paramref name="point"/>,
    /// choosing one of four possible routes: clockwise or counterclockwise, and smaller or larger.
    /// </para>
    /// <para>
    /// The arc sweep is always less than 360 degrees. The method appends a line
    /// to the last point if either radii are zero, or if last point is equal to <paramref name="point"/>.
    /// In addition the method scales the radii to fit last point and <paramref name="point"/> if both
    /// are greater than zero but too small to describe an arc.
    /// </para>
    /// </summary>
    /// <param name="radiusX">The x-radius of the ellipsis.</param>
    /// <param name="radiusY">The y-radius of the ellipsis.</param>
    /// <param name="rotation">The rotation along the X-axis; measured in degrees clockwise.</param>
    /// <param name="largeArc">
    /// The large arc flag, and is <see langword="false"/> if an arc spanning less than or equal to 180 degrees
    /// is chosen, or <see langword="true"/> if an arc spanning greater than 180 degrees is chosen.
    /// </param>
    /// <param name="sweep">
    /// The sweep flag, and is <see langword="false"/> if the line joining center to arc sweeps through decreasing
    /// angles, or <see langword="true"/> if it sweeps through increasing angles.
    /// </param>
    /// <param name="point">The end point of the arc.</param>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point);

    /// <summary>
    /// Ends the figure.
    /// </summary>
    public void EndFigure();

    /// <summary>
    /// Provides a callback to enable custom logic to request decoration details.
    /// A custom <see cref="TextRun"/> might use alternative triggers to determine what decorations it needs access to.
    /// </summary>
    /// <returns>The text decorations the render wants render info for.</returns>
    public TextDecorations EnabledDecorations();

    /// <summary>
    /// Sets the details of a text decoration to be rendered.
    /// This only gets called if the decoration type was requested via <see cref="EnabledDecorations"/>
    /// and after the glyph has been rendered via <see cref="BeginGlyph"/> and <see cref="EndGlyph"/>.
    /// </summary>
    /// <param name="textDecorations">The type of decoration these details correspond to.</param>
    /// <param name="start">The start position from where to draw the decorations from.</param>
    /// <param name="end">The end position from where to draw the decorations to.</param>
    /// <param name="thickness">The thickness to draw the decoration.</param>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness);
}

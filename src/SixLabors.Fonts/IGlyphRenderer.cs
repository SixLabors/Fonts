// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Primitives;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that can have a glyph renered to it as a series of actions.
    /// </summary>
    public interface IGlyphRenderer
    {
        /// <summary>
        /// Begins the figure.
        /// </summary>
        void BeginFigure();

        /// <summary>
        /// Sets a new start point to draw lines from
        /// </summary>
        /// <param name="point">The point.</param>
        void MoveTo(PointF point);

        /// <summary>
        /// Draw a quadratic bezier curve connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void QuadraticBezierTo(PointF secondControlPoint, PointF point);

        /// <summary>
        /// Draw a Cubics bezier curve connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="thirdControlPoint">The third control point.</param>
        /// <param name="point">The point.</param>
        void CubicBezierTo(PointF secondControlPoint, PointF thirdControlPoint, PointF point);

        /// <summary>
        /// Draw a straight line connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        void LineTo(PointF point);

        /// <summary>
        /// Ends the figure.
        /// </summary>
        void EndFigure();

        /// <summary>
        /// Ends the glyph.
        /// </summary>
        void EndGlyph();

        /// <summary>
        /// Begins the glyph.
        /// </summary>
        /// <param name="bounds">The bounds the glyph will be rendered at and at what size.</param>
        /// <param name="paramaters">The set of paramaters that uniquely represents a version of a glyph in at particular font size, font family, font style and DPI.</param>
        /// <returns>Returns true if the glyph should be rendered othersie it returns false.</returns>
        bool BeginGlyph(RectangleF bounds, GlyphRendererParameters paramaters);

        /// <summary>
        /// Called once all glyphs have completed rendering
        /// </summary>
        void EndText();

        /// <summary>
        /// Called before any glyphs have been rendered.
        /// </summary>
        /// <param name="bounds">The bounds the text will be rendered at and at whats size.</param>
        void BeginText(RectangleF bounds);
    }
}

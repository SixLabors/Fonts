// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that can have a glyph rendered to it as a series of actions.
    /// </summary>
    public interface IGlyphRenderer
    {
        /// <summary>
        /// Begins the figure.
        /// </summary>
        void BeginFigure();

        /// <summary>
        /// Sets a new start point to draw lines from.
        /// </summary>
        /// <param name="point">The point.</param>
        void MoveTo(Vector2 point);

        /// <summary>
        /// Draw a quadratic bezier curve connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point);

        /// <summary>
        /// Draw a cubic bezier curve connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="thirdControlPoint">The third control point.</param>
        /// <param name="point">The point.</param>
        void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point);

        /// <summary>
        /// Draw a straight line connecting the previous point to <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        void LineTo(Vector2 point);

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
        /// <param name="parameters">
        /// The set of parameters that uniquely represents a version of a glyph in at particular font size, font family, font style and DPI.
        /// </param>
        /// <returns>Returns true if the glyph should be rendered otherwise it returns false.</returns>
        bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters);

        /// <summary>
        /// Called once all glyphs have completed rendering.
        /// </summary>
        void EndText();

        /// <summary>
        /// Called before any glyphs have been rendered.
        /// </summary>
        /// <param name="bounds">The rectangle within the text will be rendered.</param>
        void BeginText(in FontRectangle bounds);

        /// <summary>
        /// Provides a callback to enable custom logic to request decoration details.
        /// A custom <see cref="TextRun"/> might use alternative triggers to determine what decorations it needs access to.
        /// </summary>
        /// <returns>The text decorations the render wants render info for.</returns>
        public TextDecorations EnabledDecorations();

        /// <summary>
        /// Provides the positions required for drawing text decorations onto the <see cref="IGlyphRenderer"/>
        /// </summary>
        /// <param name="textDecorations">The type of decoration these details correspond to.</param>
        /// <param name="start">The start position from where to draw the decorations from.</param>
        /// <param name="end">The end position from where to draw the decorations to.</param>
        /// <param name="thickness">The thickness to draw the decoration.</param>
        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

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
        /// Moves to.
        /// </summary>
        /// <param name="point">The point.</param>
        void MoveTo(Vector2 point);

        /// <summary>
        /// Quadratics the bezier to.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point);

        /// <summary>
        /// Cubics the bezier to.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="thirdControlPoint">The third control point.</param>
        /// <param name="point">The point.</param>
        void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point);

        /// <summary>
        /// Lines to.
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
        void BeginGlyph();
    }
}

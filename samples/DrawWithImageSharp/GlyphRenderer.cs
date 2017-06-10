using SixLabors.Primitives;
using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    internal class GlyphBuilder : IGlyphRenderer
    {
        private readonly PathBuilder builder = new PathBuilder();
        private readonly List<IPath> paths = new List<IPath>();
        private PointF currentPoint = default(PointF);

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBuilder"/> class.
        /// </summary>
        public GlyphBuilder()
            : this(Vector2.Zero)
        {
            // glyphs are renderd realative to bottom left so invert the Y axis to allow it to render on top left origin surface
            this.builder = new PathBuilder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBuilder"/> class.
        /// </summary>
        /// <param name="origin">The origin.</param>
        public GlyphBuilder(Vector2 origin)
        {
            this.builder = new PathBuilder();
            this.builder.SetOrigin(origin);
        }

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IEnumerable<IPath> Paths => this.paths;

        /// <summary>
        /// Begins the glyph.
        /// </summary>
        void IGlyphRenderer.BeginGlyph(PointF location, SizeF size)
        {
            this.builder.Clear();
        }

        /// <summary>
        /// Begins the figure.
        /// </summary>
        void IGlyphRenderer.BeginFigure()
        {
            this.builder.StartFigure();
        }

        /// <summary>
        /// Draws a cubic bezier from the current point  to the <paramref name="point"/>
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="thirdControlPoint">The third control point.</param>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.CubicBezierTo(PointF secondControlPoint, PointF thirdControlPoint, PointF point)
        {
            this.builder.AddBezier(this.currentPoint, secondControlPoint, thirdControlPoint, point);
            this.currentPoint = point;
        }

        /// <summary>
        /// Ends the glyph.
        /// </summary>
        void IGlyphRenderer.EndGlyph()
        {
            this.paths.Add(this.builder.Build());//.Transform(matrix));
        }

        /// <summary>
        /// Ends the figure.
        /// </summary>
        void IGlyphRenderer.EndFigure()
        {
            this.builder.CloseFigure();
        }

        /// <summary>
        /// Draws a line from the current point  to the <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.LineTo(PointF point)
        {
            this.builder.AddLine(this.currentPoint, point);
            this.currentPoint = point;
        }

        /// <summary>
        /// Moves to current point to the supplied vector.
        /// </summary>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.MoveTo(PointF point)
        {
            this.builder.StartFigure();
            this.currentPoint = point;
        }

        /// <summary>
        /// Draws a quadratics bezier from the current point  to the <paramref name="point"/>
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.QuadraticBezierTo(PointF secondControlPoint, PointF point)
        {
            Vector2 secondControlPointVector = secondControlPoint;
            Vector2 pointVector = point;
            Vector2 currentPointVector = this.currentPoint;
            Vector2 c1 = (((secondControlPointVector - currentPointVector) * 2) / 3) + currentPointVector;
            Vector2 c2 = (((secondControlPointVector - pointVector) * 2) / 3) + pointVector;

            this.builder.AddBezier(this.currentPoint, c1, c2, point);
            this.currentPoint = point;
        }

        public void EndText()
        {
        }

        public void BeginText(PointF location, SizeF size)
        {
        }
    }
}

using System.Collections.Generic;
using System.Numerics;

using SixLabors.Fonts;
using SixLabors.Primitives;
using System.Linq;

namespace SixLabors.Shapes.Temp
{
    /// <summary>
    /// rendering surface that Fonts can use to generate Shapes.
    /// </summary>
    internal class BaseGlyphBuilder : IGlyphRenderer
    {
        protected readonly PathBuilder builder = new PathBuilder();
        private readonly List<FontRectangle> glyphBounds = new List<FontRectangle>();
        private readonly List<IPath> paths = new List<IPath>();
        private Vector2 currentPoint = default(Vector2);

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBuilder"/> class.
        /// </summary>
        public BaseGlyphBuilder()
        {
            // glyphs are renderd realative to bottom left so invert the Y axis to allow it to render on top left origin surface
            this.builder = new PathBuilder();
        }

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPathCollection Paths => new PathCollection(this.paths);

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPathCollection Boxes => new PathCollection(this.glyphBounds.Select(x => new SixLabors.Shapes.RectangularPolygon(x.Location, x.Size)));

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPath TextBox { get; private set; }

        void IGlyphRenderer.EndText()
        {
        }

        void IGlyphRenderer.BeginText(FontRectangle rect)
        {
            this.TextBox = new SixLabors.Shapes.RectangularPolygon(rect.Location, rect.Size);
            BeginText(rect);
        }

        protected virtual void BeginText(FontRectangle rect)
        {
        }

        /// <summary>
        /// Begins the glyph.
        /// </summary>
        /// <param name="location">The offset that the glyph will be rendered at.</param>
        /// <param name="size">The size.</param>
        bool IGlyphRenderer.BeginGlyph(FontRectangle rect, GlyphRendererParameters cachKey)
        {
            this.builder.Clear();
            this.glyphBounds.Add(rect);
            return BeginGlyph(rect, cachKey);
        }

        protected virtual bool BeginGlyph(FontRectangle rect, GlyphRendererParameters cachKey)
        {
            BeginGlyph(rect);
            return true;
        }

        protected virtual void BeginGlyph(FontRectangle rect)
        {
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
        void IGlyphRenderer.CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            this.builder.AddBezier(this.currentPoint, secondControlPoint, thirdControlPoint, point);
            this.currentPoint = point;
        }

        /// <summary>
        /// Ends the glyph.
        /// </summary>
        void IGlyphRenderer.EndGlyph()
        {
            this.paths.Add(this.builder.Build());
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
        void IGlyphRenderer.LineTo(Vector2 point)
        {
            this.builder.AddLine(this.currentPoint, point);
            this.currentPoint = point;
        }

        /// <summary>
        /// Moves to current point to the supplied vector.
        /// </summary>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.MoveTo(Vector2 point)
        {
            this.builder.StartFigure();
            this.currentPoint = point;
        }

        /// <summary>
        /// Draws a quadratics bezier from the current point  to the <paramref name="point"/>
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.QuadraticBezierTo(Vector2 secondControlPoint, Vector2 endPoint)
        {
            Vector2 startPointVector = this.currentPoint;
            Vector2 controlPointVector = secondControlPoint;
            Vector2 endPointVector = endPoint;

            Vector2 c1 = (((controlPointVector - startPointVector) * 2) / 3) + startPointVector;
            Vector2 c2 = (((controlPointVector - endPointVector) * 2) / 3) + endPointVector;

            this.builder.AddBezier(startPointVector, c1, c2, endPoint);
            this.currentPoint = endPoint;
        }
    }
}

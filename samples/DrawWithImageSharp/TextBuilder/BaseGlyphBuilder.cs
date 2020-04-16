using System.Collections.Generic;
using System.Numerics;

using SixLabors.Fonts;
using System.Linq;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.Shapes.Temp
{
    /// <summary>
    /// rendering surface that Fonts can use to generate Shapes.
    /// </summary>
    internal class BaseGlyphBuilder : IColorGlyphRenderer
    {
        protected readonly PathBuilder builder = new PathBuilder();
        private readonly List<FontRectangle> glyphBounds = new List<FontRectangle>();
        private readonly List<IPath> paths = new List<IPath>();
        private readonly List<Color?> colors = new List<Color?>();
        private Vector2 currentPoint = default(Vector2);
        private Color? currentColor = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBuilder"/> class.
        /// </summary>
        public BaseGlyphBuilder()
        {
            // glyphs are renderd realative to bottom left so invert the Y axis to allow it to render on top left origin surface
            this.builder = new PathBuilder();
        }

        /// <summary>
        /// Get the colors for each path, where null means use user provided brush
        /// </summary>
        public IEnumerable<Color?> PathColors => this.colors;

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPathCollection Paths => new PathCollection(this.paths);

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPathCollection Boxes => new PathCollection(this.glyphBounds.Select(x => new RectangularPolygon(x.Location, x.Size)));

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPath TextBox { get; private set; }

        void IGlyphRenderer.EndText()
        {
        }

        void IGlyphRenderer.BeginText(FontRectangle rect)
        {
            this.TextBox = new RectangularPolygon(rect.Location, rect.Size);
            this.BeginText(rect);
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
            this.currentColor = null;
            this.builder.Clear();
            this.glyphBounds.Add(rect);
            return this.BeginGlyph(rect, cachKey);
        }

        protected virtual bool BeginGlyph(FontRectangle rect, GlyphRendererParameters cachKey)
        {
            this.BeginGlyph(rect);
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
            this.colors.Add(this.currentColor);
        }

        void IColorGlyphRenderer.SetColor(GlyphColor color)
        {
            this.currentColor = new Color(new Rgba32(color.Red, color.Green, color.Blue, color.Alpha));
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

using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    public class GlyphBuilder : IGlyphRenderer
    {
        PathBuilder builder = new PathBuilder();
        Vector2 currentPoint = default(Vector2);
        private Vector2 origin;

        public List<IPath> Paths { get; private set; } = new List<IPath>();

        public GlyphBuilder()
        {
        }
        
        public void BeginGlyph()
        {
            var matrix = Matrix3x2.CreateScale(1, -1);
            builder = new PathBuilder(matrix); // TODO add clear to path builder
            builder.SetOrigin(Vector2.Transform(this.origin, matrix));
        }

        public void BeginFigure()
        {
            builder.StartFigure();
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            builder.AddBezier(currentPoint, secondControlPoint, thirdControlPoint, point);
            this.currentPoint = point;
        }

        public void EndGlyph()
        {
            Paths.Add(builder.Build());
        }

        public void EndFigure()
        {

            builder.CloseFigure();
        }

        public void LineTo(Vector2 point)
        {
            builder.AddLine(currentPoint, point);
            currentPoint = point;
        }

        public void MoveTo(Vector2 point)
        {
            builder.StartFigure();
            currentPoint = point;
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            var c1 = (((secondControlPoint - this.currentPoint) * 2) / 3) + this.currentPoint;
            var c2 = (((secondControlPoint - point) * 2) / 3) + point;

            builder.AddBezier(currentPoint, c1, c2, point);
            this.currentPoint = point;
        }

        public void SetOrigin(Vector2 vector)
        {
            this.origin = vector;
            builder.SetOrigin(vector);
        }
    }
}

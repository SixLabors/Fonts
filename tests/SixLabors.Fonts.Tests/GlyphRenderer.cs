using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tests
{
    using System.Numerics;
    using Xunit;

    public class GlyphRenderer : IGlyphRenderer
    {
        public int FiguresCount = 0;
        public List<Vector2> ControlPoints = new List<Vector2>();
        public List<Vector2> ControlPointsOnCurve = new List<Vector2>();
        public void BeginGlyph()
        {

        }

        public void BeginFigure()
        {
            FiguresCount++;
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            ControlPoints.Add(secondControlPoint);
            ControlPoints.Add(thirdControlPoint);
            ControlPoints.Add(point);
            ControlPointsOnCurve.Add(point);
        }

        public void EndGlyph()
        {
            
        }

        public void EndFigure()
        {
            
        }

        public void LineTo(Vector2 point)
        {
            ControlPoints.Add(point);
            ControlPointsOnCurve.Add(point);
        }

        public void MoveTo(Vector2 point)
        {
            ControlPoints.Add(point);
            ControlPointsOnCurve.Add(point);
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            ControlPoints.Add(secondControlPoint);
            ControlPoints.Add(point);
            ControlPointsOnCurve.Add(point);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tests
{
    using SixLabors.Primitives;
    using System.Numerics;
    using Xunit;

    public class GlyphRenderer : IGlyphRenderer
    {
        public int FiguresCount = 0;
        public List<PointF> ControlPoints = new List<PointF>();
        public List<PointF> ControlPointsOnCurve = new List<PointF>();

        public void BeginGlyph(PointF location, SizeF size)
        {
        }

        public void BeginFigure()
        {
            this.FiguresCount++;
        }

        public void CubicBezierTo(PointF secondControlPoint, PointF thirdControlPoint, PointF point)
        {
            this.ControlPoints.Add(secondControlPoint);
            this.ControlPoints.Add(thirdControlPoint);
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void EndGlyph()
        {
            
        }

        public void EndFigure()
        {
            
        }

        public void LineTo(PointF point)
        {
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void MoveTo(PointF point)
        {
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void QuadraticBezierTo(PointF secondControlPoint, PointF point)
        {
            this.ControlPoints.Add(secondControlPoint);
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }


        public void EndText()
        {
        }

        public void BeginText(PointF location, SizeF size)
        {
        }
    }
}

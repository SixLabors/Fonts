using System.Collections.Generic;

namespace SixLabors.Fonts.Tests
{
    using SixLabors.Primitives;

    public class GlyphRenderer : IGlyphRenderer
    {
        public int FiguresCount = 0;

        public List<PointF> ControlPoints { get; } = new List<PointF>();

        public List<PointF> ControlPointsOnCurve { get; } = new List<PointF>();

        public List<RectangleF> GlyphRects { get; } = new List<RectangleF>();

        public List<GlyphRendererParameters> GlyphKeys { get; } = new List<GlyphRendererParameters>();

        public bool BeginGlyph(RectangleF rect, GlyphRendererParameters cacheKey)
        {
            this.GlyphRects.Add(rect);
            this.GlyphKeys.Add(cacheKey);
            return true;
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

        public void BeginText(RectangleF rect)
        {
        }
    }
}
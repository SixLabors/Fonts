using System.Collections.Generic;
using System.Numerics;

namespace SixLabors.Fonts.Tests
{
    public class GlyphRenderer : IGlyphRenderer
    {
        public int FiguresCount = 0;

        public List<Vector2> ControlPoints { get; } = new List<Vector2>();

        public List<Vector2> ControlPointsOnCurve { get; } = new List<Vector2>();

        public List<FontRectangle> GlyphRects { get; } = new List<FontRectangle>();

        public List<GlyphRendererParameters> GlyphKeys { get; } = new List<GlyphRendererParameters>();

        public bool BeginGlyph(FontRectangle rect, GlyphRendererParameters cacheKey)
        {
            this.GlyphRects.Add(rect);
            this.GlyphKeys.Add(cacheKey);
            return true;
        }

        public void BeginFigure()
        {
            this.FiguresCount++;
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
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

        public void LineTo(Vector2 point)
        {
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void MoveTo(Vector2 point)
        {
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            this.ControlPoints.Add(secondControlPoint);
            this.ControlPoints.Add(point);
            this.ControlPointsOnCurve.Add(point);
        }

        public void EndText()
        {
        }

        public void BeginText(FontRectangle rect)
        {
        }
    }
}

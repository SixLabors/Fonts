using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public interface IRenderSurface
    {
        void BeginFigure();
        void MoveTo(Vector2 point);
        void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point);
        void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point);
        void LineTo(Vector2 point);
        void EndFigure();
        void EndGlyph();
        void BeginGlyph();
    }
}

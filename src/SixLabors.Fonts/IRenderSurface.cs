using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public interface IGlyphRender
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

    public interface IMultiGlyphRenderer : IGlyphRender
    {
        // note: at this point we can map rotations * resolutions for the glyph based on the origin set :)
        void SetOrigin(Vector2 vector);
    }
}

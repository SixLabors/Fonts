using System;
using System.Collections.Generic;
using System.Numerics;

using SixLabors.Fonts;
using SixLabors.Shapes;
using SixLabors.Primitives;

namespace SixLabors.Shapes.Temp
{
    /// <summary>
    /// rendering surface that Fonts can use to generate Shapes by following a path
    /// </summary>
    internal class PathGlyphBuilder : GlyphBuilder
    {
        private readonly IPath path;

        private float offsetY = 0;

        const float Pi = (float)Math.PI;
        const float HalfPi = Pi / 2f;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBuilder"/> class.
        /// </summary>
        /// <param name="path">The path to render the glyps along.</param>
        public PathGlyphBuilder(IPath path)
            : base()
        {
            this.path = path;
        }

        protected override void BeginText(RectangleF rect)
        {
            this.offsetY = rect.Height;
        }

        protected override void BeginGlyph(RectangleF rect)
        {
            var point = this.path.PointAlongPath(rect.X);

            var targetPoint = point.Point + new Vector2(0, rect.Y - this.offsetY);

            // due to how matrix combining works you have to combine thins in the revers order of operation
            // this one rotates the glype then moves it.
            var matrix = Matrix3x2.CreateTranslation(targetPoint - (Vector2)rect.Location) * Matrix3x2.CreateRotation(point.Angle - Pi, point.Point);
            this.builder.SetTransform(matrix);
        }
    }
}

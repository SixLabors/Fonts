using System;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;

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

        protected override void BeginText(FontRectangle rect)
        {
            this.offsetY = rect.Height;
        }

        protected override void BeginGlyph(FontRectangle rect)
        {
            SegmentInfo point = this.path.PointAlongPath(rect.X);

            Vector2 targetPoint = (Vector2)point.Point + new Vector2(0, rect.Y - this.offsetY);

            // due to how matrix combining works you have to combine thins in the revers order of operation
            // this one rotates the glype then moves it.
            Matrix3x2 matrix = Matrix3x2.CreateTranslation(targetPoint - rect.Location) * Matrix3x2.CreateRotation(point.Angle - Pi, point.Point);
            this.builder.SetTransform(matrix);
        }
    }
}

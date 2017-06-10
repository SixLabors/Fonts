using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    public struct Glyph
    {
        private readonly GlyphInstance instance;
        private readonly float pointSize;

        internal Glyph(GlyphInstance instance, float pointSize)
        {
            this.instance = instance;
            this.pointSize = pointSize;
        }

        /// <summary>
        /// Renders to.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        public void RenderTo(IGlyphRenderer surface, PointF location, float dpi)
        {
            this.RenderTo(surface, location, dpi, PointF.Empty);
        }

        /// <summary>
        /// Renders to.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="offset">The offset.</param>
        public void RenderTo(IGlyphRenderer surface, PointF location, float dpi, PointF offset)
        {
            this.RenderTo(surface, location, dpi, dpi, offset);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpiX">The X dpi.</param>
        /// <param name="dpiY">The Y dpi.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, PointF location, float dpiX, float dpiY)
        {
            this.RenderTo(surface, location, dpiX, dpiY, PointF.Empty);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpiX">The dpi.</param>
        /// <param name="dpiY">The dpi.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, PointF location, float dpiX, float dpiY, PointF offset)
        {
            this.instance.RenderTo(surface, this.pointSize, location, new Vector2(dpiX, dpiY), offset);
        }
    }
}
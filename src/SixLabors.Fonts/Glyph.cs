// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    public readonly struct Glyph
    {
        private readonly float pointSize;

        internal Glyph(GlyphMetrics glyphMetrics, float pointSize)
        {
            this.GlyphMetrics = glyphMetrics;
            this.pointSize = pointSize;
        }

        /// <summary>
        /// Gets the glyph metrics.
        /// </summary>
        public GlyphMetrics GlyphMetrics { get; }

        /// <summary>
        /// Calculates the bounding box
        /// </summary>
        /// <param name="location">The location to calculate from.</param>
        /// <param name="dpi">The dpi scale the bounds in relation to.</param>
        /// <returns>The bounding box</returns>
        public FontRectangle BoundingBox(Vector2 location, Vector2 dpi)
            => this.GlyphMetrics.BoundingBox(location, this.pointSize * dpi);

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="lineHeight">The line height.</param>
        internal void RenderTo(IGlyphRenderer surface, Vector2 location, float dpi, float lineHeight)
            => this.RenderTo(surface, location, dpi, dpi, lineHeight);

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpiX">The dpi along the X axis.</param>
        /// <param name="dpiY">The dpi along the Y axis.</param>
        /// <param name="lineHeight">The line height.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        internal void RenderTo(IGlyphRenderer surface, Vector2 location, float dpiX, float dpiY, float lineHeight)
            => this.GlyphMetrics.RenderTo(surface, this.pointSize, location, new Vector2(dpiX, dpiY), lineHeight);
    }
}

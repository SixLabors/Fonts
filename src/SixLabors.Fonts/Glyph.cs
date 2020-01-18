// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    public readonly struct Glyph
    {
        private readonly GlyphInstance instance;
        private readonly float pointSize;

        internal Glyph(GlyphInstance instance, float pointSize)
        {
            this.instance = instance;
            this.pointSize = pointSize;
        }

        /// <summary>
        /// Gets the glyph instance.
        /// </summary>
        /// <value>
        /// The glyph instance.
        /// </value>
        public GlyphInstance Instance => this.instance;

        /// <summary>
        /// Calculates the bounding box
        /// </summary>
        /// <param name="location">location to calualte from.</param>
        /// <param name="dpi">dpi to calualtes in relation to</param>
        /// <returns>The bounding box</returns>
        public FontRectangle BoundingBox(Vector2 location, Vector2 dpi)
        {
            return this.instance.BoundingBox(location, this.pointSize * dpi);
        }

        /// <summary>
        /// Renders to.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="lineHeight">The line height.</param>
        internal void RenderTo(IGlyphRenderer surface, Vector2 location, float dpi, float lineHeight)
        {
            this.RenderTo(surface, location, dpi, dpi, lineHeight);
        }

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
        {
            this.instance.RenderTo(surface, this.pointSize, location, new Vector2(dpiX, dpiY), lineHeight);
        }
    }
}

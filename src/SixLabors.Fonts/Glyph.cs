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
        /// Calculates the bounding box.
        /// </summary>
        /// <param name="location">The location to calculate from.</param>
        /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at.</param>
        /// <returns>The bounding box</returns>
        public FontRectangle BoundingBox(Vector2 location, float dpi)
            => this.GlyphMetrics.GetBoundingBox(location, this.pointSize * dpi);

        /// <summary>
        /// Renders the glyph to the render surface relative to a top left origin.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="location">The location to render the glyph at.</param>
        /// <param name="offset">The offset of the glyph vector relative to the top-left position of the glyph advance.</param>
        /// <param name="options">The options to render using.</param>
        internal void RenderTo(IGlyphRenderer surface, Vector2 location, Vector2 offset, TextOptions options)
            => this.GlyphMetrics.RenderTo(surface, location, offset, options);
    }
}

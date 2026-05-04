// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <content>
/// Visitor types for streaming laid-out glyphs through the layout pipeline.
/// </content>
internal static partial class TextLayout
{
    /// <summary>
    /// Receives laid-out glyphs streamed from the layout pipeline.
    /// Implementations are value types so the generic dispatch is specialized by the JIT and no boxing or
    /// delegate allocation is required.
    /// </summary>
    internal interface IGlyphLayoutVisitor
    {
        /// <summary>
        /// Invoked once for each laid-out glyph in layout order.
        /// </summary>
        /// <param name="glyph">The laid-out glyph.</param>
        public void Visit(in GlyphLayout glyph);
    }

    /// <summary>
    /// Collects streamed glyphs into a <see cref="List{T}"/>.
    /// </summary>
    internal readonly struct GlyphLayoutCollector : IGlyphLayoutVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphLayoutCollector"/> struct.
        /// </summary>
        /// <param name="glyphs">The list to collect streamed glyphs into.</param>
        public GlyphLayoutCollector(List<GlyphLayout> glyphs) => this.Glyphs = glyphs;

        /// <summary>
        /// Gets the accumulated glyphs.
        /// </summary>
        public List<GlyphLayout> Glyphs { get; }

        /// <inheritdoc/>
        public readonly void Visit(in GlyphLayout glyph) => this.Glyphs.Add(glyph);
    }

    /// <summary>
    /// Accumulates the union of glyph ink bounds as glyphs are streamed, avoiding the allocation
    /// of a <see cref="List{T}"/> and a second iteration pass.
    /// </summary>
    internal struct GlyphBoundsAccumulator : IGlyphLayoutVisitor
    {
        private readonly float dpi;
        private float left;
        private float top;
        private float right;
        private float bottom;
        private bool any;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBoundsAccumulator"/> struct.
        /// </summary>
        /// <param name="dpi">The device-independent pixels per unit for the containing <see cref="TextOptions"/>.</param>
        public GlyphBoundsAccumulator(float dpi)
        {
            this.dpi = dpi;
            this.left = float.MaxValue;
            this.top = float.MaxValue;
            this.right = float.MinValue;
            this.bottom = float.MinValue;
            this.any = false;
        }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph)
        {
            FontRectangle box = glyph.BoundingBox(this.dpi);

            if (box.Left < this.left)
            {
                this.left = box.Left;
            }

            if (box.Top < this.top)
            {
                this.top = box.Top;
            }

            if (box.Right > this.right)
            {
                this.right = box.Right;
            }

            if (box.Bottom > this.bottom)
            {
                this.bottom = box.Bottom;
            }

            this.any = true;
        }

        /// <summary>
        /// Returns the accumulated ink bounds, or <see cref="FontRectangle.Empty"/> if no glyphs were visited.
        /// </summary>
        /// <returns>The union of the ink bounds of all visited glyphs.</returns>
        public readonly FontRectangle Result()
            => this.any ? FontRectangle.FromLTRB(this.left, this.top, this.right, this.bottom) : FontRectangle.Empty;
    }
}

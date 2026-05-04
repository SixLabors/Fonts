// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <content>
/// Visitor types for streaming laid-out glyphs into <see cref="TextBlock"/> operations.
/// </content>
public sealed partial class TextBlock
{
    /// <summary>
    /// Defines the per-glyph bounds measurement collected by <see cref="GlyphBoundsVisitor"/>.
    /// </summary>
    private enum GlyphBoundsMeasurement
    {
        Advance,
        Size,
        Bounds,
        RenderableBounds
    }

    /// <summary>
    /// Accumulates the union of rendered glyph bounds as glyphs stream from layout.
    /// </summary>
    private struct GlyphBoundsAccumulator : TextLayout.IGlyphLayoutVisitor
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
        /// <param name="dpi">The target DPI.</param>
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
        /// Returns the accumulated rendered bounds.
        /// </summary>
        /// <returns>The rendered bounds of all visited glyphs.</returns>
        public readonly FontRectangle Result()
            => this.any ? FontRectangle.FromLTRB(this.left, this.top, this.right, this.bottom) : FontRectangle.Empty;
    }

    /// <summary>
    /// Builds the full <see cref="TextMetrics"/> per-glyph arrays while glyphs stream from layout.
    /// </summary>
    private struct TextMetricsVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly float dpi;
        private readonly GlyphBounds[] characterAdvances;
        private readonly GlyphBounds[] characterSizes;
        private readonly GlyphBounds[] characterBounds;
        private readonly GlyphBounds[] characterRenderableBounds;
        private int index;
        private float left;
        private float top;
        private float right;
        private float bottom;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextMetricsVisitor"/> struct.
        /// </summary>
        /// <param name="dpi">The target DPI.</param>
        /// <param name="characterAdvances">The logical advance array to fill.</param>
        /// <param name="characterSizes">The normalized rendered size array to fill.</param>
        /// <param name="characterBounds">The rendered bounds array to fill.</param>
        /// <param name="characterRenderableBounds">The renderable bounds array to fill.</param>
        public TextMetricsVisitor(
            float dpi,
            GlyphBounds[] characterAdvances,
            GlyphBounds[] characterSizes,
            GlyphBounds[] characterBounds,
            GlyphBounds[] characterRenderableBounds)
        {
            this.dpi = dpi;
            this.characterAdvances = characterAdvances;
            this.characterSizes = characterSizes;
            this.characterBounds = characterBounds;
            this.characterRenderableBounds = characterRenderableBounds;
            this.index = 0;
            this.left = float.MaxValue;
            this.top = float.MaxValue;
            this.right = float.MinValue;
            this.bottom = float.MinValue;
        }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph)
        {
            FontRectangle glyphBox = glyph.BoundingBox(this.dpi);
            FontRectangle advanceRect = new(glyph.BoxLocation.X * this.dpi, glyph.BoxLocation.Y * this.dpi, glyph.AdvanceX * this.dpi, glyph.AdvanceY * this.dpi);
            FontRectangle renderableRect = FontRectangle.Union(advanceRect, glyphBox);

            CodePoint codePoint = glyph.Glyph.GlyphMetrics.CodePoint;
            int graphemeIndex = glyph.GraphemeIndex;
            int stringIndex = glyph.StringIndex;

            FontRectangle advanceBox = new(0, 0, glyph.AdvanceX * this.dpi, glyph.AdvanceY * this.dpi);
            FontRectangle sizeBox = new(0, 0, glyphBox.Width, glyphBox.Height);

            this.characterAdvances[this.index] = new GlyphBounds(codePoint, in advanceBox, graphemeIndex, stringIndex);
            this.characterSizes[this.index] = new GlyphBounds(codePoint, in sizeBox, graphemeIndex, stringIndex);
            this.characterBounds[this.index] = new GlyphBounds(codePoint, in glyphBox, graphemeIndex, stringIndex);
            this.characterRenderableBounds[this.index] = new GlyphBounds(codePoint, in renderableRect, graphemeIndex, stringIndex);

            if (glyphBox.Left < this.left)
            {
                this.left = glyphBox.Left;
            }

            if (glyphBox.Top < this.top)
            {
                this.top = glyphBox.Top;
            }

            if (glyphBox.Right > this.right)
            {
                this.right = glyphBox.Right;
            }

            if (glyphBox.Bottom > this.bottom)
            {
                this.bottom = glyphBox.Bottom;
            }

            this.index++;
        }

        /// <summary>
        /// Returns the accumulated rendered bounds.
        /// </summary>
        /// <returns>The rendered bounds of all visited glyphs.</returns>
        public readonly FontRectangle Bounds()
            => this.index == 0 ? FontRectangle.Empty : FontRectangle.FromLTRB(this.left, this.top, this.right, this.bottom);
    }

    /// <summary>
    /// Builds one per-glyph bounds array while glyphs stream from layout.
    /// </summary>
    private struct GlyphBoundsVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly GlyphBounds[] characterBounds;
        private readonly float dpi;
        private readonly GlyphBoundsMeasurement measurement;
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBoundsVisitor"/> struct.
        /// </summary>
        /// <param name="characterBounds">The target array to fill.</param>
        /// <param name="dpi">The target DPI.</param>
        /// <param name="measurement">The bounds measurement to collect.</param>
        public GlyphBoundsVisitor(GlyphBounds[] characterBounds, float dpi, GlyphBoundsMeasurement measurement)
        {
            this.characterBounds = characterBounds;
            this.dpi = dpi;
            this.measurement = measurement;
            this.index = 0;
            this.HasSize = false;
        }

        /// <summary>
        /// Gets a value indicating whether any visited glyph had non-empty bounds.
        /// </summary>
        public bool HasSize { get; private set; }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph)
        {
            FontRectangle bounds;
            switch (this.measurement)
            {
                case GlyphBoundsMeasurement.Advance:
                    bounds = new(0, 0, glyph.AdvanceX * this.dpi, glyph.AdvanceY * this.dpi);
                    break;

                case GlyphBoundsMeasurement.Size:
                    FontRectangle sizeBounds = glyph.BoundingBox(this.dpi);
                    bounds = new(0, 0, sizeBounds.Width, sizeBounds.Height);
                    break;

                case GlyphBoundsMeasurement.Bounds:
                    bounds = glyph.BoundingBox(this.dpi);
                    break;

                default:
                    FontRectangle glyphBounds = glyph.BoundingBox(this.dpi);
                    FontRectangle advance = new(glyph.BoxLocation.X * this.dpi, glyph.BoxLocation.Y * this.dpi, glyph.AdvanceX * this.dpi, glyph.AdvanceY * this.dpi);
                    bounds = FontRectangle.Union(advance, glyphBounds);
                    break;
            }

            this.HasSize |= bounds.Width > 0 || bounds.Height > 0;
            this.characterBounds[this.index] = new GlyphBounds(glyph.Glyph.GlyphMetrics.CodePoint, in bounds, glyph.GraphemeIndex, glyph.StringIndex);
            this.index++;
        }
    }

    /// <summary>
    /// Renders glyphs as they stream from layout.
    /// </summary>
    private readonly struct GlyphRendererVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly IGlyphRenderer renderer;
        private readonly TextOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphRendererVisitor"/> struct.
        /// </summary>
        /// <param name="renderer">The target renderer.</param>
        /// <param name="options">The text options used for rendering.</param>
        public GlyphRendererVisitor(IGlyphRenderer renderer, TextOptions options)
        {
            this.renderer = renderer;
            this.options = options;
        }

        /// <inheritdoc/>
        public readonly void Visit(in GlyphLayout glyph)
            => glyph.Glyph.RenderTo(this.renderer, glyph.GraphemeIndex, glyph.PenLocation, glyph.Offset, glyph.LayoutMode, this.options);
    }
}

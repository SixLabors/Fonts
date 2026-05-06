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
    internal enum GlyphBoundsMeasurement
    {
        Advance,
        Bounds,
        RenderableBounds
    }

    /// <summary>
    /// Coalesces consecutive laid-out glyph entries that belong to the same grapheme.
    /// </summary>
    private struct GraphemeMetricsAccumulator
    {
        private readonly GraphemeMetrics[] graphemes;
        private readonly float dpi;
        private int count;
        private int graphemeIndex;
        private int stringIndex;
        private int bidiLevel;
        private bool isLineBreak;
        private bool contributesToMeasurement;
        private FontRectangle advanceBounds;
        private FontRectangle bounds;
        private bool hasCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphemeMetricsAccumulator"/> struct.
        /// </summary>
        /// <param name="graphemes">The target grapheme array to fill.</param>
        /// <param name="dpi">The target DPI.</param>
        public GraphemeMetricsAccumulator(GraphemeMetrics[] graphemes, float dpi)
        {
            this.graphemes = graphemes;
            this.dpi = dpi;
            this.count = 0;
            this.graphemeIndex = 0;
            this.stringIndex = 0;
            this.bidiLevel = 0;
            this.isLineBreak = false;
            this.contributesToMeasurement = false;
            this.advanceBounds = FontRectangle.Empty;
            this.bounds = FontRectangle.Empty;
            this.hasCurrent = false;
        }

        /// <summary>
        /// Gets the number of graphemes emitted so far.
        /// </summary>
        public readonly int Count => this.count;

        /// <summary>
        /// Adds one laid-out glyph entry to the current grapheme, flushing the previous grapheme when needed.
        /// </summary>
        /// <param name="glyph">The laid-out glyph entry.</param>
        /// <param name="contributesToMeasurement">Whether the glyph contributes to visual measurements.</param>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
        {
            FontRectangle advanceBounds = glyph.MeasureAdvance(this.dpi);
            FontRectangle bounds = glyph.MeasureBounds(this.dpi);

            if (!this.hasCurrent)
            {
                this.Start(in glyph, in advanceBounds, in bounds, contributesToMeasurement);
                return;
            }

            if (glyph.GraphemeIndex != this.graphemeIndex)
            {
                this.Flush();
                this.Start(in glyph, in advanceBounds, in bounds, contributesToMeasurement);
                return;
            }

            this.advanceBounds = FontRectangle.Union(this.advanceBounds, advanceBounds);
            this.bounds = FontRectangle.Union(this.bounds, bounds);
            this.isLineBreak |= CodePoint.IsNewLine(glyph.CodePoint);
            this.contributesToMeasurement |= contributesToMeasurement;
        }

        /// <summary>
        /// Flushes the current line's pending grapheme.
        /// </summary>
        public void EndLine() => this.Flush();

        /// <summary>
        /// Starts a new grapheme from the first emitted glyph in a consecutive grapheme run.
        /// </summary>
        /// <param name="glyph">The first glyph in the grapheme.</param>
        /// <param name="advanceBounds">The positioned logical advance bounds for <paramref name="glyph"/>.</param>
        /// <param name="bounds">The rendered bounds for <paramref name="glyph"/>.</param>
        /// <param name="contributesToMeasurement">Whether the glyph contributes to visual measurements.</param>
        private void Start(
            in GlyphLayout glyph,
            in FontRectangle advanceBounds,
            in FontRectangle bounds,
            bool contributesToMeasurement)
        {
            this.graphemeIndex = glyph.GraphemeIndex;
            this.stringIndex = glyph.StringIndex;
            this.bidiLevel = glyph.BidiLevel;
            this.isLineBreak = CodePoint.IsNewLine(glyph.CodePoint);
            this.contributesToMeasurement = contributesToMeasurement;
            this.advanceBounds = advanceBounds;
            this.bounds = bounds;
            this.hasCurrent = true;
        }

        /// <summary>
        /// Emits the current grapheme while preserving the visual order produced by text layout.
        /// </summary>
        private void Flush()
        {
            if (!this.hasCurrent)
            {
                return;
            }

            FontRectangle renderableBounds = FontRectangle.Union(this.advanceBounds, this.bounds);
            this.graphemes[this.count] = new GraphemeMetrics(
                this.advanceBounds,
                this.bounds,
                renderableBounds,
                this.graphemeIndex,
                this.stringIndex,
                this.bidiLevel,
                this.isLineBreak,
                this.contributesToMeasurement);

            this.count++;
            this.hasCurrent = false;
        }
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
        public readonly void BeginLine(int lineIndex)
        {
        }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
        {
            if (!contributesToMeasurement)
            {
                return;
            }

            FontRectangle box = glyph.MeasureBounds(this.dpi);
            if (box.Width <= 0 && box.Height <= 0)
            {
                return;
            }

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

        /// <inheritdoc/>
        public readonly void EndLine()
        {
        }
    }

    /// <summary>
    /// Builds the <see cref="TextMetrics"/> bounds and grapheme metrics array while glyphs stream from layout.
    /// </summary>
    private struct TextMetricsVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly float dpi;
        private GraphemeMetricsAccumulator graphemes;
        private float left;
        private float top;
        private float right;
        private float bottom;
        private bool hasBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextMetricsVisitor"/> struct.
        /// </summary>
        /// <param name="dpi">The target DPI.</param>
        /// <param name="graphemes">The grapheme metrics array to fill.</param>
        public TextMetricsVisitor(
            float dpi,
            GraphemeMetrics[] graphemes)
        {
            this.dpi = dpi;
            this.graphemes = new(graphemes, dpi);
            this.left = float.MaxValue;
            this.top = float.MaxValue;
            this.right = float.MinValue;
            this.bottom = float.MinValue;
            this.hasBounds = false;
        }

        /// <inheritdoc/>
        public readonly void BeginLine(int lineIndex)
        {
        }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
        {
            FontRectangle glyphBox = glyph.MeasureBounds(this.dpi);
            bool hasGlyphBox = glyphBox.Width > 0 || glyphBox.Height > 0;

            if (contributesToMeasurement && hasGlyphBox && glyphBox.Left < this.left)
            {
                this.left = glyphBox.Left;
            }

            if (contributesToMeasurement && hasGlyphBox && glyphBox.Top < this.top)
            {
                this.top = glyphBox.Top;
            }

            if (contributesToMeasurement && hasGlyphBox && glyphBox.Right > this.right)
            {
                this.right = glyphBox.Right;
            }

            if (contributesToMeasurement && hasGlyphBox && glyphBox.Bottom > this.bottom)
            {
                this.bottom = glyphBox.Bottom;
            }

            this.hasBounds |= contributesToMeasurement && hasGlyphBox;
            this.graphemes.Visit(in glyph, contributesToMeasurement);
        }

        /// <summary>
        /// Returns the accumulated rendered bounds.
        /// </summary>
        /// <returns>The rendered bounds of all visited glyphs.</returns>
        public readonly FontRectangle Bounds()
            => this.hasBounds ? FontRectangle.FromLTRB(this.left, this.top, this.right, this.bottom) : FontRectangle.Empty;

        /// <inheritdoc/>
        public void EndLine() => this.graphemes.EndLine();
    }

    /// <summary>
    /// Builds the per-line grapheme metrics results while glyphs stream from layout.
    /// </summary>
    private struct LineLayoutVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly TextLayout.TextBox textBox;
        private readonly TextOptions options;
        private readonly float wrappingLength;
        private readonly LineMetrics[] metrics;
        private readonly LineLayout[] lines;
        private readonly GraphemeMetrics[] graphemes;
        private GraphemeMetricsAccumulator graphemeAccumulator;
        private int lineIndex;
        private int lineGraphemeStart;
        private int metricIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineLayoutVisitor"/> struct.
        /// </summary>
        /// <param name="textBox">The shaped and line-broken text box.</param>
        /// <param name="options">The text options used for layout.</param>
        /// <param name="wrappingLength">The wrapping length in pixels.</param>
        /// <param name="graphemes">The grapheme metrics array to fill.</param>
        /// <param name="metrics">The line metrics aligned with the line-broken text box.</param>
        /// <param name="lines">The line layout array to fill.</param>
        /// <param name="dpi">The target DPI.</param>
        public LineLayoutVisitor(
            TextLayout.TextBox textBox,
            TextOptions options,
            float wrappingLength,
            GraphemeMetrics[] graphemes,
            LineMetrics[] metrics,
            LineLayout[] lines,
            float dpi)
        {
            this.textBox = textBox;
            this.options = options;
            this.wrappingLength = wrappingLength;
            this.metrics = metrics;
            this.lines = lines;
            this.graphemes = graphemes;
            this.graphemeAccumulator = new(graphemes, dpi);
            this.lineIndex = 0;
            this.lineGraphemeStart = 0;
            this.metricIndex = 0;
        }

        /// <inheritdoc/>
        public void BeginLine(int lineIndex)
        {
            this.lineGraphemeStart = this.graphemeAccumulator.Count;
            this.metricIndex = lineIndex;
        }

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
            => this.graphemeAccumulator.Visit(in glyph, contributesToMeasurement);

        /// <inheritdoc/>
        public void EndLine()
        {
            this.graphemeAccumulator.EndLine();

            // TextLayout owns the visual line loop, so the slice is recorded here instead of
            // reconstructing line membership from metrics after glyph emission.
            ReadOnlyMemory<GraphemeMetrics> lineGraphemes = new(this.graphemes, this.lineGraphemeStart, this.graphemeAccumulator.Count - this.lineGraphemeStart);
            this.lines[this.lineIndex] = new LineLayout(
                this.textBox,
                this.options,
                this.wrappingLength,
                this.metricIndex,
                in this.metrics[this.metricIndex],
                lineGraphemes);

            this.lineIndex++;
        }
    }

    /// <summary>
    /// Builds one per-glyph bounds array while glyphs stream from layout.
    /// </summary>
    private struct GlyphBoundsVisitor : TextLayout.IGlyphLayoutVisitor
    {
        private readonly GlyphBounds[] glyphBounds;
        private readonly float dpi;
        private readonly GlyphBoundsMeasurement measurement;
        private readonly int lineIndex;
        private int count;
        private int currentLineIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBoundsVisitor"/> struct.
        /// </summary>
        /// <param name="glyphBounds">The target array to fill.</param>
        /// <param name="dpi">The target DPI.</param>
        /// <param name="measurement">The bounds measurement to collect.</param>
        public GlyphBoundsVisitor(
            GlyphBounds[] glyphBounds,
            float dpi,
            GlyphBoundsMeasurement measurement)
            : this(glyphBounds, dpi, measurement, -1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphBoundsVisitor"/> struct.
        /// </summary>
        /// <param name="glyphBounds">The target array to fill.</param>
        /// <param name="dpi">The target DPI.</param>
        /// <param name="measurement">The bounds measurement to collect.</param>
        /// <param name="lineIndex">The line index to collect.</param>
        public GlyphBoundsVisitor(
            GlyphBounds[] glyphBounds,
            float dpi,
            GlyphBoundsMeasurement measurement,
            int lineIndex)
        {
            this.glyphBounds = glyphBounds;
            this.dpi = dpi;
            this.measurement = measurement;
            this.lineIndex = lineIndex;
            this.count = 0;
            this.currentLineIndex = -1;
        }

        /// <inheritdoc/>
        public void BeginLine(int lineIndex)
            => this.currentLineIndex = lineIndex;

        /// <inheritdoc/>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
        {
            if (this.lineIndex >= 0 && this.currentLineIndex != this.lineIndex)
            {
                return;
            }

            FontRectangle bounds;
            switch (this.measurement)
            {
                case GlyphBoundsMeasurement.Advance:
                    bounds = glyph.MeasureAdvance(this.dpi);
                    break;

                case GlyphBoundsMeasurement.Bounds:
                    bounds = glyph.MeasureBounds(this.dpi);
                    break;

                default:
                    FontRectangle glyphBounds = glyph.MeasureBounds(this.dpi);
                    FontRectangle advance = glyph.MeasureAdvance(this.dpi);
                    bounds = FontRectangle.Union(advance, glyphBounds);
                    break;
            }

            this.glyphBounds[this.count] = new GlyphBounds(glyph.Glyph.GlyphMetrics.CodePoint, in bounds, glyph.GraphemeIndex, glyph.StringIndex);
            this.count++;
        }

        /// <inheritdoc/>
        public readonly void EndLine()
        {
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
        public readonly void BeginLine(int lineIndex)
        {
        }

        /// <inheritdoc/>
        public readonly void Visit(in GlyphLayout glyph, bool contributesToMeasurement)
        {
            if (!contributesToMeasurement)
            {
                return;
            }

            glyph.Glyph.RenderTo(this.renderer, glyph.GraphemeIndex, glyph.GlyphOrigin, glyph.DecorationOrigin, glyph.LayoutMode, this.options);
        }

        /// <inheritdoc/>
        public readonly void EndLine()
        {
        }
    }
}

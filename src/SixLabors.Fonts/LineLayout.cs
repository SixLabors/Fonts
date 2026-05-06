// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents one laid-out line from a <see cref="TextBlock"/>.
/// </summary>
public sealed class LineLayout
{
    private readonly TextLayout.TextBox textBox;
    private readonly TextOptions options;
    private readonly float wrappingLength;
    private readonly int lineIndex;
    private readonly LayoutMode layoutMode;
    private readonly ReadOnlyMemory<GraphemeMetrics> graphemeMetrics;
    private GlyphBounds[]? glyphAdvances;
    private GlyphBounds[]? glyphBounds;
    private GlyphBounds[]? glyphRenderableBounds;

    internal LineLayout(
        TextLayout.TextBox textBox,
        TextOptions options,
        float wrappingLength,
        int lineIndex,
        in LineMetrics metrics,
        ReadOnlyMemory<GraphemeMetrics> graphemeMetrics)
    {
        this.textBox = textBox;
        this.options = options;
        this.wrappingLength = wrappingLength;
        this.lineIndex = lineIndex;
        this.layoutMode = options.LayoutMode;
        this.LineMetrics = metrics;
        this.graphemeMetrics = graphemeMetrics;
    }

    /// <summary>
    /// Gets the measured line metrics.
    /// </summary>
    public LineMetrics LineMetrics { get; }

    /// <summary>
    /// Gets the grapheme metrics entries for this line in final layout order.
    /// </summary>
    public ReadOnlySpan<GraphemeMetrics> GraphemeMetrics => this.graphemeMetrics.Span;

    /// <summary>
    /// Hit tests the supplied point against this line's grapheme advance bounds.
    /// </summary>
    /// <param name="point">The point in pixel units.</param>
    /// <returns>The hit-tested grapheme position.</returns>
    public TextHit HitTest(Vector2 point)
        => TextInteraction.HitTestLine(this.lineIndex, this.GraphemeMetrics, point, this.layoutMode);

    /// <summary>
    /// Gets the caret position for the supplied grapheme index.
    /// </summary>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <returns>The caret position in pixel units.</returns>
    public CaretPosition GetCaretPosition(int graphemeIndex)
        => TextInteraction.GetCaretPositionLine(
            this.lineIndex,
            this.LineMetrics,
            this.GraphemeMetrics,
            graphemeIndex,
            this.layoutMode);

    /// <summary>
    /// Gets the caret position for the supplied hit.
    /// </summary>
    /// <param name="hit">The hit-tested grapheme position.</param>
    /// <returns>The caret position in pixel units.</returns>
    public CaretPosition GetCaretPosition(TextHit hit)
        => this.GetCaretPosition(hit.GraphemeInsertionIndex);

    /// <summary>
    /// Moves the supplied caret by the requested operation within this line.
    /// </summary>
    /// <param name="caret">The current caret position.</param>
    /// <param name="movement">The movement operation.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public CaretPosition MoveCaret(CaretPosition caret, CaretMovement movement)
        => TextInteraction.MoveCaretLine(
            this.lineIndex,
            this.LineMetrics,
            this.GraphemeMetrics,
            caret,
            movement,
            this.layoutMode);

    /// <summary>
    /// Gets selection bounds for the supplied grapheme range.
    /// </summary>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <returns>A read-only memory region containing the selection bounds in visual order and pixel units.</returns>
    public ReadOnlyMemory<FontRectangle> GetSelectionBounds(int graphemeStart, int graphemeEnd)
        => TextInteraction.GetSelectionBoundsLine(
            this.LineMetrics,
            this.GraphemeMetrics,
            graphemeStart,
            graphemeEnd,
            this.layoutMode);

    /// <summary>
    /// Gets selection bounds between two hit-tested grapheme positions.
    /// </summary>
    /// <param name="anchor">The fixed selection endpoint.</param>
    /// <param name="focus">The active selection endpoint.</param>
    /// <returns>A read-only memory region containing the selection bounds in visual order and pixel units.</returns>
    public ReadOnlyMemory<FontRectangle> GetSelectionBounds(TextHit anchor, TextHit focus)
        => TextInteraction.GetSelectionBoundsLine(
            this.LineMetrics,
            this.GraphemeMetrics,
            anchor.GraphemeInsertionIndex,
            focus.GraphemeInsertionIndex,
            this.layoutMode);

    /// <summary>
    /// Gets selection bounds between two caret positions.
    /// </summary>
    /// <param name="anchor">The fixed selection endpoint.</param>
    /// <param name="focus">The active selection endpoint.</param>
    /// <returns>A read-only memory region containing the selection bounds in visual order and pixel units.</returns>
    public ReadOnlyMemory<FontRectangle> GetSelectionBounds(CaretPosition anchor, CaretPosition focus)
        => TextInteraction.GetSelectionBoundsLine(
            this.LineMetrics,
            this.GraphemeMetrics,
            anchor.GraphemeIndex,
            focus.GraphemeIndex,
            this.layoutMode);

    /// <inheritdoc cref="TextBlock.MeasureGlyphAdvances(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphAdvances()
        => this.glyphAdvances ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Advance,
            this.lineIndex);

    /// <inheritdoc cref="TextBlock.MeasureGlyphBounds(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphBounds()
        => this.glyphBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Bounds,
            this.lineIndex);

    /// <inheritdoc cref="TextBlock.MeasureGlyphRenderableBounds(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphRenderableBounds()
        => this.glyphRenderableBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.RenderableBounds,
            this.lineIndex);
}

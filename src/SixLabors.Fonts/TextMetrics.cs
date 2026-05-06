// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates the full set of measurement results for laid-out text.
/// </summary>
public sealed class TextMetrics
{
    private readonly TextBlock textBlock;
    private readonly TextLayout.TextBox textBox;
    private readonly float wrappingLength;
    private readonly LayoutMode layoutMode;
    private readonly GraphemeMetrics[] graphemeMetrics;
    private readonly LineMetrics[] lineMetrics;
    private GlyphBounds[]? glyphAdvances;
    private GlyphBounds[]? glyphBounds;
    private GlyphBounds[]? glyphRenderableBounds;

    internal TextMetrics(
        TextBlock textBlock,
        TextLayout.TextBox textBox,
        float wrappingLength,
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle renderableBounds,
        int lineCount,
        GraphemeMetrics[] graphemes,
        LineMetrics[] lines)
    {
        this.textBlock = textBlock;
        this.textBox = textBox;
        this.wrappingLength = wrappingLength;
        this.layoutMode = textBlock.Options.LayoutMode;
        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.LineCount = lineCount;
        this.graphemeMetrics = graphemes;
        this.lineMetrics = lines;
    }

    /// <summary>
    /// Gets the logical advance rectangle of the text in pixel units.
    /// </summary>
    /// <remarks>
    /// Reflects line-box height and horizontal or vertical text advance from the layout model.
    /// Does not guarantee that all rendered glyph pixels fit within the returned rectangle.
    /// </remarks>
    public FontRectangle Advance { get; }

    /// <summary>
    /// Gets the rendered glyph bounds of the text in pixel units.
    /// </summary>
    /// <remarks>
    /// This is the tight ink bounds enclosing all rendered glyphs and may be smaller or larger
    /// than the logical advance. May have a non-zero origin.
    /// </remarks>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the union of the logical advance rectangle (positioned at the text options origin)
    /// and the rendered glyph bounds in pixel units.
    /// </summary>
    /// <remarks>
    /// Use this rectangle when both typographic advance and rendered glyph overshoot
    /// must fit within the same bounding box.
    /// </remarks>
    public FontRectangle RenderableBounds { get; }

    /// <summary>
    /// Gets the number of laid-out lines in the text.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Gets the grapheme metrics entries in final layout order.
    /// </summary>
    public ReadOnlySpan<GraphemeMetrics> GraphemeMetrics => this.graphemeMetrics;

    /// <summary>
    /// Gets the per-line layout metrics for the text.
    /// </summary>
    public ReadOnlySpan<LineMetrics> LineMetrics => this.lineMetrics;

    /// <summary>
    /// Hit tests the supplied point against the laid-out grapheme advance bounds.
    /// </summary>
    /// <param name="point">The point in pixel units.</param>
    /// <returns>The hit-tested grapheme position.</returns>
    public TextHit HitTest(Vector2 point)
        => TextInteraction.HitTest(
            this.LineMetrics,
            this.GraphemeMetrics,
            point,
            this.layoutMode);

    /// <summary>
    /// Gets the caret position for the supplied grapheme index.
    /// </summary>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <returns>The caret position in pixel units.</returns>
    public CaretPosition GetCaretPosition(int graphemeIndex)
        => TextInteraction.GetCaretPosition(
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
    /// Moves the supplied caret by the requested operation.
    /// </summary>
    /// <param name="caret">The current caret position.</param>
    /// <param name="movement">The movement operation.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public CaretPosition MoveCaret(CaretPosition caret, CaretMovement movement)
        => TextInteraction.MoveCaret(
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
        => TextInteraction.GetSelectionBounds(
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
        => TextInteraction.GetSelectionBounds(
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
        => TextInteraction.GetSelectionBounds(
            this.LineMetrics,
            this.GraphemeMetrics,
            anchor.GraphemeIndex,
            focus.GraphemeIndex,
            this.layoutMode);

    /// <inheritdoc cref="TextBlock.MeasureGlyphAdvances(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphAdvances()
        => this.glyphAdvances ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Advance);

    /// <inheritdoc cref="TextBlock.MeasureGlyphBounds(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphBounds()
        => this.glyphBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Bounds);

    /// <inheritdoc cref="TextBlock.MeasureGlyphRenderableBounds(float)"/>
    public ReadOnlyMemory<GlyphBounds> MeasureGlyphRenderableBounds()
        => this.glyphRenderableBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.RenderableBounds);
}

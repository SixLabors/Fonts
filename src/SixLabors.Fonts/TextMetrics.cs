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
    private readonly TextBox textBox;
    private readonly float wrappingLength;
    private readonly LayoutMode layoutMode;
    private readonly TextDirection textDirection;
    private readonly GraphemeMetrics[] graphemeMetrics;
    private readonly LineMetrics[] lineMetrics;
    private readonly WordMetrics[] wordMetrics;
    private GlyphMetrics[]? glyphMetrics;

    internal TextMetrics(
        TextBlock textBlock,
        TextBox textBox,
        float wrappingLength,
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle renderableBounds,
        int lineCount,
        GraphemeMetrics[] graphemes,
        LineMetrics[] lines,
        WordMetrics[] words)
    {
        this.textBlock = textBlock;
        this.textBox = textBox;
        this.wrappingLength = wrappingLength;
        this.layoutMode = textBlock.Options.LayoutMode;
        this.textDirection = textBox.TextLines.Count == 0
            ? (textBlock.Options.TextDirection == TextDirection.RightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight)
            : textBox.TextDirection();

        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.LineCount = lineCount;
        this.graphemeMetrics = graphemes;
        this.lineMetrics = lines;
        this.wordMetrics = words;
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
    /// Gets the word-boundary segment metrics in source order.
    /// </summary>
    public ReadOnlySpan<WordMetrics> WordMetrics => this.wordMetrics;

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
    /// Gets the caret position for the supplied hit.
    /// </summary>
    /// <param name="hit">The hit-tested grapheme position.</param>
    /// <returns>The caret position in pixel units.</returns>
    public CaretPosition GetCaretPosition(TextHit hit)
        => TextInteraction.GetCaretPosition(
            this.LineMetrics,
            this.GraphemeMetrics,
            hit.GraphemeInsertionIndex,
            this.layoutMode);

    /// <summary>
    /// Gets an absolute caret position in the laid-out text.
    /// </summary>
    /// <param name="placement">The absolute caret placement.</param>
    /// <returns>The caret position in pixel units.</returns>
    public CaretPosition GetCaret(CaretPlacement placement)
        => TextInteraction.GetCaret(
            this.LineMetrics,
            this.GraphemeMetrics,
            placement,
            this.layoutMode,
            this.textDirection);

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
            this.WordMetrics,
            caret,
            movement,
            this.layoutMode,
            this.textDirection);

    /// <summary>
    /// Gets the word metrics for the word-boundary segment containing the supplied hit-tested grapheme position.
    /// </summary>
    /// <param name="hit">The hit-tested grapheme position.</param>
    /// <returns>The word metrics containing the hit grapheme.</returns>
    public WordMetrics GetWordMetrics(TextHit hit)
        => TextInteraction.GetWordMetrics(this.WordMetrics, hit.GraphemeIndex);

    /// <summary>
    /// Gets the word metrics for the word-boundary segment containing the supplied caret position.
    /// </summary>
    /// <param name="caret">The caret position.</param>
    /// <returns>The word metrics containing the caret's grapheme insertion index.</returns>
    public WordMetrics GetWordMetrics(CaretPosition caret)
        => TextInteraction.GetWordMetrics(this.WordMetrics, caret.GraphemeIndex);

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

    /// <summary>
    /// Gets selection bounds for the supplied grapheme metrics.
    /// </summary>
    /// <param name="metrics">The grapheme metrics to select.</param>
    /// <returns>A read-only memory region containing the selection bounds in visual order and pixel units.</returns>
    public ReadOnlyMemory<FontRectangle> GetSelectionBounds(GraphemeMetrics metrics)
        => TextInteraction.GetSelectionBounds(
            this.LineMetrics,
            this.GraphemeMetrics,
            metrics,
            this.layoutMode);

    /// <summary>
    /// Gets selection bounds for the supplied word metrics.
    /// </summary>
    /// <param name="metrics">The word metrics to select.</param>
    /// <returns>A read-only memory region containing the selection bounds in visual order and pixel units.</returns>
    public ReadOnlyMemory<FontRectangle> GetSelectionBounds(WordMetrics metrics)
        => TextInteraction.GetSelectionBounds(
            this.LineMetrics,
            this.GraphemeMetrics,
            metrics.GraphemeStart,
            metrics.GraphemeEnd,
            this.layoutMode);

    /// <inheritdoc cref="TextBlock.GetGlyphMetrics(float)"/>
    public ReadOnlyMemory<GlyphMetrics> GetGlyphMetrics()
        => this.glyphMetrics ??= TextBlock.GetGlyphMetricsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength);
}

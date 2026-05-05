// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates the full set of measurement results for laid-out text.
/// </summary>
public sealed class TextMetrics
{
    private readonly TextBlock textBlock;
    private readonly TextLayout.TextBox textBox;
    private readonly float wrappingLength;
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

    /// <inheritdoc cref="TextBlock.MeasureGlyphAdvances(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphAdvances()
        => this.glyphAdvances ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Advance);

    /// <inheritdoc cref="TextBlock.MeasureGlyphBounds(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphBounds()
        => this.glyphBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Bounds);

    /// <inheritdoc cref="TextBlock.MeasureGlyphRenderableBounds(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphRenderableBounds()
        => this.glyphRenderableBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.textBlock.Options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.RenderableBounds);
}

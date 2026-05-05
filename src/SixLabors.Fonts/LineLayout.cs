// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

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

    /// <inheritdoc cref="TextBlock.MeasureGlyphAdvances(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphAdvances()
        => this.glyphAdvances ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Advance,
            this.lineIndex);

    /// <inheritdoc cref="TextBlock.MeasureGlyphBounds(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphBounds()
        => this.glyphBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.Bounds,
            this.lineIndex);

    /// <inheritdoc cref="TextBlock.MeasureGlyphRenderableBounds(float)"/>
    public ReadOnlySpan<GlyphBounds> MeasureGlyphRenderableBounds()
        => this.glyphRenderableBounds ??= TextBlock.MeasureGlyphBoundsArray(
            this.textBox,
            this.options,
            this.wrappingLength,
            TextBlock.GlyphBoundsMeasurement.RenderableBounds,
            this.lineIndex);
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates logic for laying out and then measuring text properties.
/// </summary>
public static class TextMeasurer
{
    /// <inheritdoc cref="Measure(ReadOnlySpan{char}, TextOptions)"/>
    public static TextMetrics Measure(string text, TextOptions options)
        => Measure(text.AsSpan(), options);

    /// <summary>
    /// Measures the full set of layout metrics for the supplied text in a single pass.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>A <see cref="TextMetrics"/> instance containing every measurement for the laid-out text.</returns>
    public static TextMetrics Measure(ReadOnlySpan<char> text, TextOptions options)
    {
        TextBlock block = new(text, options);
        return block.Measure(options.WrappingLength);
    }

    /// <inheritdoc cref="MeasureAdvance(ReadOnlySpan{char}, TextOptions)"/>
    public static FontRectangle MeasureAdvance(string text, TextOptions options)
        => MeasureAdvance(text.AsSpan(), options);

    /// <summary>
    /// Measures the logical advance of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The logical advance rectangle of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureAdvance(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        TextBlock block = new(text, options);
        return block.MeasureAdvance(options.WrappingLength);
    }

    /// <inheritdoc cref="MeasureBounds(ReadOnlySpan{char}, TextOptions)"/>
    public static FontRectangle MeasureBounds(string text, TextOptions options)
        => MeasureBounds(text.AsSpan(), options);

    /// <inheritdoc cref="MeasureRenderableBounds(ReadOnlySpan{char}, TextOptions)"/>
    public static FontRectangle MeasureRenderableBounds(string text, TextOptions options)
        => MeasureRenderableBounds(text.AsSpan(), options);

    /// <summary>
    /// Measures the rendered glyph bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The rendered glyph bounds of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        TextBlock block = new(text, options);
        return block.MeasureBounds(options.WrappingLength);
    }

    /// <summary>
    /// Measures the full renderable bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>
    /// The union of the logical advance rectangle and the rendered glyph bounds if the text was to be rendered.
    /// </returns>
    public static FontRectangle MeasureRenderableBounds(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        TextBlock block = new(text, options);
        return block.MeasureRenderableBounds(options.WrappingLength);
    }

    /// <inheritdoc cref="MeasureGlyphAdvances(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphAdvances(string text, TextOptions options)
        => MeasureGlyphAdvances(text.AsSpan(), options);

    /// <summary>
    /// Measures the positioned logical advance bounds of each laid-out glyph entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The list of per-entry positioned logical advance bounds of the text if it was to be rendered.</returns>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphAdvances(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.MeasureGlyphAdvances(options.WrappingLength);
    }

    /// <inheritdoc cref="MeasureGlyphBounds(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphBounds(string text, TextOptions options)
        => MeasureGlyphBounds(text.AsSpan(), options);

    /// <inheritdoc cref="MeasureGlyphRenderableBounds(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphRenderableBounds(string text, TextOptions options)
        => MeasureGlyphRenderableBounds(text.AsSpan(), options);

    /// <summary>
    /// Measures the rendered glyph bounds of each laid-out glyph entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The list of per-entry rendered glyph bounds of the text if it was to be rendered.</returns>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphBounds(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.MeasureGlyphBounds(options.WrappingLength);
    }

    /// <summary>
    /// Measures the full renderable bounds of each laid-out glyph entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The list of per-entry renderable bounds of the text if it was to be rendered.</returns>
    public static ReadOnlySpan<GlyphBounds> MeasureGlyphRenderableBounds(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.MeasureGlyphRenderableBounds(options.WrappingLength);
    }

    /// <inheritdoc cref="GetGraphemeMetrics(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlySpan<GraphemeMetrics> GetGraphemeMetrics(string text, TextOptions options)
        => GetGraphemeMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets the positioned metrics of each laid-out grapheme in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The list of per-grapheme metrics entries of the text if it was to be rendered.</returns>
    public static ReadOnlySpan<GraphemeMetrics> GetGraphemeMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.GetGraphemeMetrics(options.WrappingLength);
    }

    /// <inheritdoc cref="CountLines(ReadOnlySpan{char}, TextOptions)"/>
    public static int CountLines(string text, TextOptions options)
        => CountLines(text.AsSpan(), options);

    /// <summary>
    /// Gets the number of laid-out lines contained within the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The laid-out line count.</returns>
    public static int CountLines(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        TextBlock block = new(text, options);
        return block.CountLines(options.WrappingLength);
    }

    /// <inheritdoc cref="GetLineMetrics(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlySpan<LineMetrics> GetLineMetrics(string text, TextOptions options)
        => GetLineMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets per-line layout metrics for the supplied text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>
    /// A collection of <see cref="LineMetrics"/> in pixel units, one entry per laid-out line.
    /// </returns>
    public static ReadOnlySpan<LineMetrics> GetLineMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.GetLineMetrics(options.WrappingLength);
    }
}

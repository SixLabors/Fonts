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
    /// <returns>A <see cref="TextMetrics"/> value containing every measurement for the laid-out text.</returns>
    public static TextMetrics Measure(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return TextMetrics.Empty;
        }

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

    /// <inheritdoc cref="MeasureSize(ReadOnlySpan{char}, TextOptions)"/>
    public static FontRectangle MeasureSize(string text, TextOptions options)
        => MeasureSize(text.AsSpan(), options);

    /// <summary>
    /// Measures the normalized rendered size of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>The rendered size of the text with the origin normalized to <c>(0, 0)</c>.</returns>
    public static FontRectangle MeasureSize(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        TextBlock block = new(text, options);
        return block.MeasureSize(options.WrappingLength);
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

    /// <inheritdoc cref="TryMeasureCharacterAdvances(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/>
    public static bool TryMeasureCharacterAdvances(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
        => TryMeasureCharacterAdvances(text.AsSpan(), options, out advances);

    /// <summary>
    /// Measures the logical advance of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <param name="advances">The list of per-entry logical advances of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty advances.</returns>
    public static bool TryMeasureCharacterAdvances(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
    {
        if (text.IsEmpty)
        {
            advances = [];
            return false;
        }

        TextBlock block = new(text, options);
        return block.TryMeasureCharacterAdvances(options.WrappingLength, out advances);
    }

    /// <inheritdoc cref="TryMeasureCharacterSizes(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/>
    public static bool TryMeasureCharacterSizes(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
        => TryMeasureCharacterSizes(text.AsSpan(), options, out sizes);

    /// <summary>
    /// Measures the normalized rendered size of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <param name="sizes">The list of per-entry rendered sizes with the origin normalized to <c>(0, 0)</c>.</param>
    /// <returns>Whether any of the entries had non-empty dimensions.</returns>
    public static bool TryMeasureCharacterSizes(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
    {
        if (text.IsEmpty)
        {
            sizes = [];
            return false;
        }

        TextBlock block = new(text, options);
        return block.TryMeasureCharacterSizes(options.WrappingLength, out sizes);
    }

    /// <inheritdoc cref="TryMeasureCharacterBounds(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/>
    public static bool TryMeasureCharacterBounds(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryMeasureCharacterBounds(text.AsSpan(), options, out bounds);

    /// <inheritdoc cref="TryMeasureCharacterRenderableBounds(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/>
    public static bool TryMeasureCharacterRenderableBounds(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryMeasureCharacterRenderableBounds(text.AsSpan(), options, out bounds);

    /// <summary>
    /// Measures the rendered glyph bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <param name="bounds">The list of per-entry rendered glyph bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
    {
        if (text.IsEmpty)
        {
            bounds = [];
            return false;
        }

        TextBlock block = new(text, options);
        return block.TryMeasureCharacterBounds(options.WrappingLength, out bounds);
    }

    /// <summary>
    /// Measures the full renderable bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <param name="bounds">The list of per-entry renderable bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    public static bool TryMeasureCharacterRenderableBounds(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
    {
        if (text.IsEmpty)
        {
            bounds = [];
            return false;
        }

        TextBlock block = new(text, options);
        return block.TryMeasureCharacterRenderableBounds(options.WrappingLength, out bounds);
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
    public static LineMetrics[] GetLineMetrics(string text, TextOptions options)
        => GetLineMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets per-line layout metrics for the supplied text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>
    /// An array of <see cref="LineMetrics"/> in pixel units, one entry per laid-out line.
    /// </returns>
    public static LineMetrics[] GetLineMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextBlock block = new(text, options);
        return block.GetLineMetrics(options.WrappingLength);
    }
}

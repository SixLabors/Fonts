// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Rendering;

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

    /// <summary>
    /// Measures the logical advance of a single glyph, identified by its glyph id, in pixel units.
    /// </summary>
    /// <remarks>
    /// The advance is computed directly from the font's cached per-glyph metrics without decoding
    /// outlines or running the layout engine. Matching the text-level advance contract, the
    /// rectangle is zero-based: the advance width by the em height for horizontal layouts, and
    /// the advance the layout direction consumes for vertical layouts, independent of
    /// <see cref="GlyphOptions.Origin"/> and <see cref="GlyphOptions.TextBaseline"/>. Positioned
    /// geometry is reported by the bounds overloads.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>
    /// The logical advance rectangle of the glyph if it was to be rendered, or
    /// <see cref="FontRectangle.Empty"/> when the font does not contain the glyph or the glyph
    /// never renders.
    /// </returns>
    public static FontRectangle MeasureAdvance(ushort glyphId, GlyphOptions options)
    {
        Guard.NotNull(options, nameof(options));

        return TryGetMeasurableGlyphMetrics(glyphId, options, out FontGlyphMetrics? metrics)
            ? GetGlyphAdvance(metrics, options)
            : FontRectangle.Empty;
    }

    /// <summary>
    /// Measures the rendered bounds of a single glyph, identified by its glyph id, in pixel units.
    /// </summary>
    /// <remarks>
    /// The bounds are computed directly from the font's cached per-glyph metrics without decoding
    /// outlines or running the layout engine, and are identical to the bounding box the renderer
    /// reports for the same glyph and options through
    /// <see cref="TextRenderer.RenderTo(IGlyphRenderer, ushort, GlyphOptions)"/>.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>
    /// The rendered bounds of the glyph at <see cref="GlyphOptions.Origin"/> if it was to be
    /// rendered, or <see cref="FontRectangle.Empty"/> when the font does not contain the glyph or
    /// the glyph never renders.
    /// </returns>
    public static FontRectangle MeasureBounds(ushort glyphId, GlyphOptions options)
    {
        Guard.NotNull(options, nameof(options));

        return TryGetMeasurableGlyphMetrics(glyphId, options, out FontGlyphMetrics? metrics)
            ? GetGlyphBounds(metrics, options)
            : FontRectangle.Empty;
    }

    /// <summary>
    /// Measures the full renderable bounds of a single glyph, identified by its glyph id, in pixel units.
    /// </summary>
    /// <remarks>
    /// The bounds are computed directly from the font's cached per-glyph metrics without decoding
    /// outlines or running the layout engine.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>
    /// The union of the advance placed at <see cref="GlyphOptions.Origin"/> and the rendered
    /// bounds of the glyph if it was to be rendered, or <see cref="FontRectangle.Empty"/> when
    /// the font does not contain the glyph or the glyph never renders.
    /// </returns>
    public static FontRectangle MeasureRenderableBounds(ushort glyphId, GlyphOptions options)
    {
        Guard.NotNull(options, nameof(options));

        return TryGetMeasurableGlyphMetrics(glyphId, options, out FontGlyphMetrics? metrics)
            ? FontRectangle.Union(GetAbsoluteAdvance(metrics, options), GetGlyphBounds(metrics, options))
            : FontRectangle.Empty;
    }

    /// <summary>
    /// Measures the logical advance of positioned glyphs in pixel units.
    /// </summary>
    /// <remarks>
    /// Matching the text-level advance contract, the rectangle is zero-based: the extent the
    /// run's advance cells cover at their run origins, reported independent of position and of
    /// <see cref="GlyphOptions.TextBaseline"/>. Glyph ids the font does not contain and glyphs
    /// that never render are skipped, matching renderer behavior.
    /// </remarks>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <returns>
    /// The zero-based logical advance extent of the run if it was to be rendered, or
    /// <see cref="FontRectangle.Empty"/> when no glyph in the run participates in rendering.
    /// </returns>
    public static FontRectangle MeasureAdvance(GlyphRun glyphRun, GlyphOptions options)
    {
        // Match the text-level advance contract: measure the extent the positioned cells
        // cover, then report it zero-based.
        FontRectangle extent = MeasureGlyphRun(glyphRun, options, static (metrics, options) => GetAbsoluteAdvance(metrics, options));
        return new FontRectangle(0, 0, extent.Width, extent.Height);
    }

    /// <summary>
    /// Measures the union of rendered glyph bounds for positioned glyphs in pixel units.
    /// </summary>
    /// <remarks>
    /// Each glyph is measured at its own run origin exactly as
    /// <see cref="TextRenderer.RenderTo(IGlyphRenderer, GlyphRun, GlyphOptions)"/>
    /// renders it; <see cref="GlyphOptions.Origin"/> is replaced per glyph and restored. The
    /// result matches the union of the bounding boxes the renderer reports for the same run and
    /// options. Glyph ids the font does not contain and glyphs that never render are skipped,
    /// matching renderer behavior.
    /// </remarks>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <returns>
    /// The union of the rendered glyph bounds of the run if it was to be rendered, or
    /// <see cref="FontRectangle.Empty"/> when no glyph in the run participates in rendering.
    /// </returns>
    public static FontRectangle MeasureBounds(GlyphRun glyphRun, GlyphOptions options)
        => MeasureGlyphRun(glyphRun, options, static (metrics, options) => GetGlyphBounds(metrics, options));

    /// <summary>
    /// Measures the full renderable bounds of positioned glyphs in pixel units.
    /// </summary>
    /// <remarks>
    /// Each glyph is measured at its own run origin exactly as
    /// <see cref="TextRenderer.RenderTo(IGlyphRenderer, GlyphRun, GlyphOptions)"/>
    /// renders it; <see cref="GlyphOptions.Origin"/> is replaced per glyph and restored. Glyph ids
    /// the font does not contain and glyphs that never render are skipped, matching renderer
    /// behavior.
    /// </remarks>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <returns>
    /// The union of the advances placed at their run origins and the rendered glyph bounds of
    /// the run if it was to be rendered, or <see cref="FontRectangle.Empty"/> when no glyph in
    /// the run participates in rendering.
    /// </returns>
    public static FontRectangle MeasureRenderableBounds(GlyphRun glyphRun, GlyphOptions options)
        => MeasureGlyphRun(
            glyphRun,
            options,
            static (metrics, options) => FontRectangle.Union(GetAbsoluteAdvance(metrics, options), GetGlyphBounds(metrics, options)));

    /// <inheritdoc cref="GetGlyphMetrics(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlyMemory<GlyphMetrics> GetGlyphMetrics(string text, TextOptions options)
        => GetGlyphMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets the positioned metrics of each laid-out glyph entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing the per-glyph metrics entries of the text if it was to be rendered.</returns>
    public static ReadOnlyMemory<GlyphMetrics> GetGlyphMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return ReadOnlyMemory<GlyphMetrics>.Empty;
        }

        TextBlock block = new(text, options);
        return block.GetGlyphMetrics(options.WrappingLength);
    }

    /// <summary>
    /// Gets the positioned metrics of a single glyph, identified by its glyph id, in pixel units.
    /// </summary>
    /// <remarks>
    /// The metrics are computed directly from the font's cached per-glyph metrics without
    /// decoding outlines or running the layout engine, positioned at
    /// <see cref="GlyphOptions.Origin"/>. The entry's grapheme index is
    /// <see cref="GlyphOptions.GraphemeIndex"/> and its string index is <c>0</c>: glyph ids carry
    /// no source text, so the index identifies the glyph within the measured input instead. A
    /// glyph the font does not contain, or one that never renders, produces an entry with empty
    /// rectangles.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>The positioned metrics entry of the glyph if it was to be rendered.</returns>
    public static GlyphMetrics GetGlyphMetrics(ushort glyphId, GlyphOptions options)
    {
        Guard.NotNull(options, nameof(options));

        return CreateGlyphMetrics(glyphId, options, options.GraphemeIndex, index: 0);
    }

    /// <summary>
    /// Gets the positioned metrics of each glyph in a positioned run in pixel units.
    /// </summary>
    /// <remarks>
    /// The metrics are computed directly from the font's cached per-glyph metrics without
    /// decoding outlines or running the layout engine. Each glyph is measured at its own run
    /// origin exactly as
    /// <see cref="TextRenderer.RenderTo(IGlyphRenderer, GlyphRun, GlyphOptions)"/>
    /// renders it; <see cref="GlyphOptions.Origin"/> is replaced per glyph and restored. One
    /// entry is returned per input glyph so results correlate with run indices: each entry's
    /// grapheme index is <see cref="GlyphOptions.GraphemeIndex"/> plus the run index and its
    /// string index is the run index. Glyph ids the font does not contain, and glyphs that
    /// never render, produce entries with empty rectangles.
    /// </remarks>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <returns>A read-only memory region containing one positioned metrics entry per input glyph.</returns>
    public static ReadOnlyMemory<GlyphMetrics> GetGlyphMetrics(GlyphRun glyphRun, GlyphOptions options)
    {
        Guard.NotNull(glyphRun, nameof(glyphRun));
        Guard.NotNull(options, nameof(options));

        if (glyphRun.Count == 0)
        {
            return ReadOnlyMemory<GlyphMetrics>.Empty;
        }

        ReadOnlySpan<ushort> glyphIds = glyphRun.GlyphIds.Span;
        ReadOnlySpan<Vector2> origins = glyphRun.Origins.Span;
        Vector2 originalOrigin = options.Origin;
        int originalGraphemeIndex = options.GraphemeIndex;

        GlyphMetrics[] metrics = new GlyphMetrics[glyphIds.Length];
        try
        {
            for (int i = 0; i < glyphIds.Length; i++)
            {
                options.Origin = origins[i];
                metrics[i] = CreateGlyphMetrics(glyphIds[i], options, originalGraphemeIndex + i, i);
            }
        }
        finally
        {
            options.Origin = originalOrigin;
        }

        return metrics;
    }

    /// <inheritdoc cref="GetIntersections(ReadOnlySpan{char}, TextOptions, float, float)"/>
    public static ReadOnlyMemory<float> GetIntersections(string text, TextOptions options, float lowerLimit, float upperLimit)
        => GetIntersections(text.AsSpan(), options, lowerLimit, upperLimit);

    /// <summary>
    /// Gets the x-axis intervals where the laid-out text's glyph outlines cross a horizontal
    /// band, in pixel units.
    /// </summary>
    /// <remarks>
    /// The intervals are computed from the exact outline geometry the renderer would draw
    /// (including hinting), so text decorations can be broken precisely around descenders.
    /// Glyphs whose bounds do not touch the band skip outline decoding entirely. The band and
    /// the returned x-values share the laid-out text's coordinate space.
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <param name="lowerLimit">One edge of the horizontal band.</param>
    /// <param name="upperLimit">The other edge of the horizontal band.</param>
    /// <returns>
    /// A read-only memory region containing merged, x-sorted interval pairs
    /// (start, end, start, end, ...); empty when no outline crosses the band.
    /// </returns>
    public static ReadOnlyMemory<float> GetIntersections(ReadOnlySpan<char> text, TextOptions options, float lowerLimit, float upperLimit)
    {
        if (text.IsEmpty)
        {
            return ReadOnlyMemory<float>.Empty;
        }

        TextBlock block = new(text, options);
        return block.GetIntersections(options.WrappingLength, lowerLimit, upperLimit);
    }

    /// <summary>
    /// Gets the x-axis intervals where a single glyph's outline, identified by its glyph id,
    /// crosses a horizontal band, in pixel units.
    /// </summary>
    /// <remarks>
    /// The intervals are computed from the exact outline geometry the renderer would draw
    /// (including hinting) at <see cref="GlyphOptions.Origin"/>. The band and the returned
    /// x-values share the glyph origin's coordinate space.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <param name="lowerLimit">One edge of the horizontal band.</param>
    /// <param name="upperLimit">The other edge of the horizontal band.</param>
    /// <returns>
    /// A read-only memory region containing merged, x-sorted interval pairs
    /// (start, end, start, end, ...); empty when the outline does not cross the band.
    /// </returns>
    public static ReadOnlyMemory<float> GetIntersections(ushort glyphId, GlyphOptions options, float lowerLimit, float upperLimit)
    {
        Guard.NotNull(options, nameof(options));

        GlyphIntersectionCollector collector = new(lowerLimit, upperLimit);
        TextRenderer.RenderTo(collector, glyphId, options);
        return collector.BuildIntersections();
    }

    /// <summary>
    /// Gets the x-axis intervals where positioned glyph outlines cross a horizontal band,
    /// in pixel units.
    /// </summary>
    /// <remarks>
    /// The intervals are computed from the exact outline geometry the renderer would draw
    /// (including hinting), each glyph at its own run origin, so text decorations can be broken
    /// precisely around descenders. Glyphs whose bounds do not touch the band skip outline
    /// decoding entirely. The band and the returned x-values share the run origins' coordinate
    /// space.
    /// </remarks>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <param name="lowerLimit">One edge of the horizontal band.</param>
    /// <param name="upperLimit">The other edge of the horizontal band.</param>
    /// <returns>
    /// A read-only memory region containing merged, x-sorted interval pairs
    /// (start, end, start, end, ...); empty when no outline crosses the band.
    /// </returns>
    public static ReadOnlyMemory<float> GetIntersections(GlyphRun glyphRun, GlyphOptions options, float lowerLimit, float upperLimit)
    {
        Guard.NotNull(glyphRun, nameof(glyphRun));
        Guard.NotNull(options, nameof(options));

        GlyphIntersectionCollector collector = new(lowerLimit, upperLimit);
        TextRenderer.RenderTo(collector, glyphRun, options);
        return collector.BuildIntersections();
    }

    /// <inheritdoc cref="GetGraphemeMetrics(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlyMemory<GraphemeMetrics> GetGraphemeMetrics(string text, TextOptions options)
        => GetGraphemeMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets the positioned metrics of each laid-out grapheme in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing the per-grapheme metrics entries of the text if it was to be rendered.</returns>
    public static ReadOnlyMemory<GraphemeMetrics> GetGraphemeMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return ReadOnlyMemory<GraphemeMetrics>.Empty;
        }

        TextBlock block = new(text, options);
        return block.GetGraphemeMetrics(options.WrappingLength);
    }

    /// <inheritdoc cref="GetWordMetrics(ReadOnlySpan{char}, TextOptions)"/>
    public static ReadOnlyMemory<WordMetrics> GetWordMetrics(string text, TextOptions options)
        => GetWordMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets the positioned metrics of each Unicode word-boundary segment in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing the per-word-boundary segment metrics entries of the text if it was to be rendered.</returns>
    public static ReadOnlyMemory<WordMetrics> GetWordMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return ReadOnlyMemory<WordMetrics>.Empty;
        }

        TextBlock block = new(text, options);
        return block.GetWordMetrics(options.WrappingLength);
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
    public static ReadOnlyMemory<LineMetrics> GetLineMetrics(string text, TextOptions options)
        => GetLineMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets per-line layout metrics for the supplied text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    /// <returns>
    /// A read-only memory region containing <see cref="LineMetrics"/> in pixel units, one entry per laid-out line.
    /// </returns>
    public static ReadOnlyMemory<LineMetrics> GetLineMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return ReadOnlyMemory<LineMetrics>.Empty;
        }

        TextBlock block = new(text, options);
        return block.GetLineMetrics(options.WrappingLength);
    }

    /// <summary>
    /// Measures each positioned glyph of a run at its own origin and unions the results.
    /// Mirrors <see cref="TextRenderer.Render(GlyphRun, GlyphOptions)"/>:
    /// <see cref="GlyphOptions.Origin"/> is replaced per glyph and restored afterwards.
    /// </summary>
    /// <param name="glyphRun">The positioned glyphs.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <param name="measure">The per-glyph measurement to union.</param>
    /// <returns>The union of the per-glyph measurements, or <see cref="FontRectangle.Empty"/>.</returns>
    private static FontRectangle MeasureGlyphRun(
        GlyphRun glyphRun,
        GlyphOptions options,
        Func<FontGlyphMetrics, GlyphOptions, FontRectangle> measure)
    {
        Guard.NotNull(glyphRun, nameof(glyphRun));
        Guard.NotNull(options, nameof(options));

        ReadOnlySpan<ushort> glyphIds = glyphRun.GlyphIds.Span;
        ReadOnlySpan<Vector2> origins = glyphRun.Origins.Span;
        Vector2 originalOrigin = options.Origin;

        FontRectangle bounds = default;
        bool hasBounds = false;
        try
        {
            for (int i = 0; i < glyphIds.Length; i++)
            {
                if (!TryGetMeasurableGlyphMetrics(glyphIds[i], options, out FontGlyphMetrics? metrics))
                {
                    continue;
                }

                options.Origin = origins[i];
                FontRectangle glyphBounds = measure(metrics, options);
                bounds = hasBounds ? FontRectangle.Union(bounds, glyphBounds) : glyphBounds;
                hasBounds = true;
            }
        }
        finally
        {
            options.Origin = originalOrigin;
        }

        return hasBounds ? bounds : FontRectangle.Empty;
    }

    /// <summary>
    /// Resolves the cached per-glyph metrics that participate in rendering, mirroring the
    /// metric selection and skip rules the renderer applies so measurement and rendering
    /// always agree.
    /// </summary>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <param name="metrics">Receives the glyph metrics when the glyph participates in rendering.</param>
    /// <returns><see langword="true"/> when the glyph participates in rendering; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetMeasurableGlyphMetrics(
        ushort glyphId,
        GlyphOptions options,
        [NotNullWhen(true)] out FontGlyphMetrics? metrics)
    {
        if (!options.Font.FontMetrics.TryGetGlyphMetrics(
                glyphId,
                options.TextAttributes,
                options.TextDecorations,
                options.LayoutMode,
                options.ColorFontSupport,
                out metrics)
            || FontGlyphMetrics.ShouldSkipGlyphRendering(metrics.CodePoint))
        {
            metrics = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds one positioned metrics entry for a glyph id at <see cref="GlyphOptions.Origin"/>.
    /// Non-participating glyphs (ids the font does not contain, or glyphs that never render)
    /// produce an entry with empty rectangles so run results stay index-correlated.
    /// </summary>
    /// <param name="glyphId">The glyph identifier within the font face referenced by <paramref name="options"/>.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <param name="graphemeIndex">The grapheme index recorded on the entry.</param>
    /// <param name="index">The index of the glyph within the measured input, recorded as the entry's string index.</param>
    /// <returns>The positioned metrics entry.</returns>
    private static GlyphMetrics CreateGlyphMetrics(ushort glyphId, GlyphOptions options, int graphemeIndex, int index)
    {
        if (!TryGetMeasurableGlyphMetrics(glyphId, options, out FontGlyphMetrics? metrics))
        {
            return new GlyphMetrics(
                default,
                FontRectangle.Empty,
                FontRectangle.Empty,
                FontRectangle.Empty,
                options.Font,
                graphemeIndex,
                index);
        }

        FontRectangle advance = GetGlyphAdvance(metrics, options);
        FontRectangle bounds = GetGlyphBounds(metrics, options);
        return new GlyphMetrics(
            metrics.CodePoint,
            advance,
            bounds,
            FontRectangle.Union(GetAbsoluteAdvance(metrics, options), bounds),
            options.Font,
            graphemeIndex,
            index);
    }

    /// <summary>
    /// Applies the configured baseline anchor to a glyph origin in pixel units, matching the
    /// shift the renderer applies before positioning the glyph.
    /// </summary>
    /// <param name="options">The glyph options.</param>
    /// <param name="layoutMode">The resolved per-glyph layout mode.</param>
    /// <returns>The anchored origin.</returns>
    private static Vector2 GetAnchoredOrigin(GlyphOptions options, GlyphLayoutMode layoutMode)
    {
        // Mirror the renderer's exact operation order (normalize to layout units, apply the
        // combined anchor and baseline-shift offset, convert back to pixels) so measured
        // bounds stay bit-identical to rendered bounds.
        Vector2 origin = options.Origin / options.Dpi;
        if (layoutMode == GlyphLayoutMode.Horizontal)
        {
            origin.Y -= TextLayout.GetBaselineOffset(options, false);
        }
        else
        {
            origin.X -= TextLayout.GetBaselineOffset(options, true);
        }

        return origin * options.Dpi;
    }

    /// <summary>
    /// Computes one glyph's rendered bounds at <see cref="GlyphOptions.Origin"/>: the same
    /// per-glyph layout mode and scaled size feed the same bounding-box computation the
    /// renderer hands to <see cref="IGlyphRenderer.BeginGlyph"/>.
    /// </summary>
    /// <param name="metrics">The glyph metrics.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>The rendered glyph bounds.</returns>
    private static FontRectangle GetGlyphBounds(FontGlyphMetrics metrics, GlyphOptions options)
    {
        GlyphLayoutMode layoutMode = options.GetGlyphLayoutMode(metrics.CodePoint);
        return metrics.GetBoundingBox(
            layoutMode,
            GetAnchoredOrigin(options, layoutMode),
            metrics.GetScaledSize(options.Font.Size, options.Dpi));
    }

    /// <summary>
    /// Computes one glyph's logical advance: a zero-based measure of the space the glyph
    /// consumes, matching the text-level advance contract. The origin and baseline anchoring
    /// never move it; positioned geometry is reported by the bounds overloads.
    /// </summary>
    /// <param name="metrics">The glyph metrics.</param>
    /// <param name="options">The glyph options, including the font and layout mode.</param>
    /// <returns>The zero-based logical advance rectangle.</returns>
    private static FontRectangle GetGlyphAdvance(FontGlyphMetrics metrics, GlyphOptions options)
    {
        float scaledSize = metrics.GetScaledSize(options.Font.Size, options.Dpi);
        Vector2 scale = new(scaledSize / metrics.ScaleFactor.X, scaledSize / metrics.ScaleFactor.Y);
        float emHeight = metrics.UnitsPerEm * scale.Y;

        switch (options.GetGlyphLayoutMode(metrics.CodePoint))
        {
            case GlyphLayoutMode.Vertical:
                return new FontRectangle(0, 0, metrics.AdvanceWidth * scale.X, metrics.AdvanceHeight * scale.Y);
            case GlyphLayoutMode.VerticalRotated:
                // A rotated glyph advances along the column by its horizontal advance and its
                // line box lies across the column.
                return new FontRectangle(0, 0, emHeight, metrics.AdvanceWidth * scale.X);
            default:
                return new FontRectangle(0, 0, metrics.AdvanceWidth * scale.X, emHeight);
        }
    }

    /// <summary>
    /// Places a glyph's zero-based advance at <see cref="GlyphOptions.Origin"/>, mirroring the
    /// absolute-advance composition the text-level renderable bounds use.
    /// </summary>
    /// <param name="metrics">The glyph metrics.</param>
    /// <param name="options">The glyph options, including the font, origin, and layout mode.</param>
    /// <returns>The advance rectangle placed at the origin.</returns>
    private static FontRectangle GetAbsoluteAdvance(FontGlyphMetrics metrics, GlyphOptions options)
    {
        FontRectangle advance = GetGlyphAdvance(metrics, options);
        return new FontRectangle(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
    }
}

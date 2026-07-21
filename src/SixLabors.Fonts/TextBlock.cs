// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts;

/// <summary>
/// Represents text prepared for repeated line layout, measurement, and rendering.
/// </summary>
/// <remarks>
/// Instances are safe to measure and render concurrently. The block retains the most recent
/// line-broken layout; concurrent calls can only duplicate deterministic layout work, never
/// observe partial state.
/// </remarks>
public sealed partial class TextBlock
{
    /// <summary>
    /// The most recent line-broken layout, retained so repeated measurement and rendering at an
    /// unchanged wrapping length (the common redraw case) skip the full line-breaking rebuild.
    /// Published with volatile handoff; a concurrent race only duplicates deterministic work.
    /// </summary>
    private CachedTextLayout? cachedLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    /// <param name="text">The text to prepare.</param>
    /// <param name="options">The text options used to prepare, measure, and render the block.</param>
    /// <remarks>
    /// <see cref="TextOptions.WrappingLength"/> and <see cref="TextOptions.VisibleBounds"/> are ignored while
    /// preparing the block; pass the wrapping length (use <c>-1</c> to disable wrapping) and any visible
    /// bounds to the measurement or rendering methods.
    /// </remarks>
    public TextBlock(string text, TextOptions options)
        : this(text.AsSpan(), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    /// <param name="text">The text to prepare.</param>
    /// <param name="options">The text options used to prepare, measure, and render the block.</param>
    /// <remarks>
    /// <see cref="TextOptions.WrappingLength"/> and <see cref="TextOptions.VisibleBounds"/> are ignored while
    /// preparing the block; pass the wrapping length (use <c>-1</c> to disable wrapping) and any visible
    /// bounds to the measurement or rendering methods.
    /// </remarks>
    public TextBlock(ReadOnlySpan<char> text, TextOptions options)
    {
        this.Options = options;

        if (text.IsEmpty)
        {
            this.LogicalLine = new(new TextLine(), [], [], []);
            return;
        }

        ShapedText shaped = TextLayout.ShapeText(text, options);
        this.LogicalLine = TextLayout.ComposeLogicalLine(shaped, text, options);
    }

    /// <summary>
    /// Gets the text options used by this block.
    /// </summary>
    internal TextOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether underline and overline decorations skip over glyph ink
    /// when this block is rendered.
    /// </summary>
    public TextDecorationSkipInk TextDecorationSkipInk => this.Options.TextDecorationSkipInk;

    /// <summary>
    /// Gets the prepared logical line and line break opportunities.
    /// </summary>
    internal LogicalTextLine LogicalLine { get; }

    /// <summary>
    /// Breaks this block into lines for the supplied wrapping length.
    /// </summary>
    /// <remarks>
    /// The result is retained for the most recent wrapping length, so repeated measurement and
    /// rendering at one length share a single line-breaking pass.
    /// </remarks>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The line-broken text box.</returns>
    internal TextBox BreakLines(float wrappingLength)
        => this.GetOrCreateLayout(wrappingLength).TextBox;

    /// <summary>
    /// Gets the retained layout for the supplied wrapping length, or line-breaks and retains a new one.
    /// </summary>
    /// <remarks>
    /// The retained <see cref="TextBox"/> has its lazy aggregates computed before publication so
    /// concurrent readers never mutate shared state after the handoff. Concurrent calls with
    /// different wrapping lengths recompute deterministically; the last writer's layout stays
    /// retained.
    /// </remarks>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The layout for the supplied wrapping length.</returns>
    private CachedTextLayout GetOrCreateLayout(float wrappingLength)
    {
        CachedTextLayout? cached = Volatile.Read(ref this.cachedLayout);
        if (cached is not null && cached.WrappingLength == wrappingLength)
        {
            return cached;
        }

        TextBox textBox = TextLayout.BreakLines(this.LogicalLine, this.Options, wrappingLength);

        // Compute the box's memoized aggregates now: after publication the box is shared across
        // calls (and potentially threads), and lazy initialization on a shared instance would
        // race. The aggregate methods throw on an empty line list, so guard that case.
        if (textBox.TextLines.Count > 0)
        {
            _ = textBox.ScaledMaxAdvance();
            _ = textBox.ScaledMinY();
        }

        _ = textBox.CountGlyphLayouts();

        CachedTextLayout created = new(wrappingLength, textBox);
        Volatile.Write(ref this.cachedLayout, created);
        return created;
    }

    /// <summary>
    /// Gets the rendered bounds for a retained layout, computing and publishing them on first use.
    /// </summary>
    /// <param name="layout">The retained layout.</param>
    /// <param name="wrappingLength">The wrapping length the layout was produced for.</param>
    /// <returns>The rendered glyph bounds.</returns>
    private FontRectangle GetOrComputeBounds(CachedTextLayout layout, float wrappingLength)
    {
        if (layout.TryGetBounds(out FontRectangle bounds))
        {
            return bounds;
        }

        bounds = GetBounds(layout.TextBox, this.Options, wrappingLength);
        layout.SetBounds(in bounds);
        return bounds;
    }

    /// <summary>
    /// Measures the full set of layout metrics for this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A <see cref="TextMetrics"/> instance containing every measurement for the laid-out text.</returns>
    public TextMetrics Measure(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        float dpi = this.Options.Dpi;
        bool isHorizontal = this.Options.LayoutMode.IsHorizontal();

        FontRectangle advance = GetAdvance(textBox, dpi, isHorizontal);

        GraphemeMetrics[] graphemes = new GraphemeMetrics[CountGraphemeMetrics(textBox)];
        WordMetrics[] wordMetrics = new WordMetrics[this.LogicalLine.WordSegments.Count];

        GraphemeAndWordMetricsVisitor visitor = new(dpi, graphemes, this.LogicalLine.WordSegments, wordMetrics);
        TextLayout.LayoutText(textBox, this.Options, wrappingLength, ref visitor);

        FontRectangle bounds = visitor.Bounds();
        FontRectangle absoluteAdvance = new(this.Options.Origin.X, this.Options.Origin.Y, advance.Width, advance.Height);
        FontRectangle renderableBounds = FontRectangle.Union(absoluteAdvance, bounds);

        LineMetrics[] lineMetrics = GetLineMetrics(textBox, this.Options, wrappingLength);

        return new TextMetrics(
            this,
            textBox,
            wrappingLength,
            advance,
            bounds,
            renderableBounds,
            textBox.TextLines.Count,
            graphemes,
            lineMetrics,
            wordMetrics);
    }

    /// <summary>
    /// Measures the logical advance of this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The logical advance rectangle.</returns>
    public FontRectangle MeasureAdvance(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        return GetAdvance(textBox, this.Options.Dpi, this.Options.LayoutMode.IsHorizontal());
    }

    /// <summary>
    /// Measures the rendered glyph bounds of this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The rendered glyph bounds.</returns>
    public FontRectangle MeasureBounds(float wrappingLength)
    {
        CachedTextLayout layout = this.GetOrCreateLayout(wrappingLength);
        return this.GetOrComputeBounds(layout, wrappingLength);
    }

    /// <summary>
    /// Measures the union of logical advance and rendered glyph bounds at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The full renderable bounds.</returns>
    public FontRectangle MeasureRenderableBounds(float wrappingLength)
    {
        CachedTextLayout layout = this.GetOrCreateLayout(wrappingLength);
        TextBox textBox = layout.TextBox;
        FontRectangle advance = GetAdvance(textBox, this.Options.Dpi, this.Options.LayoutMode.IsHorizontal());
        FontRectangle absoluteAdvance = new(this.Options.Origin.X, this.Options.Origin.Y, advance.Width, advance.Height);
        FontRectangle bounds = this.GetOrComputeBounds(layout, wrappingLength);
        return FontRectangle.Union(absoluteAdvance, bounds);
    }

    /// <summary>
    /// Gets the positioned metrics of each laid-out glyph entry.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing per-glyph metrics entries.</returns>
    public ReadOnlyMemory<GlyphMetrics> GetGlyphMetrics(float wrappingLength)
        => this.GetGlyphMetricsArray(wrappingLength);

    /// <summary>
    /// Gets the x-axis intervals where the laid-out glyph outlines cross a horizontal band.
    /// Glyphs whose bounds do not touch the band skip outline decoding entirely.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="lowerLimit">One edge of the horizontal band.</param>
    /// <param name="upperLimit">The other edge of the horizontal band.</param>
    /// <returns>
    /// A read-only memory region containing merged, x-sorted interval pairs
    /// (start, end, start, end, ...); empty when no outline crosses the band.
    /// </returns>
    public ReadOnlyMemory<float> GetIntersections(float wrappingLength, float lowerLimit, float upperLimit)
    {
        GlyphIntersectionCollector collector = new(lowerLimit, upperLimit);
        this.RenderTo(collector, wrappingLength);
        return collector.BuildIntersections();
    }

    /// <summary>
    /// Gets the positioned metrics of each laid-out grapheme.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing per-grapheme metrics entries.</returns>
    public ReadOnlyMemory<GraphemeMetrics> GetGraphemeMetrics(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        return GetGraphemeMetricsArray(textBox, this.Options, wrappingLength);
    }

    /// <summary>
    /// Gets the positioned metrics of each Unicode word-boundary segment.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing per-word-boundary segment metrics entries.</returns>
    public ReadOnlyMemory<WordMetrics> GetWordMetrics(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        WordMetrics[] wordMetrics = new WordMetrics[this.LogicalLine.WordSegments.Count];
        WordMetricsVisitor visitor = new(this.LogicalLine.WordSegments, wordMetrics, this.Options.Dpi);
        TextLayout.LayoutText(textBox, this.Options, wrappingLength, ref visitor);
        return wordMetrics;
    }

    /// <summary>
    /// Gets the number of laid-out lines at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The laid-out line count.</returns>
    public int CountLines(float wrappingLength)
        => this.BreakLines(wrappingLength).TextLines.Count;

    /// <summary>
    /// Gets per-line layout metrics at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing <see cref="LineMetrics"/> in pixel units.</returns>
    public ReadOnlyMemory<LineMetrics> GetLineMetrics(float wrappingLength)
        => GetLineMetrics(this.BreakLines(wrappingLength), this.Options, wrappingLength);

    /// <summary>
    /// Gets visual line layouts for this block at the supplied wrapping length.
    /// </summary>
    /// <remarks>
    /// The returned memory contains every laid-out line, including lines produced by hard line breaks.
    /// </remarks>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A read-only memory region containing <see cref="LineLayout"/> entries in final layout order.</returns>
    public ReadOnlyMemory<LineLayout> GetLineLayouts(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        if (textBox.TextLines.Count == 0)
        {
            return ReadOnlyMemory<LineLayout>.Empty;
        }

        return this.GetLineLayouts(textBox, wrappingLength);
    }

    /// <summary>
    /// Creates an enumerator that lays out this block one line at a time.
    /// </summary>
    /// <returns>A line layout enumerator for this block.</returns>
    public LineLayoutEnumerator EnumerateLineLayouts()
        => new(this);

    /// <summary>
    /// Gets a single line layout for an already line-broken text line.
    /// </summary>
    /// <param name="textLine">The line to lay out.</param>
    /// <param name="wrappingLength">The wrapping length in pixels.</param>
    /// <param name="textDirection">The block-level text direction used for alignment.</param>
    /// <returns>The line layout for the supplied line.</returns>
    internal LineLayout GetLineLayout(
        TextLine textLine,
        float wrappingLength,
        TextDirection textDirection)
    {
        TextBox textBox = new([textLine], textDirection);

        return this.GetLineLayouts(textBox, wrappingLength)[0];
    }

    /// <summary>
    /// Gets visual line layouts for an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="wrappingLength">The wrapping length in pixels.</param>
    /// <returns>The line layouts for the supplied text box.</returns>
    private LineLayout[] GetLineLayouts(TextBox textBox, float wrappingLength)
    {
        GraphemeMetrics[] graphemes = new GraphemeMetrics[CountGraphemeMetrics(textBox)];
        LineMetrics[] metrics = GetLineMetrics(textBox, this.Options, wrappingLength);
        LineLayout[] lines = new LineLayout[textBox.TextLines.Count];

        WordMetrics[] wordMetrics = new WordMetrics[this.LogicalLine.WordSegments.Count];
        LineLayoutVisitor visitor = new(textBox, this.Options, wrappingLength, graphemes, metrics, lines, this.LogicalLine.WordSegments, wordMetrics, this.Options.Dpi);
        TextLayout.LayoutText(textBox, this.Options, wrappingLength, ref visitor);

        return lines;
    }

    /// <summary>
    /// Renders this block to the supplied glyph renderer at the supplied wrapping length.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    public void RenderTo(IGlyphRenderer renderer, float wrappingLength)
    {
        // Repeated rendering at one wrapping length reuses both the retained line-broken box and
        // its rendered bounds, so a warm call performs exactly one layout pass.
        CachedTextLayout layout = this.GetOrCreateLayout(wrappingLength);
        FontRectangle rect = this.GetOrComputeBounds(layout, wrappingLength);

        RenderTo(renderer, layout.TextBox, this.Options, wrappingLength, rect);
    }

    /// <summary>
    /// Renders the visible portion of this block to the supplied glyph renderer at the supplied
    /// wrapping length, culling whole lines that lie outside the supplied visible bounds.
    /// </summary>
    /// <remarks>
    /// Culling is line-granular: every glyph on a line intersecting <paramref name="visibleBounds"/>
    /// is rendered, and the comparison inflates each line box by one line height on each side so
    /// ink or decorations overhanging a line box never disappear at the band edges. A culled line
    /// still advances layout, so visible lines render at positions identical to a full render, and
    /// <see cref="IGlyphRenderer.BeginText"/> always receives the full text bounds.
    /// </remarks>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="visibleBounds">The visible region in pixels, in the same space as the rendered output.</param>
    public void RenderTo(IGlyphRenderer renderer, float wrappingLength, in FontRectangle visibleBounds)
    {
        CachedTextLayout layout = this.GetOrCreateLayout(wrappingLength);
        FontRectangle rect = this.GetOrComputeBounds(layout, wrappingLength);

        // The layout walk runs in layout units (pixels divided by DPI) and culls along the block
        // flow axis: Y for horizontal layouts, X for vertical layouts.
        float dpi = this.Options.Dpi;
        bool isHorizontal = this.Options.LayoutMode.IsHorizontal();
        float visibleFlowMin = (isHorizontal ? visibleBounds.Top : visibleBounds.Left) / dpi;
        float visibleFlowMax = (isHorizontal ? visibleBounds.Bottom : visibleBounds.Right) / dpi;

        renderer.BeginText(in rect);

        GlyphRendererVisitor visitor = new(renderer, this.Options, -1);
        TextLayout.LayoutText(layout.TextBox, this.Options, wrappingLength, visibleFlowMin, visibleFlowMax, ref visitor);

        renderer.EndText();
    }

    /// <summary>
    /// Renders an already line-broken text box to the supplied glyph renderer.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for rendering.</param>
    /// <param name="wrappingLength">The wrapping length in pixels.</param>
    /// <param name="bounds">The bounds passed to the renderer.</param>
    /// <param name="lineIndex">The line index to render, or <c>-1</c> to render every line.</param>
    internal static void RenderTo(
        IGlyphRenderer renderer,
        TextBox textBox,
        TextOptions options,
        float wrappingLength,
        in FontRectangle bounds,
        int lineIndex = -1)
    {
        renderer.BeginText(in bounds);

        GlyphRendererVisitor visitor = new(renderer, options, lineIndex);
        TextLayout.LayoutText(textBox, options, wrappingLength, ref visitor);

        renderer.EndText();
    }

    /// <summary>
    /// Measures the rendered glyph bounds of an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The union of the rendered glyph bounds.</returns>
    private static FontRectangle GetBounds(TextBox textBox, TextOptions options, float wrappingLength)
    {
        if (textBox.TextLines.Count == 0)
        {
            return FontRectangle.Empty;
        }

        RenderedRectangleAccumulator visitor = new(options.Dpi);
        TextLayout.LayoutText(textBox, options, wrappingLength, ref visitor);
        return visitor.Result();
    }

    /// <summary>
    /// Gets per-line layout metrics for an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used to calculate line metrics.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>An array of <see cref="LineMetrics"/> in pixel units.</returns>
    private static LineMetrics[] GetLineMetrics(TextBox textBox, TextOptions options, float wrappingLength)
    {
        if (textBox.TextLines.Count == 0)
        {
            return [];
        }

        LineMetrics[] metrics = new LineMetrics[textBox.TextLines.Count];

        // Determine the line-box extent used for alignment within the flow direction.
        float maxScaledAdvance = textBox.ScaledMaxAdvance();
        if (options.TextAlignment != TextAlignment.Start && wrappingLength > 0)
        {
            maxScaledAdvance = MathF.Max(wrappingLength / options.Dpi, maxScaledAdvance);
        }

        TextDirection direction = textBox.TextDirection();
        LayoutMode layoutMode = options.LayoutMode;

        bool isHorizontalLayout = layoutMode.IsHorizontal();
        float lineOffset = isHorizontalLayout ? options.Origin.Y : options.Origin.X;

        bool reverseLineOrder = layoutMode is
            LayoutMode.HorizontalBottomTop
            or LayoutMode.VerticalRightLeft
            or LayoutMode.VerticalMixedRightLeft;

        if (options.TextBaseline != TextBaseline.LineBox)
        {
            // Baseline anchoring places the first laid-out line's selected reference line on
            // the origin; shift the reported line starts by the matching amount so metrics
            // agree with rendering.
            TextLine anchorLine = textBox.TextLines[reverseLineOrder ? textBox.TextLines.Count - 1 : 0];
            if (isHorizontalLayout)
            {
                float anchorDelta = anchorLine.ScaledMaxDelta;
                float anchorCore = anchorLine.ScaledMaxAscender + anchorLine.ScaledMaxDescender + (2 * anchorDelta);
                float anchorExtra = anchorLine.ScaledMaxLineHeight - anchorCore;
                float anchorBaseline = (anchorExtra * .5F) + anchorLine.ScaledMaxAscender + anchorDelta;
                lineOffset -= (anchorBaseline + TextLayout.GetBaselineOffset(options.TextBaseline, options.Font, false)) * options.Dpi;
            }
            else
            {
                // Columns anchor their central axis, half the unscaled column width from the pen.
                float anchorCentral = anchorLine.ScaledMaxLineHeight / options.LineSpacing * .5F;
                lineOffset -= (anchorCentral + TextLayout.GetBaselineOffset(options.TextBaseline, options.Font, true)) * options.Dpi;
            }
        }

        int i = reverseLineOrder ? textBox.TextLines.Count - 1 : 0;
        int step = reverseLineOrder ? -1 : 1;
        int graphemeOffset = 0;

        while (i >= 0 && i < textBox.TextLines.Count)
        {
            TextLine line = textBox.TextLines[i];

            // Calculate the line start position in the current flow direction.
            float offset = isHorizontalLayout
                ? TextLayout.CalculateLineOffsetX(
                    line.ScaledLineAdvance,
                    maxScaledAdvance,
                    options.HorizontalAlignment,
                    options.TextAlignment,
                    direction)
                : TextLayout.CalculateLineOffsetY(
                    line.ScaledLineAdvance,
                    maxScaledAdvance,
                    options.VerticalAlignment,
                    options.TextAlignment,
                    direction);

            // Delta captured during layout when ascender/descender were symmetrically
            // adjusted to match browser-like line-box behavior.
            float delta = line.ScaledMaxDelta;

            // Core typographic region within the line box.
            // We add back 2*delta to recover the pre-adjustment ascender+descender span
            // used for deriving guide positions.
            float coreHeight = line.ScaledMaxAscender + line.ScaledMaxDescender + (2 * delta);

            // Additional leading in the line box (for example from line spacing).
            float extra = line.ScaledMaxLineHeight - coreHeight;

            // Baseline position within the line box.
            float baseline = (extra * 0.5f) + line.ScaledMaxAscender + delta;

            // Ascender line position relative to the same origin.
            float ascender = baseline - line.ScaledMaxAscender + delta;

            // Descender line position relative to the same origin.
            float descender = baseline + line.ScaledMaxDescender + delta;
            Vector2 start = isHorizontalLayout
                ? new(options.Origin.X + (offset * options.Dpi), lineOffset)
                : new(lineOffset, options.Origin.Y + (offset * options.Dpi));

            Vector2 extent = isHorizontalLayout
                ? new(line.ScaledLineAdvance * options.Dpi, line.ScaledMaxLineHeight * options.Dpi)
                : new(line.ScaledMaxLineHeight * options.Dpi, line.ScaledLineAdvance * options.Dpi);

            // Bidi reordering mutates entries into visual order, so the source
            // start is the minimum original source index rather than line[0].
            int stringIndex = line[0].StringIndex;
            int graphemeIndex = line[0].GraphemeIndex;
            for (int j = 1; j < line.Count; j++)
            {
                stringIndex = Math.Min(stringIndex, line[j].StringIndex);
                graphemeIndex = Math.Min(graphemeIndex, line[j].GraphemeIndex);
            }

            metrics[i] = new LineMetrics(
                ascender * options.Dpi,
                baseline * options.Dpi,
                descender * options.Dpi,
                line.ScaledMaxLineHeight * options.Dpi,
                start,
                extent,
                stringIndex,
                graphemeIndex,
                line.GraphemeCount,
                graphemeOffset);

            graphemeOffset += line.GraphemeCount;
            lineOffset += line.ScaledMaxLineHeight * options.Dpi;
            i += step;
        }

        return metrics;
    }

    /// <summary>
    /// Counts grapheme metrics entries across all lines in an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <returns>The number of grapheme metrics entries.</returns>
    private static int CountGraphemeMetrics(TextBox textBox)
    {
        int count = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            count += textBox.TextLines[i].GraphemeCount;
        }

        return count;
    }

    /// <summary>
    /// Gets grapheme metrics entries by streaming laid-out glyphs.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The grapheme metrics entries.</returns>
    internal static GraphemeMetrics[] GetGraphemeMetricsArray(
        TextBox textBox,
        TextOptions options,
        float wrappingLength)
    {
        int count = CountGraphemeMetrics(textBox);
        if (count == 0)
        {
            return [];
        }

        GraphemeMetrics[] graphemes = new GraphemeMetrics[count];
        GraphemeMetricsVisitor visitor = new(options.Dpi, graphemes);
        TextLayout.LayoutText(textBox, options, wrappingLength, ref visitor);
        return graphemes;
    }

    /// <summary>
    /// Finds the source-order word-boundary range containing the supplied grapheme index.
    /// </summary>
    /// <param name="wordSegments">The source-order word-boundary segments.</param>
    /// <param name="graphemeIndex">The grapheme index to locate.</param>
    /// <returns>The matching word metrics index, or <c>-1</c> when no range contains the grapheme.</returns>
    private static int FindWordMetricIndex(List<WordSegmentRun> wordSegments, int graphemeIndex)
    {
        int min = 0;
        int max = wordSegments.Count - 1;
        while (min <= max)
        {
            int mid = (min + max) >> 1;
            WordSegmentRun segment = wordSegments[mid];
            if (graphemeIndex < segment.GraphemeStart)
            {
                max = mid - 1;
                continue;
            }

            if (graphemeIndex >= segment.GraphemeEnd)
            {
                min = mid + 1;
                continue;
            }

            return mid;
        }

        return -1;
    }

    /// <summary>
    /// Gets a value indicating whether positioned metrics have been added to a word segment.
    /// </summary>
    /// <param name="metrics">The word metrics to inspect.</param>
    /// <returns><see langword="true"/> when a grapheme has been accumulated for the segment.</returns>
    private static bool HasWordMetrics(in WordMetrics metrics)

        // Default WordMetrics has no source range. Any real word segment has an exclusive end
        // index, so the range is the sentinel that avoids treating FontRectangle.Empty as geometry.
        => metrics.GraphemeEnd != 0 || metrics.StringEnd != 0;

    /// <summary>
    /// Gets one per-glyph metrics collection by streaming laid-out glyphs.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The positioned glyph metrics.</returns>
    internal GlyphMetrics[] GetGlyphMetricsArray(float wrappingLength)
    {
        TextBox textBox = this.BreakLines(wrappingLength);
        return GetGlyphMetricsArray(textBox, this.Options, wrappingLength);
    }

    /// <summary>
    /// Gets one per-glyph metrics collection by streaming laid-out glyphs.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="lineIndex">The line index to collect, or <c>-1</c> to collect every line.</param>
    /// <returns>The positioned glyph metrics.</returns>
    internal static GlyphMetrics[] GetGlyphMetricsArray(
        TextBox textBox,
        TextOptions options,
        float wrappingLength,
        int lineIndex = -1)
    {
        int count = lineIndex < 0 ? textBox.CountGlyphLayouts() : textBox.TextLines[lineIndex].CountGlyphLayouts();
        if (count == 0)
        {
            return [];
        }

        GlyphMetrics[] result = new GlyphMetrics[count];
        GlyphMetricsVisitor visitor = new(result, options.Dpi, lineIndex);
        TextLayout.LayoutText(textBox, options, wrappingLength, ref visitor);
        return result;
    }

    /// <summary>
    /// Measures the logical advance of an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="dpi">The target DPI.</param>
    /// <param name="isHorizontalLayout">Whether the layout direction is horizontal.</param>
    /// <returns>The logical advance rectangle.</returns>
    internal static FontRectangle GetAdvance(TextBox textBox, float dpi, bool isHorizontalLayout)
    {
        if (textBox.TextLines.Count == 0)
        {
            return FontRectangle.Empty;
        }

        if (isHorizontalLayout)
        {
            float width = 0;
            float height = 0;
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                TextLine line = textBox.TextLines[i];
                width = MathF.Max(width, line.ScaledLineAdvance);
                height += line.ScaledMaxLineHeight;
            }

            return new FontRectangle(0, 0, width * dpi, height * dpi);
        }

        float verticalWidth = 0;
        float verticalHeight = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLine line = textBox.TextLines[i];
            verticalWidth += line.ScaledMaxLineHeight;
            verticalHeight = MathF.Max(verticalHeight, line.ScaledLineAdvance);
        }

        return new FontRectangle(0, 0, verticalWidth * dpi, verticalHeight * dpi);
    }

    /// <summary>
    /// Retains one line-broken layout and its lazily computed rendered bounds.
    /// </summary>
    private sealed class CachedTextLayout
    {
        /// <summary>
        /// The rendered glyph bounds; valid only after <see cref="hasBounds"/> is set.
        /// </summary>
        private FontRectangle bounds;

        /// <summary>
        /// Release/acquire gate for <see cref="bounds"/>: the volatile write in
        /// <see cref="SetBounds"/> publishes the fully written rectangle to readers.
        /// </summary>
        private volatile bool hasBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedTextLayout"/> class.
        /// </summary>
        /// <param name="wrappingLength">The wrapping length the layout was produced for.</param>
        /// <param name="textBox">The line-broken text box.</param>
        public CachedTextLayout(float wrappingLength, TextBox textBox)
        {
            this.WrappingLength = wrappingLength;
            this.TextBox = textBox;
        }

        /// <summary>
        /// Gets the wrapping length the layout was produced for.
        /// </summary>
        public float WrappingLength { get; }

        /// <summary>
        /// Gets the line-broken text box.
        /// </summary>
        public TextBox TextBox { get; }

        /// <summary>
        /// Tries to read the previously published rendered bounds.
        /// </summary>
        /// <param name="value">Receives the rendered bounds when available.</param>
        /// <returns><see langword="true"/> when bounds have been published.</returns>
        public bool TryGetBounds(out FontRectangle value)
        {
            if (this.hasBounds)
            {
                value = this.bounds;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Publishes the rendered bounds for this layout.
        /// </summary>
        /// <param name="value">The rendered bounds.</param>
        public void SetBounds(in FontRectangle value)
        {
            // The field write precedes the volatile flag write, so a reader observing the flag
            // also observes the completed rectangle. Concurrent writers store the same
            // deterministic value.
            this.bounds = value;
            this.hasBounds = true;
        }
    }
}

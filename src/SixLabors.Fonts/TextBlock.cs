// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts;

/// <summary>
/// Represents text prepared for repeated line layout, measurement, and rendering.
/// </summary>
public sealed partial class TextBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    /// <param name="text">The text to prepare.</param>
    /// <param name="options">The text options used to prepare, measure, and render the block.</param>
    /// <remarks>
    /// <see cref="TextOptions.WrappingLength"/> is ignored while preparing the block; pass the wrapping length
    /// to the measurement or rendering method. Use <c>-1</c> there to disable wrapping.
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
    /// <see cref="TextOptions.WrappingLength"/> is ignored while preparing the block; pass the wrapping length
    /// to the measurement or rendering method. Use <c>-1</c> there to disable wrapping.
    /// </remarks>
    public TextBlock(ReadOnlySpan<char> text, TextOptions options)
    {
        ShapedText shaped = TextLayout.ShapeText(text, options);
        this.LogicalLine = TextLayout.ComposeLogicalLine(shaped, text, options);
        this.Options = options;
    }

    /// <summary>
    /// Gets the text options used by this block.
    /// </summary>
    internal TextOptions Options { get; }

    /// <summary>
    /// Gets the prepared logical line and line break opportunities.
    /// </summary>
    internal LogicalTextLine LogicalLine { get; }

    /// <summary>
    /// Breaks this block into lines for the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The line-broken text box.</returns>
    internal TextLayout.TextBox BreakLines(float wrappingLength)
        => TextLayout.BreakLines(this.LogicalLine, this.Options, wrappingLength);

    /// <summary>
    /// Measures the full set of layout metrics for this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>A <see cref="TextMetrics"/> value containing every measurement for the laid-out text.</returns>
    public TextMetrics Measure(float wrappingLength)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        float dpi = this.Options.Dpi;
        bool isHorizontal = this.Options.LayoutMode.IsHorizontal();

        FontRectangle advance = GetAdvance(textBox, dpi, isHorizontal);

        int count = CountGlyphLayouts(textBox);
        GlyphBounds[] characterAdvances = new GlyphBounds[count];
        GlyphBounds[] characterSizes = new GlyphBounds[count];
        GlyphBounds[] characterBounds = new GlyphBounds[count];
        GlyphBounds[] characterRenderableBounds = new GlyphBounds[count];

        TextMetricsVisitor visitor = new(
            dpi,
            characterAdvances,
            characterSizes,
            characterBounds,
            characterRenderableBounds);

        TextLayout.LayoutText(textBox, this.Options, wrappingLength, ref visitor);

        FontRectangle bounds = visitor.Bounds();
        FontRectangle size = new(0, 0, bounds.Width, bounds.Height);
        FontRectangle absoluteAdvance = new(this.Options.Origin.X, this.Options.Origin.Y, advance.Width, advance.Height);
        FontRectangle renderableBounds = FontRectangle.Union(absoluteAdvance, bounds);

        LineMetrics[] lineMetrics = GetLineMetrics(textBox, this.Options, wrappingLength);

        return new TextMetrics(
            advance,
            bounds,
            size,
            renderableBounds,
            textBox.TextLines.Count,
            characterAdvances,
            characterSizes,
            characterBounds,
            characterRenderableBounds,
            lineMetrics);
    }

    /// <summary>
    /// Measures the logical advance of this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The logical advance rectangle.</returns>
    public FontRectangle MeasureAdvance(float wrappingLength)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        return GetAdvance(textBox, this.Options.Dpi, this.Options.LayoutMode.IsHorizontal());
    }

    /// <summary>
    /// Measures the normalized rendered size of this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The rendered size with the origin normalized to <c>(0, 0)</c>.</returns>
    public FontRectangle MeasureSize(float wrappingLength)
    {
        FontRectangle bounds = this.MeasureBounds(wrappingLength);
        return new FontRectangle(0, 0, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Measures the rendered glyph bounds of this block at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The rendered glyph bounds.</returns>
    public FontRectangle MeasureBounds(float wrappingLength)
        => GetBounds(this.BreakLines(wrappingLength), this.Options, wrappingLength);

    /// <summary>
    /// Measures the union of logical advance and rendered glyph bounds at the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The full renderable bounds.</returns>
    public FontRectangle MeasureRenderableBounds(float wrappingLength)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        FontRectangle advance = GetAdvance(textBox, this.Options.Dpi, this.Options.LayoutMode.IsHorizontal());
        FontRectangle absoluteAdvance = new(this.Options.Origin.X, this.Options.Origin.Y, advance.Width, advance.Height);
        FontRectangle bounds = GetBounds(textBox, this.Options, wrappingLength);
        return FontRectangle.Union(absoluteAdvance, bounds);
    }

    /// <summary>
    /// Measures the logical advance of each laid-out character entry.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="advances">The list of per-entry logical advances.</param>
    /// <returns>Whether any of the entries had non-empty advances.</returns>
    public bool TryMeasureCharacterAdvances(float wrappingLength, out ReadOnlySpan<GlyphBounds> advances)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        return TryMeasureCharacterBounds(textBox, this.Options, wrappingLength, GlyphBoundsMeasurement.Advance, out advances);
    }

    /// <summary>
    /// Measures the normalized rendered size of each laid-out character entry.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="sizes">The list of per-entry rendered sizes.</param>
    /// <returns>Whether any of the entries had non-empty dimensions.</returns>
    public bool TryMeasureCharacterSizes(float wrappingLength, out ReadOnlySpan<GlyphBounds> sizes)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        return TryMeasureCharacterBounds(textBox, this.Options, wrappingLength, GlyphBoundsMeasurement.Size, out sizes);
    }

    /// <summary>
    /// Measures the rendered glyph bounds of each laid-out character entry.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="bounds">The list of per-entry rendered glyph bounds.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    public bool TryMeasureCharacterBounds(float wrappingLength, out ReadOnlySpan<GlyphBounds> bounds)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        return TryMeasureCharacterBounds(textBox, this.Options, wrappingLength, GlyphBoundsMeasurement.Bounds, out bounds);
    }

    /// <summary>
    /// Measures the full renderable bounds of each laid-out character entry.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="bounds">The list of per-entry renderable bounds.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    public bool TryMeasureCharacterRenderableBounds(float wrappingLength, out ReadOnlySpan<GlyphBounds> bounds)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        return TryMeasureCharacterBounds(textBox, this.Options, wrappingLength, GlyphBoundsMeasurement.RenderableBounds, out bounds);
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
    /// <returns>An array of <see cref="LineMetrics"/> in pixel units.</returns>
    public LineMetrics[] GetLineMetrics(float wrappingLength)
        => GetLineMetrics(this.BreakLines(wrappingLength), this.Options, wrappingLength);

    /// <summary>
    /// Renders this block to the supplied glyph renderer at the supplied wrapping length.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    public void RenderTo(IGlyphRenderer renderer, float wrappingLength)
    {
        TextLayout.TextBox textBox = this.BreakLines(wrappingLength);
        FontRectangle rect = GetBounds(textBox, this.Options, wrappingLength);

        renderer.BeginText(in rect);

        GlyphRendererVisitor visitor = new(renderer, this.Options);
        TextLayout.LayoutText(textBox, this.Options, wrappingLength, ref visitor);

        renderer.EndText();
    }

    /// <summary>
    /// Measures the rendered glyph bounds of an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns>The union of the rendered glyph bounds.</returns>
    private static FontRectangle GetBounds(TextLayout.TextBox textBox, TextOptions options, float wrappingLength)
    {
        if (textBox.TextLines.Count == 0)
        {
            return FontRectangle.Empty;
        }

        GlyphBoundsAccumulator visitor = new(options.Dpi);
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
    private static LineMetrics[] GetLineMetrics(TextLayout.TextBox textBox, TextOptions options, float wrappingLength)
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
        int glyphIndex = 0;

        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLayout.TextLine line = textBox.TextLines[i];

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

            // Bidi reordering mutates entries into visual order, so the source
            // start is the minimum original source index rather than line[0].
            int stringIndex = line[0].StringIndex;
            int graphemeIndex = line[0].GraphemeIndex;
            for (int j = 1; j < line.Count; j++)
            {
                stringIndex = Math.Min(stringIndex, line[j].StringIndex);
                graphemeIndex = Math.Min(graphemeIndex, line[j].GraphemeIndex);
            }

            int glyphCount = CountGlyphLayouts(line);

            metrics[i] = new LineMetrics(
                ascender * options.Dpi,
                baseline * options.Dpi,
                descender * options.Dpi,
                line.ScaledMaxLineHeight * options.Dpi,
                offset * options.Dpi,
                line.ScaledLineAdvance * options.Dpi,
                stringIndex,
                graphemeIndex,
                line.GraphemeCount,
                glyphIndex,
                glyphCount);

            glyphIndex += glyphCount;
        }

        return metrics;
    }

    /// <summary>
    /// Counts the laid-out glyph entries emitted from a line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <returns>The number of glyph entries that layout will emit.</returns>
    private static int CountGlyphLayouts(TextLayout.TextBox textBox)
    {
        int count = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            count += CountGlyphLayouts(textBox.TextLines[i]);
        }

        return count;
    }

    /// <summary>
    /// Counts the laid-out glyph entries emitted from a line.
    /// </summary>
    /// <param name="line">The line to inspect.</param>
    /// <returns>The number of glyph entries that layout will emit.</returns>
    private static int CountGlyphLayouts(TextLayout.TextLine line)
    {
        int count = 0;
        for (int i = 0; i < line.Count; i++)
        {
            TextLayout.TextLine.GlyphLayoutData data = line[i];
            count += data.IsNewLine ? 1 : data.Metrics.Count;
        }

        return count;
    }

    /// <summary>
    /// Measures one per-character bounds collection by streaming laid-out glyphs.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="measurement">The bounds measurement to collect.</param>
    /// <param name="bounds">The measured glyph bounds.</param>
    /// <returns>Whether any glyph has non-empty bounds.</returns>
    private static bool TryMeasureCharacterBounds(
        TextLayout.TextBox textBox,
        TextOptions options,
        float wrappingLength,
        GlyphBoundsMeasurement measurement,
        out ReadOnlySpan<GlyphBounds> bounds)
    {
        int count = CountGlyphLayouts(textBox);
        if (count == 0)
        {
            bounds = [];
            return false;
        }

        GlyphBounds[] result = new GlyphBounds[count];
        GlyphBoundsVisitor visitor = new(result, options.Dpi, measurement);
        TextLayout.LayoutText(textBox, options, wrappingLength, ref visitor);
        bounds = result;
        return visitor.HasSize;
    }

    /// <summary>
    /// Measures the logical advance of an already line-broken text box.
    /// </summary>
    /// <param name="textBox">The shaped and line-broken text box.</param>
    /// <param name="dpi">The target DPI.</param>
    /// <param name="isHorizontalLayout">Whether the layout direction is horizontal.</param>
    /// <returns>The logical advance rectangle.</returns>
    private static FontRectangle GetAdvance(TextLayout.TextBox textBox, float dpi, bool isHorizontalLayout)
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
                TextLayout.TextLine line = textBox.TextLines[i];
                width = MathF.Max(width, line.ScaledLineAdvance);
                height += line.ScaledMaxLineHeight;
            }

            return new FontRectangle(0, 0, width * dpi, height * dpi);
        }

        float verticalWidth = 0;
        float verticalHeight = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLayout.TextLine line = textBox.TextLines[i];
            verticalWidth += line.ScaledMaxLineHeight;
            verticalHeight = MathF.Max(verticalHeight, line.ScaledLineAdvance);
        }

        return new FontRectangle(0, 0, verticalWidth * dpi, verticalHeight * dpi);
    }
}

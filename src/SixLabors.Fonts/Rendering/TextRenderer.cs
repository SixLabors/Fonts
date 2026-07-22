// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Encapsulates logic for laying out and then rendering text to a <see cref="IGlyphRenderer"/> surface.
/// </summary>
public class TextRenderer
{
    /// <summary>
    /// Indicates that no laid-out advance is available for a glyph: rendering starts from a
    /// glyph id rather than laid-out text, so decorations derive their length from the glyph's
    /// own metric advance.
    /// </summary>
    private static readonly Vector2 NoLayoutAdvance = new(-1F);

    private readonly IGlyphRenderer renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextRenderer"/> class.
    /// </summary>
    /// <param name="renderer">The renderer.</param>
    public TextRenderer(IGlyphRenderer renderer) => this.renderer = renderer;

    /// <summary>
    /// Renders the text to the <paramref name="renderer"/>.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public static void RenderTo(IGlyphRenderer renderer, ReadOnlySpan<char> text, TextOptions options)
        => new TextRenderer(renderer).Render(text, options);

    /// <summary>
    /// Renders the text to the <paramref name="renderer"/>.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public static void RenderTo(IGlyphRenderer renderer, string text, TextOptions options)
        => new TextRenderer(renderer).Render(text, options);

    /// <summary>
    /// Renders a glyph id to the <paramref name="renderer"/>.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="options">The glyph options.</param>
    public static void RenderTo(IGlyphRenderer renderer, ushort glyphId, GlyphOptions options)
        => new TextRenderer(renderer).Render(glyphId, options);

    /// <summary>
    /// Renders glyph ids to the <paramref name="renderer"/>.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="glyphRun">The glyph run.</param>
    /// <param name="options">The glyph options.</param>
    public static void RenderTo(IGlyphRenderer renderer, GlyphRun glyphRun, GlyphOptions options)
        => new TextRenderer(renderer).Render(glyphRun, options);

    /// <summary>
    /// Renders the text to the configured renderer.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public void Render(string text, TextOptions options)
        => this.Render(text.AsSpan(), options);

    /// <summary>
    /// Renders the text to the configured renderer.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public void Render(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            this.renderer.BeginText(FontRectangle.Empty);
            this.renderer.EndText();
            return;
        }

        ShapedText shaped = TextLayout.ShapeText(text, options);
        LogicalTextLine logicalLine = TextLayout.ComposeLogicalLine(shaped, text, options);
        this.RenderText(logicalLine, options);
    }

    /// <summary>
    /// Renders a glyph id to the configured renderer.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="options">The glyph options.</param>
    public void Render(ushort glyphId, GlyphOptions options)
    {
        FontMetrics fontMetrics = options.Font.FontMetrics;
        if (!fontMetrics.TryGetGlyphMetrics(
            glyphId,
            options.TextAttributes,
            options.TextDecorations,
            options.LayoutMode,
            options.ColorFontSupport,
            out FontGlyphMetrics? metrics))
        {
            return;
        }

        Vector2 origin = options.Origin / options.Dpi;
        GlyphLayoutMode glyphLayoutMode = options.GetGlyphLayoutMode(metrics.CodePoint);

        if (glyphLayoutMode == GlyphLayoutMode.Horizontal)
        {
            // The renderer positions glyphs by their alphabetic baseline; shifting the origin
            // by the combined anchor and baseline-shift offset puts the selected reference
            // line, shifted as requested, on the caller's origin.
            origin.Y -= TextLayout.GetBaselineOffset(options, false);
        }
        else
        {
            // Vertical rendering positions glyphs about the column's central axis; the same
            // combined offset applies along X from that axis.
            origin.X -= TextLayout.GetBaselineOffset(options, true);
        }

        if (options.VisibleBounds is FontRectangle visibleBounds)
        {
            // The origin is the baseline-anchored render position in layout units (pixels
            // divided by DPI), so the ink box computed against it compares directly with the
            // scaled region. Inflating by the scaled line height for the glyph's orientation
            // gives em-box anchored decorations the same tolerance culled text receives, and
            // rejecting here also skips the per-glyph metrics clone below.
            FontRectangle box = metrics.GetBoundingBox(glyphLayoutMode, origin, options.Font.Size);
            IMetricsHeader metricsHeader = glyphLayoutMode == GlyphLayoutMode.Vertical
                ? fontMetrics.VerticalMetrics
                : fontMetrics.HorizontalMetrics;

            float tolerance = metricsHeader.LineHeight * (options.Font.Size / metrics.ScaleFactor.Y);
            float dpi = options.Dpi;
            if (box.Right + tolerance < visibleBounds.Left / dpi ||
                box.Left - tolerance > visibleBounds.Right / dpi ||
                box.Bottom + tolerance < visibleBounds.Top / dpi ||
                box.Top - tolerance > visibleBounds.Bottom / dpi)
            {
                return;
            }
        }

        TextRun textRun = options.CreateTextRun();
        FontGlyphMetrics renderMetrics = metrics.CloneForRendering(textRun);

        renderMetrics.RenderTo(
            this.renderer,
            options.GraphemeIndex,
            origin,
            origin,
            NoLayoutAdvance,
            glyphLayoutMode,
            textRun,
            options.Font.Size,
            options.Dpi,
            options.HintingMode,
            options.TextDecorationSkipInk,
            options.DecorationPositioningMode,
            fontMetrics);
    }

    /// <summary>
    /// Renders glyph ids to the configured renderer.
    /// </summary>
    /// <param name="glyphRun">The glyph run.</param>
    /// <param name="options">The glyph options.</param>
    public void Render(GlyphRun glyphRun, GlyphOptions options)
    {
        Guard.NotNull(glyphRun, nameof(glyphRun));
        Guard.NotNull(options, nameof(options));

        ReadOnlySpan<ushort> glyphIds = glyphRun.GlyphIds.Span;
        ReadOnlySpan<Vector2> origins = glyphRun.Origins.Span;
        Vector2 originalOrigin = options.Origin;
        int originalGraphemeIndex = options.GraphemeIndex;

        try
        {
            for (int i = 0; i < glyphIds.Length; i++)
            {
                options.Origin = origins[i];
                options.GraphemeIndex = originalGraphemeIndex + i;

                this.Render(glyphIds[i], options);
            }
        }
        finally
        {
            options.Origin = originalOrigin;
            options.GraphemeIndex = originalGraphemeIndex;
        }
    }

    /// <summary>
    /// Line-breaks and renders prepared text without retaining any layout state. When
    /// <see cref="TextOptions.VisibleBounds"/> is set, whole lines outside the region are
    /// culled and breaking stops at the region when line order and alignment permit.
    /// </summary>
    /// <remarks>
    /// A full render reports the rendered ink bounds to <see cref="IGlyphRenderer.BeginText"/>,
    /// matching <see cref="TextBlock.RenderTo(IGlyphRenderer, float)"/>. A culled render
    /// reports the logical advance bounds of the broken lines instead: ink bounds would cost
    /// an extra pass over the visible glyphs and are unknowable once breaking stops early.
    /// </remarks>
    /// <param name="logicalLine">The prepared logical line and line break opportunities.</param>
    /// <param name="options">The text options used for layout and rendering.</param>
    private void RenderText(in LogicalTextLine logicalLine, TextOptions options)
    {
        float wrappingLength = options.WrappingLength;
        float dpi = options.Dpi;
        bool isHorizontal = options.LayoutMode.IsHorizontal();

        float visibleFlowMin = float.NegativeInfinity;
        float visibleFlowMax = float.PositiveInfinity;
        if (options.VisibleBounds is FontRectangle visibleBounds)
        {
            visibleFlowMin = (isHorizontal ? visibleBounds.Top : visibleBounds.Left) / dpi;
            visibleFlowMax = (isHorizontal ? visibleBounds.Bottom : visibleBounds.Right) / dpi;
        }

        TextBox textBox = BreakVisibleLines(logicalLine, options, wrappingLength, isHorizontal, visibleFlowMax);
        if (textBox.TextLines.Count == 0)
        {
            this.renderer.BeginText(FontRectangle.Empty);
            this.renderer.EndText();
            return;
        }

        FontRectangle bounds;
        if (options.VisibleBounds is null)
        {
            // The full render keeps the shipped BeginText contract: the rendered ink bounds,
            // accumulated in the same measuring pass TextBlock uses.
            TextBlock.RenderedRectangleAccumulator accumulator = new(dpi);
            TextLayout.LayoutText(textBox, options, wrappingLength, ref accumulator);
            bounds = accumulator.Result();
        }
        else
        {
            // The logical advance box comes straight from the line aggregates in one scan
            // over the broken lines.
            FontRectangle advance = TextBlock.GetAdvance(textBox, dpi, isHorizontal);
            bounds = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
        }

        this.renderer.BeginText(in bounds);

        TextBlock.GlyphRendererVisitor visitor = new(this.renderer, options, -1);
        TextLayout.LayoutText(textBox, options, wrappingLength, visibleFlowMin, visibleFlowMax, ref visitor);

        this.renderer.EndText();
    }

    /// <summary>
    /// Line-breaks prepared text, stopping once the flow position passes the visible band
    /// when line order and alignment permit. An infinite band breaks every line.
    /// </summary>
    /// <param name="logicalLine">The prepared logical line and line break opportunities.</param>
    /// <param name="options">The text options used for layout.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="isHorizontal">Whether the layout direction is horizontal.</param>
    /// <param name="visibleFlowMax">The upper visible-band edge along the block flow axis in layout units.</param>
    /// <returns>The line-broken text box, possibly truncated after the visible band.</returns>
    private static TextBox BreakVisibleLines(
        in LogicalTextLine logicalLine,
        TextOptions options,
        float wrappingLength,
        bool isHorizontal,
        float visibleFlowMax)
    {
        TextDirection textDirection = TextLayout.GetTextDirection(logicalLine, options);
        if (TextLayout.LayoutRequiresFullLineSet(options, textDirection))
        {
            return TextLayout.BreakLines(logicalLine, options, wrappingLength);
        }

        // A line can never be taller than the tallest entry in the prepared text, and
        // TextLine.Add keeps that maximum current while the logical line is composed. Once the
        // running flow position is more than two of those heights past the end of the visible
        // band, no later line can still be visible: one height covers the line itself, the
        // other covers the walk's one-line-height visibility tolerance. Line spacing below one
        // shifts each line upward when centering it, so that shift widens the margin too.
        float maxLineExtent = logicalLine.TextLine.ScaledMaxLineHeight;
        float stopSlack = maxLineExtent * 2F;
        if (options.LineSpacing < 1F)
        {
            stopSlack += maxLineExtent * (1F - options.LineSpacing) / (2F * options.LineSpacing);
        }

        List<TextLine> textLines = [];
        TextLineBreakEnumerator lineEnumerator = new(logicalLine, options);
        float flowPosition = (isHorizontal ? options.Origin.Y : options.Origin.X) / options.Dpi;

        while (lineEnumerator.MoveNext(wrappingLength))
        {
            TextLine line = lineEnumerator.Current;
            textLines.Add(line);
            flowPosition += line.ScaledMaxLineHeight;

            if (flowPosition - visibleFlowMax > stopSlack)
            {
                break;
            }
        }

        return new TextBox(textLines, textDirection);
    }
}

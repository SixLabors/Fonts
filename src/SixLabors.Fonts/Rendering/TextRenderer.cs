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
        TextBlock block = new(text, options);
        block.RenderTo(this.renderer, options.WrappingLength);
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

        TextRun textRun = options.CreateTextRun();
        FontGlyphMetrics renderMetrics = metrics.CloneForRendering(textRun);
        Vector2 origin = options.Origin / options.Dpi;
        GlyphLayoutMode glyphLayoutMode = options.GetGlyphLayoutMode(metrics.CodePoint);

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
}

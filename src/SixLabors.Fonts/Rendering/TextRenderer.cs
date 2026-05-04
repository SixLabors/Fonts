// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Encapsulates logic for laying out and then rendering text to a <see cref="IGlyphRenderer"/> surface.
/// </summary>
public class TextRenderer
{
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
    public static void RenderTextTo(IGlyphRenderer renderer, ReadOnlySpan<char> text, TextOptions options)
        => new TextRenderer(renderer).RenderText(text, options);

    /// <summary>
    /// Renders the text to the <paramref name="renderer"/>.
    /// </summary>
    /// <param name="renderer">The target renderer.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public static void RenderTextTo(IGlyphRenderer renderer, string text, TextOptions options)
        => new TextRenderer(renderer).RenderText(text, options);

    /// <summary>
    /// Renders the text to the configured renderer.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public void RenderText(string text, TextOptions options)
        => this.RenderText(text.AsSpan(), options);

    /// <summary>
    /// Renders the text to the configured renderer.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="options">The text options. <see cref="TextOptions.WrappingLength"/> controls wrapping; use <c>-1</c> to disable wrapping.</param>
    public void RenderText(ReadOnlySpan<char> text, TextOptions options)
    {
        TextBlock block = new(text, options);
        block.RenderTo(this.renderer, options.WrappingLength);
    }
}

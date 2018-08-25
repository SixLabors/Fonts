// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that can have a glyph renered to it as a series of actions.
    /// </summary>
    public static class IGlyphRendererExtensions
    {
        /// <summary>
        /// Renders the text.
        /// </summary>
        /// <param name="renderer">The target renderer surface.</param>
        /// <param name="text">The text.</param>
        /// <param name="options">The options.</param>
        /// <returns>Returns the orginonal <paramref name="renderer"/></returns>
        public static IGlyphRenderer Render(this IGlyphRenderer renderer, ReadOnlySpan<char> text, RendererOptions options)
        {
            new TextRenderer(renderer).RenderText(text, options);
            return renderer;
        }
    }
}

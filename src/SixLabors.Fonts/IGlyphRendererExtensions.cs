using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

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
        public static IGlyphRenderer Render(this IGlyphRenderer renderer, string text, RendererOptions options)
        {
            new TextRenderer(renderer).RenderText(text, options);
            return renderer;
        }
    }
}

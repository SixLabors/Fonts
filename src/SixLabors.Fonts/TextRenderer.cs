using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapulated logic for laying out and then rendering text to a <see cref="IGlyphRenderer"/> surface.
    /// </summary>
    public class TextRenderer
    {
        private TextLayout layoutEngine;
        private IGlyphRenderer renderer;

        internal TextRenderer(IGlyphRenderer renderer, TextLayout layoutEngine)
        {
            this.layoutEngine = layoutEngine;
            this.renderer = renderer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextRenderer"/> class.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        public TextRenderer(IGlyphRenderer renderer)
            : this(renderer, TextLayout.Default)
        {
        }

        /// <summary>
        /// Renders the text to the <paramref name="renderer"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <param name="renderer">The target renderer.</param>
        public static void RenderTextTo(string text, RendererOptions options, IGlyphRenderer renderer)
        {
            new TextRenderer(renderer).RenderText(text, options);
        }

        /// <summary>
        /// Renders the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        public void RenderText(string text, RendererOptions options)
        {
            ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

            var dpi = new Vector2(options.DpiX, options.DpiY);

            RectangleF rect = TextMeasurer.GetBounds(glyphsToRender, dpi);

            this.renderer.BeginText(rect);

            foreach (GlyphLayout g in glyphsToRender.Where(x => x.Glyph.HasValue))
            {
                g.Glyph.Value.RenderTo(this.renderer, g.Location, options.DpiX, options.DpiY, g.LineHeight);
            }

            this.renderer.EndText();
        }
    }
}

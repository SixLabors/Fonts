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
        /// Renders the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        public void RenderText(string text, FontSpan style)
        {
            this.RenderText(text, style, PointF.Empty);
        }

        /// <summary>
        /// Renders the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <param name="location">The location.</param>
        public void RenderText(string text, FontSpan style, PointF location)
        {
            ImmutableArray<GlyphLayout> glyphsToRender;
            var dpi = new Vector2(style.DpiX, style.DpiY);

            if (location != PointF.Empty)
            {
                Vector2 locationVector = location;

                locationVector = locationVector / dpi;

                glyphsToRender = this.layoutEngine.GenerateLayout(text, style, locationVector);
            }
            else
            {
                glyphsToRender = this.layoutEngine.GenerateLayout(text, style);
            }

            RectangleF rect = TextMeasurer.GetBounds(glyphsToRender, dpi);

            rect.Offset(location);

            this.renderer.BeginText(rect);

            foreach (GlyphLayout g in glyphsToRender.Where(x => x.Glyph.HasValue))
            {
                g.Glyph.Value.RenderTo(this.renderer, g.Location, style.DpiX, style.DpiY, g.LineHeight);
            }

            this.renderer.EndText();
        }
    }
}

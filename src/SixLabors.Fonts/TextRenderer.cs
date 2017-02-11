using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class TextRenderer
    {
        private TextLayout layoutEngine;
        private IMultiGlyphRenderer renderer;

        internal TextRenderer(IMultiGlyphRenderer renderer, TextLayout layoutEngine)
        {
            this.layoutEngine = layoutEngine;
            this.renderer = renderer;
        }

        public TextRenderer(IMultiGlyphRenderer renderer)
            : this(renderer, new TextLayout())
        {
        }

        public void RenderText(string text, FontStyle style, Vector2 dpi)
        {
            var glyphsToRender = layoutEngine.GenerateLayout(text, style);

            foreach (var g in glyphsToRender)
            {
                renderer.SetOrigin(g.Location * dpi);
                g.Glyph.RenderTo(renderer, g.PointSize, dpi);
            }
        }

        public void RenderText(string text, FontStyle style, float dpi)
        {
            RenderText(text, style, new Vector2(dpi));
        }
    }
}

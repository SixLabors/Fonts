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

        public void RenderText(string text, FontStyle style)
        {
            var glyphsToRender = layoutEngine.GenerateLayout(text, style);

            foreach (var g in glyphsToRender)
            {
                renderer.SetOrigin(g.Location);
                g.Glyph.RenderTo(renderer,  g.PointSize);
            }
        }
    }
}

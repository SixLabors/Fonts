using System.Collections.Generic;

namespace SixLabors.Fonts.Tests
{
    public class ColorGlyphRenderer : GlyphRenderer, IColorGlyphRenderer
    {
        public List<GlyphColor> Colors { get; } = new List<GlyphColor>();
        public void SetColor(GlyphColor color)
        {
            this.Colors.Add(color);
        }
    }
}

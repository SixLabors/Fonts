using System.Numerics;

namespace SixLabors.Fonts
{
    internal interface IFontInstance
    {
        FontDescription Description { get; }
        ushort EmSize { get; }
        int LineHeight { get; }

        GlyphInstance GetGlyph(char character);
        Vector2 GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph);
    }
}
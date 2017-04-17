using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal abstract class GlyphLoader
    {
        public abstract Glyphs.GlyphVector CreateGlyph(GlyphTable table);

        public static GlyphLoader Load(BinaryReader reader)
        {
            short contoursCount = reader.ReadInt16();
            Bounds bounds = Bounds.Load(reader);

            if (contoursCount >= 0)
            {
                return SimpleGlyphLoader.LoadSimpleGlyph(reader, contoursCount, bounds);
            }
            else
            {
                return CompositeGlyphLoader.LoadCompositeGlyph(reader, bounds);
            }
        }
    }
}

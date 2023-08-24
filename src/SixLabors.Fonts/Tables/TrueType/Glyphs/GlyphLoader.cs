// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    internal abstract class GlyphLoader
    {
        public abstract GlyphVector CreateGlyph(GlyphTable table);

        public static GlyphLoader Load(BigEndianBinaryReader reader)
        {
            short contoursCount = reader.ReadInt16();
            var bounds = Bounds.Load(reader);

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

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.Woff
{
    internal sealed class Woff2GlyphLoader : GlyphLoader
    {
        private GlyphVector glyphVector;

        public Woff2GlyphLoader(GlyphVector glyphVector) => this.glyphVector = glyphVector;

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            if (this.glyphVector.Bounds == default)
            {
                this.glyphVector.Bounds = Bounds.Load(this.glyphVector.ControlPoints);
            }

            return this.glyphVector;
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    internal class EmptyGlyphLoader : GlyphLoader
    {
        private bool loop;
        private readonly Bounds fallbackEmptyBounds;
        private GlyphVector? glyph;

        public EmptyGlyphLoader(Bounds fallbackEmptyBounds)
            => this.fallbackEmptyBounds = fallbackEmptyBounds;

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            if (this.loop)
            {
                this.glyph ??= GlyphVector.Empty(this.fallbackEmptyBounds);
                return this.glyph.Value;
            }

            this.loop = true;
            this.glyph ??= GlyphVector.Empty(table.GetGlyph(0).Bounds);
            return this.glyph.Value;
        }
    }
}

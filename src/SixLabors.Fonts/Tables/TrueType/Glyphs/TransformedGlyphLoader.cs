// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    internal sealed class TransformedGlyphLoader : GlyphLoader
    {
        private readonly GlyphVector glyphVector;

        public TransformedGlyphLoader(GlyphVector glyphVector) => this.glyphVector = glyphVector;

        public override GlyphVector CreateGlyph(GlyphTable table) => this.glyphVector;
    }
}

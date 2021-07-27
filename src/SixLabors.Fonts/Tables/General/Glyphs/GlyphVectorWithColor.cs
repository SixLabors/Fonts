// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal readonly struct GlyphVectorWithColor
    {
        internal GlyphVectorWithColor(GlyphVector vector, GlyphColor color)
        {
            this.Vector = vector;
            this.Color = color;
        }

        public GlyphVector Vector { get; }

        public GlyphColor Color { get; }
    }
}

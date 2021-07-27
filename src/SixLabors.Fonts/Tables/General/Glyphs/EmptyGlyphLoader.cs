// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
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
                if (this.glyph is null)
                {
                    this.glyph = new GlyphVector(Array.Empty<Vector2>(), Array.Empty<bool>(), Array.Empty<ushort>(), this.fallbackEmptyBounds);
                }

                return this.glyph.Value;
            }

            this.loop = true;
            return table.GetGlyph(0);
        }
    }
}

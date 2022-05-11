// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal readonly struct GlyphNameMap
    {
        public readonly ushort glyphIndex;
        public readonly string glyphName;

        public GlyphNameMap(ushort glyphIndex, string glyphName)
        {
            this.glyphIndex = glyphIndex;
            this.glyphName = glyphName;
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal struct Cff1GlyphData
    {
        public Cff1GlyphData(ushort glyphIndex, Type2Instruction[] glyphInstructions)
        {
            this.GlyphIndex = glyphIndex;
            this.GlyphInstructions = glyphInstructions;
            this.GlyphName = null;
        }

        public ushort GlyphIndex { get; }

        public Type2Instruction[] GlyphInstructions { get; }

        public string? GlyphName { get; set; }
    }
}

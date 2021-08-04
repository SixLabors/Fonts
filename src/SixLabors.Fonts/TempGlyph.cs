// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    internal struct TempGlyph
    {
        public readonly ushort GlyphIndex;
        public readonly short NumContour;

        public ushort InstructionLen;
        public bool CompositeHasInstructions;

        public TempGlyph(ushort glyphIndex, short contourCount)
        {
            this.GlyphIndex = glyphIndex;
            this.NumContour = contourCount;

            this.InstructionLen = 0;
            this.CompositeHasInstructions = false;
        }
    }
}

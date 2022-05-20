// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

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

        public readonly ushort GlyphIndex { get; }

        public Type2Instruction[] GlyphInstructions { get; }

        public string? GlyphName { get; set; }

        public Bounds GetBounds()
        {
            // TODO: Boxing.
            IGlyphRenderer finder = CffBoundsFinder.Create();
            CffEvaluationEngine.Run(ref finder, this.GlyphInstructions, Vector2.One, Vector2.Zero, default, default);
            return ((CffBoundsFinder)finder).GetBounds();
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.Cff
{
    internal struct Cff1GlyphData
    {
        public Cff1GlyphData(ushort glyphIndex, ReadOnlyMemory<Type2Instruction> glyphInstructions)
        {
            this.GlyphIndex = glyphIndex;
            this.GlyphInstructions = glyphInstructions;
            this.GlyphName = null;
        }

        public readonly ushort GlyphIndex { get; }

        public ReadOnlyMemory<Type2Instruction> GlyphInstructions { get; }

        public string? GlyphName { get; set; }

        public Bounds GetBounds() => CffEvaluationEngine.GetBounds(this.GlyphInstructions.Span, Vector2.One, Vector2.Zero);
    }
}

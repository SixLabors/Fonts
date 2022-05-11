// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Cff1GlyphData
    {
        public string? Name { get; set; }

        public ushort SIDName { get; set; }

        public Type2Instruction[] GlyphInstructions { get; set; } = Array.Empty<Type2Instruction>();
    }
}

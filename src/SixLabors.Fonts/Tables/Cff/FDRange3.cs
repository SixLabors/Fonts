// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Represents an element in an font dictionary array.
    /// </summary>
    internal readonly struct FDRange3
    {
        public FDRange3(ushort first, byte fontDictionary)
        {
            this.First = first;
            this.FontDictionary = fontDictionary;
        }

        /// <summary>
        /// Gets the first glyph index in range
        /// </summary>
        public ushort First { get; }

        /// <summary>
        /// Gets the font dictionary index for all glyphs in range
        /// </summary>
        public byte FontDictionary { get; }

        public override string ToString() => $"First {this.First}, Dictionary {this.FontDictionary}.";
    }
}

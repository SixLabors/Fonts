// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Interface for Glyph collections.
    /// </summary>
    public interface IGlyphCollection
    {
        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        void GetCodePointAndGlyphIds(int index, out CodePoint codePoint, out int offset, out IEnumerable<int> glyphIds);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Interface for Glyph collections.
    /// </summary>
    public interface IGlyphCollection
    {
        /// <summary>
        /// Gets the collection count.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        void GetCodePointAndGlyphIds(int index, out CodePoint codePoint, out int offset, out ReadOnlySpan<int> glyphIds);

        /// <summary>
        /// Gets the glyph ids at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="ReadOnlySpan{T}"/>.</returns>
        ReadOnlySpan<int> GetGlyphIds(int index);
    }
}

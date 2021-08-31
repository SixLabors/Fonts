// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph ids that are mapped to input codepoints.
    /// </summary>
    public interface IGlyphSubstitutionCollection
    {
        /// <summary>
        /// Gets the number of glyphs ids contained in the collection.
        /// This may be more or less than original input codepoint count (due to substitution process).
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the glyph index at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="ushort"/>.</returns>
        ushort this[int index] { get; }

        /// <summary>
        /// Adds the glyph id and the codepoint it represents to the collection.
        /// </summary>
        /// <param name="glyphId">The id of the glyph to add.</param>
        /// <param name="codePoint">The codepoint the glyph represents.</param>
        /// <param name="index">The zero-based index within the input codepoint collection.</param>
        void AddGlyph(ushort glyphId, CodePoint codePoint, int index);

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the glyph id and the range of codepoints it represents at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="glyphId">The glyph id.</param>
        /// <param name="range">The codepoint range.</param>
        void GetGlyphIdAndRange(int index, out ushort glyphId, out CodePointRange range);

        /// <summary>
        /// Performs a 1:1 replacement of a glyph id at the given position.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        void Replace(int index, ushort glyphId);

        /// <summary>
        /// Replaces a series of glyph ids starting at the given position with a new id.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to replace.</param>
        /// <param name="count">The number of elements to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        void Replace(int index, int count, ushort glyphId);

        /// <summary>
        /// Replaces a single glyph id with a collection of glyph ids.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphIds">The collection of replacement glyph ids.</param>
        void Replace(int index, IEnumerable<ushort> glyphIds);
    }
}

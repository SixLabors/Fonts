// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        /// Gets the glyph ids at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="ReadOnlySpan{UInt16}"/>.</returns>
        ReadOnlySpan<ushort> this[int index] { get; }

        /// <summary>
        /// Adds the glyph id and the codepoint it represents to the collection.
        /// </summary>
        /// <param name="glyphId">The id of the glyph to add.</param>
        /// <param name="codePoint">The codepoint the glyph represents.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        void AddGlyph(ushort glyphId, CodePoint codePoint, int offset);

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the specified glyph ids matching the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="glyphIds">
        /// When this method returns, contains the metrics associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the glyphIds parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IGlyphSubstitutionCollection"/> contains glyph ids
        /// for the specified offset; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryGetGlyphIdsAtOffset(int offset, [NotNullWhen(true)] out IEnumerable<ushort>? glyphIds);

        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        /// <param name="script">The Unicode script.</param>
        void GetGlyphIdsAndScript(int index, out IEnumerable<ushort> glyphIds, out Script script);

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

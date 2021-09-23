// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines the contract for glyph shaping collections.
    /// </summary>
    public interface IGlyphShapingCollection
    {
        /// <summary>
        /// Gets the collection count.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the glyph ids at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <returns>The <see cref="ReadOnlySpan{UInt16}"/>.</returns>
        ReadOnlySpan<ushort> this[int index] { get; }

        /// <summary>
        /// Gets the shaping features at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index within the input element collection.</param>
        /// <param name="value">
        /// When this method returns, contains the set of feature tags ids associated with the specified id,
        /// if the value is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IGlyphShapingCollection"/> contains glyph ids
        /// for the specified offset; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryGetShapingFeatures(int index, [NotNullWhen(true)] out IReadOnlySet<Tag>? value);

        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="direction">The resolved text direction for the codepoint.</param>
        /// <param name="offset">The zero-based index within the input element collection.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        void GetGlyphData(int index, out CodePoint codePoint, out TextDirection direction, out int offset, out ReadOnlySpan<ushort> glyphIds);

        /// <summary>
        /// Adds the shaping feature to the collection which should be applied to the glyph at a specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element.</param>
        /// <param name="feature">The feature to apply.</param>
        void AddShapingFeature(int index, Tag feature);
    }
}

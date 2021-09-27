// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    public sealed class GlyphSubstitutionCollection : IGlyphShapingCollection
    {
        /// <summary>
        /// Contains a map between the index of a map within the collection and its offset.
        /// </summary>
        private readonly List<int> offsets = new();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyph ids.
        /// </summary>
        private readonly Dictionary<int, GlyphShapingData> glyphs = new();

        /// <summary>
        /// Gets the number of glyphs ids contained in the collection.
        /// This may be more or less than original input codepoint count (due to substitution process).
        /// </summary>
        public int Count => this.offsets.Count;

        /// <inheritdoc />
        public ReadOnlySpan<ushort> this[int index] => this.glyphs[this.offsets[index]].GlyphIds;

        /// <inheritdoc />
        public GlyphShapingData GetGlyphShapingData(int index)
            => this.glyphs[this.offsets[index]];

        internal GlyphShapingData GetGlyphShapingData(int index, out int offset)
        {
            offset = this.offsets[index];
            return this.glyphs[offset];
        }

        /// <inheritdoc />
        public void AddShapingFeature(int index, Tag feature)
            => this.glyphs[this.offsets[index]].Features.Add(feature);

        /// <summary>
        /// Adds the glyph id and the codepoint it represents to the collection.
        /// </summary>
        /// <param name="glyphId">The id of the glyph to add.</param>
        /// <param name="codePoint">The codepoint the glyph represents.</param>
        /// <param name="direction">The resolved text direction for the codepoint.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        public void AddGlyph(ushort glyphId, CodePoint codePoint, TextDirection direction, int offset)
        {
            this.glyphs.Add(offset, new GlyphShapingData(codePoint, direction, new[] { glyphId }));
            this.offsets.Add(offset);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            this.offsets.Clear();
            this.glyphs.Clear();
        }

        /// <summary>
        /// Gets the specified glyph ids matching the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="data">
        /// When this method returns, contains the shaping data associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the data parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="GlyphSubstitutionCollection"/> contains glyph ids
        /// for the specified offset; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetGlyphShapingDataAtOffset(int offset, out GlyphShapingData data)
            => this.glyphs.TryGetValue(offset, out data);

        /// <summary>
        /// Performs a 1:1 replacement of a glyph id at the given position.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        public void Replace(int index, ushort glyphId)
        {
            int offset = this.offsets[index];
            GlyphShapingData current = this.glyphs[offset];
            this.glyphs[offset] = new GlyphShapingData(current.CodePoint, current.Direction, new[] { glyphId }, current.Features);
        }

        /// <summary>
        /// Replaces a series of glyph ids starting at the given position with a new id.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to replace.</param>
        /// <param name="count">The number of elements to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        public void Replace(int index, int count, ushort glyphId)
        {
            // Remove the count starting at the at index.
            int offset = this.offsets[index];
            GlyphShapingData current = this.glyphs[offset];
            for (int i = 0; i < count; i++)
            {
                this.glyphs.Remove(this.offsets[i + index]);
            }

            this.offsets.RemoveRange(index, count);

            // Assign our new id at the index.
            this.glyphs[offset] = new GlyphShapingData(current.CodePoint, current.Direction, new[] { glyphId }, current.Features);
            this.offsets.Insert(index, offset);
        }

        /// <summary>
        /// Replaces a single glyph id with a collection of glyph ids.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphIds">The collection of replacement glyph ids.</param>
        public void Replace(int index, IEnumerable<ushort> glyphIds)
        {
            int offset = this.offsets[index];
            GlyphShapingData current = this.glyphs[offset];
            this.glyphs[offset] = new GlyphShapingData(current.CodePoint, current.Direction, glyphIds.ToArray(), current.Features);
        }
    }
}
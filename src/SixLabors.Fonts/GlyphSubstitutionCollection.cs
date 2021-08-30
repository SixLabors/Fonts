// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    internal class GlyphSubstitutionCollection
    {
        private readonly List<ushort> glyphIndices = new List<ushort>();
        private readonly List<int> inputCodePointIndices = new List<int>();
        private ushort originalCodePointOffset;
        private readonly List<GlyphIndexToCodePoint> glyphIndexToCodePointMap = new List<GlyphIndexToCodePoint>();

        /// <summary>
        /// Gets the glyph count.
        /// This may be more or less than original user codepoint count (from substitution process).
        /// </summary>
        public int Count => this.glyphIndices.Count;

        /// <summary>
        /// Gets the glyph index at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="ushort"/>.</returns>
        public ushort this[int index] => this.glyphIndices[index];

        /// <summary>
        /// Clears the collection.
        /// </summary>
        public void Clear()
        {
            this.glyphIndices.Clear();
            this.originalCodePointOffset = 0;
            this.inputCodePointIndices.Clear();
            this.glyphIndexToCodePointMap.Clear();
        }

        /// <summary>
        /// Add a codepoint index and its glyph index.
        /// </summary>
        /// <param name="codePointIndex">Index to codepoint element in code point collection.</param>
        /// <param name="glyphIndex">Map to glyphindex</param>
        public void AddGlyph(int codePointIndex, ushort glyphIndex)
        {
            // So we can monitor what substituion process
            this.inputCodePointIndices.Add(codePointIndex);
            this.glyphIndices.Add(glyphIndex);

            var glyphIndexToCodePointMap = new GlyphIndexToCodePoint(this.originalCodePointOffset, 1);
            this.glyphIndexToCodePointMap.Add(glyphIndexToCodePointMap);
            this.originalCodePointOffset++;
        }

        public void GetGlyphIndexAndMap(int index, out ushort glyphIndex, out ushort codepointOffset, out ushort mapLength)
        {
            glyphIndex = this.glyphIndices[index];
            GlyphIndexToCodePoint glyphIndexToUserCodePoint = this.glyphIndexToCodePointMap[index];
            codepointOffset = glyphIndexToUserCodePoint.CodePointOffset;
            mapLength = glyphIndexToUserCodePoint.Length;
        }

        /// <summary>
        /// Performs a 1:1 replacement of a glyph index at the given position.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="newGlyphIndex">The replacement glyph index.</param>
        public void Replace(int index, ushort newGlyphIndex)
            => this.glyphIndices[index] = newGlyphIndex;

        /// <summary>
        /// Replaces a series of glyph indices starting at the given position with a new index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to replace.</param>
        /// <param name="count">The number of elements to replace.</param>
        /// <param name="newGlyphIndex">The replacement glyph index.</param>
        public void Replace(int index, int count, ushort newGlyphIndex)
        {
            // e.g. f-i ligation
            // original 'f' glyph and 'i' glyph are removed
            // and then replace with a single glyph.
            this.glyphIndices.RemoveRange(index, count);
            this.glyphIndices.Insert(index, newGlyphIndex);
            GlyphIndexToCodePoint intitial = this.glyphIndexToCodePointMap[index];

            var replacement = new GlyphIndexToCodePoint(intitial.CodePointOffset, (ushort)count);
            this.glyphIndexToCodePointMap.RemoveRange(index, count);
            this.glyphIndexToCodePointMap.Insert(index, replacement);
        }

        /// <summary>
        /// Replaces a single glyph index with a collection of glyph indices.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="newGlyphIndices">The collection of replacement glyph indices.</param>
        public void Replace(int index, ushort[] newGlyphIndices)
        {
            this.glyphIndices.RemoveAt(index);
            this.glyphIndices.InsertRange(index, newGlyphIndices);
            GlyphIndexToCodePoint current = this.glyphIndexToCodePointMap[index];
            this.glyphIndexToCodePointMap.RemoveAt(index);

            // Insert
            int j = newGlyphIndices.Length;
            for (int i = 0; i < j; ++i)
            {
                var newGlyph = new GlyphIndexToCodePoint(current.CodePointOffset, 1);

                // May point to the same user codepoint.
                this.glyphIndexToCodePointMap.Insert(index, newGlyph);
            }
        }

        /// <summary>
        /// Maps from glyph index to original user code point.
        /// </summary>
        private readonly struct GlyphIndexToCodePoint
        {
            public GlyphIndexToCodePoint(ushort codePointOffset, ushort length)
            {
                this.CodePointOffset = codePointOffset;
                this.Length = length;
            }

            /// <summary>
            /// Gets the offset from start layout codepoint
            /// </summary>
            public ushort CodePointOffset { get; }

            /// <summary>
            /// Gets the number of codepoints represented by the glyph.
            /// </summary>
            public readonly ushort Length { get; }

            public override string ToString()
                => $"CodePointOffset: {this.CodePointOffset} - Length: {this.Length}";
        }
    }
}

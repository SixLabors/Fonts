// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    internal sealed class GlyphSubstitutionCollection : IGlyphShapingCollection
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
        /// Initializes a new instance of the <see cref="GlyphSubstitutionCollection"/> class.
        /// </summary>
        /// <param name="textOptions">The text options.</param>
        public GlyphSubstitutionCollection(TextOptions textOptions)
        {
            this.TextOptions = textOptions;
            this.IsVerticalLayoutMode = textOptions.LayoutMode.IsVertical();
        }

        /// <summary>
        /// Gets the number of glyphs ids contained in the collection.
        /// This may be more or less than original input codepoint count (due to substitution process).
        /// </summary>
        public int Count => this.offsets.Count;

        /// <inheritdoc />
        public bool IsVerticalLayoutMode { get; }

        /// <inheritdoc />
        public TextOptions TextOptions { get; }

        /// <summary>
        /// Gets or sets the running id of any ligature glyphs contained withing this collection are a member of.
        /// </summary>
        public int LigatureId { get; set; } = 1;

        /// <inheritdoc />
        public ReadOnlySpan<ushort> this[int index] => this.glyphs[this.offsets[index]].GlyphIds;

        /// <inheritdoc />
        public GlyphShapingData GetGlyphShapingData(int index)
            => this.glyphs[this.offsets[index]];

        /// <summary>
        /// Gets the shaping data at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <returns>The <see cref="GlyphShapingData"/>.</returns>
        internal GlyphShapingData GetGlyphShapingData(int index, out int offset)
        {
            offset = this.offsets[index];
            return this.glyphs[offset];
        }

        /// <inheritdoc />
        public void AddShapingFeature(int index, TagEntry feature)
            => this.glyphs[this.offsets[index]].Features.Add(feature);

        /// <inheritdoc />
        public void EnableShapingFeature(int index, Tag feature)
        {
            List<TagEntry> features = this.glyphs[this.offsets[index]].Features;
            foreach (TagEntry tagEntry in features)
            {
                if (tagEntry.Tag == feature)
                {
                    tagEntry.Enabled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Adds the glyph id and the codepoint it represents to the collection.
        /// </summary>
        /// <param name="glyphId">The id of the glyph to add.</param>
        /// <param name="codePoint">The codepoint the glyph represents.</param>
        /// <param name="direction">The resolved text direction for the codepoint.</param>
        /// <param name="textRun">The text run this glyph belongs to.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        public void AddGlyph(ushort glyphId, CodePoint codePoint, TextDirection direction, TextRun textRun, int offset)
        {
            this.glyphs.Add(offset, new(textRun)
            {
                CodePoint = codePoint,
                Direction = direction,
                GlyphIds = new[] { glyphId },
            });
            this.offsets.Add(offset);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            this.offsets.Clear();
            this.glyphs.Clear();
            this.LigatureId = 1;
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
        public bool TryGetGlyphShapingDataAtOffset(int offset, [NotNullWhen(true)] out GlyphShapingData? data)
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
            current.GlyphIds = new[] { glyphId };
        }

        /// <summary>
        /// Performs a 1:1 replacement of a glyph id at the given position while removing a series of glyph ids at the given positions within the sequence.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="removalIndices">The indices at which to remove elements.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        /// <param name="ligatureId">The ligature id.</param>
        public void Replace(int index, ReadOnlySpan<int> removalIndices, ushort glyphId, int ligatureId)
        {
            // Remove the glyphs at each index.
            // TODO: We will have to offset these indices by the leading index of the collection
            // that the current shaper is working against.
            int codePointCount = 0;
            for (int i = removalIndices.Length - 1; i >= 0; i--)
            {
                int match = removalIndices[i];
                int matchOffset = this.offsets[match];
                codePointCount += this.glyphs[matchOffset].CodePointCount;
                this.glyphs.Remove(matchOffset);
                this.offsets.RemoveAt(match);
            }

            // Assign our new id at the index.
            int offset = this.offsets[index];
            GlyphShapingData current = this.glyphs[offset];
            current.CodePointCount += codePointCount;
            current.GlyphIds = new[] { glyphId };
            current.LigatureId = ligatureId;
            current.LigatureComponent = -1;
            current.MarkAttachment = -1;
            current.CursiveAttachment = -1;
        }

        /// <summary>
        /// Replaces a single glyph id with a collection of glyph ids.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphIds">The collection of replacement glyph ids.</param>
        public void Replace(int index, ReadOnlySpan<ushort> glyphIds)
        {
            // TODO: FontKit stores the ids in sequence with increasing ligature component values.
            int offset = this.offsets[index];
            GlyphShapingData current = this.glyphs[offset];
            current.GlyphIds = glyphIds.ToArray();
            current.LigatureComponent = 0;
            current.MarkAttachment = -1;
            current.CursiveAttachment = -1;
        }
    }
}

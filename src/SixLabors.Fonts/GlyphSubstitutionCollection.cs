// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    internal class GlyphSubstitutionCollection : IGlyphSubstitutionCollection
    {
        private readonly List<ushort> glyphIds = new List<ushort>();
        private readonly List<int> codePointIndices = new List<int>();
        private ushort offset;
        private readonly List<CodePointRange> ranges = new List<CodePointRange>();

        /// <inheritdoc/>
        public int Count => this.glyphIds.Count;

        /// <inheritdoc/>
        public ushort this[int index] => this.glyphIds[index];

        /// <inheritdoc/>
        public void AddGlyph(ushort glyphId, CodePoint codePoint, int index)
        {
            // So we can monitor substitution processes
            this.codePointIndices.Add(index);
            this.glyphIds.Add(glyphId);

            var range = new CodePointRange(this.offset, CodePoint.GetScript(codePoint), 1);
            this.ranges.Add(range);
            this.offset++;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.offset = 0;
            this.glyphIds.Clear();
            this.codePointIndices.Clear();
            this.ranges.Clear();
        }

        /// <inheritdoc/>
        public void GetGlyphIdAndRange(int index, out ushort glyphId, out CodePointRange range)
        {
            glyphId = this.glyphIds[index];
            range = this.ranges[index];
        }

        /// <inheritdoc/>
        public void Replace(int index, ushort glyphId)
            => this.glyphIds[index] = glyphId;

        /// <inheritdoc/>
        public void Replace(int index, int count, ushort glyphId)
        {
            // e.g. f-i ligation
            // original 'f' glyph and 'i' glyph are removed
            // and then replace with a single glyph.
            this.glyphIds.RemoveRange(index, count);
            this.glyphIds.Insert(index, glyphId);
            CodePointRange intitial = this.ranges[index];

            var replacement = new CodePointRange(intitial.Start, intitial.Script, (ushort)count);
            this.ranges.RemoveRange(index, count);
            this.ranges.Insert(index, replacement);
        }

        /// <inheritdoc/>
        public void Replace(int index, IEnumerable<ushort> glyphIds)
        {
            this.glyphIds.RemoveAt(index);
            this.glyphIds.InsertRange(index, glyphIds);
            CodePointRange current = this.ranges[index];
            this.ranges.RemoveAt(index);

            // Insert
            // TODO: Check this.
            foreach (ushort id in glyphIds)
            {
                var range = new CodePointRange(current.Start, current.Script, 1);

                // May point to the same user codepoint.
                this.ranges.Insert(index++, range);
            }
        }
    }
}

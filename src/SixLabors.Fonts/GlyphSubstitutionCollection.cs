// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    internal class GlyphSubstitutionCollection : IGlyphSubstitutionCollection
    {
        /// <summary>
        /// Contains a map between the index of a map within the collection and its offset.
        /// </summary>
        private readonly Dictionary<int, int> offsets = new Dictionary<int, int>();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyph ids.
        /// </summary>
        private readonly Dictionary<int, CodePointGlyphs> map = new Dictionary<int, CodePointGlyphs>();

        /// <inheritdoc/>
        public int Count { get; private set; }

        /// <inheritdoc/>
        public ReadOnlySpan<int> this[int index] => this.map[this.offsets[index]].GlyphIds;

        /// <inheritdoc/>
        public void AddGlyph(int glyphId, CodePoint codePoint, int offset)
        {
            this.map.Add(offset, new CodePointGlyphs(codePoint, new[] { glyphId }));
            this.offsets[this.Count++] = offset;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.Count = 0;
            this.map.Clear();
            this.offsets.Clear();
        }

        /// <inheritdoc/>
        public bool TryGetCodePointAndGlyphIdsAtOffset(int offset, [NotNullWhen(true)] out CodePoint? codePoint, [NotNullWhen(true)] out IEnumerable<int>? glyphIds)
        {
            if (this.map.TryGetValue(offset, out CodePointGlyphs value))
            {
                codePoint = value.CodePoint;
                glyphIds = value.GlyphIds;
                return true;
            }

            codePoint = null;
            glyphIds = null;
            return false;
        }

        /// <inheritdoc/>
        public void GetCodePointAndGlyphIds(int index, out CodePoint codePoint, out IEnumerable<int> glyphIds)
        {
            CodePointGlyphs value = this.map[this.offsets[index]];
            codePoint = value.CodePoint;
            glyphIds = value.GlyphIds;
        }

        /// <inheritdoc/>
        public void Replace(int index, int glyphId)
        {
            int offset = this.offsets[index];
            this.map[offset] = new CodePointGlyphs(this.map[offset].CodePoint, new[] { glyphId });
        }

        /// <inheritdoc/>
        public void Replace(int index, int count, int glyphId)
        {
            // Remove range at index
            int offset = this.offsets[index];
            CodePoint codePoint = this.map[offset].CodePoint;
            for (int i = index; i < index + count; i++)
            {
                this.map.Remove(this.offsets[i]);
                this.offsets.Remove(i);
            }

            // Assign at index
            this.Count -= count - 1;
            this.map[offset] = new CodePointGlyphs(codePoint, new[] { glyphId });
            this.offsets[index] = offset;

            // Shuffle offsets following index down.
            for (int i = 1; i < this.Count; i++)
            {
                this.offsets[index + i] = this.offsets[index + i + 1];
            }

            this.offsets.Remove(this.Count);
        }

        /// <inheritdoc/>
        public void Replace(int index, IEnumerable<int> glyphIds)
        {
            int offset = this.offsets[index];
            this.map[offset] = new CodePointGlyphs(this.map[offset].CodePoint, glyphIds.ToArray());
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private readonly struct CodePointGlyphs
        {
            public CodePointGlyphs(CodePoint codePoint, int[] glyphIds)
            {
                this.CodePoint = codePoint;
                this.GlyphIds = glyphIds;
            }

            public CodePoint CodePoint { get; }

            public int[] GlyphIds { get; }

            private string DebuggerDisplay
                => FormattableString
                .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScript(this.CodePoint)} : [{string.Join(",", this.GlyphIds)}]");
        }
    }
}

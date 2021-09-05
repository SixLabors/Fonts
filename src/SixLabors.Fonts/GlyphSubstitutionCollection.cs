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
        private List<int> offsets = new List<int>();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyph ids.
        /// </summary>
        private readonly Dictionary<int, CodePointGlyphs> map = new Dictionary<int, CodePointGlyphs>();

        /// <inheritdoc/>
        public int Count => this.offsets.Count;

        /// <inheritdoc/>
        public ReadOnlySpan<int> this[int index] => this.map[this.offsets[index]].GlyphIds;

        /// <inheritdoc/>
        public void AddGlyph(int glyphId, CodePoint codePoint, int offset)
        {
            this.map.Add(offset, new CodePointGlyphs(codePoint, new[] { glyphId }));
            this.offsets.Add(offset);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.offsets.Clear();
            this.map.Clear();
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
            // Remove the count starting at the at index.
            int offset = this.offsets[index];
            CodePoint codePoint = this.map[offset].CodePoint;
            for (int i = 0; i < count; i++)
            {
                this.map.Remove(this.offsets[i + index]);
            }

            this.offsets.RemoveRange(index, count);

            // Assign our new id at the index.
            this.map[offset] = new CodePointGlyphs(codePoint, new[] { glyphId });
            this.offsets.Insert(index, offset);
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

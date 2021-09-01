// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<int, ScriptGlyphs> map = new Dictionary<int, ScriptGlyphs>();

        /// <inheritdoc/>
        public int Count { get; private set; }

        /// <inheritdoc/>
        public ushort this[int index] => this.map[this.offsets[index]].GlyphIds[0];

        /// <inheritdoc/>
        public void AddGlyph(ushort glyphId, CodePoint codePoint, int offset)
        {
            this.map.Add(offset, new ScriptGlyphs(CodePoint.GetScript(codePoint), new[] { glyphId }));
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
        public void GetGlyphIdsAndScript(int index, out IEnumerable<ushort> glyphIds, out Script script)
        {
            // TODO: We should add a TryGet for the offset and we should delete the indexer
            ScriptGlyphs result = this.map[this.offsets[index]];
            glyphIds = result.GlyphIds;
            script = result.Script;
        }

        /// <inheritdoc/>
        public void Replace(int index, ushort glyphId)
        {
            int offset = this.offsets[index];
            this.map[offset] = new ScriptGlyphs(this.map[offset].Script, new[] { glyphId });
        }

        /// <inheritdoc/>
        public void Replace(int index, int count, ushort glyphId)
        {
            int offset = this.offsets[index];
            ScriptGlyphs current = this.map[offset];
            for (int i = 0; i < count; i++)
            {
                this.map.Remove(this.offsets[index + i]);
                this.offsets.Remove(index + i);
            }

            this.offsets[index] = offset;
            this.map[offset] = new ScriptGlyphs(current.Script, new[] { glyphId });
            this.Count -= count - 1;
        }

        /// <inheritdoc/>
        public void Replace(int index, IEnumerable<ushort> glyphIds)
        {
            int offset = this.offsets[index];
            ushort[] ids = glyphIds.ToArray();
            this.map[offset] = new ScriptGlyphs(this.map[offset].Script, ids);
            this.Count += ids.Length - 1;
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private readonly struct ScriptGlyphs
        {
            public ScriptGlyphs(Script script, ushort[] glyphIds)
            {
                this.Script = script;
                this.GlyphIds = glyphIds;
            }

            public Script Script { get; }

            public ushort[] GlyphIds { get; }

            private string DebuggerDisplay => FormattableString.Invariant($"{this.Script} : [{string.Join(",", this.GlyphIds)}]");
        }
    }
}

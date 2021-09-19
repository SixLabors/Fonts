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
    /// Represents a collection of glyph metrics that are mapped to input codepoints.
    /// </summary>
    public sealed class GlyphPositioningCollection
    {
        /// <summary>
        /// Contains a map between the index of a map within the collection, it's codepoint
        /// and glyph ids.
        /// </summary>
        private readonly Dictionary<int, CodePointGlyphs> glyphs = new();

        /// <summary>
        /// Contains a map between the index of a map within the collection and its offset.
        /// </summary>
        private readonly Dictionary<int, int> offsets = new();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyphss.
        /// </summary>
        private readonly Dictionary<int, GlyphMetrics[]> map = new();

        /// <summary>
        /// The text layout mode.
        /// </summary>
        private readonly LayoutMode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphPositioningCollection"/> class.
        /// </summary>
        /// <param name="mode">The text layout mode.</param>
        public GlyphPositioningCollection(LayoutMode mode) => this.mode = mode;

        /// <summary>
        /// Gets the number of glyphs indexes contained in the collection.
        /// </summary>
        public int Count => this.offsets.Count;

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            this.glyphs.Clear();
            this.offsets.Clear();
            this.map.Clear();
        }

        /// <summary>
        /// Gets the glyph metrics at the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="metrics">
        /// When this method returns, contains the glyph metrics associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the metrics parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>The <see cref="T:GlyphMetrics[]"/>.</returns>
        public bool TryGetGlypMetricsAtOffset(int offset, [NotNullWhen(true)] out GlyphMetrics[]? metrics)
            => this.map.TryGetValue(offset, out metrics);

        /// <summary>
        /// Gets the glyph ids at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="ReadOnlySpan{UInt16}"/>.</returns>
        public ReadOnlySpan<int> GetGlyphIds(int index) => this.glyphs[index].GlyphIds;

        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        public void GetCodePointAndGlyphIds(int index, out CodePoint codePoint, out int offset, out IEnumerable<int> glyphIds)
        {
            offset = this.offsets[index];
            CodePointGlyphs value = this.glyphs[index];
            codePoint = value.CodePoint;
            glyphIds = value.GlyphIds;
        }

        /// <summary>
        /// Adds the collection of glyph ids to the metrics collection.
        /// Adding subsequent collections will overwrite any glyphs that have been previously
        /// identified as fallbacks.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="collection">The glyph substitution collection.</param>
        /// <param name="options">The renderer options.</param>
        /// <returns><see langword="true"/> if the metrics collection does not contain any fallbacks; otherwise <see langword="false"/>.</returns>
        public bool TryAddOrUpdate(IFontMetrics fontMetrics, GlyphSubstitutionCollection collection, RendererOptions options)
        {
            bool hasFallBacks = false;
            for (int i = 0; i < collection.Count; i++)
            {
                collection.GetCodePointAndGlyphIds(i, out CodePoint codePoint, out int offset, out IEnumerable<int>? glyphIds);

                bool mapped = this.map.TryGetValue(offset, out GlyphMetrics[]? metrics);
                if (mapped && metrics![0].GlyphType != GlyphType.Fallback)
                {
                    // We've already got the correct glyph.
                    continue;
                }

                var m = new List<GlyphMetrics>(glyphIds.Count());
                foreach (int id in glyphIds)
                {
                    // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                    // cache the original in the font metrics and only update our collection.
                    foreach (GlyphMetrics gm in fontMetrics.GetGlyphMetrics(codePoint, id, options.ColorFontSupport))
                    {
                        if (gm.GlyphType == GlyphType.Fallback)
                        {
                            hasFallBacks = true;
                            if (mapped)
                            {
                                // If the glyphs are fallbacks we don't want them as
                                // we've already captured them on the first run.
                                break;
                            }
                        }

                        m.Add(new GlyphMetrics(gm, codePoint));
                    }
                }

                if (m.Count > 0)
                {
                    this.glyphs[i] = new CodePointGlyphs(codePoint, glyphIds.ToArray());
                    this.offsets[i] = offset;
                    this.map[offset] = m.ToArray();
                }
            }

            return !hasFallBacks;
        }

        /// <summary>
        /// Applies an offset to the glyphs at the given index and id.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="index">The zero-based index of the elements to offset.</param>
        /// <param name="glyphId">The id of the glyph to offset.</param>
        /// <param name="x">The x-offset.</param>
        /// <param name="y">The y-offset.</param>
        public void Offset(IFontMetrics fontMetrics, ushort index, ushort glyphId, short x, short y)
        {
            foreach (GlyphMetrics m in this.map[this.offsets[index]])
            {
                if (m.GlyphId == glyphId && m.FontMetrics == fontMetrics)
                {
                    m.ApplyOffset(x, y);
                }
            }
        }

        /// <summary>
        /// Updates the advanced metrics of the glyphs at the given index and id.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="index">The zero-based index of the elements to offset.</param>
        /// <param name="glyphId">The id of the glyph to offset.</param>
        /// <param name="x">The x-advance.</param>
        /// <param name="y">The y-advance.</param>
        public void Advance(IFontMetrics fontMetrics, ushort index, ushort glyphId, short x, short y)
        {
            foreach (GlyphMetrics m in this.map[this.offsets[index]])
            {
                if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
                {
                    m.ApplyAdvance(x, this.mode == LayoutMode.Horizontal ? (short)0 : y);
                }
            }
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

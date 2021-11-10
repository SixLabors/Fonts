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
    /// Represents a collection of glyph metrics that are mapped to input codepoints.
    /// </summary>
    internal sealed class GlyphPositioningCollection : IGlyphShapingCollection
    {
        /// <summary>
        /// Contains a map between the index of a map within the collection, it's codepoint
        /// and glyph ids.
        /// </summary>
        private readonly List<GlyphShapingData> glyphs = new();

        /// <summary>
        /// Contains a map between the index of a map within the collection and its offset.
        /// </summary>
        private readonly List<int> offsets = new();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyphs.
        /// </summary>
        private readonly Dictionary<int, GlyphMetrics[]> map = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphPositioningCollection"/> class.
        /// </summary>
        /// <param name="mode">The text layout mode.</param>
        public GlyphPositioningCollection(LayoutMode mode) => this.IsVerticalLayoutMode = mode.IsVertical();

        /// <inheritdoc />
        public int Count => this.offsets.Count;

        /// <inheritdoc />
        public bool IsVerticalLayoutMode { get; }

        /// <inheritdoc />
        public ReadOnlySpan<ushort> this[int index] => this.glyphs[index].GlyphIds;

        /// <inheritdoc />
        public GlyphShapingData GetGlyphShapingData(int index) => this.glyphs[index];

        /// <inheritdoc />
        public void AddShapingFeature(int index, TagEntry feature)
            => this.glyphs[index].Features.Add(feature);

        /// <inheritdoc />
        public void EnableShapingFeature(int index, Tag feature)
        {
            List<TagEntry> features = this.glyphs[index].Features;
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
        /// Gets the glyph metrics at the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="metrics">
        /// When this method returns, contains the glyph metrics associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the metrics parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>The <see cref="T:GlyphMetrics[]"/>.</returns>
        public bool TryGetGlyphMetricsAtOffset(int offset, [NotNullWhen(true)] out GlyphMetrics[]? metrics)
            => this.map.TryGetValue(offset, out metrics);

        /// <summary>
        /// Adds the collection of glyph ids to the metrics collection.
        /// Adding subsequent collections will overwrite any glyphs that have been previously
        /// identified as fallbacks.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="collection">The glyph substitution collection.</param>
        /// <param name="options">The renderer options.</param>
        /// <returns><see langword="true"/> if the metrics collection does not contain any fallbacks; otherwise <see langword="false"/>.</returns>
        public bool TryAddOrUpdate(FontMetrics fontMetrics, GlyphSubstitutionCollection collection, RendererOptions options)
        {
            if (this.Count == 0)
            {
                return this.Add(fontMetrics, collection, options);
            }

            bool hasFallBacks = false;
            List<int> orphans = new();
            for (int i = 0; i < this.offsets.Count; i++)
            {
                int offset = this.offsets[i];
                if (!collection.TryGetGlyphShapingDataAtOffset(offset, out GlyphShapingData? data))
                {
                    // If a font had glyphs but a follow up font also has them and can substitute. e.g ligatures
                    // then we end up with orphaned fallbacks. We need to remove them.
                    orphans.Add(i);
                    continue;
                }

                GlyphMetrics[] metrics = this.map[offset];
                if (metrics[0].GlyphType != GlyphType.Fallback)
                {
                    // We've already got the correct glyph.
                    continue;
                }

                CodePoint codePoint = data.CodePoint;
                ushort[] glyphIds = data.GlyphIds;

                var m = new List<GlyphMetrics>(glyphIds.Length);
                foreach (ushort id in glyphIds)
                {
                    // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                    // cache the original in the font metrics and only update our collection.
                    foreach (GlyphMetrics gm in fontMetrics.GetGlyphMetrics(codePoint, id, options.ColorFontSupport))
                    {
                        if (gm.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
                        {
                            // If the glyphs are fallbacks we don't want them as
                            // we've already captured them on the first run.
                            hasFallBacks = true;
                            break;
                        }

                        m.Add(new GlyphMetrics(gm, codePoint));
                    }
                }

                if (m.Count > 0)
                {
                    GlyphMetrics[] gm = m.ToArray();
                    this.map[offset] = gm;
                    this.glyphs[i] = new(data, true) { Bounds = new(0, 0, gm[0].AdvanceWidth, gm[0].AdvanceHeight) };
                    this.offsets[i] = offset;
                }
            }

            // Remove any orphans.
            for (int i = orphans.Count - 1; i >= 0; i--)
            {
                int idx = orphans[i];
                this.map.Remove(this.offsets[idx]);
                this.offsets.RemoveAt(idx);
                this.glyphs.RemoveAt(idx);
            }

            return !hasFallBacks;
        }

        private bool Add(FontMetrics fontMetrics, GlyphSubstitutionCollection collection, RendererOptions options)
        {
            bool hasFallBacks = false;
            for (int i = 0; i < collection.Count; i++)
            {
                GlyphShapingData data = collection.GetGlyphShapingData(i, out int offset);
                CodePoint codePoint = data.CodePoint;
                ushort[] glyphIds = data.GlyphIds;

                var m = new List<GlyphMetrics>(glyphIds.Length);
                foreach (ushort id in glyphIds)
                {
                    // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                    // cache the original in the font metrics and only update our collection.
                    foreach (GlyphMetrics gm in fontMetrics.GetGlyphMetrics(codePoint, id, options.ColorFontSupport))
                    {
                        if (gm.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
                        {
                            hasFallBacks = true;
                        }

                        m.Add(new GlyphMetrics(gm, codePoint));
                    }
                }

                if (m.Count > 0)
                {
                    GlyphMetrics[] gm = m.ToArray();
                    this.map[offset] = gm;
                    if (this.IsVerticalLayoutMode)
                    {
                        this.glyphs.Add(new(data, true) { Bounds = new(0, 0, 0, gm[0].AdvanceHeight) });
                    }
                    else
                    {
                        this.glyphs.Add(new(data, true) { Bounds = new(0, 0, gm[0].AdvanceWidth, 0) });
                    }

                    this.offsets.Add(offset);
                }
            }

            return !hasFallBacks;
        }

        /// <summary>
        /// Updates the position of the glyph at the specified index.
        /// </summary>
        /// <param name="fontMetrics">The font metrics.</param>
        /// <param name="index">The zero-based index of the element.</param>
        public void UpdatePosition(FontMetrics fontMetrics, ushort index)
        {
            GlyphShapingData data = this.GetGlyphShapingData(index);
            ushort glyphId = data.GlyphIds[0];
            foreach (GlyphMetrics m in this.map[this.offsets[index]])
            {
                if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
                {
                    m.ApplyOffset((short)data.Bounds.X, (short)data.Bounds.Y);
                    m.SetAdvanceWidth((ushort)data.Bounds.Width);
                    m.SetAdvanceHeight((ushort)data.Bounds.Height);
                }
            }
        }

        /// <summary>
        /// Updates the advanced metrics of the glyphs at the given index and id,
        /// adding dx and dy to the current advance.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="index">The zero-based index of the element.</param>
        /// <param name="glyphId">The id of the glyph to offset.</param>
        /// <param name="dx">The delta x-advance.</param>
        /// <param name="dy">The delta y-advance.</param>
        public void Advance(FontMetrics fontMetrics, ushort index, ushort glyphId, short dx, short dy)
        {
            foreach (GlyphMetrics m in this.map[this.offsets[index]])
            {
                if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
                {
                    m.ApplyAdvance(dx, this.IsVerticalLayoutMode ? dy : (short)0);
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether the element at the given index should be processed.
        /// </summary>
        /// <param name="fontMetrics">The font face with metrics.</param>
        /// <param name="index">The zero-based index of the elements to offset.</param>
        /// <returns><see langword="true"/> if the element should be processed; otherwise, <see langword="false"/>.</returns>
        public bool ShouldProcess(FontMetrics fontMetrics, ushort index)
            => this.map[this.offsets[index]][0].FontMetrics == fontMetrics;
    }
}

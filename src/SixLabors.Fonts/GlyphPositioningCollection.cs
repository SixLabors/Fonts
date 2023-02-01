// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        /// Contains a map the index of a map within the collection, non-sequential codepoint offsets, and their glyph ids, point size, and mtrics.
        /// </summary>
        private readonly List<GlyphPositioningData> glyphs = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphPositioningCollection"/> class.
        /// </summary>
        /// <param name="textOptions">The text options.</param>
        public GlyphPositioningCollection(TextOptions textOptions)
        {
            this.TextOptions = textOptions;
            this.IsVerticalLayoutMode = textOptions.LayoutMode.IsVertical();
        }

        /// <inheritdoc />
        public int Count => this.glyphs.Count;

        /// <inheritdoc />
        public bool IsVerticalLayoutMode { get; }

        /// <inheritdoc />
        public TextOptions TextOptions { get; }

        /// <inheritdoc />
        public ushort this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.glyphs[index].Data.GlyphId;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GlyphShapingData GetGlyphShapingData(int index) => this.glyphs[index].Data;

        /// <inheritdoc />
        public void AddShapingFeature(int index, TagEntry feature)
            => this.glyphs[index].Data.Features.Add(feature);

        /// <inheritdoc />
        public void EnableShapingFeature(int index, Tag feature)
        {
            List<TagEntry> features = this.glyphs[index].Data.Features;
            for (int i = 0; i < features.Count; i++)
            {
                TagEntry tagEntry = features[i];
                if (tagEntry.Tag == feature)
                {
                    tagEntry.Enabled = true;
                    features[i] = tagEntry;
                    break;
                }
            }
        }

        /// <inheritdoc />
        public void DisableShapingFeature(int index, Tag feature)
        {
            List<TagEntry> features = this.glyphs[index].Data.Features;
            for (int i = 0; i < features.Count; i++)
            {
                TagEntry tagEntry = features[i];
                if (tagEntry.Tag == feature)
                {
                    tagEntry.Enabled = false;
                    features[i] = tagEntry;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the glyph metrics at the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="pointSize">The font size in PT units of the font containing this glyph.</param>
        /// <param name="isDecomposed">Whether the glyph is the result of a decomposition substitution.</param>
        /// <param name="metrics">
        /// When this method returns, contains the glyph metrics associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the metrics parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>The metrics.</returns>
        public bool TryGetGlyphMetricsAtOffset(int offset, out float pointSize, out bool isDecomposed, [NotNullWhen(true)] out IReadOnlyList<GlyphMetrics>? metrics)
        {
            List<GlyphMetrics> match = new();
            pointSize = 0;
            isDecomposed = false;
            for (int i = 0; i < this.glyphs.Count; i++)
            {
                if (this.glyphs[i].Offset == offset)
                {
                    GlyphPositioningData glyph = this.glyphs[i];
                    isDecomposed = glyph.Data.IsDecomposed;
                    pointSize = glyph.PointSize;
                    match.AddRange(glyph.Metrics);
                }
                else if (match.Count > 0)
                {
                    // Offsets, though non-sequential, are sorted, so we can stop searching.
                    break;
                }
            }

            metrics = match;
            return match.Count > 0;
        }

        /// <summary>
        /// Updates the collection of glyph ids to the metrics collection to overwrite any glyphs that have been previously
        /// identified as fallbacks.
        /// </summary>
        /// <param name="font">The font face with metrics.</param>
        /// <param name="collection">The glyph substitution collection.</param>
        /// <returns><see langword="true"/> if the metrics collection does not contain any fallbacks; otherwise <see langword="false"/>.</returns>
        public bool TryUpdate(Font font, GlyphSubstitutionCollection collection)
        {
            FontMetrics fontMetrics = font.FontMetrics;
            ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
            bool hasFallBacks = false;
            List<int> orphans = new();
            for (int i = 0; i < this.glyphs.Count; i++)
            {
                GlyphPositioningData current = this.glyphs[i];
                if (current.Metrics[0].GlyphType != GlyphType.Fallback)
                {
                    // We've already got the correct glyph.
                    continue;
                }

                int offset = current.Offset;
                float pointSize = current.PointSize;
                if (collection.TryGetGlyphShapingDataAtOffset(offset, out IReadOnlyList<GlyphShapingData>? data))
                {
                    ushort shiftXY = 0;
                    int replacementCount = 0;
                    for (int j = 0; j < data.Count; j++)
                    {
                        GlyphShapingData shape = data[j];
                        ushort id = shape.GlyphId;
                        CodePoint codePoint = shape.CodePoint;
                        bool isDecomposed = shape.IsDecomposed;

                        // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                        // cache the original in the font metrics and only update our collection.
                        var metrics = new List<GlyphMetrics>(data.Count);
                        TextAttributes textAttributes = shape.TextRun.TextAttributes;
                        TextDecorations textDecorations = shape.TextRun.TextDecorations;
                        foreach (GlyphMetrics gm in fontMetrics.GetGlyphMetrics(codePoint, id, textAttributes, textDecorations, colorFontSupport))
                        {
                            if (gm.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
                            {
                                // If the glyphs are fallbacks we don't want them as
                                // we've already captured them on the first run.
                                hasFallBacks = true;
                                break;
                            }

                            // Clone and offset the glyph for rendering.
                            // If the glyph is the result of a decomposition substitution we need to offset it.
                            // We slip the text run in here while we clone so we have it available to the renderer.
                            GlyphMetrics clone = gm.CloneForRendering();
                            if (isDecomposed)
                            {
                                if (!this.IsVerticalLayoutMode)
                                {
                                    clone.ApplyOffset((short)shiftXY, 0);
                                    shiftXY += clone.AdvanceWidth;
                                }
                                else
                                {
                                    clone.ApplyOffset(0, (short)shiftXY);
                                    shiftXY += clone.AdvanceHeight;
                                }
                            }

                            metrics.Add(clone);
                        }

                        if (metrics.Count > 0)
                        {
                            if (j == 0)
                            {
                                // There should only be a single fallback glyph at this position from the previous collection.
                                this.glyphs.RemoveAt(i);
                            }

                            // Track the number of inserted glyphs at the offset so we can correctly increment our position.
                            ushort maxAdvancedWidth = 0;
                            ushort maxAdvancedHeight = 0;
                            for (int k = 0; k < metrics.Count; k++)
                            {
                                maxAdvancedWidth = Math.Max(maxAdvancedWidth, metrics[k].AdvanceWidth);
                                maxAdvancedHeight = Math.Max(maxAdvancedHeight, metrics[k].AdvanceHeight);
                            }

                            this.glyphs.Insert(i += replacementCount, new(offset, new(shape, true) { Bounds = new(0, 0, maxAdvancedWidth, maxAdvancedHeight) }, pointSize, metrics.ToArray()));
                            replacementCount++;
                        }
                    }
                }
                else
                {
                    // If a font had glyphs but a follow up font also has them and can substitute. e.g ligatures
                    // then we end up with orphaned fallbacks. We need to remove them.
                    orphans.Add(i);
                }
            }

            // Remove any orphans.
            for (int i = orphans.Count - 1; i >= 0; i--)
            {
                this.glyphs.RemoveAt(orphans[i]);
            }

            return !hasFallBacks;
        }

        /// <summary>
        /// Adds the collection of glyph ids to the metrics collection.
        /// identified as fallbacks.
        /// </summary>
        /// <param name="font">The font face with metrics.</param>
        /// <param name="collection">The glyph substitution collection.</param>
        /// <returns><see langword="true"/> if the metrics collection does not contain any fallbacks; otherwise <see langword="false"/>.</returns>
        public bool TryAdd(Font font, GlyphSubstitutionCollection collection)
        {
            bool hasFallBacks = false;
            FontMetrics fontMetrics = font.FontMetrics;
            ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
            ushort shiftXY = 0;
            for (int i = 0; i < collection.Count; i++)
            {
                GlyphShapingData data = collection.GetGlyphShapingData(i, out int offset);
                CodePoint codePoint = data.CodePoint;
                ushort id = data.GlyphId;
                List<GlyphMetrics> metrics = new();

                bool isDecomposed = data.IsDecomposed;
                if (!isDecomposed)
                {
                    shiftXY = 0;
                }

                // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                // cache the original in the font metrics and only update our collection.
                TextAttributes textAttributes = data.TextRun.TextAttributes;
                TextDecorations textDecorations = data.TextRun.TextDecorations;
                foreach (GlyphMetrics gm in fontMetrics.GetGlyphMetrics(codePoint, id, textAttributes, textDecorations, colorFontSupport))
                {
                    if (gm.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
                    {
                        hasFallBacks = true;
                    }

                    // Clone and offset the glyph for rendering.
                    // If the glyph is the result of a decomposition substitution we need to offset it.
                    GlyphMetrics clone = gm.CloneForRendering();
                    if (isDecomposed)
                    {
                        if (!this.IsVerticalLayoutMode)
                        {
                            clone.ApplyOffset((short)shiftXY, 0);
                            shiftXY += clone.AdvanceWidth;
                        }
                        else
                        {
                            clone.ApplyOffset(0, (short)shiftXY);
                            shiftXY += clone.AdvanceHeight;
                        }
                    }

                    metrics.Add(clone);
                }

                if (metrics.Count > 0)
                {
                    GlyphMetrics[] gm = metrics.ToArray();
                    if (this.IsVerticalLayoutMode)
                    {
                        this.glyphs.Add(new(offset, new(data, true) { Bounds = new(0, 0, 0, gm[0].AdvanceHeight) }, font.Size, gm));
                    }
                    else
                    {
                        this.glyphs.Add(new(offset, new(data, true) { Bounds = new(0, 0, gm[0].AdvanceWidth, 0) }, font.Size, gm));
                    }
                }
            }

            return !hasFallBacks;
        }

        /// <summary>
        /// Updates the position of the glyph at the specified index.
        /// </summary>
        /// <param name="fontMetrics">The font metrics.</param>
        /// <param name="index">The zero-based index of the element.</param>
        public void UpdatePosition(FontMetrics fontMetrics, int index)
        {
            GlyphShapingData data = this.GetGlyphShapingData(index);
            bool isDirtyXY = data.Bounds.IsDirtyXY;
            bool isDirtyWH = data.Bounds.IsDirtyWH;
            if (!isDirtyXY && !isDirtyWH)
            {
                return;
            }

            ushort glyphId = data.GlyphId;
            foreach (GlyphMetrics m in this.glyphs[index].Metrics)
            {
                if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
                {
                    if (isDirtyXY)
                    {
                        m.ApplyOffset((short)data.Bounds.X, (short)data.Bounds.Y);
                    }

                    if (isDirtyWH)
                    {
                        m.SetAdvanceWidth((ushort)data.Bounds.Width);
                        m.SetAdvanceHeight((ushort)data.Bounds.Height);
                    }
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
        public void Advance(FontMetrics fontMetrics, int index, ushort glyphId, short dx, short dy)
        {
            foreach (GlyphMetrics m in this.glyphs[index].Metrics)
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
        /// <param name="index">The zero-based index of the elements to position.</param>
        /// <returns><see langword="true"/> if the element should be processed; otherwise, <see langword="false"/>.</returns>
        public bool ShouldProcess(FontMetrics fontMetrics, int index)
            => this.glyphs[index].Metrics[0].FontMetrics == fontMetrics;

        private class GlyphPositioningData
        {
            public GlyphPositioningData(int offset, GlyphShapingData data, float pointSize, GlyphMetrics[] metrics)
            {
                this.Offset = offset;
                this.Data = data;
                this.PointSize = pointSize;
                this.Metrics = metrics;
            }

            public int Offset { get; set; }

            public GlyphShapingData Data { get; set; }

            public float PointSize { get; set; }

            public GlyphMetrics[] Metrics { get; set; }
        }
    }
}

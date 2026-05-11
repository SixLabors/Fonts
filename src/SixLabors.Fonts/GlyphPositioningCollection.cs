// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a collection of glyph metrics that are mapped to input codepoints.
/// </summary>
internal sealed class GlyphPositioningCollection : IGlyphShapingCollection
{
    /// <summary>
    /// Contains a map the index of a map within the collection, non-sequential codepoint offsets, and their glyph ids, point size, and mtrics.
    /// </summary>
    private readonly List<GlyphPositioningData> glyphs = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphPositioningCollection"/> class.
    /// </summary>
    /// <param name="textOptions">The text options.</param>
    public GlyphPositioningCollection(TextOptions textOptions) => this.TextOptions = textOptions;

    /// <inheritdoc />
    public int Count => this.glyphs.Count;

    /// <inheritdoc />
    public TextOptions TextOptions { get; }

    /// <inheritdoc />
    public GlyphShapingData this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.glyphs[index].Data;
    }

    /// <inheritdoc />
    public void AddShapingFeature(int index, TagEntry feature)
    {
        GlyphShapingData data = this.glyphs[index].Data;
        data.Features.Add(feature);
        if (feature.Enabled)
        {
            data.EnabledFeatureTags.Add(feature.Tag);
        }
    }

    /// <inheritdoc />
    public void EnableShapingFeature(int index, Tag feature)
    {
        GlyphShapingData data = this.glyphs[index].Data;
        List<TagEntry> features = data.Features;
        for (int i = 0; i < features.Count; i++)
        {
            TagEntry tagEntry = features[i];
            if (tagEntry.Tag == feature)
            {
                tagEntry.Enabled = true;
                features[i] = tagEntry;
                data.EnabledFeatureTags.Add(feature);
                break;
            }
        }
    }

    /// <inheritdoc />
    public void DisableShapingFeature(int index, Tag feature)
    {
        GlyphShapingData data = this.glyphs[index].Data;
        List<TagEntry> features = data.Features;
        for (int i = 0; i < features.Count; i++)
        {
            TagEntry tagEntry = features[i];
            if (tagEntry.Tag == feature)
            {
                tagEntry.Enabled = false;
                features[i] = tagEntry;
                data.EnabledFeatureTags.Remove(feature);
                break;
            }
        }
    }

    /// <summary>
    /// Gets the glyph metrics at the given codepoint offset.
    /// </summary>
    /// <param name="offset">The zero-based index within the input codepoint collection.</param>
    /// <param name="startIndex">
    /// The index within the glyph list to start searching from. Updated to the position of the match
    /// so that subsequent calls with increasing offsets avoid rescanning from the beginning.
    /// </param>
    /// <param name="pointSize">The font size in PT units of the font containing this glyph.</param>
    /// <param name="isSubstituted">Whether the glyph is the result of a substitution.</param>
    /// <param name="isVerticalSubstitution">Whether the glyph is the result of a vertical substitution.</param>
    /// <param name="isDecomposed">Whether the glyph is the result of a decomposition substitution.</param>
    /// <param name="data">
    /// When this method returns, contains the glyph metrics associated with the specified offset,
    /// if the value is found; otherwise, the default value for the type of the metrics parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>The metrics.</returns>
    public bool TryGetGlyphMetricsAtOffset(
        int offset,
        ref int startIndex,
        out float pointSize,
        out bool isSubstituted,
        out bool isVerticalSubstitution,
        out bool isDecomposed,
        [NotNullWhen(true)] out IReadOnlyList<GlyphPositioningData>? data)
    {
        List<GlyphPositioningData> match = [];
        pointSize = 0;
        isSubstituted = false;
        isVerticalSubstitution = false;
        isDecomposed = false;

        Tag vert = KnownFeatureTags.VerticalAlternates;
        Tag vrt2 = KnownFeatureTags.VerticalAlternatesAndRotation;
        Tag vrtr = KnownFeatureTags.VerticalAlternatesForRotation;

        for (int i = startIndex; i < this.glyphs.Count; i++)
        {
            if (this.glyphs[i].Offset == offset)
            {
                if (match.Count == 0)
                {
                    startIndex = i;
                }

                GlyphPositioningData glyph = this.glyphs[i];
                if (!glyph.Data.IsPlaceholder)
                {
                    isSubstituted = glyph.Data.IsSubstituted;
                    isDecomposed = glyph.Data.IsDecomposed;

                    foreach (Tag feature in glyph.Data.AppliedFeatures)
                    {
                        isVerticalSubstitution |= feature == vert;
                        isVerticalSubstitution |= feature == vrt2;
                        isVerticalSubstitution |= feature == vrtr;
                    }

                    pointSize = glyph.PointSize;
                }

                match.Add(glyph);
            }
            else if (match.Count > 0)
            {
                // Offsets, though non-sequential, are sorted, so we can stop searching.
                break;
            }
        }

        data = match;
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
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
        bool hasFallBacks = false;
        List<int> orphans = [];

        Tag vert = KnownFeatureTags.VerticalAlternates;
        Tag vrt2 = KnownFeatureTags.VerticalAlternatesAndRotation;
        Tag vrtr = KnownFeatureTags.VerticalAlternatesForRotation;

        for (int i = 0; i < this.glyphs.Count; i++)
        {
            GlyphPositioningData current = this.glyphs[i];
            if (current.Metrics.GlyphType != GlyphType.Fallback)
            {
                // We've already got the correct glyph.
                continue;
            }

            int offset = current.Offset;
            float pointSize = current.PointSize;
            if (collection.TryGetGlyphShapingDataAtOffset(offset, out IReadOnlyList<GlyphShapingData>? data))
            {
                int replacementCount = 0;
                for (int j = 0; j < data.Count; j++)
                {
                    GlyphShapingData shape = data[j];
                    ushort id = shape.GlyphId;
                    CodePoint codePoint = shape.CodePoint;

                    // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
                    // cache the original in the font metrics and only update our collection.
                    TextAttributes textAttributes = shape.TextRun.TextAttributes;
                    TextDecorations textDecorations = shape.TextRun.TextDecorations;

                    bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode);
                    foreach (Tag feature in shape.AppliedFeatures)
                    {
                        isVertical |= feature == vert;
                        isVertical |= feature == vrt2;
                        isVertical |= feature == vrtr;
                    }

                    FontGlyphMetrics metrics = fontMetrics.GetGlyphMetrics(codePoint, id, textAttributes, textDecorations, layoutMode, colorFontSupport);
                    {
                        // If the glyphs are fallbacks we don't want them as
                        // we've already captured them on the first run.
                        if (metrics.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
                        {
                            hasFallBacks = true;
                        }
                    }

                    if (metrics.GlyphType != GlyphType.Fallback)
                    {
                        if (replacementCount == 0)
                        {
                            // There should only be a single fallback glyph at this position from the previous collection.
                            this.glyphs.RemoveAt(i);
                        }

                        // We only want a single dimensional advance for positioning.
                        GlyphShapingBounds bounds = isVertical
                            ? new(0, 0, 0, metrics.AdvanceHeight)
                            : new(0, 0, metrics.AdvanceWidth, 0);

                        // Track the number of inserted glyphs at the offset so we can correctly increment our position.
                        this.glyphs.Insert(i += replacementCount, new(offset, new(shape, true) { Bounds = bounds }, font, pointSize, metrics.CloneForRendering(shape.TextRun)));
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
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;

        Tag vert = KnownFeatureTags.VerticalAlternates;
        Tag vrt2 = KnownFeatureTags.VerticalAlternatesAndRotation;
        Tag vrtr = KnownFeatureTags.VerticalAlternatesForRotation;

        for (int i = 0; i < collection.Count; i++)
        {
            GlyphShapingData data = collection.GetGlyphShapingData(i, out int offset);
            CodePoint codePoint = data.CodePoint;
            ushort id = data.GlyphId;

            if (data.IsPlaceholder)
            {
                // Placeholders are synthetic glyphs: they need layout metrics but must not
                // go through font glyph lookup, fallback resolution, or GPOS positioning.
                StreamFontMetrics streamFontMetrics = fontMetrics is FileFontMetrics fileFontMetrics
                    ? fileFontMetrics.StreamFontMetrics
                    : (StreamFontMetrics)fontMetrics;

                FontGlyphMetrics placeholderMetrics = new PlaceholderGlyphMetrics(
                    streamFontMetrics,
                    data.TextRun.Placeholder.GetValueOrDefault(),
                    font.Size,
                    this.TextOptions.Dpi,
                    data.TextRun);

                GlyphShapingBounds placeholderBounds = layoutMode.IsVertical()
                    ? new(0, 0, 0, placeholderMetrics.AdvanceHeight)
                    : new(0, 0, placeholderMetrics.AdvanceWidth, 0);

                GlyphShapingData placeholderData = new(data, true)
                {
                    Bounds = placeholderBounds,
                    IsPositioned = true
                };

                this.glyphs.Add(new(offset, placeholderData, font, font.Size, placeholderMetrics));
                continue;
            }

            // Perform a semi-deep clone (FontMetrics is not cloned) so we can continue to
            // cache the original in the font metrics and only update our collection.
            TextAttributes textAttributes = data.TextRun.TextAttributes;
            TextDecorations textDecorations = data.TextRun.TextDecorations;

            bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode);
            foreach (Tag feature in data.AppliedFeatures)
            {
                isVertical |= feature == vert;
                isVertical |= feature == vrt2;
                isVertical |= feature == vrtr;
            }

            FontGlyphMetrics metrics = fontMetrics.GetGlyphMetrics(codePoint, id, textAttributes, textDecorations, layoutMode, colorFontSupport);

            if (metrics.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
            {
                hasFallBacks = true;
            }

            // We only want a single dimensional advance for positioning.
            GlyphShapingBounds bounds = isVertical
                ? new(0, 0, 0, metrics.AdvanceHeight)
                : new(0, 0, metrics.AdvanceWidth, 0);

            this.glyphs.Add(new(offset, new(data, true) { Bounds = bounds }, font, font.Size, metrics.CloneForRendering(data.TextRun)));
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
        GlyphShapingData data = this[index];
        bool isDirtyXY = data.Bounds.IsDirtyXY;
        bool isDirtyWH = data.Bounds.IsDirtyWH;
        if (!isDirtyXY && !isDirtyWH)
        {
            // No change required but the glyph has been processed.
            data.IsPositioned = true;
            return;
        }

        ushort glyphId = data.GlyphId;
        FontGlyphMetrics m = this.glyphs[index].Metrics;

        if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
        {
            if (isDirtyXY)
            {
                m.ApplyOffset((short)data.Bounds.X, (short)data.Bounds.Y);
                data.IsPositioned = true;
            }

            if (isDirtyWH)
            {
                m.SetAdvanceWidth((ushort)data.Bounds.Width);
                m.SetAdvanceHeight((ushort)data.Bounds.Height);
                data.IsPositioned = true;
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
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        Tag vert = KnownFeatureTags.VerticalAlternates;
        Tag vrt2 = KnownFeatureTags.VerticalAlternatesAndRotation;
        Tag vrtr = KnownFeatureTags.VerticalAlternatesForRotation;

        GlyphPositioningData glyph = this.glyphs[index];
        FontGlyphMetrics m = glyph.Metrics;

        if (m.GlyphId == glyphId && fontMetrics == m.FontMetrics)
        {
            bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(m.CodePoint, layoutMode);

            foreach (Tag feature in glyph.Data.AppliedFeatures)
            {
                isVertical |= feature == vert;
                isVertical |= feature == vrt2;
                isVertical |= feature == vrtr;
            }

            m.ApplyAdvance(dx, isVertical ? dy : (short)0);
        }
    }

    /// <summary>
    /// Returns a value indicating whether the element at the given index should be processed.
    /// </summary>
    /// <param name="fontMetrics">The font face with metrics.</param>
    /// <param name="index">The zero-based index of the elements to position.</param>
    /// <returns><see langword="true"/> if the element should be processed; otherwise, <see langword="false"/>.</returns>
    public bool ShouldProcess(FontMetrics fontMetrics, int index)
    {
        GlyphPositioningData data = this.glyphs[index];
        if (data.Data.IsPositioned)
        {
            return false;
        }

        return data.Metrics.FontMetrics == fontMetrics;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class GlyphPositioningData
    {
        public GlyphPositioningData(int offset, GlyphShapingData data, Font font, float pointSize, FontGlyphMetrics metrics)
        {
            this.Offset = offset;
            this.Data = data;
            this.Font = font;
            this.PointSize = pointSize;
            this.Metrics = metrics;
        }

        public int Offset { get; set; }

        public GlyphShapingData Data { get; set; }

        public Font Font { get; set; }

        public float PointSize { get; set; }

        public FontGlyphMetrics Metrics { get; set; }

        private string DebuggerDisplay => FormattableString.Invariant($"Offset: {this.Offset}, Data: {this.Data.ToDebuggerDisplay()}");
    }
}

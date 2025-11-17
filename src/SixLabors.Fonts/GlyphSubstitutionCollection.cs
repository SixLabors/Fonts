// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a collection of glyph indices that are mapped to input codepoints.
/// </summary>
internal sealed class GlyphSubstitutionCollection : IGlyphShapingCollection
{
    /// <summary>
    /// Contains a map the index of a map within the collection, non-sequential codepoint offsets, and their glyph ids.
    /// </summary>
    private readonly List<OffsetGlyphDataPair> glyphs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphSubstitutionCollection"/> class.
    /// </summary>
    /// <param name="textOptions">The text options.</param>
    public GlyphSubstitutionCollection(TextOptions textOptions) => this.TextOptions = textOptions;

    /// <summary>
    /// Gets the number of glyphs ids contained in the collection.
    /// This may be more or less than original input codepoint count (due to substitution process).
    /// </summary>
    public int Count => this.glyphs.Count;

    /// <inheritdoc />
    public TextOptions TextOptions { get; }

    /// <summary>
    /// Gets or sets the running id of any ligature glyphs contained withing this collection are a member of.
    /// </summary>
    public int LigatureId { get; set; } = 1;

    /// <inheritdoc />
    public GlyphShapingData this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.glyphs[index].Data;
    }

    /// <summary>
    /// Gets the shaping data at the specified position.
    /// </summary>
    /// <param name="index">The zero-based index of the elements to get.</param>
    /// <param name="offset">The zero-based index within the input codepoint collection.</param>
    /// <returns>The <see cref="GlyphShapingData"/>.</returns>
    internal GlyphShapingData GetGlyphShapingData(int index, out int offset)
    {
        OffsetGlyphDataPair pair = this.glyphs[index];
        offset = pair.Offset;
        return pair.Data;
    }

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
    /// Adds a clone of the glyph shaping data to the collection at the specified offset.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="offset">The zero-based index within the input codepoint collection.</param>
    public void AddGlyph(GlyphShapingData data, int offset)
        => this.glyphs.Add(new(offset, new(data, false)));

    /// <summary>
    /// Adds the glyph id and the codepoint it represents to the collection.
    /// </summary>
    /// <param name="glyphId">The id of the glyph to add.</param>
    /// <param name="codePoint">The codepoint the glyph represents.</param>
    /// <param name="direction">The resolved text direction for the codepoint.</param>
    /// <param name="textRun">The text run this glyph belongs to.</param>
    /// <param name="offset">The zero-based index within the input codepoint collection.</param>
    public void AddGlyph(ushort glyphId, CodePoint codePoint, TextDirection direction, TextRun textRun, int offset)
        => this.glyphs.Add(new(offset, new(textRun)
        {
            CodePoint = codePoint,
            Direction = direction,
            GlyphId = glyphId,
        }));

    /// <summary>
    /// Moves the specified glyph to the specified position.
    /// </summary>
    /// <param name="fromIndex">The index to move from.</param>
    /// <param name="toIndex">The index to move to.</param>
    public void MoveGlyph(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            return;
        }

        GlyphShapingData data = this[fromIndex];
        if (fromIndex > toIndex)
        {
            // Move item to the right
            for (int i = fromIndex; i > toIndex; i--)
            {
                this.glyphs[i].Data = this.glyphs[i - 1].Data;
            }
        }
        else
        {
            // Move item to the left
            for (int i = fromIndex; i < toIndex; i++)
            {
                this.glyphs[i].Data = this.glyphs[i + 1].Data;
            }
        }

        this.glyphs[toIndex].Data = data;
    }

    /// <summary>
    /// Performs a stable sort of the glyphs by the comparison delegate starting at the specified index.
    /// </summary>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <param name="comparer">The comparison delegate.</param>
    public void Sort(int startIndex, int endIndex, Comparison<GlyphShapingData> comparer)
    {
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            int j = i;
            while (j > startIndex && comparer(this[j - 1], this[i]) > 0)
            {
                j--;
            }

            if (i == j)
            {
                continue;
            }

            // Move item i to occupy place for item j, shift what's in between.
            this.MoveGlyph(i, j);
        }
    }

    /// <summary>
    /// Removes all elements from the collection.
    /// </summary>
    public void Clear()
    {
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
    public bool TryGetGlyphShapingDataAtOffset(int offset, [NotNullWhen(true)] out IReadOnlyList<GlyphShapingData>? data)
    {
        List<GlyphShapingData> match = [];
        for (int i = 0; i < this.glyphs.Count; i++)
        {
            if (this.glyphs[i].Offset == offset)
            {
                match.Add(this.glyphs[i].Data);
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
    /// Performs a 1:1 replacement of a glyph id at the given position.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="feature">The feature to apply to the glyph at the specified index.</param>
    public void Replace(int index, ushort glyphId, Tag feature)
    {
        GlyphShapingData current = this.glyphs[index].Data;
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.MarkAttachment = -1;
        current.CursiveAttachment = -1;
        current.IsSubstituted = true;
        current.AppliedFeatures.Add(feature);
    }

    /// <summary>
    /// Performs a 1:1 replacement of a glyph id at the given position while removing a series of glyph ids at the given positions within the sequence.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="removalIndices">The indices at which to remove elements.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="ligatureId">The ligature id.</param>
    /// <param name="feature">The feature to apply to the glyph at the specified index.</param>
    public void Replace(int index, ReadOnlySpan<int> removalIndices, ushort glyphId, int ligatureId, Tag feature)
    {
        // Remove the glyphs at each index.
        int codePointCount = 0;
        CodePoint codePoint = default;
        for (int i = removalIndices.Length - 1; i >= 0; i--)
        {
            int match = removalIndices[i];
            codePointCount += this.glyphs[match].Data.CodePointCount;
            CodePoint currentCodePoint = this.glyphs[match].Data.CodePoint;
            if (!UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) || UnicodeUtility.ShouldRenderWhiteSpaceOnly(codePoint))
            {
                if (!CodePoint.IsZeroWidthJoiner(currentCodePoint) && !CodePoint.IsZeroWidthNonJoiner(currentCodePoint))
                {
                    codePoint = currentCodePoint;
                }
            }

            this.glyphs.RemoveAt(match);
        }

        // Assign our new id at the index.
        GlyphShapingData current = this.glyphs[index].Data;
        if (codePoint != default)
        {
            current.CodePoint = codePoint;
        }

        current.CodePointCount += codePointCount;
        current.GlyphId = glyphId;
        current.LigatureId = ligatureId;
        current.IsLigated = true;
        current.LigatureComponent = -1;
        current.MarkAttachment = -1;
        current.CursiveAttachment = -1;
        current.IsSubstituted = true;
        current.AppliedFeatures.Add(feature);
    }

    /// <summary>
    /// Performs a 1:1 replacement of a glyph id at the given position while removing a series of glyph ids.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="count">The number of glyphs to remove.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="feature">The feature to apply to the glyph at the specified index.</param>
    public void Replace(int index, int count, ushort glyphId, Tag feature)
    {
        // Remove the glyphs at each index.
        int codePointCount = 0;
        CodePoint codePoint = default;
        for (int i = count; i > 0; i--)
        {
            int match = index + i;
            codePointCount += this.glyphs[match].Data.CodePointCount;
            CodePoint currentCodePoint = this.glyphs[match].Data.CodePoint;
            if (!UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) || UnicodeUtility.ShouldRenderWhiteSpaceOnly(codePoint))
            {
                if (!CodePoint.IsZeroWidthJoiner(currentCodePoint) && !CodePoint.IsZeroWidthNonJoiner(currentCodePoint))
                {
                    codePoint = currentCodePoint;
                }
            }

            this.glyphs.RemoveAt(match);
        }

        // Assign our new id at the index.
        GlyphShapingData current = this.glyphs[index].Data;
        if (codePoint != default)
        {
            current.CodePoint = codePoint;
        }

        current.CodePointCount += codePointCount;
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.MarkAttachment = -1;
        current.CursiveAttachment = -1;
        current.IsSubstituted = true;
        current.AppliedFeatures.Add(feature);
    }

    /// <summary>
    /// Replaces a single glyph id with a collection of glyph ids.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="glyphIds">The collection of replacement glyph ids.</param>
    /// <param name="feature">The feature to apply to the glyph at the specified index.</param>
    public void Replace(int index, ReadOnlySpan<ushort> glyphIds, Tag feature)
    {
        if (glyphIds.Length > 0)
        {
            OffsetGlyphDataPair pair = this.glyphs[index];
            GlyphShapingData current = pair.Data;
            current.GlyphId = glyphIds[0];
            current.LigatureComponent = 0;
            current.MarkAttachment = -1;
            current.CursiveAttachment = -1;
            current.IsSubstituted = true;
            current.IsDecomposed = true;

            // Add additional glyphs from the rest of the sequence.
            if (glyphIds.Length > 1)
            {
                glyphIds = glyphIds[1..];
                for (int i = 0; i < glyphIds.Length; i++)
                {
                    GlyphShapingData data = new(current, false)
                    {
                        GlyphId = glyphIds[i],
                        LigatureComponent = i + 1
                    };

                    data.AppliedFeatures.Add(feature);

                    this.glyphs.Insert(++index, new(pair.Offset, data));
                }
            }
        }
        else
        {
            // Spec disallows removal of glyphs in this manner but it's common enough practice to allow it.
            // https://github.com/MicrosoftDocs/typography-issues/issues/673
            this.glyphs.RemoveAt(index);
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private class OffsetGlyphDataPair
    {
        public OffsetGlyphDataPair(int offset, GlyphShapingData data)
        {
            this.Offset = offset;
            this.Data = data;
        }

        public int Offset { get; set; }

        public GlyphShapingData Data { get; set; }

        private string DebuggerDisplay => FormattableString.Invariant($"Offset: {this.Offset}, Data: {this.Data.ToDebuggerDisplay()}");
    }
}

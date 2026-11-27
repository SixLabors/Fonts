// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// A Reverse Chaining Contextual Single Substitution subtable describes single glyph substitutions
/// in context with an ability to look back and/or look ahead in the sequence of glyphs.
/// The difference from other chaining lookups is that processing is applied in reverse order (from end of glyph sequence).
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-8-reverse-chaining-contextual-single-substitution-subtable"/>
/// </summary>
internal static class LookupType8SubTable
{
    /// <summary>
    /// Loads the reverse chaining contextual single substitution lookup subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort substFormat = reader.ReadUInt16();

        return substFormat switch
        {
            1 => LookupType8Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements reverse chaining contextual single substitution format 1 (coverage-based glyph contexts).
/// Substitution is processed in reverse order from the end of the glyph sequence.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#81-reverse-chaining-contextual-single-substitution-format-1-coverage-based-glyph-contexts"/>
/// </summary>
internal sealed class LookupType8Format1SubTable : LookupSubTable
{
    /// <summary>
    /// The array of substitute glyph IDs, ordered by coverage index.
    /// </summary>
    private readonly ushort[] substituteGlyphIds;

    /// <summary>
    /// The coverage table that defines the set of input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// The array of coverage tables for the backtrack sequence.
    /// </summary>
    private readonly CoverageTable[] backtrackCoverageTables;

    /// <summary>
    /// The array of coverage tables for the lookahead sequence.
    /// </summary>
    private readonly CoverageTable[] lookaheadCoverageTables;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType8Format1SubTable"/> class.
    /// </summary>
    /// <param name="substituteGlyphIds">The array of substitute glyph IDs.</param>
    /// <param name="coverageTable">The coverage table defining input glyphs.</param>
    /// <param name="backtrackCoverageTables">The coverage tables for the backtrack sequence.</param>
    /// <param name="lookaheadCoverageTables">The coverage tables for the lookahead sequence.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType8Format1SubTable(
        ushort[] substituteGlyphIds,
        CoverageTable coverageTable,
        CoverageTable[] backtrackCoverageTables,
        CoverageTable[] lookaheadCoverageTables,
        LookupFlags lookupFlags,
        ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.substituteGlyphIds = substituteGlyphIds;
        this.coverageTable = coverageTable;
        this.backtrackCoverageTables = backtrackCoverageTables;
        this.lookaheadCoverageTables = lookaheadCoverageTables;
    }

    /// <inheritdoc/>
    public override bool IsReverse => true;

    /// <summary>
    /// Loads the reverse chaining contextual single substitution format 1 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType8Format1SubTable"/>.</returns>
    public static LookupType8Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // ReverseChainSingleSubstFormat1
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | Type     | Name                                          | Description                                  |
        // +==========+===============================================+==============================================+
        // | uint16   | substFormat                                   | Format identifier: format = 1                |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | Offset16 | coverageOffset                                | Offset to Coverage table, from beginning     |
        // |          |                                               | of substitution subtable.                    |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | uint16   | backtrackGlyphCount                           | Number of glyphs in the backtrack sequence.  |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | Offset16 | backtrackCoverageOffsets[backtrackGlyphCount] | Array of offsets to coverage tables in       |
        // |          |                                               | backtrack sequence, in glyph sequence        |
        // |          |                                               | order.                                       |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | uint16   | lookaheadGlyphCount                           | Number of glyphs in lookahead sequence.      |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | Offset16 | lookaheadCoverageOffsets[lookaheadGlyphCount] | Array of offsets to coverage tables in       |
        // |          |                                               | lookahead sequence, in glyph sequence order. |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | uint16   | glyphCount                                    | Number of glyph IDs in the                   |
        // |          |                                               | substituteGlyphIDs array.                    |
        // +----------+-----------------------------------------------+----------------------------------------------+
        // | uint16   | substituteGlyphIDs[glyphCount]                | Array of substitute glyph IDs — ordered      |
        // |          |                                               | by Coverage index.                           |
        // +----------+-----------------------------------------------+----------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort backtrackGlyphCount = reader.ReadUInt16();

        using Buffer<ushort> backtrackCoverageOffsetsBuffer = new(backtrackGlyphCount);
        Span<ushort> backtrackCoverageOffsets = backtrackCoverageOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(backtrackCoverageOffsets);

        ushort lookaheadGlyphCount = reader.ReadUInt16();

        using Buffer<ushort> lookaheadCoverageOffsetsBuffer = new(lookaheadGlyphCount);
        Span<ushort> lookaheadCoverageOffsets = lookaheadCoverageOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(lookaheadCoverageOffsets);

        ushort glyphCount = reader.ReadUInt16();
        ushort[] substituteGlyphIds = reader.ReadUInt16Array(glyphCount);

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
        CoverageTable[] backtrackCoverageTables = CoverageTable.LoadArray(reader, offset, backtrackCoverageOffsets);
        CoverageTable[] lookaheadCoverageTables = CoverageTable.LoadArray(reader, offset, lookaheadCoverageOffsets);

        return new LookupType8Format1SubTable(
            substituteGlyphIds,
            coverageTable,
            backtrackCoverageTables,
            lookaheadCoverageTables,
            lookupFlags,
            markFilteringSet);
    }

    /// <inheritdoc/>
    public override void CollectDigest(ref GlyphSetDigest digest) => this.coverageTable.CollectDigest(ref digest);

    /// <inheritdoc/>
    public override bool TrySubstitution(FontMetrics fontMetrics, GSubTable table, ShapingBuffer buffer, Tag feature, uint lookupMask, int index, int count)
    {
        ushort glyphId = buffer[index].GlyphId;
        int offset = this.coverageTable.CoverageIndexOf(glyphId);
        if ((uint)offset >= (uint)this.substituteGlyphIds.Length || buffer.IsNestedApplication)
        {
            return false;
        }

        SkippingGlyphIterator iterator = new(fontMetrics, buffer, index, this.LookupFlags, this.MarkFilteringSet);
        SkippingGlyphIterator backtrack = iterator;
        int backtrackStart = backtrack.StartBacktrack();
        if (!AdvancedTypographicUtils.MatchBacktrackCoverageSequence(backtrack, this.backtrackCoverageTables, backtrackStart, buffer.SpanEnd))
        {
            return false;
        }

        // Lookahead starts after the covered glyph. Lookup filtering and transparent
        // formatting controls are handled identically on both sides of the context.
        int end = index + count;
        if (!AdvancedTypographicUtils.MatchCoverageSequence(iterator, this.lookaheadCoverageTables, index + 1, end, 0, true))
        {
            return false;
        }

        buffer.ReplaceInPlace(index, this.substituteGlyphIds[offset], feature);
        return true;
    }

    /// <inheritdoc />
    public override bool WouldApply(ReadOnlySpan<ushort> glyphs, bool zeroContext)
        => glyphs.Length == 1 && this.coverageTable.CoverageIndexOf(glyphs[0]) > -1;
}

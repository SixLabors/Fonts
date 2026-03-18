// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// A Multiple Substitution (MultipleSubst) subtable replaces a single glyph with more than one glyph,
/// as when multiple glyphs replace a single ligature. The subtable has a single format: MultipleSubstFormat1.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-2-multiple-substitution-subtable"/>
/// </summary>
internal static class LookupType2SubTable
{
    /// <summary>
    /// Loads the multiple substitution lookup subtable from the given offset.
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
            1 => LookupType2Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements multiple substitution format 1. Each input glyph is replaced by a sequence
/// of glyphs defined by the corresponding sequence table.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#21-multiple-substitution-format-1"/>
/// </summary>
internal sealed class LookupType2Format1SubTable : LookupSubTable
{
    /// <summary>
    /// The array of sequence tables, ordered by coverage index.
    /// </summary>
    private readonly SequenceTable[] sequenceTables;

    /// <summary>
    /// The coverage table that defines the set of input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType2Format1SubTable"/> class.
    /// </summary>
    /// <param name="sequenceTables">The array of sequence tables.</param>
    /// <param name="coverageTable">The coverage table defining input glyphs.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType2Format1SubTable(SequenceTable[] sequenceTables, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.sequenceTables = sequenceTables;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the multiple substitution format 1 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType2Format1SubTable"/>.</returns>
    public static LookupType2Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // Multiple Substitution Format 1
        // +----------+--------------------------------+-----------------------------------------------------------------+
        // | Type     | Name                           | Description                                                     |
        // +==========+================================+=================================================================+
        // | uint16   | substFormat                    | Format identifier: format = 1                                   |
        // +----------+--------------------------------+-----------------------------------------------------------------+
        // | Offset16 | coverageOffset                 | Offset to Coverage table, from beginning of substitution        |
        // |          |                                | subtable                                                        |
        // +----------+--------------------------------+-----------------------------------------------------------------+
        // | uint16   | sequenceCount                  | Number of Sequence table offsets in the sequenceOffsets array   |
        // +----------+--------------------------------+-----------------------------------------------------------------+
        // | Offset16 | sequenceOffsets[sequenceCount] | Array of offsets to Sequence tables. Offsets are from beginning |
        // |          |                                | of substitution subtable, ordered by Coverage index             |
        // +----------+--------------------------------+-----------------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort sequenceCount = reader.ReadUInt16();

        using Buffer<ushort> sequenceOffsetsBuffer = new(sequenceCount);
        Span<ushort> sequenceOffsets = sequenceOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(sequenceOffsets);

        SequenceTable[] sequenceTables = new SequenceTable[sequenceCount];
        for (int i = 0; i < sequenceTables.Length; i++)
        {
            // Sequence Table
            // +--------+--------------------------------+------------------------------------------------------+
            // | Type   | Name                           | Description                                          |
            // +========+================================+======================================================+
            // | uint16 | glyphCount                     | Number of glyph IDs in the substituteGlyphIDs array. |
            // |        |                                | This must always be greater than 0.                  |
            // +--------+--------------------------------+------------------------------------------------------+
            // | uint16 | substituteGlyphIDs[glyphCount] | String of glyph IDs to substitute                    |
            // +--------+--------------------------------+------------------------------------------------------+
            reader.Seek(offset + sequenceOffsets[i], SeekOrigin.Begin);
            ushort glyphCount = reader.ReadUInt16();
            sequenceTables[i] = new SequenceTable(reader.ReadUInt16Array(glyphCount));
        }

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType2Format1SubTable(sequenceTables, coverageTable, lookupFlags, markFilteringSet);
    }

    /// <inheritdoc />
    public override bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        GlyphSubstitutionCollection collection,
        Tag feature,
        int index,
        int count)
    {
        ushort glyphId = collection[index].GlyphId;
        if (glyphId == 0)
        {
            return false;
        }

        int offset = this.coverageTable.CoverageIndexOf(glyphId);

        if (offset > -1 && offset < this.sequenceTables.Length)
        {
            collection.Replace(index, this.sequenceTables[offset].SubstituteGlyphs, feature);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Represents a sequence table containing an ordered list of substitute glyph IDs
    /// that replace a single input glyph.
    /// </summary>
    public readonly struct SequenceTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceTable"/> struct.
        /// </summary>
        /// <param name="substituteGlyphs">The array of substitute glyph IDs.</param>
        public SequenceTable(ushort[] substituteGlyphs)
            => this.SubstituteGlyphs = substituteGlyphs;

        /// <summary>
        /// Gets the array of substitute glyph IDs.
        /// </summary>
        public ushort[] SubstituteGlyphs { get; }
    }
}

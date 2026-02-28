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

internal sealed class LookupType2Format1SubTable : LookupSubTable
{
    private readonly SequenceTable[] sequenceTables;
    private readonly CoverageTable coverageTable;

    private LookupType2Format1SubTable(SequenceTable[] sequenceTables, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.sequenceTables = sequenceTables;
        this.coverageTable = coverageTable;
    }

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

        if (offset > -1)
        {
            collection.Replace(index, this.sequenceTables[offset].SubstituteGlyphs, feature);
            return true;
        }

        return false;
    }

    public readonly struct SequenceTable
    {
        public SequenceTable(ushort[] substituteGlyphs)
            => this.SubstituteGlyphs = substituteGlyphs;

        public ushort[] SubstituteGlyphs { get; }
    }
}

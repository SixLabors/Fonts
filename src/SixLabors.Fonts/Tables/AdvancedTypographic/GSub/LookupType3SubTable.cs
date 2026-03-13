// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// An Alternate Substitution (AlternateSubst) subtable identifies any number of aesthetic alternatives
/// from which a user can choose a glyph variant to replace the input glyph.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-3-alternate-substitution-subtable"/>
/// </summary>
internal static class LookupType3SubTable
{
    /// <summary>
    /// Loads the alternate substitution lookup subtable from the given offset.
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
            1 => LookupType3Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements alternate substitution format 1. Each input glyph can be replaced with any
/// one of a set of alternate glyphs.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#31-alternate-substitution-format-1"/>
/// </summary>
internal sealed class LookupType3Format1SubTable : LookupSubTable
{
    /// <summary>
    /// The array of alternate set tables, ordered by coverage index.
    /// </summary>
    private readonly AlternateSetTable[] alternateSetTables;

    /// <summary>
    /// The coverage table that defines the set of input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType3Format1SubTable"/> class.
    /// </summary>
    /// <param name="alternateSetTables">The array of alternate set tables.</param>
    /// <param name="coverageTable">The coverage table defining input glyphs.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType3Format1SubTable(AlternateSetTable[] alternateSetTables, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.alternateSetTables = alternateSetTables;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the alternate substitution format 1 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType3Format1SubTable"/>.</returns>
    public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // Alternate Substitution Format 1
        // +----------+----------------------------------------+---------------------------------------------------------------+
        // | Type     | Name                                   | Description                                                   |
        // +==========+========================================+===============================================================+
        // | uint16   | substFormat                            | Format identifier: format = 1                                 |
        // +----------+----------------------------------------+---------------------------------------------------------------+
        // | Offset16 | coverageOffset                         | Offset to Coverage table, from beginning of substitution      |
        // |          |                                        | subtable                                                      |
        // +----------+----------------------------------------+---------------------------------------------------------------+
        // | uint16   | alternateSetCount                      | Number of AlternateSet tables                                 |
        // +----------+----------------------------------------+---------------------------------------------------------------+
        // | Offset16 | alternateSetOffsets[alternateSetCount] | Array of offsets to AlternateSet tables. Offsets are from     |
        // |          |                                        | beginning of substitution subtable, ordered by Coverage index |
        // +----------+----------------------------------------+---------------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort alternateSetCount = reader.ReadUInt16();

        using Buffer<ushort> alternateSetOffsetsBuffer = new(alternateSetCount);
        Span<ushort> alternateSetOffsets = alternateSetOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(alternateSetOffsets);

        AlternateSetTable[] alternateTables = new AlternateSetTable[alternateSetCount];
        for (int i = 0; i < alternateTables.Length; i++)
        {
            // AlternateSet Table
            // +--------+-------------------------------+----------------------------------------------------+
            // | Type   | Name                          | Description                                        |
            // +========+===============================+====================================================+
            // | uint16 | glyphCount                    | Number of glyph IDs in the alternateGlyphIDs array |
            // +--------+-------------------------------+----------------------------------------------------+
            // | uint16 | alternateGlyphIDs[glyphCount] | Array of alternate glyph IDs, in arbitrary order   |
            // +--------+-------------------------------+----------------------------------------------------+
            reader.Seek(offset + alternateSetOffsets[i], SeekOrigin.Begin);
            ushort glyphCount = reader.ReadUInt16();
            alternateTables[i] = new AlternateSetTable(reader.ReadUInt16Array(glyphCount));
        }

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType3Format1SubTable(alternateTables, coverageTable, lookupFlags, markFilteringSet);
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

        if (offset > -1)
        {
            // TODO: We're just choosing the first alternative here.
            // It looks like the choice is arbitrary and should be determined by
            // the client.
            collection.Replace(index, this.alternateSetTables[offset].AlternateGlyphs[0], feature);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Represents an alternate set table containing an array of alternate glyph IDs
    /// for a single input glyph.
    /// </summary>
    public readonly struct AlternateSetTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlternateSetTable"/> struct.
        /// </summary>
        /// <param name="alternateGlyphs">The array of alternate glyph IDs.</param>
        public AlternateSetTable(ushort[] alternateGlyphs)
            => this.AlternateGlyphs = alternateGlyphs;

        /// <summary>
        /// Gets the array of alternate glyph IDs, in arbitrary order.
        /// </summary>
        public readonly ushort[] AlternateGlyphs { get; }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// Single substitution (SingleSubst) subtables tell a client to replace a single glyph with another glyph.
/// The subtables can be either of two formats. Both formats require two distinct sets of glyph indices:
/// one that defines input glyphs (specified in the Coverage table), and one that defines the output glyphs.
/// Format 1 requires less space than Format 2, but it is less flexible.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-1-single-substitution-subtable"/>
/// </summary>
internal static class LookupType1SubTable
{
    /// <summary>
    /// Loads the single substitution lookup subtable from the given offset.
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
            1 => LookupType1Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType1Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements single substitution format 1. The substitute glyph ID is calculated by adding
/// a delta value to the original glyph ID.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#11-single-substitution-format-1"/>
/// </summary>
internal sealed class LookupType1Format1SubTable : LookupSubTable
{
    /// <summary>
    /// The delta value to add to the original glyph ID to produce the substitute glyph ID.
    /// </summary>
    private readonly ushort deltaGlyphId;

    /// <summary>
    /// The coverage table that defines the set of input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType1Format1SubTable"/> class.
    /// </summary>
    /// <param name="deltaGlyphId">The delta value to add to the original glyph ID.</param>
    /// <param name="coverageTable">The coverage table defining input glyphs.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType1Format1SubTable(ushort deltaGlyphId, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.deltaGlyphId = deltaGlyphId;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the single substitution format 1 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType1Format1SubTable"/>.</returns>
    public static LookupType1Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // SingleSubstFormat1
        // +----------+----------------+----------------------------------------------------------+
        // | Type     | Name           | Description                                              |
        // +==========+================+==========================================================+
        // | uint16   | substFormat    | Format identifier: format = 1                            |
        // +----------+----------------+----------------------------------------------------------+
        // | Offset16 | coverageOffset | Offset to Coverage table, from beginning of substitution |
        // |          |                | subtable                                                 |
        // +----------+----------------+----------------------------------------------------------+
        // | int16    | deltaGlyphID   | Add to original glyph ID to get substitute glyph ID      |
        // +----------+----------------+----------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort deltaGlyphId = reader.ReadUInt16();
        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType1Format1SubTable(deltaGlyphId, coverageTable, lookupFlags, markFilteringSet);
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

        if (this.coverageTable.CoverageIndexOf(glyphId) > -1)
        {
            collection.Replace(index, (ushort)(glyphId + this.deltaGlyphId), feature);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Implements single substitution format 2. Each input glyph is mapped to a specific
/// substitute glyph via an array ordered by coverage index.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#12-single-substitution-format-2"/>
/// </summary>
internal sealed class LookupType1Format2SubTable : LookupSubTable
{
    /// <summary>
    /// The coverage table that defines the set of input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// The array of substitute glyph IDs, ordered by coverage index.
    /// </summary>
    private readonly ushort[] substituteGlyphs;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType1Format2SubTable"/> class.
    /// </summary>
    /// <param name="substituteGlyphs">The array of substitute glyph IDs.</param>
    /// <param name="coverageTable">The coverage table defining input glyphs.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType1Format2SubTable(ushort[] substituteGlyphs, CoverageTable coverageTable, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.substituteGlyphs = substituteGlyphs;
        this.coverageTable = coverageTable;
    }

    /// <summary>
    /// Loads the single substitution format 2 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType1Format2SubTable"/>.</returns>
    public static LookupType1Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        // SingleSubstFormat2
        // +----------+--------------------------------+-----------------------------------------------------------+
        // | Type     | Name                           | Description                                               |
        // +==========+================================+===========================================================+
        // | uint16   | substFormat                    | Format identifier: format = 2                             |
        // +----------+--------------------------------+-----------------------------------------------------------+
        // | Offset16 | coverageOffset                 | Offset to Coverage table, from beginning of substitution  |
        // |          |                                | subtable                                                  |
        // +----------+--------------------------------+-----------------------------------------------------------+
        // | uint16   | glyphCount                     | Number of glyph IDs in the substituteGlyphIDs array       |
        // +----------+--------------------------------+-----------------------------------------------------------+
        // | uint16   | substituteGlyphIDs[glyphCount] | Array of substitute glyph IDs — ordered by Coverage index |
        // +----------+--------------------------------+-----------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort glyphCount = reader.ReadUInt16();
        ushort[] substituteGlyphIds = reader.ReadUInt16Array(glyphCount);
        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

        return new LookupType1Format2SubTable(substituteGlyphIds, coverageTable, lookupFlags, markFilteringSet);
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

        if (offset > -1 && offset < this.substituteGlyphs.Length)
        {
            collection.Replace(index, this.substituteGlyphs[offset], feature);
            return true;
        }

        return false;
    }
}

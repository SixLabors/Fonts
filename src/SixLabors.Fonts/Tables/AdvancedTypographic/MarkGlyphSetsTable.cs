// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The MarkGlyphSets table allows the definition of sets of mark glyphs that can be used
/// in lookup flag mark filtering. This provides more flexibility than the MarkAttachmentType.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table"/>
/// </summary>
internal sealed class MarkGlyphSetsTable
{
    /// <summary>
    /// Gets or sets the format identifier.
    /// </summary>
    public ushort Format { get; internal set; }

    /// <summary>
    /// Gets or sets the array of offsets to Coverage tables, from the beginning of the MarkGlyphSets table.
    /// </summary>
    public uint[]? CoverageOffset { get; internal set; }

    /// <summary>
    /// Gets the loaded Coverage tables for each mark glyph set.
    /// </summary>
    public CoverageTable[]? Coverages { get; private set; }

    /// <summary>
    /// Loads the <see cref="MarkGlyphSetsTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the GDEF table to the MarkGlyphSets table.</param>
    /// <returns>The <see cref="MarkGlyphSetsTable"/>.</returns>
    public static MarkGlyphSetsTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);

        MarkGlyphSetsTable markGlyphSetsTable = new()
        {
            Format = reader.ReadUInt16()
        };

        ushort markSetCount = reader.ReadUInt16();
        uint[] coverageOffsets = reader.ReadUInt32Array(markSetCount);
        markGlyphSetsTable.CoverageOffset = coverageOffsets;

        // Load the referenced Coverage tables now so we can use them during shaping.
        // Coverage offsets are relative to the start of the MarkGlyphSets table.
        CoverageTable[] coverages = new CoverageTable[markSetCount];
        for (int i = 0; i < markSetCount; i++)
        {
            long covOffset = offset + coverageOffsets[i];
            coverages[i] = CoverageTable.Load(reader, covOffset);
        }

        markGlyphSetsTable.Coverages = coverages;
        return markGlyphSetsTable;
    }

    /// <summary>
    /// Determines whether the specified glyph is contained in the given mark glyph set.
    /// </summary>
    /// <param name="markGlyphSetIndex">The index of the mark glyph set.</param>
    /// <param name="glyphId">The glyph identifier to look up.</param>
    /// <returns><see langword="true"/> if the glyph is in the set; otherwise, <see langword="false"/>.</returns>
    public bool Contains(ushort markGlyphSetIndex, ushort glyphId)
    {
        CoverageTable[]? coverages = this.Coverages;
        if (coverages is null)
        {
            return false;
        }

        int i = markGlyphSetIndex;
        if ((uint)i >= (uint)coverages.Length)
        {
            return false;
        }

        return coverages[i].CoverageIndexOf(glyphId) >= 0;
    }
}

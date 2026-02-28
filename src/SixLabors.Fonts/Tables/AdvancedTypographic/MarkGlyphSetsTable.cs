// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal sealed class MarkGlyphSetsTable
{
    public ushort Format { get; internal set; }

    public uint[]? CoverageOffset { get; internal set; }

    public CoverageTable[]? Coverages { get; private set; }

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

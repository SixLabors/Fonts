// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal sealed class MarkGlyphSetsTable
{
    public ushort Format { get; internal set; }

    public ushort[]? CoverageOffset { get; internal set; }

    public static MarkGlyphSetsTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);

        MarkGlyphSetsTable markGlyphSetsTable = new()
        {
            Format = reader.ReadUInt16()
        };
        ushort markSetCount = reader.ReadUInt16();
        markGlyphSetsTable.CoverageOffset = reader.ReadUInt16Array(markSetCount);

        return markGlyphSetsTable;
    }
}

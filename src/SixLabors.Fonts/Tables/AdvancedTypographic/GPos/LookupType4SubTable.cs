// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Mark-to-Base Attachment Positioning Subtable. The MarkToBase attachment (MarkBasePos) subtable is used to position combining mark glyphs with respect to base glyphs.
/// For example, the Arabic, Hebrew, and Thai scripts combine vowels, diacritical marks, and tone marks with base glyphs.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable"/>
/// </summary>
internal static class LookupType4SubTable
{
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort format = reader.ReadUInt16();

        return format switch
        {
            1 => LookupType4Format1SubTable.Load(reader, offset, lookupFlags),
            _ => new NotImplementedSubTable(),
        };
    }

    internal sealed class LookupType4Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable markCoverage;
        private readonly CoverageTable baseCoverage;
        private readonly MarkArrayTable markArrayTable;
        private readonly BaseArrayTable baseArrayTable;

        public LookupType4Format1SubTable(
            CoverageTable markCoverage,
            CoverageTable baseCoverage,
            MarkArrayTable markArrayTable,
            BaseArrayTable baseArrayTable,
            LookupFlags lookupFlags)
            : base(lookupFlags)
        {
            this.markCoverage = markCoverage;
            this.baseCoverage = baseCoverage;
            this.markArrayTable = markArrayTable;
            this.baseArrayTable = baseArrayTable;
        }

        public static LookupType4Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
        {
            // MarkBasePosFormat1 Subtable.
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Type               |  Name                           | Description                                          |
            // +====================+=================================+======================================================+
            // | uint16             | posFormat                       | Format identifier: format = 1                        |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | markCoverageOffset              | Offset to markCoverage table,                        |
            // |                    |                                 | from beginning of MarkBasePos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | baseCoverageOffset              | Offset to baseCoverage table,                        |
            // |                    |                                 | from beginning of MarkBasePos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | uint16             | markClassCount                  | Number of classes defined for marks.                 |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | markArrayOffset                 | Offset to MarkArray table,                           |
            // |                    |                                 | from beginning of MarkBasePos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | baseArrayOffset                 | Offset to BaseArray table,                           |
            // |                    |                                 | from beginning of MarkBasePos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            ushort markCoverageOffset = reader.ReadOffset16();
            ushort baseCoverageOffset = reader.ReadOffset16();
            ushort markClassCount = reader.ReadUInt16();
            ushort markArrayOffset = reader.ReadOffset16();
            ushort baseArrayOffset = reader.ReadOffset16();

            var markCoverage = CoverageTable.Load(reader, offset + markCoverageOffset);
            var baseCoverage = CoverageTable.Load(reader, offset + baseCoverageOffset);
            var markArrayTable = new MarkArrayTable(reader, offset + markArrayOffset);
            var baseArrayTable = new BaseArrayTable(reader, offset + baseArrayOffset, markClassCount);

            return new LookupType4Format1SubTable(markCoverage, baseCoverage, markArrayTable, baseArrayTable, lookupFlags);
        }

        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            // Mark-to-Base Attachment Positioning Subtable.
            // Implements: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable
            ushort glyphId = collection[index].GlyphId;
            if (glyphId == 0)
            {
                return false;
            }

            int markIndex = this.markCoverage.CoverageIndexOf(glyphId);
            if (markIndex == -1)
            {
                return false;
            }

            // Search backward for a base glyph.
            int baseGlyphIndex = index;
            while (--baseGlyphIndex >= 0)
            {
                GlyphShapingData data = collection[baseGlyphIndex];
                if (!AdvancedTypographicUtils.IsMarkGlyph(fontMetrics, data.GlyphId, data) && !(data.LigatureComponent > 0))
                {
                    break;
                }
            }

            if (baseGlyphIndex < 0)
            {
                return false;
            }

            ushort baseGlyphId = collection[baseGlyphIndex].GlyphId;
            int baseIndex = this.baseCoverage.CoverageIndexOf(baseGlyphId);
            if (baseIndex < 0)
            {
                return false;
            }

            MarkRecord markRecord = this.markArrayTable.MarkRecords[markIndex];
            AnchorTable baseAnchor = this.baseArrayTable.BaseRecords[baseIndex].BaseAnchorTables[markRecord.MarkClass];
            AdvancedTypographicUtils.ApplyAnchor(fontMetrics, collection, index, baseAnchor, markRecord, baseGlyphIndex, feature);

            return true;
        }
    }
}

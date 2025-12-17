// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Lookup Type 6: Mark-to-Mark Attachment Positioning Subtable.
/// The MarkToMark attachment (MarkMarkPos) subtable is identical in form to the MarkToBase attachment subtable, although its function is different.
/// MarkToMark attachment defines the position of one mark relative to another mark as when, for example,
/// positioning tone marks with respect to vowel diacritical marks in Vietnamese.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable"/>
/// </summary>
internal static class LookupType6SubTable
{
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort subTableFormat = reader.ReadUInt16();

        return subTableFormat switch
        {
            1 => LookupType6Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }

    internal sealed class LookupType6Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable mark1Coverage;
        private readonly CoverageTable mark2Coverage;
        private readonly MarkArrayTable mark1ArrayTable;
        private readonly Mark2ArrayTable mark2ArrayTable;

        public LookupType6Format1SubTable(
            CoverageTable mark1Coverage,
            CoverageTable mark2Coverage,
            MarkArrayTable mark1ArrayTable,
            Mark2ArrayTable mark2ArrayTable,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.mark1Coverage = mark1Coverage;
            this.mark2Coverage = mark2Coverage;
            this.mark1ArrayTable = mark1ArrayTable;
            this.mark2ArrayTable = mark2ArrayTable;
        }

        public static LookupType6Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            // MarkMarkPosFormat1 Subtable.
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Type               |  Name                           | Description                                          |
            // +====================+=================================+======================================================+
            // | uint16             | posFormat                       | Format identifier: format = 1                        |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | mark1CoverageOffset             | Offset to Combining Mark Coverage table,             |
            // |                    |                                 | from beginning of MarkMarkPos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | mark2CoverageOffset             | Offset to Base Mark Coverage table,                  |
            // |                    |                                 | from beginning of MarkMarkPos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | uint16             | markClassCount                  | Number of Combining Mark classes defined             |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | mark1ArrayOffset                | Offset to MarkArray table for mark1,                 |
            // |                    |                                 | from beginning of MarkMarkPos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | mark2ArrayOffset                | Offset to Mark2Array table for mark2,                |
            // |                    |                                 | from beginning of MarkMarkPos subtable.              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            ushort mark1CoverageOffset = reader.ReadOffset16();
            ushort mark2CoverageOffset = reader.ReadOffset16();
            ushort markClassCount = reader.ReadUInt16();
            ushort mark1ArrayOffset = reader.ReadOffset16();
            ushort mark2ArrayOffset = reader.ReadOffset16();

            CoverageTable mark1Coverage = CoverageTable.Load(reader, offset + mark1CoverageOffset);
            CoverageTable mark2Coverage = CoverageTable.Load(reader, offset + mark2CoverageOffset);
            MarkArrayTable mark1ArrayTable = new(reader, offset + mark1ArrayOffset);
            Mark2ArrayTable mark2ArrayTable = new(reader, markClassCount, offset + mark2ArrayOffset);

            return new LookupType6Format1SubTable(mark1Coverage, mark2Coverage, mark1ArrayTable, mark2ArrayTable, lookupFlags, markFilteringSet);
        }

        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            // Mark to mark positioning.
            // Implements: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable
            ushort glyphId = collection[index].GlyphId;
            if (glyphId == 0)
            {
                return false;
            }

            int mark1Index = this.mark1Coverage.CoverageIndexOf(glyphId);
            if (mark1Index < 0)
            {
                return false;
            }

            // Get the previous mark to attach to.
            // HarfBuzz: search backwards for a suitable mark glyph until a non-mark glyph.
            // It clears ignore flags when searching, but keeps mark attachment / filtering behavior.
            LookupFlags searchFlags = this.LookupFlags & ~(LookupFlags.IgnoreMarks | LookupFlags.IgnoreBaseGlyphs | LookupFlags.IgnoreLigatures);

            SkippingGlyphIterator it = new(fontMetrics, collection, index, searchFlags, this.MarkFilteringSet);

            int j = it.Prev();
            if (j < 0)
            {
                return false;
            }

            GlyphShapingData prevGlyph = collection[j];
            if (!AdvancedTypographicUtils.IsMarkGlyph(fontMetrics, prevGlyph.GlyphId, prevGlyph))
            {
                return false;
            }

            GlyphShapingData curGlyph = collection[index];

            bool good;
            int id1 = curGlyph.LigatureId;
            int id2 = prevGlyph.LigatureId;
            int comp1 = curGlyph.LigatureComponent;
            int comp2 = prevGlyph.LigatureComponent;

            if (id1 == id2)
            {
                if (id1 == 0)
                {
                    // Marks belonging to the same base.
                    good = true;
                }
                else
                {
                    // Marks belonging to the same ligature component.
                    good = comp1 == comp2;
                }
            }
            else
            {
                // If ligature ids don't match, one of the marks itself may be a ligature.
                good = (id1 > 0 && comp1 <= 0) || (id2 > 0 && comp2 <= 0);
            }

            if (!good)
            {
                return false;
            }

            int mark2Index = this.mark2Coverage.CoverageIndexOf(prevGlyph.GlyphId);
            if (mark2Index < 0)
            {
                return false;
            }

            MarkRecord markRecord = this.mark1ArrayTable.MarkRecords[mark1Index];
            AnchorTable? baseAnchor = this.mark2ArrayTable.Mark2Records[mark2Index].MarkAnchorTable[markRecord.MarkClass];
            AdvancedTypographicUtils.ApplyAnchor(fontMetrics, collection, index, baseAnchor, markRecord, j, feature);

            return true;
        }
    }
}

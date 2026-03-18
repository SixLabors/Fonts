// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Mark-to-Ligature Attachment Positioning Subtable.
/// The MarkToLigature attachment (MarkLigPos) subtable is used to position combining mark glyphs with respect to ligature base glyphs.
/// With MarkToBase attachment, described previously, each base glyph has an attachment point defined for each class of marks.
/// MarkToLigature attachment is similar, except that each ligature glyph is defined to have multiple components (in a virtual sense — not actual glyphs),
/// and each component has a separate set of attachment points defined for the different mark classes.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable"/>
/// </summary>
internal static class LookupType5SubTable
{
    /// <summary>
    /// Loads the mark-to-ligature attachment positioning subtable from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort subTableFormat = reader.ReadUInt16();

        return subTableFormat switch
        {
            1 => LookupType5Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }

    /// <summary>
    /// MarkToLigature Attachment Positioning Format 1: positions combining mark glyphs relative to ligature glyph components.
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-to-ligature-attachment-positioning-format-1-mark-to-ligature-attachment-point"/>
    /// </summary>
    internal sealed class LookupType5Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable markCoverage;
        private readonly CoverageTable ligatureCoverage;
        private readonly MarkArrayTable markArrayTable;
        private readonly LigatureArrayTable ligatureArrayTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType5Format1SubTable"/> class.
        /// </summary>
        /// <param name="markCoverage">The mark coverage table.</param>
        /// <param name="ligatureCoverage">The ligature coverage table.</param>
        /// <param name="markArrayTable">The mark array table.</param>
        /// <param name="ligatureArrayTable">The ligature array table.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        public LookupType5Format1SubTable(
            CoverageTable markCoverage,
            CoverageTable ligatureCoverage,
            MarkArrayTable markArrayTable,
            LigatureArrayTable ligatureArrayTable,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.markCoverage = markCoverage;
            this.ligatureCoverage = ligatureCoverage;
            this.markArrayTable = markArrayTable;
            this.ligatureArrayTable = ligatureArrayTable;
        }

        /// <summary>
        /// Loads the Format 1 mark-to-ligature attachment positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType5Format1SubTable"/>.</returns>
        public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            // MarkLigPosFormat1 Subtable.
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Type               |  Name                           | Description                                          |
            // +====================+=================================+======================================================+
            // | uint16             | posFormat                       | Format identifier: format = 1                        |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | markCoverageOffset              | Offset to markCoverage table,                        |
            // |                    |                                 | from beginning of MarkLigPos subtable.               |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | ligatureCoverageOffset          | Offset to ligatureCoverage table,                    |
            // |                    |                                 | from beginning of MarkLigPos subtable.               |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | uint16             | markClassCount                  | Number of defined mark classes                       |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | markArrayOffset                 | Offset to MarkArray table, from beginning            |
            // |                    |                                 | of MarkLigPos subtable.                              |
            // +--------------------+---------------------------------+------------------------------------------------------+
            // | Offset16           | ligatureArrayOffset             | Offset to LigatureArray table,                       |
            // |                    |                                 | from beginning of MarkLigPos subtable.               |
            // +--------------------+---------------------------------+------------------------------------------------------+
            ushort markCoverageOffset = reader.ReadOffset16();
            ushort ligatureCoverageOffset = reader.ReadOffset16();
            ushort markClassCount = reader.ReadUInt16();
            ushort markArrayOffset = reader.ReadOffset16();
            ushort ligatureArrayOffset = reader.ReadOffset16();

            CoverageTable markCoverage = CoverageTable.Load(reader, offset + markCoverageOffset);
            CoverageTable ligatureCoverage = CoverageTable.Load(reader, offset + ligatureCoverageOffset);
            MarkArrayTable markArrayTable = new(reader, offset + markArrayOffset);
            LigatureArrayTable ligatureArrayTable = new(reader, offset + ligatureArrayOffset, markClassCount);

            return new LookupType5Format1SubTable(markCoverage, ligatureCoverage, markArrayTable, ligatureArrayTable, lookupFlags, markFilteringSet);
        }

        /// <inheritdoc/>
        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            // Mark-to-Ligature Attachment Positioning.
            // Implements: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable
            ushort glyphId = collection[index].GlyphId;
            if (glyphId == 0)
            {
                return false;
            }

            int markIndex = this.markCoverage.CoverageIndexOf(glyphId);
            if (markIndex < 0 || markIndex >= this.markArrayTable.MarkRecords.Length)
            {
                return false;
            }

            // Search backward for a base glyph.
            int baseGlyphIndex = index;
            while (--baseGlyphIndex >= 0)
            {
                GlyphShapingData data = collection[baseGlyphIndex];
                if (!AdvancedTypographicUtils.IsMarkGlyph(fontMetrics, data.GlyphId, data))
                {
                    break;
                }
            }

            if (baseGlyphIndex < 0)
            {
                return false;
            }

            ushort baseGlyphId = collection[baseGlyphIndex].GlyphId;
            int ligatureIndex = this.ligatureCoverage.CoverageIndexOf(baseGlyphId);
            if (ligatureIndex < 0 || ligatureIndex >= this.ligatureArrayTable.LigatureAttachTables.Length)
            {
                return false;
            }

            // We must now check whether the ligature ID of the current mark glyph
            // is identical to the ligature ID of the found ligature.
            // If yes, we can directly use the component index. If not, we attach the mark
            // glyph to the last component of the ligature.
            LigatureAttachTable ligatureAttach = this.ligatureArrayTable.LigatureAttachTables[ligatureIndex];
            GlyphShapingData markGlyph = collection[index];
            GlyphShapingData ligGlyph = collection[baseGlyphIndex];
            int compIndex = ligGlyph.LigatureId > 0 && ligGlyph.LigatureId == markGlyph.LigatureId && markGlyph.LigatureComponent > 0
                ? Math.Min(markGlyph.LigatureComponent, ligGlyph.CodePointCount) - 1
                : ligGlyph.CodePointCount - 1;

            MarkRecord markRecord = this.markArrayTable.MarkRecords[markIndex];
            AnchorTable baseAnchor = ligatureAttach.ComponentRecords[compIndex].LigatureAnchorTables[markRecord.MarkClass];
            AdvancedTypographicUtils.ApplyAnchor(fontMetrics, collection, index, baseAnchor, markRecord, baseGlyphIndex, feature);

            return true;
        }
    }
}

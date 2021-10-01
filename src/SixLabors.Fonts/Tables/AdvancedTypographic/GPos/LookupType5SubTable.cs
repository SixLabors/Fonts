// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
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
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort subTableFormat = reader.ReadUInt16();

            return subTableFormat switch
            {
                1 => LookupType5Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {subTableFormat}. Should be '1'."),
            };
        }

        internal sealed class LookupType5Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable markCoverage;
            private readonly CoverageTable ligatureCoverage;
            private readonly MarkArrayTable markArrayTable;
            private readonly LigatureArrayTable ligatureArrayTable;

            public LookupType5Format1SubTable(CoverageTable markCoverage, CoverageTable ligatureCoverage, MarkArrayTable markArrayTable, LigatureArrayTable ligatureArrayTable)
            {
                this.markCoverage = markCoverage;
                this.ligatureCoverage = ligatureCoverage;
                this.markArrayTable = markArrayTable;
                this.ligatureArrayTable = ligatureArrayTable;
            }

            public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset)
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

                var markCoverage = CoverageTable.Load(reader, offset + markCoverageOffset);
                var ligatureCoverage = CoverageTable.Load(reader, offset + ligatureCoverageOffset);
                var markArrayTable = new MarkArrayTable(reader, offset + markArrayOffset);
                var ligatureArrayTable = new LigatureArrayTable(reader, offset + ligatureArrayOffset, markClassCount);

                return new LookupType5Format1SubTable(markCoverage, ligatureCoverage, markArrayTable, ligatureArrayTable);
            }

            public override bool TryUpdatePosition(
                IFontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                // Mark-to-Ligature Attachment Positioning.
                // Implements: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable
                ushort glyphId = collection[index][0];
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
                int baseGlyphIterator = index;
                while (--baseGlyphIterator >= 0)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(baseGlyphIterator);
                    if (!CodePoint.IsMark(data.CodePoint))
                    {
                        break;
                    }
                }

                if (baseGlyphIterator < 0)
                {
                    return false;
                }

                ushort baseGlyphIndex = (ushort)baseGlyphIterator;
                ushort baseGlyphId = collection[baseGlyphIndex][0];
                int ligatureIndex = this.ligatureCoverage.CoverageIndexOf(baseGlyphId);
                if (ligatureIndex < 0)
                {
                    return false;
                }

                LigatureAttachTable ligatureAttach = this.ligatureArrayTable.LigatureAttachTables[ligatureIndex];
                ushort markGlyphId = glyphId;
                ushort ligGlyphId = baseGlyphId;

                // TODO: figure out how to calculate the compIndex, see fontKit.
                int compIndex = 0;

                MarkRecord markRecord = this.markArrayTable.MarkRecords[markIndex];
                AnchorTable baseAnchor = ligatureAttach.ComponentRecords[compIndex].LigatureAnchorTables[markRecord.MarkClass];
                AdvancedTypographicUtils.ApplyAnchor(fontMetrics, collection, index, baseAnchor, markRecord, baseGlyphIndex, baseGlyphId, glyphId);

                return true;
            }
        }
    }
}

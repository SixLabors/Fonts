// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Lookup Type 6: Mark-to-Mark Attachment Positioning Subtable.
    /// The MarkToMark attachment (MarkMarkPos) subtable is identical in form to the MarkToBase attachment subtable, although its function is different.
    /// MarkToMark attachment defines the position of one mark relative to another mark as when, for example,
    /// positioning tone marks with respect to vowel diacritical marks in Vietnamese.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable"/>
    /// </summary>
    internal static class LookupType6SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort subTableFormat = reader.ReadUInt16();

            return subTableFormat switch
            {
                1 => LookupType6Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {subTableFormat}. Should be '1'."),
            };
        }

        internal sealed class LookupType6Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable mark1Coverage;
            private readonly CoverageTable mark2Coverage;
            private readonly MarkArrayTable mark1ArrayTable;
            private readonly Mark2ArrayTable mark2ArrayTable;

            public LookupType6Format1SubTable(CoverageTable mark1Coverage, CoverageTable mark2Coverage, MarkArrayTable mark1ArrayTable, Mark2ArrayTable mark2ArrayTable)
            {
                this.mark1Coverage = mark1Coverage;
                this.mark2Coverage = mark2Coverage;
                this.mark1ArrayTable = mark1ArrayTable;
                this.mark2ArrayTable = mark2ArrayTable;
            }

            public static LookupType6Format1SubTable Load(BigEndianBinaryReader reader, long offset)
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

                var mark1Coverage = CoverageTable.Load(reader, offset + mark1CoverageOffset);
                var mark2Coverage = CoverageTable.Load(reader, offset + mark2CoverageOffset);
                var mark1ArrayTable = new MarkArrayTable(reader, offset + mark1ArrayOffset);
                var mark2ArrayTable = new Mark2ArrayTable(reader, markClassCount, offset + mark2ArrayOffset);

                return new LookupType6Format1SubTable(mark1Coverage, mark2Coverage, mark1ArrayTable, mark2ArrayTable);
            }

            public override bool TryUpdatePosition(
                IFontShaper shaper,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                // Mark to mark positioning.
                // Implements: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                int mark1Index = this.mark1Coverage.CoverageIndexOf(glyphId);
                if (mark1Index == -1)
                {
                    return false;
                }

                // Get the previous mark to attach to.
                if (index < 1)
                {
                    return false;
                }

                // TODO: fontkit checks here, if Marks belonging to the same base or Marks belonging to the same ligature component.
                int prevIdx = index - 1;
                ushort prevGlyphId = collection[prevIdx][0];
                GlyphShapingData prevData = collection.GetGlyphShapingData(prevIdx);
                if (!shaper.TryGetGlyphClass(prevGlyphId, out GlyphClassDef? glyphClass) && !CodePoint.IsMark(prevData.CodePoint))
                {
                    return false;
                }

                if (glyphClass != GlyphClassDef.MarkGlyph)
                {
                    return false;
                }

                int mark2Index = this.mark2Coverage.CoverageIndexOf(prevGlyphId);
                if (mark2Index == -1)
                {
                    return false;
                }

                MarkRecord markRecord = this.mark1ArrayTable.MarkRecords[mark1Index];
                AnchorTable baseAnchor = this.mark2ArrayTable.Mark2Records[mark2Index].MarkAnchorTable[markRecord.MarkClass];
                AdvancedTypographicUtils.ApplyAnchor(shaper, collection, index, baseAnchor, markRecord, (ushort)prevIdx, prevGlyphId, glyphId);

                return true;
            }
        }
    }
}

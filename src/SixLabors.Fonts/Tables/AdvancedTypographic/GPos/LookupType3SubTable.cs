// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Numerics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Cursive Attachment Positioning Subtable.
    /// Some cursive fonts are designed so that adjacent glyphs join when rendered with their default positioning.
    /// However, if positioning adjustments are needed to join the glyphs, a cursive attachment positioning (CursivePos) subtable can describe
    /// how to connect the glyphs by aligning two anchor points: the designated exit point of a glyph, and the designated entry point of the following glyph.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#cursive-attachment-positioning-format1-cursive-attachment"/>
    /// </summary>
    internal static class LookupType3SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort posFormat = reader.ReadUInt16();

            return posFormat switch
            {
                1 => LookupType3Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'posFormat' {posFormat}. Should be '1'.")
            };
        }

        internal sealed class LookupType3Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly EntryExitAnchors[] entryExitAnchors;

            public LookupType3Format1SubTable(CoverageTable coverageTable, EntryExitAnchors[] entryExitAnchors)
            {
                this.coverageTable = coverageTable;
                this.entryExitAnchors = entryExitAnchors;
            }

            public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Cursive Attachment Positioning Format1.
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Type               |  Name                           | Description                                          |
                // +====================+=================================+======================================================+
                // | uint16             | posFormat                       | Format identifier: format = 1                        |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | coverageOffset                  | Offset to Coverage table,                            |
                // |                    |                                 | from beginning of CursivePos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | uint16             | entryExitCount                  | Number of EntryExit records.                         |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | EntryExitRecord    | entryExitRecord[entryExitCount] | Array of EntryExit records, in Coverage index order. |
                // +--------------------+---------------------------------+------------------------------------------------------+
                ushort coverageOffset = reader.ReadOffset16();
                ushort entryExitCount = reader.ReadUInt16();
                var entryExitRecords = new EntryExitRecord[entryExitCount];
                for (int i = 0; i < entryExitCount; i++)
                {
                    entryExitRecords[i] = new EntryExitRecord(reader, offset);
                }

                var entryExitAnchors = new EntryExitAnchors[entryExitCount];
                for (int i = 0; i < entryExitCount; i++)
                {
                    entryExitAnchors[i] = new EntryExitAnchors(reader, offset, entryExitRecords[i]);
                }

                var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

                return new LookupType3Format1SubTable(coverageTable, entryExitAnchors);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
            {
                // Implements Cursive Attachment Positioning Subtable:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
                bool updated = false;
                for (ushort i = 0; i < count - 1; i++)
                {
                    ushort curIndex = (ushort)(i + index);
                    ushort glyphId = collection[curIndex][0];
                    if (glyphId == 0)
                    {
                        continue;
                    }

                    ushort nextIndex = (ushort)(i + index + 1);
                    ushort nextGlyphId = collection[nextIndex][0];
                    if (nextGlyphId == 0)
                    {
                        continue;
                    }

                    int coverageNext = this.coverageTable.CoverageIndexOf(nextGlyphId);
                    EntryExitAnchors nextRecord = this.entryExitAnchors[coverageNext];
                    AnchorTable? entry = nextRecord.EntryAnchor;
                    if (entry is null)
                    {
                        continue;
                    }

                    int coverage = this.coverageTable.CoverageIndexOf(glyphId);
                    EntryExitAnchors curRecord = this.entryExitAnchors[coverage];
                    AnchorTable? exit = curRecord.ExitAnchor;
                    if (exit is null)
                    {
                        continue;
                    }

                    // TODO: we need to know here if we are RTL or LTR. This assumes LTR.
                    Vector2 curOffset = collection.GetOffset(fontMetrics, curIndex, glyphId);
                    Vector2 nextOffset = collection.GetOffset(fontMetrics, nextIndex, nextGlyphId);
                    int curXOffset = (int)curOffset.X;
                    int nextXOffset = (int)nextOffset.X;
                    int curDy = exit.YCoordinate - entry.YCoordinate;
                    int curXAdvance = exit.XCoordinate + curXOffset;
                    int nextDx = entry.XCoordinate + nextXOffset;
                    collection.SetAdvanceWidth(fontMetrics, curIndex, glyphId, (ushort)curXAdvance);
                    collection.Offset(fontMetrics, curIndex, glyphId, 0, (short)-curDy);
                    collection.Advance(fontMetrics, nextIndex, nextGlyphId, (short)-nextDx, 0);
                    collection.Offset(fontMetrics, nextIndex, nextGlyphId, (short)-nextDx, 0);

                    updated = true;
                }

                return updated;
            }
        }
    }
}

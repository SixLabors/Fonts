// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

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
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort posFormat = reader.ReadUInt16();

            return posFormat switch
            {
                1 => LookupType3Format1SubTable.Load(reader, offset, lookupFlags),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'posFormat' {posFormat}. Should be '1'.")
            };
        }

        internal sealed class LookupType3Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly EntryExitAnchors[] entryExitAnchors;

            public LookupType3Format1SubTable(CoverageTable coverageTable, EntryExitAnchors[] entryExitAnchors, LookupFlags lookupFlags)
                : base(lookupFlags)
            {
                this.coverageTable = coverageTable;
                this.entryExitAnchors = entryExitAnchors;
            }

            public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
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

                return new LookupType3Format1SubTable(coverageTable, entryExitAnchors, lookupFlags);
            }

            public override bool TryUpdatePosition(
                FontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                if (count <= 1)
                {
                    return false;
                }

                // Implements Cursive Attachment Positioning Subtable:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                ushort nextIndex = (ushort)(index + 1);
                ushort nextGlyphId = collection[nextIndex][0];
                if (nextGlyphId == 0)
                {
                    return false;
                }

                int coverageNext = this.coverageTable.CoverageIndexOf(nextGlyphId);
                if (coverageNext < 0)
                {
                    return false;
                }

                EntryExitAnchors nextRecord = this.entryExitAnchors[coverageNext];
                AnchorTable? entry = nextRecord.EntryAnchor;
                if (entry is null)
                {
                    return false;
                }

                int coverage = this.coverageTable.CoverageIndexOf(glyphId);
                if (coverage < 0)
                {
                    return false;
                }

                EntryExitAnchors curRecord = this.entryExitAnchors[coverage];
                AnchorTable? exit = curRecord.ExitAnchor;
                if (exit is null)
                {
                    return false;
                }

                GlyphShapingData current = collection.GetGlyphShapingData(index);
                GlyphShapingData next = collection.GetGlyphShapingData(nextIndex);
                if (current.Direction == TextDirection.LeftToRight)
                {
                    current.Bounds.Width = exit.XCoordinate + current.Bounds.X;

                    int delta = entry.XCoordinate + next.Bounds.X;
                    next.Bounds.Width -= delta;
                    next.Bounds.X -= (short)delta;

                    next.CursiveAttachment = index;
                    current.Bounds.Y = exit.YCoordinate - entry.YCoordinate;
                }
                else
                {
                    int delta = exit.XCoordinate + current.Bounds.X;
                    current.Bounds.Width -= delta;
                    current.Bounds.X -= delta;

                    next.Bounds.Width = entry.XCoordinate + next.Bounds.X;

                    current.CursiveAttachment = nextIndex;
                    current.Bounds.Y = entry.YCoordinate - exit.YCoordinate;
                }

                return true;
            }
        }
    }
}

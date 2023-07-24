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
                _ => new NotImplementedSubTable(),
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
                int index,
                int count)
            {
                if (count <= 1)
                {
                    return false;
                }

                // Implements Cursive Attachment Positioning Subtable:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
                ushort glyphId = collection[index].GlyphId;
                if (glyphId == 0)
                {
                    return false;
                }

                int nextIndex = index + 1;
                ushort nextGlyphId = collection[nextIndex].GlyphId;
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

                GlyphShapingData current = collection[index];
                GlyphShapingData next = collection[nextIndex];

                AnchorXY exitXY = exit.GetAnchor(fontMetrics, current, collection);
                AnchorXY entryXY = entry.GetAnchor(fontMetrics, next, collection);

                bool isVerticalLayout = AdvancedTypographicUtils.IsVerticalGlyph(current.CodePoint, collection.TextOptions.LayoutMode);
                if (!isVerticalLayout)
                {
                    // Horizontal
                    if (current.Direction == TextDirection.LeftToRight)
                    {
                        current.Bounds.Width = exitXY.XCoordinate + current.Bounds.X;

                        int delta = entryXY.XCoordinate + next.Bounds.X;
                        next.Bounds.Width -= delta;
                        next.Bounds.X -= delta;
                    }
                    else
                    {
                        int delta = exitXY.XCoordinate + current.Bounds.X;
                        current.Bounds.Width -= delta;
                        current.Bounds.X -= delta;

                        next.Bounds.Width = entryXY.XCoordinate + next.Bounds.X;
                    }
                }
                else
                {
                    // Vertical : Top to bottom
                    if (current.Direction == TextDirection.LeftToRight)
                    {
                        current.Bounds.Height = exitXY.YCoordinate + current.Bounds.Y;

                        int delta = entryXY.YCoordinate + next.Bounds.Y;
                        next.Bounds.Height -= delta;
                        next.Bounds.Y -= delta;
                    }
                    else
                    {
                        int delta = exitXY.YCoordinate + current.Bounds.Y;
                        current.Bounds.Height -= delta;
                        current.Bounds.Y -= delta;

                        next.Bounds.Height = entryXY.YCoordinate + next.Bounds.Y;
                    }
                }

                int child = index;
                int parent = nextIndex;
                int xOffset = entryXY.XCoordinate - exitXY.XCoordinate;
                int yOffset = entryXY.YCoordinate - exitXY.YCoordinate;
                if (this.LookupFlags.HasFlag(LookupFlags.RightToLeft))
                {
                    (parent, child) = (child, parent);

                    xOffset = -xOffset;
                    yOffset = -yOffset;
                }

                // If child was already connected to someone else, walk through its old
                // chain and reverse the link direction, such that the whole tree of its
                // previous connection now attaches to new parent.Watch out for case
                // where new parent is on the path from old chain...
                bool horizontal = !isVerticalLayout;
                ReverseCursiveMinorOffset(collection, index, child, horizontal, parent);

                GlyphShapingData c = collection[child];
                c.CursiveAttachment = parent - child;
                if (horizontal)
                {
                    c.Bounds.Y = yOffset;
                }
                else
                {
                    c.Bounds.X = xOffset;
                }

                // If parent was attached to child, separate them.
                GlyphShapingData p = collection[parent];
                if (p.CursiveAttachment == -c.CursiveAttachment)
                {
                    p.CursiveAttachment = 0;
                }

                return true;
            }

            private static void ReverseCursiveMinorOffset(
                GlyphPositioningCollection collection,
                int position,
                int i,
                bool horizontal,
                int parent)
            {
                GlyphShapingData c = collection[i];
                int chain = c.CursiveAttachment;
                if (chain <= 0)
                {
                    return;
                }

                c.CursiveAttachment = 0;

                int j = i + chain;

                // Stop if we see new parent in the chain.
                if (j == parent)
                {
                    return;
                }

                ReverseCursiveMinorOffset(collection, position, j, horizontal, parent);

                GlyphShapingData p = collection[j];
                if (horizontal)
                {
                    p.Bounds.Y = -c.Bounds.Y;
                }
                else
                {
                    p.Bounds.X = -c.Bounds.X;
                }

                p.CursiveAttachment = -chain;
            }
        }
    }
}

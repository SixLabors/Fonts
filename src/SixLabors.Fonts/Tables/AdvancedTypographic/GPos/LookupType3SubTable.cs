// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Cursive Attachment Positioning Subtable.
/// Some cursive fonts are designed so that adjacent glyphs join when rendered with their default positioning.
/// However, if positioning adjustments are needed to join the glyphs, a cursive attachment positioning (CursivePos) subtable can describe
/// how to connect the glyphs by aligning two anchor points: the designated exit point of a glyph, and the designated entry point of the following glyph.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#cursive-attachment-positioning-format1-cursive-attachment"/>
/// </summary>
internal static class LookupType3SubTable
{
    /// <summary>
    /// Loads the cursive attachment positioning subtable from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort posFormat = reader.ReadUInt16();

        return posFormat switch
        {
            1 => LookupType3Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }

    /// <summary>
    /// Cursive Attachment Positioning Format 1: connects adjacent glyphs via entry and exit anchor points.
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#cursive-attachment-positioning-format1-cursive-attachment"/>
    /// </summary>
    internal sealed class LookupType3Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly EntryExitAnchors[] entryExitAnchors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType3Format1SubTable"/> class.
        /// </summary>
        /// <param name="coverageTable">The coverage table.</param>
        /// <param name="entryExitAnchors">The array of entry/exit anchor pairs.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        public LookupType3Format1SubTable(
            CoverageTable coverageTable,
            EntryExitAnchors[] entryExitAnchors,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.coverageTable = coverageTable;
            this.entryExitAnchors = entryExitAnchors;
        }

        /// <summary>
        /// Loads the Format 1 cursive attachment positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType3Format1SubTable"/>.</returns>
        public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
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
            EntryExitRecord[] entryExitRecords = new EntryExitRecord[entryExitCount];
            for (int i = 0; i < entryExitCount; i++)
            {
                entryExitRecords[i] = new EntryExitRecord(reader, offset);
            }

            EntryExitAnchors[] entryExitAnchors = new EntryExitAnchors[entryExitCount];
            for (int i = 0; i < entryExitCount; i++)
            {
                entryExitAnchors[i] = new EntryExitAnchors(reader, offset, entryExitRecords[i]);
            }

            CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType3Format1SubTable(coverageTable, entryExitAnchors, lookupFlags, markFilteringSet);
        }

        /// <inheritdoc/>
        public override void CollectDigest(ref GlyphSetDigest digest) => this.coverageTable.CollectDigest(ref digest);

        /// <inheritdoc/>
        /// <remarks>
        /// The entry anchor connects to the closest preceding glyph visible under
        /// the lookup flags. Default-ignorables remain in the buffer but are
        /// transparent to positioning lookups.
        /// </remarks>
        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            ShapingBuffer buffer,
            Tag feature,
            int index,
            int count)
        {
            ushort glyphId = buffer[index].GlyphId;
            if (glyphId == 0)
            {
                return false;
            }

            int coverage = this.coverageTable.CoverageIndexOf(glyphId);
            if (coverage < 0 || coverage >= this.entryExitAnchors.Length)
            {
                return false;
            }

            EntryExitAnchors currentRecord = this.entryExitAnchors[coverage];
            AnchorTable? entry = currentRecord.EntryAnchor;
            if (entry is null)
            {
                return false;
            }

            // Cursive attachment joins the current glyph to the closest preceding
            // glyph that the lookup can see. In particular, marks excluded by the
            // lookup's filtering set and default-ignorables are transparent.
            SkippingGlyphIterator iterator = new(fontMetrics, buffer, index, this.LookupFlags, this.MarkFilteringSet);
            iterator.SetMatchContext(uint.MaxValue, false);
            int previousIndex = iterator.Prev();
            if (previousIndex < 0)
            {
                return false;
            }

            ushort previousGlyphId = buffer[previousIndex].GlyphId;
            int previousCoverage = this.coverageTable.CoverageIndexOf(previousGlyphId);
            if (previousCoverage < 0 || previousCoverage >= this.entryExitAnchors.Length)
            {
                return false;
            }

            EntryExitAnchors previousRecord = this.entryExitAnchors[previousCoverage];
            AnchorTable? exit = previousRecord.ExitAnchor;
            if (exit is null)
            {
                return false;
            }

            ref GlyphShapingData previous = ref buffer[previousIndex];
            ref GlyphShapingData current = ref buffer[index];

            AnchorXY exitXY = exit.GetAnchor(fontMetrics, ref previous, buffer);
            AnchorXY entryXY = entry.GetAnchor(fontMetrics, ref current, buffer);

            ref GlyphShapingPosition previousPosition = ref buffer.PositionAt(previousIndex);
            ref GlyphShapingPosition currentPosition = ref buffer.PositionAt(index);

            bool isVerticalLayout = AdvancedTypographicUtils.IsVerticalGlyph(current.CodePoint, buffer.TextOptions.LayoutMode);
            if (!isVerticalLayout)
            {
                // Horizontal
                if (current.Direction == TextDirection.LeftToRight)
                {
                    previousPosition.Bounds.Width = (int)MathF.Round(exitXY.XCoordinate, MidpointRounding.AwayFromZero) + previousPosition.Bounds.X;

                    int delta = (int)MathF.Round(entryXY.XCoordinate, MidpointRounding.AwayFromZero) + currentPosition.Bounds.X;
                    currentPosition.Bounds.Width -= delta;
                    currentPosition.Bounds.X -= delta;
                }
                else
                {
                    int delta = (int)MathF.Round(exitXY.XCoordinate, MidpointRounding.AwayFromZero) + previousPosition.Bounds.X;
                    previousPosition.Bounds.Width -= delta;
                    previousPosition.Bounds.X -= delta;

                    currentPosition.Bounds.Width = (int)MathF.Round(entryXY.XCoordinate, MidpointRounding.AwayFromZero) + currentPosition.Bounds.X;
                }
            }
            else
            {
                // Vertical layout modes advance top-to-bottom; column progression is handled by layout.
                previousPosition.Bounds.Height = (int)MathF.Round(exitXY.YCoordinate, MidpointRounding.AwayFromZero) + previousPosition.Bounds.Y;

                int delta = (int)MathF.Round(entryXY.YCoordinate, MidpointRounding.AwayFromZero) + currentPosition.Bounds.Y;
                currentPosition.Bounds.Height -= delta;
                currentPosition.Bounds.Y -= delta;
            }

            if (previousPosition.Bounds.HasOutOfRangeAdvance || currentPosition.Bounds.HasOutOfRangeAdvance)
            {
                buffer.HasOutOfRangeAdvances = true;
            }

            int child = previousIndex;
            int parent = index;

            // Round after subtracting the anchors so fractional deltas from
            // both anchors contribute to the attachment.
            int xOffset = (int)MathF.Round(entryXY.XCoordinate - exitXY.XCoordinate, MidpointRounding.AwayFromZero);
            int yOffset = (int)MathF.Round(entryXY.YCoordinate - exitXY.YCoordinate, MidpointRounding.AwayFromZero);
            if ((this.LookupFlags & LookupFlags.RightToLeft) != LookupFlags.RightToLeft)
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
            ReverseCursiveMinorOffset(buffer, child, horizontal, parent);

            ref GlyphShapingPosition c = ref buffer.PositionAt(child);
            c.CursiveAttachment = parent - child;
            if (horizontal)
            {
                c.Bounds.Y = yOffset;
            }
            else
            {
                c.Bounds.X = xOffset;
            }

            // If parent was attached to child, separate them so the attachment
            // graph stays acyclic.
            ref GlyphShapingPosition p = ref buffer.PositionAt(parent);
            if (p.CursiveAttachment == -c.CursiveAttachment)
            {
                p.CursiveAttachment = GlyphShapingPosition.NoCursiveAttachment;

                // Bounds.X/Y carry shaping placement offsets here. Clear only the
                // detached parent's minor axis.
                if (horizontal)
                {
                    p.Bounds.Y = 0;
                }
                else
                {
                    p.Bounds.X = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Recursively reverses the cursive minor offset chain so that the entire tree
        /// of a previous connection attaches to the new parent.
        /// </summary>
        /// <param name="buffer">The glyph positioning buffer.</param>
        /// <param name="i">The current index in the chain being reversed.</param>
        /// <param name="horizontal">Whether the layout is horizontal.</param>
        /// <param name="parent">The new parent index to stop at.</param>
        private static void ReverseCursiveMinorOffset(ShapingBuffer buffer, int i, bool horizontal, int parent)
        {
            ref GlyphShapingPosition c = ref buffer.PositionAt(i);
            int chain = c.CursiveAttachment;
            if (chain == GlyphShapingPosition.NoCursiveAttachment)
            {
                return;
            }

            c.CursiveAttachment = GlyphShapingPosition.NoCursiveAttachment;

            int j = i + chain;

            // Stop if we see new parent in the chain.
            if (j == parent)
            {
                return;
            }

            ReverseCursiveMinorOffset(buffer, j, horizontal, parent);

            ref GlyphShapingPosition p = ref buffer.PositionAt(j);
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

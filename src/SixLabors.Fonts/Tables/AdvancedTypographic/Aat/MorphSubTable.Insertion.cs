// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class MorphSubTable
{
    /// <summary>
    /// Reads the glyphs that the state table can insert.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="sections">The offsets of the state table sections.</param>
    /// <param name="machine">The state table.</param>
    /// <param name="indexed">Whether actions use indices into a separate glyph array.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="featureFlags">The feature flags that enable the subtable.</param>
    /// <returns>The insertion subtable, or <see langword="null"/> if no valid insertion list exists.</returns>
    private static InsertionSubTable? LoadInsertion(BigEndianBinaryReader reader, long start, long end, ReadOnlySpan<long> sections, AatStateTable machine, bool indexed, byte coverage, uint featureFlags)
    {
        if (indexed)
        {
            return new InsertionSubTable(coverage, featureFlags, machine, ReadWords(reader, sections[3], end, sections), 0);
        }

        // Classic entries give byte offsets. Read the span that contains the
        // insertion lists that the entries can reach.
        long first = end;
        long last = start;
        foreach (AatStateTable.StateEntry entry in machine.Entries)
        {
            // Bits 5 through 9 give the current glyph's count. Bits 0 through 4 give the marked glyph's count.
            IncludeInsertion(entry.First, (entry.Flags & (ushort)InsertionFlags.CurrentCountMask) >> 5);
            IncludeInsertion(entry.Second, entry.Flags & (ushort)InsertionFlags.MarkedCountMask);
        }

        if (last <= first)
        {
            return null;
        }

        reader.Seek(first, SeekOrigin.Begin);
        return new InsertionSubTable(coverage, featureFlags, machine, reader.ReadUInt16Array((int)((last - first) / 2)), (int)(first - start));

        // Expand the read range to include each valid list of 16-bit glyph IDs.
        void IncludeInsertion(int offset, int count)
        {
            // A list starts at an even byte offset after the classic state header.
            long position = start + offset;
            if (count > 0 && offset >= 8 && (offset & 1) == 0 &&
                AatStateTable.Contains(position, count * 2, end))
            {
                first = Math.Min(first, position);
                last = Math.Max(last, position + (count * 2));
            }
        }
    }

    /// <summary>
    /// Inserts glyphs at the current or marked position in a state table.
    /// </summary>
    private sealed class InsertionSubTable : StateSubTable
    {
        private readonly ushort[] glyphs;
        private readonly int glyphBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertionSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="glyphs">The glyph IDs in the insertion lists.</param>
        /// <param name="glyphBase">The byte offset of the first glyph ID for a classic table, or zero for an extended table.</param>
        public InsertionSubTable(byte coverage, uint featureFlags, AatStateTable machine, ushort[] glyphs, int glyphBase)
            : base(coverage, featureFlags, machine)
        {
            this.glyphs = glyphs;
            this.glyphBase = glyphBase;
        }

        /// <inheritdoc />
        public override void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount)
        {
            int mark = traversal.StartIndex;
            int state = 0;
            while (buffer.CountOperations(1))
            {
                bool atEnd = traversal.AtEnd;
                ushort glyph = atEnd ? (ushort)0 : AatStateTable.GlyphIdAt(buffer, traversal.Index, out _, out _);
                if (!this.Machine.TryTransition(ref state, glyph, atEnd, out AatStateTable.StateEntry entry))
                {
                    break;
                }

                InsertionFlags flags = (InsertionFlags)entry.Flags;
                int current = traversal.Position;
                bool beforeMark = (flags & InsertionFlags.BeforeMarked) != 0;
                this.Insert(buffer, ref traversal, mark, entry.Second, entry.Flags & (ushort)InsertionFlags.MarkedCountMask, beforeMark, true, maxCount);
                if (((MarkFlags)entry.Flags & MarkFlags.SetMark) != 0)
                {
                    // A mark records the output position before this transition's edits.
                    mark = current;
                }

                bool beforeCurrent = (flags & InsertionFlags.BeforeCurrent) != 0;

                // Move the five-bit current count to the low end before inserting.
                this.Insert(buffer, ref traversal, traversal.Position, entry.First, (entry.Flags & (ushort)InsertionFlags.CurrentCountMask) >> 5, beforeCurrent, entry.Advances, maxCount);
                if (atEnd)
                {
                    break;
                }

                if (entry.Advances)
                {
                    traversal.MoveNext();
                }
            }
        }

        /// <summary>
        /// Inserts a glyph list without adding input coverage to its source cluster.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="traversal">The read order of the run.</param>
        /// <param name="index">The read position of the current or marked glyph.</param>
        /// <param name="action">The glyph list index or byte offset.</param>
        /// <param name="count">The number of glyphs to insert.</param>
        /// <param name="before">Whether to insert before the glyph at the given index.</param>
        /// <param name="advance">Whether to advance over the inserted glyphs.</param>
        /// <param name="maxCount">The maximum number of glyphs that the buffer can hold during shaping.</param>
        private void Insert(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int index, ushort action, int count, bool before, bool advance, int maxCount)
        {
            // A separate insertion array uses indices. A classic direct offset is
            // rebased to the first referenced word read from the subtable.
            bool indexed = this.glyphBase == 0;
            int glyphIndex = indexed ? action : (action - this.glyphBase) / 2;

            int length = buffer.Count + buffer.DeletedGlyphs.Count;
            if (count == 0 || count > maxCount - length ||
                (indexed ? action == ushort.MaxValue : action == 0 || action < this.glyphBase || (action & 1) != 0) ||
                (uint)glyphIndex > (uint)(this.glyphs.Length - count) || count > this.glyphs.Length)
            {
                return;
            }

            int end = traversal.EndIndex;
            bool atEnd = index == end;
            int sourceIndex = traversal.GetIndex(atEnd ? index - 1 : index);
            bool insertBefore = atEnd ? traversal.Backwards : before != traversal.Backwards;
            int insertionIndex = insertBefore ? sourceIndex : sourceIndex + 1;

            int deletionListIndex = AatStateTable.CountDeletionsBefore(buffer, insertionIndex);
            int live = insertionIndex - deletionListIndex;
            bool atSpanEnd = live == buffer.SpanEnd;

            // Count each inserted glyph and each deletion index that changes.
            // The pass reuses spare slots as forward insertion moves through the run.
            long cost = count + (long)buffer.DeletedGlyphs.Count - deletionListIndex;

            if (traversal.IsBottomToTop)
            {
                // Inserting also changes the buffer indices in the read order.
                cost += end - traversal.StartIndex + count;
            }

            // A later transition can insert fewer glyphs. If this insertion
            // exceeds the limit, keep the remaining budget for that transition.
            if (cost > buffer.RemainingOperations)
            {
                return;
            }

            buffer.CountOperations(cost);

            GlyphShapingData source = AatStateTable.GlyphDataAt(buffer, sourceIndex);
            source.CodePointCount = 0;
            int position = before || atEnd ? index : index + 1;
            traversal.Insert(position, insertionIndex, count);

            // Every inserted entry takes a state table index. Only live glyphs add a slot.
            Span<(int Index, int Owner, GlyphShapingData Glyph)> events = CollectionsMarshal.AsSpan(buffer.DeletedGlyphs);
            for (int i = deletionListIndex; i < events.Length; i++)
            {
                events[i].Index += count;
            }

            int step = traversal.Backwards ? -1 : 1;
            int glyphEnd = traversal.Backwards ? glyphIndex - 1 : glyphIndex + count;
            int firstGlyph = traversal.Backwards ? glyphIndex + count - 1 : glyphIndex;
            for (int i = firstGlyph; i != glyphEnd; i += step)
            {
                ushort glyphId = this.glyphs[i];
                source.GlyphId = glyphId;
                if (glyphId == AatStateTable.DeletedGlyph)
                {
                    buffer.DeletedGlyphs.Insert(deletionListIndex++, (insertionIndex, live, source));
                }
                else
                {
                    if (live == buffer.Count)
                    {
                        // Appends keep their source input index. Insert adopts
                        // the index of the following glyph, which is absent here.
                        buffer.AddGlyph(glyphId, source.CodePoint, source.Direction, source.TextRunIndex, source.CodePointIndex, source.StringIndex, source.GraphemeIndex);
                        buffer[live] = source;
                    }
                    else
                    {
                        buffer.Insert(live, source);
                        if (atSpanEnd)
                        {
                            // An insertion at this span's end keeps its source
                            // indices even when another span follows it.
                            buffer[live] = source;
                        }
                    }

                    live++;
                }

                insertionIndex++;
            }

            // A nonadvancing action leaves the cursor on the inserted input.
            if (advance)
            {
                traversal.MoveNext(count);
            }
        }
    }
}

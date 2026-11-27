// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class MorphSubTable
{
    /// <summary>
    /// Moves groups of glyphs around the marked range.
    /// </summary>
    private sealed class RearrangementSubTable : StateSubTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RearrangementSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        public RearrangementSubTable(byte coverage, uint featureFlags, AatStateTable machine)
            : base(coverage, featureFlags, machine)
        {
        }

        /// <inheritdoc />
        public override void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount)
        {
            int start = traversal.StartIndex;
            int end = traversal.EndIndex;
            int first = start;
            int last = start;
            int state = 0;
            while (buffer.CountOperations(1))
            {
                int cursor = traversal.Position;
                bool atEnd = traversal.AtEnd;
                ushort glyph = atEnd ? (ushort)0 : AatStateTable.GlyphIdAt(buffer, traversal.Index, out _, out _);
                if (!this.Machine.TryTransition(ref state, glyph, atEnd, out AatStateTable.StateEntry entry))
                {
                    break;
                }

                RearrangementFlags rearrangement = (RearrangementFlags)entry.Flags;
                if ((rearrangement & RearrangementFlags.MarkFirst) != 0)
                {
                    first = cursor;
                }

                if ((rearrangement & RearrangementFlags.MarkLast) != 0)
                {
                    // MarkLast includes the current glyph, except at end of text.
                    last = Math.Min(cursor + 1, end);
                }

                int verb = (int)(rearrangement & RearrangementFlags.VerbMask);
                if (verb != 0 && last > first && last - first <= AdvancedTypographicUtils.MaxContextLength)
                {
                    Rearrange(buffer, in traversal, first, last, Math.Min(cursor + 1, end), verb);
                }

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
        /// Applies one of the AAT rearrangement verbs to the marked range.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="traversal">The read order of the run.</param>
        /// <param name="start">The first position in the rearrangement.</param>
        /// <param name="end">The position after the rearrangement in read order.</param>
        /// <param name="through">The position after the current glyph in read order.</param>
        /// <param name="verb">The rearrangement to apply.</param>
        private static void Rearrange(ShapingBuffer buffer, in AatStateTable.Traversal traversal, int start, int end, int through, int verb)
        {
            // The verb selects a pair of moves. The high four bits describe the
            // left end. The low four bits describe the right end.
            // A value of 3 moves two glyphs and reverses their order.
            ReadOnlySpan<byte> moves = [0x00, 0x10, 0x01, 0x11, 0x20, 0x30, 0x02, 0x03, 0x12, 0x13, 0x21, 0x31, 0x22, 0x32, 0x23, 0x33];
            int left = moves[verb] >> 4;
            int right = moves[verb] & 15;
            int leftCount = Math.Min(2, left);
            int rightCount = Math.Min(2, right);
            if (end - start < leftCount + rightCount)
            {
                return;
            }

            // Charge the cluster scan and rewrite. Finish this rearrangement
            // before the state machine checks the budget on its next step.
            int combinedCount = traversal.CombineInputStarts(buffer, start, Math.Max(end, through));
            buffer.CountOperations(combinedCount * 2L);

            // Move glyphs from the right end to the front. Move glyphs from
            // the left end behind the middle glyphs.
            for (int i = 0; i < rightCount; i++)
            {
                MoveInReadOrder(buffer, traversal, end - rightCount + i, start + i);
            }

            for (int i = 0; i < leftCount; i++)
            {
                MoveInReadOrder(buffer, traversal, start + rightCount, end - 1);
            }

            if (left == 3)
            {
                MoveInReadOrder(buffer, traversal, end - 1, end - 2);
            }

            if (right == 3)
            {
                MoveInReadOrder(buffer, traversal, start + 1, start);
            }
        }

        /// <summary>
        /// Moves a glyph in the subtable's read order.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="traversal">The read order of the run.</param>
        /// <param name="from">The position of the glyph in read order.</param>
        /// <param name="to">The destination position in read order.</param>
        private static void MoveInReadOrder(ShapingBuffer buffer, AatStateTable.Traversal traversal, int from, int to)
        {
            if (!traversal.IsBottomToTop)
            {
                MoveGlyph(buffer, traversal.GetIndex(from), traversal.GetIndex(to));
                return;
            }

            // Adjacent positions in read order can belong to different graphemes.
            // Swap their slots so the glyphs between them stay in place.
            int step = from < to ? 1 : -1;
            int current = traversal.GetIndex(from);
            for (int i = from; i != to;)
            {
                i += step;
                int next = traversal.GetIndex(i);
                int first = Math.Min(current, next);
                int last = Math.Max(current, next);
                MoveGlyph(buffer, last, first);
                MoveGlyph(buffer, first + 1, last);
                current = next;
            }
        }

        /// <summary>
        /// Moves a glyph or deletion event to its new state table index.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="from">The source state table index.</param>
        /// <param name="to">The destination state table index.</param>
        private static void MoveGlyph(ShapingBuffer buffer, int from, int to)
        {
            if (from == to)
            {
                return;
            }

            List<(int Index, int Owner, GlyphShapingData Glyph)>? events = buffer.DeletedGlyphs;
            if (events is null || events.Count == 0)
            {
                buffer.MoveGlyph(from, to);
                return;
            }

            bool deleted = AatStateTable.TryFindDeletionIndex(buffer, from, out int first);
            (int Index, int Owner, GlyphShapingData Glyph) moved = default;
            if (deleted)
            {
                moved = events[first];
                events.RemoveAt(first);
            }

            // Every event between the source and destination moves one index
            // toward the gap. Live glyph movement is still owned by the buffer.
            Span<(int Index, int Owner, GlyphShapingData Glyph)> span = CollectionsMarshal.AsSpan(events);
            bool forward = from < to;
            int start = forward ? first : AatStateTable.CountDeletionsBefore(buffer, to);
            int end = forward ? AatStateTable.CountDeletionsBefore(buffer, to + 1) : first;
            int step = forward ? -1 : 1;
            for (int i = start; i < end; i++)
            {
                span[i].Index += step;
            }

            // A forward move shifts the affected events before the destination.
            // A backward move shifts them after it.
            int destination = forward ? end : start;
            if (deleted)
            {
                moved.Index = to;
                events.Insert(destination, moved);
            }
            else
            {
                buffer.MoveGlyph(from - first, to - destination);
            }
        }
    }
}

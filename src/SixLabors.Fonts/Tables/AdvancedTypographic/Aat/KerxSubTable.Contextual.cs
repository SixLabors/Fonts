// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class KerxSubTable
{
    /// <summary>
    /// Reads the state table and the actions for format 1 or format 4.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="format">The kerx subtable format.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="tupleCount">The number of values per kerning tuple.</param>
    /// <returns>The contextual kerning or attachment subtable, or <see langword="null"/> if it is invalid.</returns>
    private static KerxSubTable? LoadState(BigEndianBinaryReader reader, long start, long end, int glyphCount, byte format, byte coverage, uint tupleCount)
    {
        long body = start + 12;

        // +--------+-----------+-----------------------------------------------------+
        // | Format | Type      | Description                                         |
        // +--------+-----------+-----------------------------------------------------+
        // | 1 / 4  | STXHeader | Extended state table header.                        |
        // | 1      | Offset32  | Kerning-value list offset relative to STXHeader.    |
        // | 4      | uint32    | Top two bits select action type. Low 24 bits locate |
        // |        |           | anchor actions relative to STXHeader.               |
        // +--------+-----------+-----------------------------------------------------+
        if (!AatStateTable.Contains(body, 20, end))
        {
            return null;
        }

        reader.Seek(body + 16, SeekOrigin.Begin);
        uint actionField = reader.ReadUInt32();

        // Format 1 uses the full field as an offset. For format 4, keep bits
        // 0 through 23 to remove the action type and reserved bits.
        long actionStart = body + (format == 1 ? actionField : actionField & 0x00FFFFFF);
        Span<long> sections = stackalloc long[4];
        sections[3] = actionStart;

        // Both formats have one extra uint16 after the common entry
        // fields. It is an action index, with 0xFFFF denoting no action.
        AatStateTable? machine = AatStateTable.Load(reader, body, end, true, glyphCount, 6, sections);
        if (machine is null)
        {
            return null;
        }

        long actionEnd = AatStateTable.GetSectionEnd(actionStart, end, sections);
        reader.Seek(actionStart, SeekOrigin.Begin);
        short[] actions = reader.ReadInt16Array((int)((actionEnd - actionStart) / 2));

        // In format 4, move bits 30 and 31 to the low end to get the action type.
        return format == 1
            ? new ContextualSubTable(coverage, machine, actions, tupleCount)
            : new AttachmentSubTable(coverage, machine, actions, (byte)(actionField >> 30));
    }

    /// <summary>
    /// Adjusts the positions stored by a format 1 state table.
    /// </summary>
    private sealed class ContextualSubTable : KerxSubTable
    {
        private readonly AatStateTable machine;
        private readonly short[] actions;
        private readonly uint tupleCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="actions">The kerning values and end markers.</param>
        /// <param name="tupleCount">The number of values per kerning tuple.</param>
        public ContextualSubTable(byte coverage, AatStateTable machine, short[] actions, uint tupleCount)
            : base(coverage)
        {
            this.machine = machine;
            this.actions = actions;
            this.tupleCount = tupleCount;
        }

        /// <summary>
        /// Flags specific to a kerx format 1 transition.
        /// </summary>
        [Flags]
        private enum KerningFlags : ushort
        {
            /// <summary>
            /// Stores the current glyph position in kerning history.
            /// </summary>
            Push = 0x8000
        }

        /// <inheritdoc />
        public override bool Apply(FontMetrics font, ShapingBuffer buffer, AatStateTable.Traversal traversal, bool requestedKerning, AatAnchorTable? anchors, scoped Span<(int Value, bool Reset)> crossStream)
        {
            bool updated = false;

            // CoreText retains the last eight component positions between action
            // lists. Reuse the buffer's contextual-match storage for this history.
            Span<int> history = buffer.GetContextMatchPositions()[..8];

            // CoreText starts unused slots at the first glyph. An action list can
            // read around the history more than once before it reaches the terminator.
            history.Fill(traversal.Index);
            bool pushed = false;
            int head = 0;
            int state = 0;
            while (buffer.CountOperations(1))
            {
                int i = traversal.Index;
                bool atEnd = traversal.AtEnd;
                ushort glyph = atEnd ? (ushort)0 : AatStateTable.GlyphIdAt(buffer, i, out _, out _);
                if (!this.machine.TryTransition(ref state, glyph, atEnd, out AatStateTable.StateEntry entry))
                {
                    break;
                }

                if (((KerningFlags)entry.Flags & KerningFlags.Push) != 0)
                {
                    history[head] = i;
                    head = (head + 1) % history.Length;
                    pushed = true;
                }

                // The extended list ends at a single 0xFFFF. Each other
                // entry occupies one word, or tupleCount words for a tuple.
                int cursor = head;
                long stride = Math.Max(1L, this.tupleCount);
                long action = entry.First;
                for (; pushed && entry.First != ushort.MaxValue && action < this.actions.Length && buffer.CountOperations(1); action += stride)
                {
                    int value = this.actions[(int)action];
                    if (value == -1)
                    {
                        break;
                    }

                    cursor = (cursor + history.Length - 1) % history.Length;
                    int index = history[cursor];
                    if (index == traversal.EndIndex)
                    {
                        // If the terminal transition pushes end-of-text, consume
                        // a value without applying it. End-of-text has no position.
                        continue;
                    }

                    if (this.IsCrossStream)
                    {
                        AddCrossStream(ref crossStream[index - traversal.StartIndex], value, accumulate: true);
                    }
                    else if (requestedKerning)
                    {
                        int live = AatStateTable.LiveIndex(buffer, index);
                        if (live < 0)
                        {
                            continue;
                        }

                        // Contextual actions give the full adjustment to one
                        // glyph. Convert kerx Y values to the font's upward-positive axis.
                        int dx = this.IsVertical ? 0 : value;
                        int dy = this.IsVertical ? -value : 0;
                        AdvancedTypographicUtils.ApplyAdvance(font, buffer, live, buffer[live].GlyphId, dx, dy);

                        ref GlyphShapingBounds bounds = ref buffer.PositionAt(live).Bounds;
                        bounds.X += dx;
                        bounds.Y += dy;
                        updated |= value != 0;
                    }
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

            return updated;
        }
    }
}

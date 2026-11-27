// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class MorphSubTable
{
    /// <summary>
    /// Parses the lookup tables referenced by extended contextual entries.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first lookup offset.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="entries">The state table entries that reference the lookups.</param>
    /// <returns>The lookups in the order of their indices in the state entries.</returns>
    private static AatLookupTable?[] LoadContextualLookups(
        BigEndianBinaryReader reader,
        long start,
        long end,
        int glyphCount,
        ReadOnlySpan<AatStateTable.StateEntry> entries)
    {
        int count = 0;
        foreach (AatStateTable.StateEntry entry in entries)
        {
            if (entry.First != ushort.MaxValue)
            {
                count = Math.Max(count, entry.First + 1);
            }

            if (entry.Second != ushort.MaxValue)
            {
                count = Math.Max(count, entry.Second + 1);
            }
        }

        if (!AatStateTable.Contains(start, count * 4, end))
        {
            return [];
        }

        // Each index selects an Offset32 from this list. The offset is relative
        // to the beginning of the list rather than the state header.
        reader.Seek(start, SeekOrigin.Begin);
        uint[] offsets = reader.ReadUInt32Array(count);
        uint[] sortedOffsets = (uint[])offsets.Clone();
        Array.Sort(sortedOffsets);

        // Decode shared offsets once. Different lookups can still refer to
        // the same value bytes. Allow twice the data length after the offset
        // list so overlap has room and cannot cause unbounded reads.
        long remaining = 2 * (end - start - (count * 4L));
        AatLookupTable?[] lookups = new AatLookupTable?[count];
        Dictionary<uint, AatLookupTable?> loaded = [];
        for (int i = 0; i < count; i++)
        {
            uint offset = offsets[i];
            if (offset < count * 4)
            {
                continue;
            }

            if (!loaded.TryGetValue(offset, out AatLookupTable? lookup))
            {
                // The sorted copy contains this offset. Advance past its duplicates
                // to find the byte limit of the current lookup.
                int next = Array.BinarySearch(sortedOffsets, offset) + 1;
                while (next < sortedOffsets.Length && sortedOffsets[next] == offset)
                {
                    next++;
                }

                long nextLookup = next < sortedOffsets.Length ? Math.Min(start + sortedOffsets[next], end) : end;
                lookup = AatLookupTable.Load(reader, start + offset, end, nextLookup, glyphCount, 2, ref remaining);
                loaded.Add(offset, lookup);
            }

            lookups[i] = lookup;
        }

        return lookups;
    }

    /// <summary>
    /// Applies replacements to the current or marked glyph through a state table.
    /// </summary>
    private abstract class ContextualSubTable : StateSubTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        protected ContextualSubTable(byte coverage, uint featureFlags, AatStateTable machine)
            : base(coverage, featureFlags, machine)
        {
        }

        /// <inheritdoc />
        public override void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount)
        {
            int mark = traversal.Index;
            bool marked = false;
            int state = 0;
            while (buffer.CountOperations(1))
            {
                bool atEnd = traversal.AtEnd;
                int index = atEnd ? traversal.GetIndex(traversal.EndIndex - 1) : traversal.Index;
                ushort glyph = atEnd ? (ushort)0 : AatStateTable.GlyphIdAt(buffer, index, out _, out _);
                if (!this.Machine.TryTransition(ref state, glyph, atEnd, out AatStateTable.StateEntry entry))
                {
                    break;
                }

                if (!atEnd || marked)
                {
                    // The terminal action can replace the last glyph
                    // only if the machine previously established a mark.
                    this.Substitute(buffer, mark, entry.First);
                    this.Substitute(buffer, index, entry.Second);
                    if (((MarkFlags)entry.Flags & MarkFlags.SetMark) != 0)
                    {
                        mark = index;
                        marked = true;
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
        }

        /// <summary>
        /// Applies the specified classic or extended replacement action.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="index">The state table index of the glyph to replace.</param>
        /// <param name="action">The value that identifies the replacement action.</param>
        protected abstract void Substitute(ShapingBuffer buffer, int index, ushort action);
    }

    /// <summary>
    /// Uses the replacement words in a classic state table.
    /// </summary>
    private sealed class ClassicContextualSubTable : ContextualSubTable
    {
        private readonly ushort[] glyphs;
        private readonly int glyphBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicContextualSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="glyphs">The replacement glyph IDs.</param>
        /// <param name="glyphBase">The offset of the first glyph ID from the state table header.</param>
        public ClassicContextualSubTable(byte coverage, uint featureFlags, AatStateTable machine, ushort[] glyphs, int glyphBase)
            : base(coverage, featureFlags, machine)
        {
            this.glyphs = glyphs;
            this.glyphBase = glyphBase;
        }

        /// <inheritdoc />
        protected override void Substitute(ShapingBuffer buffer, int index, ushort action)
        {
            // The signed word offset is relative to the current glyph ID.
            // A zero offset or glyph word means that the glyph stays in place.
            if (action == 0)
            {
                return;
            }

            ushort glyphId = AatStateTable.GlyphIdAt(buffer, index, out bool deleted, out int deletionsBefore);
            int word = (short)action + glyphId - this.glyphBase;
            if ((uint)word < (uint)this.glyphs.Length && this.glyphs[word] != 0)
            {
                ReplaceGlyph(buffer, index, this.glyphs[word], deleted, deletionsBefore);
            }
        }
    }

    /// <summary>
    /// Uses the AAT lookups in an extended contextual subtable.
    /// </summary>
    private sealed class ExtendedContextualSubTable : ContextualSubTable
    {
        private readonly AatLookupTable?[] lookups;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedContextualSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="lookups">The substitution lookups in the order of their indices.</param>
        public ExtendedContextualSubTable(byte coverage, uint featureFlags, AatStateTable machine, AatLookupTable?[] lookups)
            : base(coverage, featureFlags, machine)
            => this.lookups = lookups;

        /// <inheritdoc />
        protected override void Substitute(ShapingBuffer buffer, int index, ushort action)
        {
            if (action >= this.lookups.Length || this.lookups[action] is not AatLookupTable lookup)
            {
                return;
            }

            ushort glyphId = AatStateTable.GlyphIdAt(buffer, index, out bool deleted, out int deletionsBefore);
            if (lookup.TryGetValue(glyphId, out uint replacement) && replacement <= ushort.MaxValue)
            {
                ReplaceGlyph(buffer, index, (ushort)replacement, deleted, deletionsBefore);
            }
        }
    }
}

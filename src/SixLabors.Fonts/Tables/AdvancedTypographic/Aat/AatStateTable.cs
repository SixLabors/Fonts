// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// A classic or extended Apple Advanced Typography state table, decoded at font
/// load so the substitution and positioning passes share the same transition logic.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html"/>
/// </summary>
internal sealed class AatStateTable
{
    /// <summary>
    /// Glyph ID reserved by AAT for a deleted glyph.
    /// </summary>
    public const ushort DeletedGlyph = 0xFFFF;

    private readonly AatLookupTable classes;
    private readonly int classCount;
    private readonly int rowCount;
    private readonly ushort[] states;
    private readonly StateEntry[] entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="AatStateTable"/> class.
    /// </summary>
    /// <param name="classes">The lookup from glyph IDs to state table classes.</param>
    /// <param name="classCount">The number of classes in each row.</param>
    /// <param name="states">The entry indices for each state and class.</param>
    /// <param name="entries">The state table entries.</param>
    private AatStateTable(AatLookupTable classes, int classCount, ushort[] states, StateEntry[] entries)
    {
        this.classes = classes;
        this.classCount = classCount;
        this.rowCount = states.Length / classCount;
        this.states = states;
        this.entries = entries;
    }

    /// <summary>
    /// Common transition flags. Each subtable format defines the other bits.
    /// </summary>
    [Flags]
    private enum EntryFlags : ushort
    {
        /// <summary>
        /// Revisits the current input in the next state.
        /// </summary>
        DontAdvance = 0x4000
    }

    /// <summary>
    /// Gets the parsed entries so a subtable can load the actions they reference.
    /// </summary>
    public ReadOnlySpan<StateEntry> Entries => this.entries;

    /// <summary>
    /// Loads the class lookup, state array, and transition entries for a state table.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="extended">Whether the table uses the extended format.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="entrySize">The number of bytes per entry.</param>
    /// <param name="sections">
    /// The section offsets from the start of the reader.
    /// The caller fills the action offsets after slot 2.
    /// On success, slots 0 through 2 contain the class, state, and entry offsets.
    /// </param>
    /// <returns>The state table, or <see langword="null"/> if it cannot be read.</returns>
    public static AatStateTable? Load(
        BigEndianBinaryReader reader,
        long start,
        long end,
        bool extended,
        int glyphCount,
        int entrySize,
        Span<long> sections)
    {
        // +---------------+------------+------------------------------------------------+
        // | Type          | Name       | Description                                    |
        // +---------------+------------+------------------------------------------------+
        // | uint16/uint32 | nClasses   | Number of classes, including four reserved.    |
        // | Offset16/32   | classTable | Offset to a classic class array or AAT lookup. |
        // | Offset16/32   | stateArray | Offset to rows of uint8/uint16 entry indices.  |
        // | Offset16/32   | entryTable | Offset to transition entries.                  |
        // | Offset16/32[] | actions    | Format-specific offsets, where applicable.     |
        // +---------------+------------+------------------------------------------------+
        // The left type in each row is classic. The right type is extended.
        // All offsets are relative to the beginning of this state header.
        int width = extended ? 4 : 2;
        int headerSize = (sections.Length + 1) * width;
        if (!Contains(start, headerSize, end))
        {
            return null;
        }

        reader.Seek(start, SeekOrigin.Begin);
        uint classCount = extended ? reader.ReadUInt32() : reader.ReadUInt16();
        for (int i = 0; i < 3; i++)
        {
            sections[i] = start + (extended ? reader.ReadOffset32() : reader.ReadOffset16());
        }

        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i] < start + headerSize || sections[i] > end)
            {
                return null;
            }
        }

        // The first four classes are reserved. A 16-bit class ID allows up to 65,536 classes.
        if (classCount is < 4 or > 65536)
        {
            return null;
        }

        long classTableOffset = sections[0];
        long stateArrayOffset = sections[1];
        long entryTableOffset = sections[2];

        // Each of these sections has a different format and requires a distinct start offset.
        if (classTableOffset == stateArrayOffset || classTableOffset == entryTableOffset || stateArrayOffset == entryTableOffset)
        {
            return null;
        }

        long classEnd = GetSectionEnd(classTableOffset, end, sections);
        AatLookupTable? classes = extended
            ? AatLookupTable.Load(reader, classTableOffset, classEnd, glyphCount, 2)
            : AatLookupTable.LoadClassicClasses(reader, classTableOffset, classEnd);

        if (classes is null)
        {
            return null;
        }

        // Only complete rows belong to the state array. Padding before the next
        // section can contain less than one row and cannot be a transition target.
        int cellSize = extended ? 2 : 1;
        long stateLength = GetSectionEnd(stateArrayOffset, end, sections) - stateArrayOffset;
        long rowCount = stateLength / (classCount * cellSize);
        if (rowCount == 0 || rowCount > 65536 || rowCount * classCount > int.MaxValue)
        {
            return null;
        }

        int stateCount = (int)(rowCount * classCount);
        reader.Seek(stateArrayOffset, SeekOrigin.Begin);
        ushort[] states;
        if (extended)
        {
            states = reader.ReadUInt16Array(stateCount);
        }
        else
        {
            states = new ushort[stateCount];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = reader.ReadUInt8();
            }
        }

        int entryCount = 1;
        foreach (ushort entry in states)
        {
            entryCount = Math.Max(entryCount, entry + 1);
        }

        long entryLength = GetSectionEnd(entryTableOffset, end, sections) - entryTableOffset;
        if (entryCount * entrySize > entryLength)
        {
            return null;
        }

        // +--------------+-------------+---------------------------------------------------+
        // | Type         | Name        | Description                                       |
        // +--------------+-------------+---------------------------------------------------+
        // | uint16       | newState    | Row index, or a state-header offset when classic. |
        // | uint16       | flags       | Transition and format-specific action flags.      |
        // | uint16[0..2] | actionWords | Additional operands defined by the format.        |
        // +--------------+-------------+---------------------------------------------------+
        reader.Seek(entryTableOffset, SeekOrigin.Begin);
        StateEntry[] entries = new StateEntry[entryCount];
        int stateOffset = (int)(stateArrayOffset - start);
        for (int i = 0; i < entries.Length; i++)
        {
            int nextState = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();
            ushort first = entrySize >= 6 ? reader.ReadUInt16() : (ushort)0;
            ushort second = entrySize == 8 ? reader.ReadUInt16() : (ushort)0;

            // Convert classic byte offsets to rows once. Invalid destinations
            // terminate this machine if actually reached by the input.
            if (!extended)
            {
                nextState = nextState >= stateOffset ? (nextState - stateOffset) / (int)classCount : -1;
            }

            entries[i] = new(nextState, flags, first, second);
        }

        return new(classes, (int)classCount, states, entries);
    }

    /// <summary>
    /// Gets the transition for a glyph or the terminal input and updates the current state.
    /// </summary>
    /// <param name="state">The current state. Receives the next state on success.</param>
    /// <param name="glyphId">The glyph ID for the transition.</param>
    /// <param name="atEnd">Whether the cursor is at the end of the run.</param>
    /// <param name="entry">The entry for the current state and glyph class.</param>
    /// <returns><see langword="true"/> if the state table contains the transition.</returns>
    public bool TryTransition(ref int state, ushort glyphId, bool atEnd, out StateEntry entry)
    {
        if ((uint)state >= (uint)this.rowCount)
        {
            entry = default;
            return false;
        }

        // Class 0 marks the end of text. Class 1 covers glyphs outside the lookup.
        // Class 2 lets a later state table respond to a deletion event.
        uint glyphClass = atEnd ? 0 : glyphId == DeletedGlyph ? 2 :
            this.classes.TryGetValue(glyphId, out uint value) ? value : 1;

        int column = glyphClass < this.classCount ? (int)glyphClass : 1;
        entry = this.entries[this.states[(state * this.classCount) + column]];
        state = entry.NextState;
        return true;
    }

    /// <summary>
    /// Gets the end of a variable-length section from its closest following section.
    /// Apple state tables permit the sections to occur in any order.
    /// </summary>
    /// <param name="start">The offset of the first byte in the section.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="sections">The offsets of the sections in the table.</param>
    /// <returns>The offset of the next section, or the table end if this is the last section.</returns>
    public static long GetSectionEnd(long start, long end, ReadOnlySpan<long> sections)
    {
        foreach (long section in sections)
        {
            if (section > start && section < end)
            {
                end = section;
            }
        }

        return end;
    }

    /// <summary>
    /// Tests a table-relative byte range before seeking or allocating its contents.
    /// </summary>
    /// <param name="offset">The offset of the first byte in the range.</param>
    /// <param name="length">The number of bytes in the range.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <returns><see langword="true"/> if the range starts at or after zero and ends at or before the limit.</returns>
    public static bool Contains(long offset, long length, long end)
        => offset >= 0 && offset <= end && length >= 0 && length <= end - offset;

    /// <summary>
    /// Counts the deletion events before a boundary in the selected index space.
    /// </summary>
    /// <remarks>
    /// The search uses <c>Owner</c> for live glyph boundaries and <c>Index</c> for state table boundaries.
    /// The deletion list must be sorted by the field used for the search.
    /// </remarks>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The live glyph boundary or the state table boundary.</param>
    /// <param name="liveBoundary">Whether the index is a boundary in the live glyph buffer.</param>
    /// <returns>The number of events before the boundary.</returns>
    public static int CountDeletionsBefore(ShapingBuffer buffer, int index, bool liveBoundary = false)
        => AdvancedTypographicUtils.CountDeletionsBefore(CollectionsMarshal.AsSpan(buffer.DeletedGlyphs), index, liveBoundary);

    /// <summary>
    /// Removes deletions that do not belong to the state table stream.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    public static void DiscardUnownedDeletions(ShapingBuffer buffer)
    {
        List<(int Index, int Owner, GlyphShapingData Glyph)> events = buffer.DeletedGlyphs;

        // Deletions outside the AAT state table have no state table index.
        // Their owner value puts them after the AAT events. Remove them before
        // searching by state table index.
        int count = CountDeletionsBefore(buffer, AdvancedTypographicUtils.NoDeletionOwner, true);
        events.RemoveRange(count, events.Count - count);
    }

    /// <summary>
    /// Checks whether the deletion list contains an event at the given state table index.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index to find.</param>
    /// <param name="deletionsBefore">Receives the count of earlier events. On a match, this is the list index of the event.</param>
    /// <returns><see langword="true"/> if an event has the given state table index.</returns>
    public static bool TryFindDeletionIndex(ShapingBuffer buffer, int index, out int deletionsBefore)
    {
        deletionsBefore = CountDeletionsBefore(buffer, index);
        ReadOnlySpan<(int Index, int Owner, GlyphShapingData Glyph)> events = CollectionsMarshal.AsSpan(buffer.DeletedGlyphs);
        return deletionsBefore < events.Length && events[deletionsBefore].Index == index;
    }

    /// <summary>
    /// Maps a state table index to a live glyph.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <returns>The live index, or -1 for a deletion event.</returns>
    public static int LiveIndex(ShapingBuffer buffer, int index)
    {
        bool deleted = TryFindDeletionIndex(buffer, index, out int deletionsBefore);

        // The live buffer omits the events before this index.
        return deleted ? -1 : index - deletionsBefore;
    }

    /// <summary>
    /// Gets the glyph ID used to select a state table transition.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <param name="deleted">Receives whether the index identifies a deletion event.</param>
    /// <param name="deletionsBefore">Receives the number of deletion events before the index.</param>
    /// <returns>The glyph ID, or <see cref="DeletedGlyph"/> for a deleted entry.</returns>
    public static ushort GlyphIdAt(ShapingBuffer buffer, int index, out bool deleted, out int deletionsBefore)
    {
        // A deletion event selects the deleted-glyph class. For a live glyph,
        // subtract the earlier events because they occupy no slots in the buffer.
        deleted = TryFindDeletionIndex(buffer, index, out deletionsBefore);
        return deleted ? DeletedGlyph : buffer[index - deletionsBefore].GlyphId;
    }

    /// <summary>
    /// Gets the input data associated with a state table index.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <returns>The live or deleted glyph data.</returns>
    public static GlyphShapingData GlyphDataAt(ShapingBuffer buffer, int index)
    {
        if (TryFindDeletionIndex(buffer, index, out int deletionsBefore))
        {
            ReadOnlySpan<(int Index, int Owner, GlyphShapingData Glyph)> events = CollectionsMarshal.AsSpan(buffer.DeletedGlyphs);
            return events[deletionsBefore].Glyph;
        }

        return buffer[index - deletionsBefore];
    }

    /// <summary>
    /// Maps positions in subtable read order to live glyph indices or state table indices.
    /// State table indices include deletion events.
    /// </summary>
    public struct Traversal
    {
        private int cursor;
        private int[]? indices;

        /// <summary>
        /// Initializes a new instance of the <see cref="Traversal"/> struct.
        /// </summary>
        /// <param name="start">The first index of the run in buffer order.</param>
        /// <param name="end">The index after the run in buffer order.</param>
        /// <param name="backwards">Whether the subtable reads the run from the end.</param>
        /// <param name="indices">
        /// The rented array of state table indices for a bottom-to-top run, if present.
        /// The caller returns the array or calls <see cref="ReleaseIndices"/>.
        /// </param>
        public Traversal(int start, int end, bool backwards, int[]? indices = null)
        {
            this.StartIndex = start;
            this.EndIndex = end;
            this.Backwards = backwards;
            this.cursor = start;
            this.indices = indices;
        }

        /// <summary>
        /// Gets the live glyph or state table index at the current position.
        /// </summary>
        public readonly int Index => this.AtEnd ? this.EndIndex : this.GetIndex(this.cursor);

        /// <summary>
        /// Gets the current position in the subtable's read order.
        /// </summary>
        public readonly int Position => this.cursor;

        /// <summary>
        /// Gets the first index of the run in buffer order.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the index after the run in buffer order.
        /// </summary>
        public int EndIndex { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the subtable reads the run from the end.
        /// </summary>
        public bool Backwards { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the run reads graphemes in bottom-to-top order.
        /// </summary>
        public readonly bool IsBottomToTop => this.indices is not null;

        /// <summary>
        /// Gets a value indicating whether the traversal has reached the end of the run.
        /// </summary>
        public readonly bool AtEnd => this.cursor == this.EndIndex;

        /// <summary>
        /// Records the bottom-to-top order while the shaping buffer stays in input order.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="start">The first state table index in the run.</param>
        /// <param name="end">The state table index after the run.</param>
        /// <returns>
        /// A pooled array. The first <c>end - start</c> entries contain state table indices
        /// in bottom-to-top order. The caller returns the array to the pool or passes it
        /// to a traversal that calls <see cref="ReleaseIndices"/>.
        /// </returns>
        public static int[] GetBottomToTopIndices(ShapingBuffer buffer, int start, int end)
        {
            int[] indices = ArrayPool<int>.Shared.Rent(end - start);
            int write = end - start;
            int first = start;
            while (first < end)
            {
                // Bottom-to-top reads the grapheme groups in reverse order.
                // It reads the glyphs within each group in their input order.
                int grapheme = GlyphDataAt(buffer, first).GraphemeIndex;
                int next = first + 1;
                while (next < end && GlyphDataAt(buffer, next).GraphemeIndex == grapheme)
                {
                    next++;
                }

                write -= next - first;
                for (int i = first; i < next; i++)
                {
                    indices[write + (i - first)] = i;
                }

                first = next;
            }

            return indices;
        }

        /// <summary>
        /// Maps a position in the subtable's read order to the run's glyph order.
        /// </summary>
        /// <param name="index">The position in read order, starting at the first index of the run.</param>
        /// <returns>
        /// A live glyph index for a run that uses live bounds. A run that uses state table
        /// bounds returns a state table index, which can identify a deletion event.
        /// </returns>
        public readonly int GetIndex(int index)
        {
            int offset = this.Backwards ? this.EndIndex - 1 - index : index - this.StartIndex;
            return this.indices is null ? this.StartIndex + offset : this.indices[offset];
        }

        /// <summary>
        /// Combines the input starts of the glyphs covered by a subtable action.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="start">The first position in read order.</param>
        /// <param name="end">The position after the range in read order.</param>
        /// <returns>The number of live glyphs in the combined range.</returns>
        public readonly int CombineInputStarts(ShapingBuffer buffer, int start, int end)
        {
            int a = this.GetIndex(start);
            int b = this.GetIndex(end - 1);
            int first = Math.Min(a, b);
            int last = Math.Max(a, b) + 1;

            // The endpoints bound a run read in one direction. Bottom-to-top
            // reads each grapheme forward and can jump between groups.
            if (this.IsBottomToTop)
            {
                for (int i = start + 1; i < end - 1; i++)
                {
                    int index = this.GetIndex(i);
                    first = Math.Min(first, index);
                    last = Math.Max(last, index + 1);
                }
            }

            int liveStart = first - CountDeletionsBefore(buffer, first);
            int liveEnd = last - CountDeletionsBefore(buffer, last);
            if (liveStart < liveEnd)
            {
                buffer.CombineInputStarts(liveStart, liveEnd);
            }

            return liveEnd - liveStart;
        }

        /// <summary>
        /// Updates the bottom-to-top order after a glyph list was inserted.
        /// </summary>
        /// <param name="position">The insertion position in read order.</param>
        /// <param name="index">The first inserted index in buffer order.</param>
        /// <param name="count">The number of inserted indices.</param>
        public void Insert(int position, int index, int count)
        {
            int end = this.EndIndex;
            this.EndIndex += count;
            if (this.indices is null)
            {
                return;
            }

            int length = end - this.StartIndex;
            int offset = this.Backwards ? end - position : position - this.StartIndex;
            int[] order = this.indices;
            if (length + count > order.Length)
            {
                order = ArrayPool<int>.Shared.Rent(length + count);
                this.indices.AsSpan(0, length).CopyTo(order);
                ArrayPool<int>.Shared.Return(this.indices);
            }

            for (int i = 0; i < length; i++)
            {
                if (order[i] >= index)
                {
                    order[i] += count;
                }
            }

            Array.Copy(order, offset, order, offset + count, length - offset);

            // The inserted glyphs stay adjacent to their source grapheme in
            // both orders. The array always stores the forward table order.
            for (int i = 0; i < count; i++)
            {
                order[offset + i] = index + i;
            }

            this.indices = order;
        }

        /// <summary>
        /// Advances past the given number of glyphs or deletion events.
        /// </summary>
        /// <param name="count">The number of positions to advance.</param>
        public void MoveNext(int count = 1) => this.cursor += count;

        /// <summary>
        /// Returns the rented bottom-to-top index array.
        /// </summary>
        public void ReleaseIndices()
        {
            if (this.indices is not null)
            {
                ArrayPool<int>.Shared.Return(this.indices);
                this.indices = null;
            }
        }

        /// <summary>
        /// Starts reading the same run in the next subtable's direction.
        /// </summary>
        /// <param name="backwards">Whether the next subtable reads the run from the end.</param>
        public void ResetDirection(bool backwards)
        {
            this.Backwards = backwards;
            this.cursor = this.StartIndex;
        }
    }

    /// <summary>
    /// A state-table transition with up to two format-dependent action words.
    /// </summary>
    public readonly struct StateEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateEntry"/> struct.
        /// </summary>
        /// <param name="nextState">The state to enter after this transition.</param>
        /// <param name="flags">The flags that control this transition.</param>
        /// <param name="first">The first action value.</param>
        /// <param name="second">The second action value.</param>
        public StateEntry(int nextState, ushort flags, ushort first, ushort second)
        {
            this.NextState = nextState;
            this.Flags = flags;
            this.First = first;
            this.Second = second;
        }

        /// <summary>
        /// Gets the destination row.
        /// </summary>
        public int NextState { get; }

        /// <summary>
        /// Gets the transition flags.
        /// </summary>
        public ushort Flags { get; }

        /// <summary>
        /// Gets a value indicating whether the transition consumes the current input.
        /// </summary>
        public bool Advances => ((EntryFlags)this.Flags & EntryFlags.DontAdvance) == 0;

        /// <summary>
        /// Gets the first format-specific operand, if present.
        /// </summary>
        public ushort First { get; }

        /// <summary>
        /// Gets the second format-specific operand, if present.
        /// </summary>
        public ushort Second { get; }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Applies one morx or mort subtable.
/// </summary>
internal abstract partial class MorphSubTable
{
    private readonly CoverageFlags coverage;
    private readonly uint featureFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="MorphSubTable"/> class.
    /// </summary>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="featureFlags">The feature flags that enable the subtable.</param>
    private MorphSubTable(byte coverage, uint featureFlags)
    {
        this.coverage = (CoverageFlags)coverage;
        this.featureFlags = featureFlags;
    }

    /// <summary>
    /// Direction bits in the high byte of the subtable coverage field.
    /// </summary>
    [Flags]
    private enum CoverageFlags : byte
    {
        /// <summary>
        /// Uses the logical glyph order before the reverse flag applies.
        /// </summary>
        LogicalOrder = 0x10,

        /// <summary>
        /// Applies the subtable in both horizontal and vertical runs.
        /// </summary>
        AllDirections = 0x20,

        /// <summary>
        /// Reverses the selected glyph order.
        /// </summary>
        Backwards = 0x40,

        /// <summary>
        /// Applies the subtable to vertical runs when <see cref="AllDirections"/> is clear.
        /// </summary>
        Vertical = 0x80
    }

    /// <summary>
    /// Action fields in a rearrangement entry.
    /// </summary>
    [Flags]
    private enum RearrangementFlags : ushort
    {
        /// <summary>
        /// Selects the rearrangement verb.
        /// </summary>
        VerbMask = 0x000F,

        /// <summary>
        /// Records the last glyph in the range.
        /// </summary>
        MarkLast = 0x2000,

        /// <summary>
        /// Records the first glyph in the range.
        /// </summary>
        MarkFirst = 0x8000
    }

    /// <summary>
    /// Mark flag shared by contextual substitution and insertion entries.
    /// </summary>
    [Flags]
    private enum MarkFlags : ushort
    {
        /// <summary>
        /// Records the current glyph for a later action.
        /// </summary>
        SetMark = 0x8000
    }

    /// <summary>
    /// Action fields in a ligature substitution entry.
    /// </summary>
    [Flags]
    private enum LigatureFlags : ushort
    {
        /// <summary>
        /// Locates the action list in the classic state table.
        /// </summary>
        ClassicActionOffsetMask = 0x3FFF,

        /// <summary>
        /// Executes the indexed action list in an extended state table.
        /// </summary>
        PerformAction = 0x2000,

        /// <summary>
        /// Pushes the current glyph onto the component stack.
        /// </summary>
        SetComponent = 0x8000
    }

    /// <summary>
    /// Action fields in a glyph insertion entry.
    /// </summary>
    [Flags]
    private enum InsertionFlags : ushort
    {
        /// <summary>
        /// Masks the number of glyphs to insert at the marked position.
        /// </summary>
        MarkedCountMask = 0x001F,

        /// <summary>
        /// Masks the number of glyphs to insert at the current position.
        /// </summary>
        CurrentCountMask = 0x03E0,

        /// <summary>
        /// Places the insertion before the marked glyph.
        /// </summary>
        BeforeMarked = 0x0400,

        /// <summary>
        /// Places the insertion before the current glyph.
        /// </summary>
        BeforeCurrent = 0x0800
    }

    /// <summary>
    /// High bits of a ligature action word.
    /// </summary>
    [Flags]
    private enum LigatureActionFlags : uint
    {
        /// <summary>
        /// Stores the accumulated ligature at this component.
        /// </summary>
        Store = 0x40000000,

        /// <summary>
        /// Stores the result and ends the action list.
        /// </summary>
        Last = 0x80000000
    }

    /// <summary>
    /// Loads the body of a substitution subtable after its common header.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="extended">Whether the table uses the extended format.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="format">The substitution subtable format.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="featureFlags">The feature flags that enable the subtable.</param>
    /// <returns>The substitution subtable, or <see langword="null"/> if the format or data is invalid.</returns>
    public static MorphSubTable? Load(
        BigEndianBinaryReader reader,
        long start,
        long end,
        bool extended,
        int glyphCount,
        int format,
        byte coverage,
        uint featureFlags)
    {
        if (format == 4)
        {
            // A noncontextual subtable consists of a single AAT glyph lookup.
            AatLookupTable? lookup = AatLookupTable.Load(reader, start, end, glyphCount, 2);
            return lookup is null ? null : new NoncontextualSubTable(coverage, featureFlags, lookup);
        }

        // One of these layout-specific headers follows the common state header.
        // +--------+----------------+---------------------------------------------+
        // | Format | Type           | Name and meaning                            |
        // +--------+----------------+---------------------------------------------+
        // | 0      |                | No extra fields for rearrangement.          |
        // | 1      | Offset16/32    | Substitution lookup list or classic glyphs. |
        // | 2      | Offset16/32[3] | Action, component, and ligature arrays.     |
        // | 5      | Offset32       | Insertion glyph array. Absent in mort.      |
        // +--------+----------------+---------------------------------------------+
        // All offsets are from the beginning of the state header.
        int width = extended ? 4 : 2;
        int extraCount = format switch
        {
            0 => 0,
            1 => 1,
            2 => 3,
            5 => extended ? 1 : 0,
            _ => -1
        };

        if (format == 5 && !extended && AatStateTable.Contains(start, 10, end))
        {
            // A widely used mort layout places an insertion-array offset
            // after the classic header and stores indices in its entries.
            // The Apple-documented byte-offset form has no extra header.
            reader.Seek(start + 2, SeekOrigin.Begin);
            int firstSection = reader.ReadOffset16();
            int states = reader.ReadOffset16();
            int entries = reader.ReadOffset16();
            int glyphArray = reader.ReadOffset16();
            if (Math.Min(firstSection, Math.Min(states, entries)) >= 10 &&
                glyphArray > Math.Max(firstSection, Math.Max(states, entries)) &&
                AatStateTable.Contains(start + glyphArray, 2, end))
            {
                extraCount = 1;
            }
        }

        if (extraCount < 0 || !AatStateTable.Contains(start, (4 + extraCount) * width, end))
        {
            return null;
        }

        Span<long> sections = stackalloc long[3 + extraCount];
        reader.Seek(start + (4 * width), SeekOrigin.Begin);
        for (int i = 3; i < sections.Length; i++)
        {
            sections[i] = start + (extended ? reader.ReadOffset32() : reader.ReadOffset16());
        }

        int entrySize = format switch
        {
            1 or 5 => 8,
            2 when extended => 6,
            _ => 4
        };

        AatStateTable? machine = AatStateTable.Load(reader, start, end, extended, glyphCount, entrySize, sections);
        if (machine is null)
        {
            return null;
        }

        switch (format)
        {
            case 0:
                return new RearrangementSubTable(coverage, featureFlags, machine);
            case 1:
                if (extended)
                {
                    long lookupStart = sections[3];
                    AatLookupTable?[] lookups = LoadContextualLookups(reader, lookupStart, AatStateTable.GetSectionEnd(lookupStart, end, sections), glyphCount, machine.Entries);
                    return new ExtendedContextualSubTable(coverage, featureFlags, machine, lookups);
                }

                ushort[] substitutions = ReadWords(reader, sections[3], end, sections);
                return new ClassicContextualSubTable(coverage, featureFlags, machine, substitutions, (int)(sections[3] - start) / 2);
            case 2:
                // The low 30 bits of each word give the component bias. The
                // high bits specify when to store the ligature and stop.
                long actionStart = sections[3];
                reader.Seek(actionStart, SeekOrigin.Begin);
                uint[] actions = reader.ReadUInt32Array((int)((AatStateTable.GetSectionEnd(actionStart, end, sections) - actionStart) / 4));
                ushort[] components = ReadWords(reader, sections[4], end, sections);
                ushort[] ligatures = ReadWords(reader, sections[5], end, sections);
                int actionBase = extended ? 0 : (int)(actionStart - start);
                int componentBase = extended ? 0 : (int)(sections[4] - start) / 2;
                int glyphBase = extended ? 0 : (int)(sections[5] - start);
                return new LigatureSubTable(coverage, featureFlags, machine, extended, actions, components, ligatures, actionBase, componentBase, glyphBase);
            default:
                return LoadInsertion(reader, start, end, sections, machine, extraCount != 0, coverage, featureFlags);
        }
    }

    /// <summary>
    /// Tests the feature mask and horizontal or vertical coverage of this subtable.
    /// AllDirections takes precedence over the vertical-only bit.
    /// </summary>
    /// <param name="flags">The enabled feature flags for the chain.</param>
    /// <param name="vertical">Whether the run uses vertical features.</param>
    /// <returns><see langword="true"/> if the subtable applies to the run.</returns>
    public bool AppliesTo(uint flags, bool vertical)
        => (flags & this.featureFlags) != 0 && ((this.coverage & CoverageFlags.AllDirections) != 0 || ((this.coverage & CoverageFlags.Vertical) != 0) == vertical);

    /// <summary>
    /// Determines whether this subtable traverses the logical input in reverse.
    /// Backwards reverses either logical or visual order as selected by coverage.
    /// </summary>
    /// <param name="rightToLeft">Whether the run uses right-to-left direction.</param>
    /// <returns><see langword="true"/> if the subtable processes the run in reverse order.</returns>
    public bool Reverses(bool rightToLeft)
        => ((this.coverage & CoverageFlags.Backwards) != 0) ^ ((this.coverage & CoverageFlags.LogicalOrder) == 0 && rightToLeft);

    /// <summary>
    /// Applies the subtable and updates the end of the run.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="traversal">The read order and bounds of the run. Insertions can extend the run.</param>
    /// <param name="maxCount">The maximum number of glyphs that the buffer can hold during shaping.</param>
    public abstract void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount);

    /// <summary>
    /// Reads a word array ending at the next section of the subtable.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="offset">The offset of the first word in the array.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="sections">The offsets of the state table sections.</param>
    /// <returns>The words before the next section.</returns>
    private static ushort[] ReadWords(BigEndianBinaryReader reader, long offset, long end, ReadOnlySpan<long> sections)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        return reader.ReadUInt16Array((int)((AatStateTable.GetSectionEnd(offset, end, sections) - offset) / 2));
    }

    /// <summary>
    /// Counts the work to move an entry between the live buffer and the deletion list.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <param name="replacement">The replacement glyph ID.</param>
    /// <param name="deleted">Whether the index identifies a deletion event.</param>
    /// <returns>The number of entries in the affected suffix, or zero if its layout stays the same.</returns>
    private static int ReplacementWork(ShapingBuffer buffer, int index, ushort replacement, bool deleted)
        => deleted == (replacement == AatStateTable.DeletedGlyph)
            ? 0
            : buffer.Count + buffer.DeletedGlyphs.Count - index;

    /// <summary>
    /// Applies one replacement within the state table's work budget.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <param name="replacement">The replacement glyph ID.</param>
    /// <param name="deleted">Whether the index identifies a deletion event.</param>
    /// <param name="deletionsBefore">The number of deletion events before the index.</param>
    private static void ReplaceGlyph(ShapingBuffer buffer, int index, ushort replacement, bool deleted, int deletionsBefore)
    {
        int work = ReplacementWork(buffer, index, replacement, deleted);
        if (buffer.CountOperations(work))
        {
            ApplyReplacement(buffer, index, replacement, deleted, deletionsBefore);
        }
    }

    /// <summary>
    /// Applies a replacement after the caller has reserved its work.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The state table index.</param>
    /// <param name="replacement">The replacement glyph ID.</param>
    /// <param name="deleted">Whether the index identifies a deletion event.</param>
    /// <param name="deletionsBefore">The number of deletion events before the index.</param>
    private static void ApplyReplacement(ShapingBuffer buffer, int index, ushort replacement, bool deleted, int deletionsBefore)
    {
        int live = index - deletionsBefore;
        if (deleted)
        {
            if (replacement != AatStateTable.DeletedGlyph)
            {
                // A later state table can replace a class 2 entry with a live glyph.
                // Keep the event in the list until the new slot has copied its data.
                List<(int Index, int Owner, GlyphShapingData Glyph)> events = buffer.DeletedGlyphs;
                ref GlyphShapingData glyph = ref CollectionsMarshal.AsSpan(events)[deletionsBefore].Glyph;
                glyph.GlyphId = replacement;
                if (live == buffer.Count)
                {
                    buffer.AddGlyph(replacement, glyph.CodePoint, glyph.Direction, glyph.TextRunIndex, glyph.CodePointIndex, glyph.StringIndex, glyph.GraphemeIndex);
                    buffer[live] = glyph;
                }
                else
                {
                    bool atSpanEnd = live == buffer.SpanEnd;
                    buffer.Insert(live, glyph);
                    if (atSpanEnd)
                    {
                        // The following glyph belongs to a different span.
                        buffer[live] = glyph;
                    }

                    buffer.SetGlyphId(live, replacement);
                }

                events.RemoveAt(deletionsBefore);
            }
        }
        else if (replacement == AatStateTable.DeletedGlyph)
        {
            buffer.Replace(live, [], default);

            // The buffer records the live index. The state table already has
            // the stream index and the position of the event in that stream.
            Span<(int Index, int Owner, GlyphShapingData Glyph)> deletions = CollectionsMarshal.AsSpan(buffer.DeletedGlyphs);
            (int Index, int Owner, GlyphShapingData Glyph) removed = deletions[^1];
            removed.Index = index;
            removed.Owner = live;
            deletions[deletionsBefore..^1].CopyTo(deletions[(deletionsBefore + 1)..]);
            deletions[deletionsBefore] = removed;
        }
        else
        {
            buffer.ReplaceInPlace(live, replacement, default);
        }
    }

    /// <summary>
    /// Base class for subtables that read transitions from a state table.
    /// </summary>
    private abstract class StateSubTable : MorphSubTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        protected StateSubTable(byte coverage, uint featureFlags, AatStateTable machine)
            : base(coverage, featureFlags)
            => this.Machine = machine;

        /// <summary>
        /// Gets the state table for this subtable.
        /// </summary>
        protected AatStateTable Machine { get; }
    }

    /// <summary>
    /// Applies a direct map from glyph IDs to replacement glyph IDs.
    /// </summary>
    private sealed class NoncontextualSubTable : MorphSubTable
    {
        private readonly AatLookupTable lookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoncontextualSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="lookup">The lookup from input glyph IDs to replacement glyph IDs.</param>
        public NoncontextualSubTable(byte coverage, uint featureFlags, AatLookupTable lookup)
            : base(coverage, featureFlags)
            => this.lookup = lookup;

        /// <inheritdoc />
        public override void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount)
        {
            for (; !traversal.AtEnd && buffer.RemainingOperations > 0; traversal.MoveNext())
            {
                int i = traversal.Index;
                ushort glyphId = AatStateTable.GlyphIdAt(buffer, i, out bool deleted, out int deletionsBefore);

                // A format 10 lookup can store values wider than glyph IDs.
                // The deletion index stays valid until the replacement starts.
                if (this.lookup.TryGetValue(glyphId, out uint replacement) && replacement <= ushort.MaxValue)
                {
                    ReplaceGlyph(buffer, i, (ushort)replacement, deleted, deletionsBefore);
                }

                // Charge the visit after the replacement has reserved its work.
                buffer.CountOperations(1);
            }
        }
    }
}

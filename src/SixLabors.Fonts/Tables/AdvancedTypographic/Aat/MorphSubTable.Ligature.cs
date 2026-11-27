// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class MorphSubTable
{
    /// <summary>
    /// Forms ligatures from a stack of component positions.
    /// </summary>
    private sealed class LigatureSubTable : StateSubTable
    {
        private readonly bool extended;
        private readonly uint[] ligatureActions;
        private readonly ushort[] ligatureComponents;
        private readonly ushort[] glyphs;
        private readonly int actionBase;
        private readonly int componentBase;
        private readonly int glyphBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="LigatureSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="featureFlags">The feature flags that enable the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="extended">Whether the table uses the extended format.</param>
        /// <param name="actions">The ligature action words.</param>
        /// <param name="components">The component contributions to the ligature index.</param>
        /// <param name="glyphs">The ligature glyph IDs.</param>
        /// <param name="actionBase">The offset of the classic action array from the state table header.</param>
        /// <param name="componentBase">The classic component array base in 16-bit words.</param>
        /// <param name="glyphBase">The offset of the classic ligature glyph array from the state table header.</param>
        public LigatureSubTable(byte coverage, uint featureFlags, AatStateTable machine, bool extended, uint[] actions, ushort[] components, ushort[] glyphs, int actionBase, int componentBase, int glyphBase)
            : base(coverage, featureFlags, machine)
        {
            this.extended = extended;
            this.ligatureActions = actions;
            this.ligatureComponents = components;
            this.glyphs = glyphs;
            this.actionBase = actionBase;
            this.componentBase = componentBase;
            this.glyphBase = glyphBase;
        }

        /// <inheritdoc />
        public override void Apply(ShapingBuffer buffer, ref AatStateTable.Traversal traversal, int maxCount)
        {
            Span<int> components = buffer.GetContextMatchPositions()[..AdvancedTypographicUtils.MaxContextLength];
            int depth = 0;
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

                LigatureFlags flags = (LigatureFlags)entry.Flags;
                if (!atEnd && (flags & LigatureFlags.SetComponent) != 0 &&
                    (depth == 0 || components[(depth - 1) % components.Length] != cursor))
                {
                    // A transition that does not advance can revisit the same
                    // component. Its record belongs on the stack only once.
                    components[depth++ % components.Length] = cursor;
                }

                // An extended entry supplies an action index. A classic entry
                // supplies a byte offset in the low 14 bits. Zero means no action.
                // Subtract the array's offset and divide by four bytes per action.
                int action = this.extended
                    ? ((flags & LigatureFlags.PerformAction) != 0 ? entry.First : -1)
                    : ((entry.Flags & (ushort)LigatureFlags.ClassicActionOffsetMask) != 0 ? ((entry.Flags & (ushort)LigatureFlags.ClassicActionOffsetMask) - this.actionBase) / 4 : -1);

                if (!atEnd && action >= 0 && depth > 0)
                {
                    this.Ligate(buffer, in traversal, components, ref depth, action);
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
        /// Evaluates a ligature action list against the component stack.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="traversal">The read order of the run.</param>
        /// <param name="components">The ring of retained component positions in read order.</param>
        /// <param name="depth">The number of component pushes. Receives the depth after the action.</param>
        /// <param name="action">The index of the first ligature action.</param>
        private void Ligate(ShapingBuffer buffer, in AatStateTable.Traversal traversal, ReadOnlySpan<int> components, ref int depth, int action)
        {
            int cursor = depth;
            int oldest = Math.Max(0, depth - components.Length);
            int ligature = 0;
            while (cursor > oldest && action < this.ligatureActions.Length && buffer.CountOperations(1))
            {
                int position = components[--cursor % components.Length];
                int index = traversal.GetIndex(position);
                uint instruction = this.ligatureActions[action++];

                // The left shift discards Last and Store and moves the delta's sign
                // bit to bit 31. The signed right shift restores the signed value.
                int delta = ((int)instruction << 2) >> 2;
                ushort glyphId = AatStateTable.GlyphIdAt(buffer, index, out bool deleted, out int deletionsBefore);
                int component = glyphId + delta - this.componentBase;
                if ((uint)component >= (uint)this.ligatureComponents.Length)
                {
                    break;
                }

                ligature += this.ligatureComponents[component];
                if (((LigatureActionFlags)instruction & (LigatureActionFlags.Last | LigatureActionFlags.Store)) != 0)
                {
                    // Extended actions sum glyph indices. Classic actions sum byte
                    // offsets from the state header and need to be rebased.
                    int glyphIndex = this.extended ? ligature : (ligature - this.glyphBase) / 2;
                    if ((uint)glyphIndex >= (uint)this.glyphs.Length)
                    {
                        break;
                    }

                    ushort replacement = this.glyphs[glyphIndex];
                    long work = ReplacementWork(buffer, index, replacement, deleted);
                    for (int i = depth - 1; i > cursor && work <= buffer.RemainingOperations; i--)
                    {
                        int componentIndex = traversal.GetIndex(components[i % components.Length]);
                        bool componentDeleted = AatStateTable.TryFindDeletionIndex(buffer, componentIndex, out _);
                        work += ReplacementWork(buffer, componentIndex, AatStateTable.DeletedGlyph, componentDeleted);
                    }

                    if (!buffer.CountOperations(work))
                    {
                        return;
                    }

                    // The budget already covers the replacement. The cluster
                    // merge changes input indices, so finish the replacement
                    // even if the merge uses the remaining budget.
                    int end = components[(depth - 1) % components.Length] + 1;
                    int combinedCount = traversal.CombineInputStarts(buffer, position, end);
                    buffer.CountOperations(combinedCount * 2L);

                    ApplyReplacement(buffer, index, replacement, deleted, deletionsBefore);
                    while (depth - 1 > cursor)
                    {
                        // Other machines still see deleted components as class 2.
                        // Each edit can change the next component's live index.
                        int componentIndex = traversal.GetIndex(components[--depth % components.Length]);
                        bool componentDeleted = AatStateTable.TryFindDeletionIndex(buffer, componentIndex, out int before);
                        ApplyReplacement(buffer, componentIndex, AatStateTable.DeletedGlyph, componentDeleted, before);
                    }
                }

                if (((LigatureActionFlags)instruction & LigatureActionFlags.Last) != 0)
                {
                    return;
                }
            }

            depth = 0;
        }
    }
}

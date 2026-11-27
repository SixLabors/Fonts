// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// The Apple extended kerning table (kerx) adjusts glyph advances,
/// cross-stream positions, and attachments.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html"/>
/// </summary>
internal sealed class KerxTable : Table
{
    private readonly KerxSubTable[] subtables;
    private readonly KerxSubTable[] pairSubtables;
    private readonly AatAnchorTable? anchors;

    /// <summary>
    /// Initializes a new instance of the <see cref="KerxTable"/> class.
    /// </summary>
    /// <param name="subtables">The positioning subtables in application order.</param>
    /// <param name="anchors">The anchor table for subtables that use anchor indices.</param>
    private KerxTable(KerxSubTable[] subtables, AatAnchorTable? anchors)
    {
        this.subtables = subtables;
        this.anchors = anchors;

        // When GPOS sets attachment links, only in-stream pair kerning can
        // supplement it. Stateful and cross-stream subtables own positioning.
        this.pairSubtables = Array.FindAll(subtables, static subtable => subtable is KerxSubTable.PairSubTable && !subtable.IsCrossStream && !subtable.IsBackwards);
        this.PairKerningFallback = this.pairSubtables.Length > 0 ? this.ApplyPairKerning : null;
    }

    /// <summary>
    /// Gets the number of positioning subtables in this table.
    /// </summary>
    public int Count => this.subtables.Length;

    /// <summary>
    /// Gets the in-stream pair kerning fallback, or <see langword="null"/> if there are no eligible subtables.
    /// </summary>
    public GPosTable.KerningFallback? PairKerningFallback { get; }

    /// <summary>
    /// Loads the extended kerning table.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The extended kerning table, or <see langword="null"/> if no usable subtables exist.</returns>
    public static KerxTable? Load(FontReader fontReader, int glyphCount)
    {
        if (!fontReader.TryGetBoundedReaderAtTablePosition("kerx", out BigEndianBinaryReader? reader))
        {
            return null;
        }

        using (reader)
        {
            uint tableLength = (uint)reader.BaseStream.Length;

            // +------------+---------------+---------------------------------+
            // | Type       | Name          | Description                     |
            // +------------+---------------+---------------------------------+
            // | uint16     | version       | Major version, 2 through 4.     |
            // | uint16     | unused        | Reserved.                       |
            // | uint32     | subtableCount | Number of subtables.            |
            // | Subtable[] | subtables     | Subtables in application order. |
            // +------------+---------------+---------------------------------+
            if (tableLength < 8 || reader.ReadUInt16() is < 2 or > 4)
            {
                return null;
            }

            _ = reader.ReadUInt16();
            uint count = reader.ReadUInt32();

            // The library caps the number of subtables to limit work per run.
            // The table must also have room for all 12-byte subtable headers.
            if (count > 1024 || !AatStateTable.Contains(8, (long)count * 12, tableLength))
            {
                return null;
            }

            List<KerxSubTable> subtables = new((int)count);
            bool usesAnchors = false;
            long offset = 8;
            for (int i = 0; i < count; i++)
            {
                if (!AatStateTable.Contains(offset, 12, tableLength))
                {
                    return null;
                }

                // +--------+------------+-------------------------------------------------+
                // | Type   | Name       | Description                                     |
                // +--------+------------+-------------------------------------------------+
                // | uint32 | length     | Subtable byte length, including this header.    |
                // | uint32 | coverage   | Format in the low byte, flags in the high byte. |
                // | uint32 | tupleCount | Number of values in each variation tuple.       |
                // +--------+------------+-------------------------------------------------+
                reader.Seek(offset, SeekOrigin.Begin);
                uint length = reader.ReadUInt32();
                uint coverage = reader.ReadUInt32();
                uint tupleCount = reader.ReadUInt32();
                if (length < 12 || !AatStateTable.Contains(offset, length, tableLength))
                {
                    return null;
                }

                // The byte cast keeps the format. Shifting by 24 moves the flags
                // from the high byte to the low byte before the second cast.
                KerxSubTable? subtable = KerxSubTable.Load(reader, offset, offset + length, glyphCount, (byte)coverage, (byte)(coverage >> 24), tupleCount);
                if (subtable is not null)
                {
                    subtables.Add(subtable);
                    usesAnchors |= subtable is KerxSubTable.AttachmentSubTable { UsesAnchorTable: true };
                }

                offset += length;
            }

            return subtables.Count == 0
                ? null
                : new([.. subtables], usesAnchors ? AatAnchorTable.Load(fontReader, glyphCount) : null);
        }
    }

    /// <summary>
    /// Applies extended kerning and resolves its attachments in the active span.
    /// </summary>
    /// <param name="fontMetrics">The font that owns the glyphs.</param>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="vertical">Whether the span uses vertical shaping.</param>
    /// <param name="kerned">Receives whether the table applied kerning in this span.</param>
    /// <returns>Whether the table changed positioning.</returns>
    public bool TryUpdatePositions(FontMetrics fontMetrics, ShapingBuffer buffer, bool vertical, out bool kerned)
    {
        kerned = false;
        int start = buffer.SpanStart;
        int end = buffer.SpanEnd;

        // Substitution can remove glyphs outside the font's state table stream.
        AatStateTable.DiscardUnownedDeletions(buffer);
        return this.ApplyRun(fontMetrics, buffer, start, end, vertical, this.subtables, ref kerned);
    }

    /// <summary>
    /// Gets the accumulated in-stream pair adjustment in font design units.
    /// </summary>
    /// <param name="left">The first glyph ID in the pair.</param>
    /// <param name="right">The second glyph ID in the pair.</param>
    /// <param name="result">The combined horizontal and vertical adjustment in design units.</param>
    /// <returns><see langword="true"/> if a subtable provides a kerning value for the pair.</returns>
    public bool TryGetKerningOffset(ushort left, ushort right, out Vector2 result)
    {
        result = Vector2.Zero;

        // Glyph zero stands for a missing character. It does not identify
        // a specific glyph that the font can kern.
        if (this.Count == 0 || left == 0 || right == 0)
        {
            return false;
        }

        bool found = false;
        foreach (KerxSubTable subtable in this.subtables)
        {
            if (subtable is not KerxSubTable.PairSubTable pairs || pairs.IsCrossStream || pairs.IsBackwards || !pairs.TryGetPairValue(left, right, out int value))
            {
                continue;
            }

            found = true;
            if (subtable.IsVertical)
            {
                // Positive table values increase spacing down the page.
                // The returned offset uses positive Y for up.
                result.Y -= value;
            }
            else
            {
                result.X += value;
            }
        }

        return found;
    }

    /// <summary>
    /// Applies in-stream pair kerning to a run that already has GPOS positioning data.
    /// </summary>
    /// <param name="fontMetrics">The font that owns the glyphs.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="start">The first glyph in the run.</param>
    /// <param name="end">The index after the last glyph in the run.</param>
    /// <param name="vertical">Whether the run uses vertical kerning.</param>
    /// <returns>Whether the table applied kerning.</returns>
    private bool ApplyPairKerning(FontMetrics fontMetrics, ShapingBuffer buffer, int start, int end, bool vertical)
    {
        // Bottom-to-top traversal visits AAT state indices. A deletion made
        // by GSUB has no slot in that stream and must not occupy an index.
        AatStateTable.DiscardUnownedDeletions(buffer);
        bool kerned = false;
        return this.ApplyRun(fontMetrics, buffer, start, end, vertical, this.pairSubtables, ref kerned);
    }

    /// <summary>
    /// Applies the subtables for one run in table order.
    /// </summary>
    /// <param name="fontMetrics">The font that owns the glyphs.</param>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="start">The first live glyph in the run.</param>
    /// <param name="end">The index after the last live glyph in the run.</param>
    /// <param name="vertical">Whether the run uses vertical kerning.</param>
    /// <param name="subtables">The subtables eligible for this positioning pass.</param>
    /// <param name="kerned">Receives whether the run applied kerning.</param>
    /// <returns>Whether the run has an applied positioning adjustment.</returns>
    private bool ApplyRun(
        FontMetrics fontMetrics,
        ShapingBuffer buffer,
        int start,
        int end,
        bool vertical,
        ReadOnlySpan<KerxSubTable> subtables,
        ref bool kerned)
    {
        bool runKerned = false;
        bool attached = false;
        bool backwards = buffer[start].Direction == TextDirection.RightToLeft;
        int stateStart = start + AatStateTable.CountDeletionsBefore(buffer, start, liveBoundary: true);
        int stateEnd = end + AatStateTable.CountDeletionsBefore(buffer, end, liveBoundary: true);
        bool bottomToTop = backwards && buffer.TextOptions.LayoutMode.IsVertical();
        int[]? order = null;
        backwards &= !bottomToTop;
        bool requestedKerning = buffer.TextOptions.KerningMode != KerningMode.None;
        (int Value, bool Reset)[]? crossStreamStorage = null;
        scoped Span<(int Value, bool Reset)> crossStream = [];
        try
        {
            foreach (KerxSubTable subtable in subtables)
            {
                if (buffer.RemainingOperations == 0)
                {
                    break;
                }

                // Use the coverage flag to match the run's orientation.
                // Only state formats 1 and 4 use the backwards flag.
                // Skip a pair subtable with this flag set.
                bool isPair = subtable is KerxSubTable.PairSubTable;
                if (subtable.IsVertical != vertical
                    || (isPair && (!requestedKerning || subtable.IsBackwards)))
                {
                    continue;
                }

                bool isAttachment = subtable is KerxSubTable.AttachmentSubTable;
                if (crossStream.IsEmpty && subtable.IsCrossStream && !isAttachment)
                {
                    // Contextual actions can adjust an earlier glyph. Keep
                    // their values by state index until the run is complete.
                    // Only a matching cross-stream subtable needs this storage.
                    int length = stateEnd - stateStart;
                    crossStreamStorage = ArrayPool<(int Value, bool Reset)>.Shared.Rent(length);
                    crossStream = crossStreamStorage.AsSpan(0, length);
                    crossStream.Clear();
                }

                // A run with no active subtable needs no read order.
                if (bottomToTop && order is null)
                {
                    order = AatStateTable.Traversal.GetBottomToTopIndices(buffer, stateStart, stateEnd);
                }

                // State actions can visit deletions. Cross-stream offsets and the
                // bottom-to-top order also use state indices. Other pairs use live glyph indices.
                // The backwards flag reverses the run's usual read direction.
                bool stateIndices = !isPair || subtable.IsCrossStream || bottomToTop;
                AatStateTable.Traversal traversal = new(
                    stateIndices ? stateStart : start,
                    stateIndices ? stateEnd : end,
                    subtable.IsBackwards != backwards,
                    order);

                bool changed = subtable.Apply(fontMetrics, buffer, traversal, requestedKerning, this.anchors, crossStream);
                attached |= changed && isAttachment;
                runKerned |= changed && !isAttachment;
            }

            if (!crossStream.IsEmpty)
            {
                // A deleted glyph still contributes to the running offset.
                // Apply the offset only to live glyphs and leave their links intact.
                int offset = 0;
                AatStateTable.Traversal traversal = new(stateStart, stateEnd, backwards, order);
                while (!traversal.AtEnd)
                {
                    int index = traversal.Index;
                    (int value, bool reset) = crossStream[index - stateStart];
                    offset = reset ? value : offset + value;
                    int live = AatStateTable.LiveIndex(buffer, index);
                    if (live >= 0)
                    {
                        ref GlyphShapingPosition position = ref buffer.PositionAt(live);

                        // An attached glyph inherits its parent's placement
                        // later. Its own cross-stream value is relative to that parent.
                        int adjustment = position.MarkAttachment == -1 ? offset : value;
                        runKerned |= adjustment != 0;
                        if (vertical)
                        {
                            position.Bounds.X += adjustment;
                        }
                        else
                        {
                            position.Bounds.Y += adjustment;
                        }
                    }

                    traversal.MoveNext();
                }
            }
        }
        finally
        {
            if (crossStreamStorage is not null)
            {
                ArrayPool<(int Value, bool Reset)>.Shared.Return(crossStreamStorage);
            }

            if (order is not null)
            {
                ArrayPool<int>.Shared.Return(order);
            }
        }

        if (attached)
        {
            AdvancedTypographicUtils.ZeroDefaultIgnorableAdvances(buffer, start, end - start);
            AdvancedTypographicUtils.ResolveMarkAttachments(buffer, start, end);
        }

        kerned |= runKerned;
        return attached || runKerned;
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.Kern;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class KerxSubTable
{
    /// <summary>
    /// Flags in the kerx format 6 matrix header.
    /// </summary>
    [Flags]
    private enum MatrixFlags : uint
    {
        /// <summary>
        /// Selects 32-bit matrix indices and values.
        /// </summary>
        LongValues = 1
    }

    /// <summary>
    /// Reads the sorted glyph pairs of a format 0 subtable.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="tupleCount">The number of values per kerning tuple.</param>
    /// <returns>The pair subtable, or <see langword="null"/> if its data is invalid.</returns>
    private static KerningPairsSubTable? LoadPairs(BigEndianBinaryReader reader, long start, long end, byte coverage, uint tupleCount)
    {
        long body = start + 12;

        // +---------------+---------------+-----------------------------------------------+
        // | Type          | Name          | Description                                   |
        // +---------------+---------------+-----------------------------------------------+
        // | uint32        | nPairs        | Number of sorted pair records.                |
        // | uint32        | searchRange   | Binary search accelerator.                    |
        // | uint32        | entrySelector | Binary search accelerator.                    |
        // | uint32        | rangeShift    | Binary search accelerator.                    |
        // | KerningPair[] | pairs         | Same record format as a legacy kern format 0. |
        // +---------------+---------------+-----------------------------------------------+
        if (!AatStateTable.Contains(body, 16, end))
        {
            return null;
        }

        uint count = reader.ReadUInt32();
        long pairsStart = body + 16;
        if (!AatStateTable.Contains(pairsStart, (long)count * 6, end))
        {
            return null;
        }

        reader.Seek(pairsStart, SeekOrigin.Begin);
        KerningPair[] pairs = new KerningPair[count];
        for (int i = 0; i < pairs.Length; i++)
        {
            pairs[i] = KerningPair.Read(reader);
        }

        // With tuples, the pair value is a byte offset from the subtable start.
        if (tupleCount != 0)
        {
            for (int i = 0; i < pairs.Length; i++)
            {
                KerningPair pair = pairs[i];
                pairs[i] = new(pair.Left, pair.Right, (short)ReadTupleValue(reader, start, end, pair.Offset, tupleCount));
            }
        }

        return new KerningPairsSubTable(coverage, pairs);
    }

    /// <summary>
    /// Loads a byte-offset format 2 matrix or an element-index format 6 matrix.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="format">The kerx subtable format.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="tupleCount">The number of values per kerning tuple.</param>
    /// <returns>The matrix subtable, or <see langword="null"/> if its data is invalid.</returns>
    private static KerningMatrixSubTable? LoadMatrix(BigEndianBinaryReader reader, long start, long end, int glyphCount, byte format, byte coverage, uint tupleCount)
    {
        // +--------+-----------+----------------------------------------------------------+
        // | Format | Type      | Description                                              |
        // +--------+-----------+----------------------------------------------------------+
        // | 2      | uint32    | Row width in bytes. Class values include the stride.     |
        // | 6      | uint32    | Flags. Bit 0 selects 32-bit matrix values and indices.   |
        // | 6      | uint16[2] | Row and column counts.                                   |
        // | 2 / 6  | Offset32  | Left or row lookup, relative to the subtable header.     |
        // | 2 / 6  | Offset32  | Right or column lookup, relative to the subtable header. |
        // | 2 / 6  | Offset32  | Kerning matrix, relative to the subtable header.         |
        // | 6      | Offset32  | Tuple vector data, relative to the subtable header.      |
        // +--------+-----------+----------------------------------------------------------+
        long body = start + 12;
        int headerSize = format == 2 ? 16 : 24;
        if (!AatStateTable.Contains(body, headerSize, end))
        {
            return null;
        }

        uint flags = reader.ReadUInt32();
        int width = 2;
        if (format == 6)
        {
            width = ((MatrixFlags)flags & MatrixFlags.LongValues) != 0 ? 4 : 2;
            _ = reader.ReadUInt32();
        }

        long left = start + reader.ReadOffset32();
        long right = start + reader.ReadOffset32();
        long array = start + reader.ReadOffset32();
        long tupleBase = format == 6 ? start + reader.ReadOffset32() : start;
        Span<long> sections = [left, right, array, format == 6 && tupleCount != 0 ? tupleBase : end];
        if (left < body + headerSize || right < body + headerSize || array < body + headerSize || array >= end)
        {
            return null;
        }

        AatLookupTable? l = AatLookupTable.Load(reader, left, AatStateTable.GetSectionEnd(left, end, sections), glyphCount, width);
        AatLookupTable? r = left == right ? l : AatLookupTable.Load(reader, right, AatStateTable.GetSectionEnd(right, end, sections), glyphCount, width);
        if (l is null || r is null)
        {
            return null;
        }

        int count = (int)((AatStateTable.GetSectionEnd(array, end, sections) - array) / width);
        reader.Seek(array, SeekOrigin.Begin);
        int[] values = new int[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = width == 2 ? reader.ReadInt16() : reader.ReadInt32();
        }

        // With tuples, a matrix entry is a byte offset. Format 2 uses the
        // subtable start as its base. Format 6 uses the tuple vector data.
        if (tupleCount != 0)
        {
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadTupleValue(reader, tupleBase, end, values[i], tupleCount);
            }
        }

        return new KerningMatrixSubTable(coverage, l, r, values, format == 2);
    }

    /// <summary>
    /// Reads the default value of a kerning tuple.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The base offset for tuple data.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="offset">The offset of the tuple from the base offset.</param>
    /// <param name="count">The number of values in the tuple.</param>
    /// <returns>The first value in the tuple, or zero if the tuple is invalid.</returns>
    private static int ReadTupleValue(BigEndianBinaryReader reader, long start, long end, int offset, uint count)
    {
        if (offset < 0 || !AatStateTable.Contains(start + offset, 2L * count, end))
        {
            return 0;
        }

        // The first tuple value gives the default kerning adjustment.
        // The other values give deltas for the font's variation coordinates.
        reader.Seek(start + offset, SeekOrigin.Begin);
        return reader.ReadInt16();
    }

    /// <summary>
    /// Applies positioning adjustments between adjacent glyphs.
    /// </summary>
    internal abstract class PairSubTable : KerxSubTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PairSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        protected PairSubTable(byte coverage)
            : base(coverage)
        {
        }

        /// <summary>
        /// Gets the adjustment for a pair in font design units.
        /// </summary>
        /// <param name="first">The first glyph ID.</param>
        /// <param name="second">The second glyph ID.</param>
        /// <param name="value">The kerning value in design units.</param>
        /// <returns><see langword="true"/> if the subtable provides a value for the pair.</returns>
        public abstract bool TryGetPairValue(ushort first, ushort second, out int value);

        /// <inheritdoc />
        public override bool Apply(FontMetrics font, ShapingBuffer buffer, AatStateTable.Traversal traversal, bool requestedKerning, AatAnchorTable? anchors, scoped Span<(int Value, bool Reset)> crossStream)
        {
            bool updated = false;
            bool isCrossStream = this.IsCrossStream;
            bool stateIndices = isCrossStream || traversal.IsBottomToTop;
            while (!traversal.AtEnd && buffer.CountOperations(1))
            {
                int left = stateIndices ? AatStateTable.LiveIndex(buffer, traversal.Index) : traversal.Index;
                traversal.MoveNext();
                if (left < 0)
                {
                    continue;
                }

                int right = -1;
                while (!traversal.AtEnd)
                {
                    right = stateIndices ? AatStateTable.LiveIndex(buffer, traversal.Index) : traversal.Index;
                    if (right < 0)
                    {
                        traversal.MoveNext();
                        continue;
                    }

                    ref GlyphShapingData glyph = ref buffer[right];
                    if (!glyph.IsDefaultIgnorable &&
                        !AdvancedTypographicUtils.IsMarkGlyph(font, glyph.GlyphId, ref glyph))
                    {
                        break;
                    }

                    traversal.MoveNext();
                }

                if (!traversal.AtEnd && this.TryGetPairValue(buffer[left].GlyphId, buffer[right].GlyphId, out int value) && value != 0)
                {
                    if (isCrossStream)
                    {
                        AddCrossStream(ref crossStream[traversal.Index - traversal.StartIndex], value, accumulate: false);
                    }
                    else
                    {
                        int dx = this.IsVertical ? 0 : value;
                        int dy = this.IsVertical ? value : 0;
                        AdvancedTypographicUtils.ApplyPairKerning(font, buffer, left, right, dx, dy, yDown: true);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }

    /// <summary>
    /// Looks up a format 0 adjustment in the sorted glyph pairs.
    /// </summary>
    private sealed class KerningPairsSubTable : PairSubTable
    {
        private readonly KerningPair[] pairs;

        /// <summary>
        /// Initializes a new instance of the <see cref="KerningPairsSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="pairs">The kerning pairs in glyph ID order.</param>
        public KerningPairsSubTable(byte coverage, KerningPair[] pairs)
            : base(coverage)
            => this.pairs = pairs;

        /// <inheritdoc />
        public override bool TryGetPairValue(ushort first, ushort second, out int value)
        {
            int index = this.pairs.AsSpan().BinarySearch(new KerningPair(first, second, 0));
            if (index < 0)
            {
                value = 0;
                return false;
            }

            value = this.pairs[index].Offset;
            return true;
        }
    }

    /// <summary>
    /// Looks up a format 2 or format 6 adjustment by glyph class.
    /// </summary>
    private sealed class KerningMatrixSubTable : PairSubTable
    {
        private readonly AatLookupTable leftClasses;
        private readonly AatLookupTable rightClasses;
        private readonly int[] values;
        private readonly bool usesByteOffsets;

        /// <summary>
        /// Initializes a new instance of the <see cref="KerningMatrixSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="leftClasses">The lookup for the first glyph of a pair.</param>
        /// <param name="rightClasses">The lookup for the second glyph of a pair.</param>
        /// <param name="values">The kerning values in the matrix.</param>
        /// <param name="usesByteOffsets">Whether the lookup values are byte offsets.</param>
        public KerningMatrixSubTable(byte coverage, AatLookupTable leftClasses, AatLookupTable rightClasses, int[] values, bool usesByteOffsets)
            : base(coverage)
        {
            this.leftClasses = leftClasses;
            this.rightClasses = rightClasses;
            this.values = values;
            this.usesByteOffsets = usesByteOffsets;
        }

        /// <inheritdoc />
        public override bool TryGetPairValue(ushort first, ushort second, out int value)
        {
            _ = this.leftClasses.TryGetValue(first, out uint left);
            _ = this.rightClasses.TryGetValue(second, out uint right);

            // Format 2 class values are byte offsets. Format 6 class
            // values are element indices, even when the elements are 32-bit.
            value = 0;
            ulong indexSum = (ulong)left + right;
            if (this.usesByteOffsets)
            {
                indexSum /= 2;
            }

            if (indexSum >= (uint)this.values.Length)
            {
                return false;
            }

            value = this.values[(int)indexSum];
            return true;
        }
    }
}

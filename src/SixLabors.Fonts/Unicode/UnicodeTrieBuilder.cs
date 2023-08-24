// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Builder class to manipulate and generate a trie.
    /// This is useful for ICU data in primitive types.
    /// Provides a compact way to store information that is indexed by Unicode
    /// values, such as character properties, types, keyboard values, etc.
    /// This is very useful when you have a block of Unicode data that contains significant
    /// values while the rest of the Unicode data is unused in the application or
    /// when you have a lot of redundance, such as where all 21,000 Han ideographs
    /// have the same value.  However, lookup is much faster than a hash table.
    /// A trie of any primitive data type serves two purposes:
    /// <ul>
    ///     <li>Fast access of the indexed values.</li>
    ///     <li>Smaller memory footprint.</li>
    /// </ul>
    /// </summary>
    internal class UnicodeTrieBuilder
    {
        // These have been kept in the original format for now to aid porting
        // and testing.
#pragma warning disable SA1310 // Field names should not contain underscore

        // Shift size for getting the index-1 table offset.
        internal const int UTRIE2_SHIFT_1 = 6 + 5;

        // Shift size for getting the index-2 table offset.
        internal const int UTRIE2_SHIFT_2 = 5;

        // Difference between the two shift sizes,
        // for getting an index-1 offset from an index-2 offset. 6=11-5
        private const int UTRIE2_SHIFT_1_2 = UTRIE2_SHIFT_1 - UTRIE2_SHIFT_2;

        // Number of index-1 entries for the BMP. 32=0x20
        // This part of the index-1 table is omitted from the serialized form.
        internal const int UTRIE2_OMITTED_BMP_INDEX_1_LENGTH = 0x10000 >> UTRIE2_SHIFT_1;

        // Number of code points per index-1 table entry. 2048=0x800
        private const int UTRIE2_CP_PER_INDEX_1_ENTRY = 1 << UTRIE2_SHIFT_1;

        // Start with allocation of 16k data entries.
        private const int INITIAL_DATA_LENGTH = 1 << 14;

        // Grow about 8x each time.
        private const int UNEWTRIE2_MEDIUM_DATA_LENGTH = 1 << 17;

        private const int INDEX_1_LENGTH = 0x110000 >> UTRIE2_SHIFT_1;

        // Number of entries in a data block. 32=0x20
        private const int UTRIE2_DATA_BLOCK_LENGTH = 1 << UTRIE2_SHIFT_2;

        // Mask for getting the lower bits for the in-data-block offset.
        internal const int UTRIE2_DATA_MASK = UTRIE2_DATA_BLOCK_LENGTH - 1;

        // Shift size for shifting left the index array values.
        // Increases possible data size with 16-bit index values at the cost
        // of compactability.
        // This requires data blocks to be aligned by UTRIE2_DATA_GRANULARITY.
        internal const int UTRIE2_INDEX_SHIFT = 2;

        // The alignment size of a data block. Also the granularity for compaction.
        internal const int UTRIE2_DATA_GRANULARITY = 1 << UTRIE2_INDEX_SHIFT;

        // The BMP part of the index-2 table is fixed and linear and starts at offset 0.
        // Length=2048=0x800=0x10000>>UTRIE2_SHIFT_2.
        private const int UTRIE2_INDEX_2_OFFSET = 0;

        // The part of the index-2 table for U+D800..U+DBFF stores values for
        // lead surrogate code _units_ not code _points_.
        // Values for lead surrogate code _points_ are indexed with this portion of the table.
        // Length=32=0x20=0x400>>UTRIE2_SHIFT_2. (There are 1024=0x400 lead surrogates.)
        internal const int UTRIE2_LSCP_INDEX_2_OFFSET = 0x10000 >> UTRIE2_SHIFT_2;
        private const int UTRIE2_LSCP_INDEX_2_LENGTH = 0x400 >> UTRIE2_SHIFT_2;

        // Count the lengths of both BMP pieces. 2080=0x820
        private const int UTRIE2_INDEX_2_BMP_LENGTH = UTRIE2_LSCP_INDEX_2_OFFSET + UTRIE2_LSCP_INDEX_2_LENGTH;

        // The 2-byte UTF-8 version of the index-2 table follows at offset 2080=0x820.
        // Length 32=0x20 for lead bytes C0..DF, regardless of UTRIE2_SHIFT_2.
        private const int UTRIE2_UTF8_2B_INDEX_2_OFFSET = UTRIE2_INDEX_2_BMP_LENGTH;
        private const int UTRIE2_UTF8_2B_INDEX_2_LENGTH = 0x800 >> 6;  // U+0800 is the first code point after 2-byte UTF-8

        // The index-1 table, only used for supplementary code points, at offset 2112=0x840.
        // Variable length, for code points up to highStart, where the last single-value range starts.
        // Maximum length 512=0x200=0x100000>>UTRIE2_SHIFT_1.
        // (For 0x100000 supplementary code points U+10000..U+10ffff.)
        //
        // The part of the index-2 table for supplementary code points starts
        // after this index-1 table.
        //
        // Both the index-1 table and the following part of the index-2 table
        // are omitted completely if there is only BMP data.
        internal const int UTRIE2_INDEX_1_OFFSET = UTRIE2_UTF8_2B_INDEX_2_OFFSET + UTRIE2_UTF8_2B_INDEX_2_LENGTH;
        private const int UTRIE2_MAX_INDEX_1_LENGTH = 0x100000 >> UTRIE2_SHIFT_1;

        // Maximum length of the build-time index-2 array.
        // Maximum number of Unicode code points (0x110000) shifted right by UTRIE2_SHIFT_2,
        // plus the part of the index-2 table for lead surrogate code points,
        // plus the build-time index gap,
        // plus the null index-2 block.
        private const int UNEWTRIE2_MAX_INDEX_2_LENGTH = (0x110000 >> UTRIE2_SHIFT_2)
            + UTRIE2_LSCP_INDEX_2_LENGTH
            + UNEWTRIE2_INDEX_GAP_LENGTH
            + UTRIE2_INDEX_2_BLOCK_LENGTH;

        private const int UNEWTRIE2_INDEX_1_LENGTH = 0x110000 >> UTRIE2_SHIFT_1;

        // Number of entries in an index-2 block. 64=0x40
        private const int UTRIE2_INDEX_2_BLOCK_LENGTH = 1 << UTRIE2_SHIFT_1_2;

        // Mask for getting the lower bits for the in-index-2-block offset.
        internal const int UTRIE2_INDEX_2_MASK = UTRIE2_INDEX_2_BLOCK_LENGTH - 1;

        // At build time, leave a gap in the index-2 table,
        // at least as long as the maximum lengths of the 2-byte UTF-8 index-2 table
        // and the supplementary index-1 table.
        // Round up to UTRIE2_INDEX_2_BLOCK_LENGTH for proper compacting.
        private const int UNEWTRIE2_INDEX_GAP_OFFSET = UTRIE2_INDEX_2_BMP_LENGTH;
        private const int UNEWTRIE2_INDEX_GAP_LENGTH =
           (UTRIE2_UTF8_2B_INDEX_2_LENGTH + UTRIE2_MAX_INDEX_1_LENGTH + UTRIE2_INDEX_2_MASK) & ~UTRIE2_INDEX_2_MASK;

        // Maximum length of the build-time data array.
        // One entry per 0x110000 code points, plus the illegal-UTF-8 block and the null block,
        // plus values for the 0x400 surrogate code units.
        private const int UNEWTRIE2_MAX_DATA_LENGTH = 0x110000 + 0x40 + 0x40 + 0x400;

        // The illegal-UTF-8 data block follows the ASCII block, at offset 128=0x80.
        // Used with linear access for single bytes 0..0xbf for simple error handling.
        // Length 64=0x40, not UTRIE2_DATA_BLOCK_LENGTH.
        private const int UTRIE2_BAD_UTF8_DATA_OFFSET = 0x80;

        // The start of non-linear-ASCII data blocks, at offset 192=0xc0.
        private const int UTRIE2_DATA_START_OFFSET = 0xc0;

        // The null data block.
        // Length 64=0x40 even if UTRIE2_DATA_BLOCK_LENGTH is smaller,
        // to work with 6-bit trail bytes from 2-byte UTF-8.
        private const int UNEWTRIE2_DATA_NULL_OFFSET = UTRIE2_DATA_START_OFFSET;

        // The null index-2 block, following the gap in the index-2 table.
        private const int UNEWTRIE2_INDEX_2_NULL_OFFSET = UNEWTRIE2_INDEX_GAP_OFFSET + UNEWTRIE2_INDEX_GAP_LENGTH;

        // The start of allocated index-2 blocks.
        private const int UNEWTRIE2_INDEX_2_START_OFFSET = UNEWTRIE2_INDEX_2_NULL_OFFSET + UTRIE2_INDEX_2_BLOCK_LENGTH;

        // The start of allocated data blocks.
        private const int UNEWTRIE2_DATA_START_OFFSET = UNEWTRIE2_DATA_NULL_OFFSET + 0x40;

        // The start of data blocks for U+0800 and above.
        // Below, compaction uses a block length of 64 for 2-byte UTF-8.
        // From here on, compaction uses UTRIE2_DATA_BLOCK_LENGTH.
        // Data values for 0x780 code points beyond ASCII.
        private const int UNEWTRIE2_DATA_0800_OFFSET = UNEWTRIE2_DATA_START_OFFSET + 0x780;

        // Maximum length of the runtime index array.
        // Limited by its own 16-bit index values, and by uint16_t UTrie2Header.indexLength.
        // (The actual maximum length is lower,
        // (0x110000>>UTRIE2_SHIFT_2)+UTRIE2_UTF8_2B_INDEX_2_LENGTH+UTRIE2_MAX_INDEX_1_LENGTH.)
        private const int UTRIE2_MAX_INDEX_LENGTH = 0xffff;

        // Maximum length of the runtime data array.
        // Limited by 16-bit index values that are left-shifted by UTRIE2_INDEX_SHIFT,
        // and by uint16_t UTrie2Header.shiftedDataLength.
        private const int UTRIE2_MAX_DATA_LENGTH = 0xffff << UTRIE2_INDEX_SHIFT;

#pragma warning restore SA1310 // Field names should not contain underscore

        private readonly uint initialValue;
        private readonly uint errorValue;
        private int highStart;
        private uint[] data;
        private int dataCapacity;
        private readonly int[] index1;
        private readonly int[] index2;
        private int firstFreeBlock;
        private bool isCompacted;
        private readonly int[] map;
        private int dataNullOffset;
        private int dataLength;
        private int index2NullOffset;
        private int index2Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeTrieBuilder"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value that is set for all code points.</param>
        /// <param name="errorValue">The value for out-of-range code points and illegal UTF-8.</param>
        public UnicodeTrieBuilder(uint initialValue = 0, uint errorValue = 0)
        {
            this.initialValue = initialValue;
            this.errorValue = errorValue;
            this.highStart = 0x110000;

            this.index1 = new int[INDEX_1_LENGTH];
            this.index2 = new int[UNEWTRIE2_MAX_INDEX_2_LENGTH];
            this.data = new uint[INITIAL_DATA_LENGTH];
            this.dataCapacity = INITIAL_DATA_LENGTH;

            this.firstFreeBlock = 0;
            this.isCompacted = false;

            // Multi-purpose per-data-block table.
            //
            // Before compacting:
            //
            // Per-data-block reference counters/free-block list.
            //  0: unused
            // >0: reference counter (number of index-2 entries pointing here)
            // <0: next free data block in free-block list
            //
            // While compacting:
            //
            // Map of adjusted indexes, used in compactData() and compactIndex2().
            // Maps from original indexes to new ones.
            this.map = new int[UNEWTRIE2_MAX_DATA_LENGTH >> UTRIE2_SHIFT_2];

            // preallocate and reset
            // - ASCII
            // - the bad-UTF-8-data block
            // - the null data block
            int i;
            for (i = 0; i < 0x80; ++i)
            {
                this.data[i] = initialValue;
            }

            for (; i < 0xc0; ++i)
            {
                this.data[i] = errorValue;
            }

            for (i = UNEWTRIE2_DATA_NULL_OFFSET; i < UNEWTRIE2_DATA_START_OFFSET; ++i)
            {
                this.data[i] = initialValue;
            }

            this.dataNullOffset = UNEWTRIE2_DATA_NULL_OFFSET;
            this.dataLength = UNEWTRIE2_DATA_START_OFFSET;

            // set the index-2 indexes for the 2=0x80>>UTRIE2_SHIFT_2 ASCII data blocks
            int j;
            for (i = 0, j = 0; j < 0x80; ++i, j += UTRIE2_DATA_BLOCK_LENGTH)
            {
                this.index2[i] = j;
                this.map[i] = 1;
            }

            // reference counts for the bad-UTF-8-data block */
            for (; j < 0xc0; ++i, j += UTRIE2_DATA_BLOCK_LENGTH)
            {
                this.map[i] = 0;
            }

            // Reference counts for the null data block: all blocks except for the ASCII blocks.
            // Plus 1 so that we don't drop this block during compaction.
            // Plus as many as needed for lead surrogate code points.
            // i==newdataNullOffset
            this.map[i++] = (0x110000 >> UTRIE2_SHIFT_2)
                - (0x80 >> UTRIE2_SHIFT_2)
                + 1
                + UTRIE2_LSCP_INDEX_2_LENGTH;

            j += UTRIE2_DATA_BLOCK_LENGTH;
            for (; j < UNEWTRIE2_DATA_START_OFFSET; ++i, j += UTRIE2_DATA_BLOCK_LENGTH)
            {
                this.map[i] = 0;
            }

            // set the remaining indexes in the BMP index-2 block
            // to the null data block
            for (i = 0x80 >> UTRIE2_SHIFT_2; i < UTRIE2_INDEX_2_BMP_LENGTH; ++i)
            {
                this.index2[i] = UNEWTRIE2_DATA_NULL_OFFSET;
            }

            // Fill the index gap with impossible values so that compaction
            // does not overlap other index-2 blocks with the gap.
            for (i = 0; i < UNEWTRIE2_INDEX_GAP_LENGTH; ++i)
            {
                this.index2[UNEWTRIE2_INDEX_GAP_OFFSET + i] = -1;
            }

            // set the indexes in the null index-2 block
            for (i = 0; i < UTRIE2_INDEX_2_BLOCK_LENGTH; ++i)
            {
                this.index2[UNEWTRIE2_INDEX_2_NULL_OFFSET + i] = UNEWTRIE2_DATA_NULL_OFFSET;
            }

            this.index2NullOffset = UNEWTRIE2_INDEX_2_NULL_OFFSET;
            this.index2Length = UNEWTRIE2_INDEX_2_START_OFFSET;

            // set the index-1 indexes for the linear index-2 block
            for (i = 0, j = 0; i < UTRIE2_OMITTED_BMP_INDEX_1_LENGTH; ++i, j += UTRIE2_INDEX_2_BLOCK_LENGTH)
            {
                this.index1[i] = j;
            }

            // set the remaining index-1 indexes to the null index-2 block
            for (; i < UNEWTRIE2_INDEX_1_LENGTH; ++i)
            {
                this.index1[i] = UNEWTRIE2_INDEX_2_NULL_OFFSET;
            }

            // Preallocate and reset data for U+0080..U+07ff,
            // for 2-byte UTF-8 which will be compacted in 64-blocks
            // even if UTRIE2_DATA_BLOCK_LENGTH is smaller.
            for (i = 0x80; i < 0x800; i += UTRIE2_DATA_BLOCK_LENGTH)
            {
                this.Set(i, initialValue);
            }
        }

        /// <summary>
        /// Gets the value for a code point as stored in the trie.
        /// </summary>
        /// <param name="c">The code point.</param>
        /// <returns>The value.</returns>
        public uint Get(int c) => this.Get(c, true);

        /// <summary>
        /// Sets a value for a given code point.
        /// </summary>
        /// <param name="codePoint">The code point.</param>
        /// <param name="value">The value.</param>
        public void Set(int codePoint, uint value)
        {
            if (codePoint is < 0 or > 0x10ffff)
            {
                throw new ArgumentOutOfRangeException("Invalid code point");
            }

            if (this.isCompacted)
            {
                throw new InvalidOperationException("Already compacted");
            }

            int block = this.GetDataBlock(codePoint, true);
            this.data[block + (codePoint & UTRIE2_DATA_MASK)] = value;
        }

        /// <summary>
        /// Set a value in a range of code points [start..end].
        /// All code points c with start &lt;= c &lt;= end will get the value if
        /// <paramref name="overwrite"/> is <see langword="true"/> or if the old value is the
        /// initial value.
        /// </summary>
        /// <param name="start">The first code point to get the value.</param>
        /// <param name="end">The last code point to get the value (inclusive).</param>
        /// <param name="value">The value.</param>
        /// <param name="overwrite">Whether old non-initial values are to be overwritten.</param>
        public void SetRange(int start, int end, uint value, bool overwrite)
        {
            if ((start > 0x10ffff) || (end > 0x10ffff) || start > end)
            {
                throw new ArgumentOutOfRangeException("Invalid code point");
            }

            if (this.isCompacted)
            {
                throw new InvalidOperationException("Already compacted");
            }

            if (!overwrite && value == this.initialValue)
            {
                return; // Nothing to do.
            }

            int block;
            int rest;
            int repeatBlock;
            int limit = end + 1;
            if ((start & UTRIE2_DATA_MASK) != 0)
            {
                int nextStart;

                // set partial block at [start..following block boundary[
                block = this.GetDataBlock(start, true);

                if (block < 0)
                {
                    throw new IndexOutOfRangeException(nameof(block));
                }

                nextStart = (start + UTRIE2_DATA_MASK) & ~UTRIE2_DATA_MASK;
                if (nextStart <= limit)
                {
                    this.FillBlock(block, start & UTRIE2_DATA_MASK, UTRIE2_DATA_BLOCK_LENGTH, value, this.initialValue, overwrite);
                    start = nextStart;
                }
                else
                {
                    this.FillBlock(block, start & UTRIE2_DATA_MASK, limit & UTRIE2_DATA_MASK, value, this.initialValue, overwrite);
                    return;
                }
            }

            // number of positions in the last, partial block
            rest = limit & UTRIE2_DATA_MASK;

            // round down limit to a block boundary
            limit &= ~UTRIE2_DATA_MASK;

            // iterate over all-value blocks
            if (value == this.initialValue)
            {
                repeatBlock = this.dataNullOffset;
            }
            else
            {
                repeatBlock = -1;
            }

            while (start < limit)
            {
                int i2;
                bool setRepeatBlock = false;

                if (value == this.initialValue && this.IsInNullBlock(start, true))
                {
                    start += UTRIE2_DATA_BLOCK_LENGTH; // nothing to do
                    continue;
                }

                // get index value
                i2 = this.GetIndex2Block(start, true);
                if (i2 < 0)
                {
                    throw new IndexOutOfRangeException(nameof(i2));
                }

                i2 += (start >> UTRIE2_SHIFT_2) & UTRIE2_INDEX_2_MASK;
                block = this.index2[i2];
                if (this.IsWritableBlock(block))
                {
                    // already allocated
                    if (overwrite && block >= UNEWTRIE2_DATA_0800_OFFSET)
                    {
                        // We overwrite all values, and it's not a
                        // protected (ASCII-linear or 2-byte UTF-8) block:
                        // replace with the repeatBlock.
                        setRepeatBlock = true;
                    }
                    else
                    {
                        // !overwrite, or protected block: just write the values into this block
                        this.FillBlock(block, 0, UTRIE2_DATA_BLOCK_LENGTH, value, this.initialValue, overwrite);
                    }
                }
                else if (this.data[block] != value && (overwrite || block == this.dataNullOffset))
                {
                    // Set the repeatBlock instead of the null block or previous repeat block:
                    //
                    // If !isWritableBlock() then all entries in the block have the same value
                    // because it's the null block or a range block (the repeatBlock from a previous
                    // call to utrie2_setRange32()).
                    // No other blocks are used multiple times before compacting.
                    //
                    // The null block is the only non-writable block with the initialValue because
                    // of the repeatBlock initialization above. (If value==initialValue, then
                    // the repeatBlock will be the null data block.)
                    //
                    // We set our repeatBlock if the desired value differs from the block's value,
                    // and if we overwrite any data or if the data is all initial values
                    // (which is the same as the block being the null block, see above).
                    setRepeatBlock = true;
                }

                if (setRepeatBlock)
                {
                    if (repeatBlock >= 0)
                    {
                        this.SetIndex2Entry(i2, repeatBlock);
                    }
                    else
                    {
                        // create and set and fill the repeatBlock
                        repeatBlock = this.GetDataBlock(start, true);
                        if (repeatBlock < 0)
                        {
                            throw new IndexOutOfRangeException(nameof(repeatBlock));
                        }

                        this.WriteBlock(repeatBlock, value);
                    }
                }

                start += UTRIE2_DATA_BLOCK_LENGTH;
            }

            if (rest > 0)
            {
                // set partial block at [last block boundary..limit[
                block = this.GetDataBlock(start, true);
                if (block < 0)
                {
                    throw new IndexOutOfRangeException(nameof(block));
                }

                this.FillBlock(block, 0, rest, value, this.initialValue, overwrite);
            }
        }

        /// <summary>
        /// Compacts the data and populates an optimized readonly Trie.
        /// </summary>
        /// <returns>The <see cref="UnicodeTrie"/>.</returns>
        public UnicodeTrie Freeze()
        {
            int allIndexesLength, i;
            if (!this.isCompacted)
            {
                this.CompactTrie();
            }

            if (this.highStart <= 0x10000)
            {
                allIndexesLength = UTRIE2_INDEX_1_OFFSET;
            }
            else
            {
                allIndexesLength = this.index2Length;
            }

            int dataMove = allIndexesLength;

            // are indexLength and dataLength within limits?
            if ((allIndexesLength > UTRIE2_MAX_INDEX_LENGTH) // for unshifted indexLength
              || ((dataMove + this.dataNullOffset) > 0xffff) // for unshifted dataNullOffset
              || ((dataMove + UNEWTRIE2_DATA_0800_OFFSET) > 0xffff) // for unshifted 2-byte UTF-8 index-2 values
              || ((dataMove + this.dataLength) > UTRIE2_MAX_DATA_LENGTH))
            {
                // for shiftedDataLength
                throw new InvalidOperationException("Trie data is too large.");
            }

            // calculate the sizes of, and allocate, the index and data arrays
            int indexLength = allIndexesLength + this.dataLength;
            uint[] data32 = new uint[indexLength];

            // write the index-2 array values shifted right by UTRIE2_INDEX_SHIFT, after adding dataMove
            int destIdx = 0;
            for (i = 0; i < UTRIE2_INDEX_2_BMP_LENGTH; i++)
            {
                data32[destIdx++] = (uint)((this.index2[i] + dataMove) >> UTRIE2_INDEX_SHIFT);
            }

            // write UTF-8 2-byte index-2 values, not right-shifted
            for (i = 0; i < 0xc2 - 0xc0; i++)
            {
                // C0..C1
                data32[destIdx++] = (uint)(dataMove + UTRIE2_BAD_UTF8_DATA_OFFSET);
            }

            for (; i < 0xe0 - 0xc0; i++)
            {
                // C2..DF
                data32[destIdx++] = (uint)(dataMove + this.index2[i << (6 - UTRIE2_SHIFT_2)]);
            }

            if (this.highStart > 0x10000)
            {
                int index1Length = (this.highStart - 0x10000) >> UTRIE2_SHIFT_1;
                int index2Offset = UTRIE2_INDEX_2_BMP_LENGTH + UTRIE2_UTF8_2B_INDEX_2_LENGTH + index1Length;

                // write 16-bit index-1 values for supplementary code points
                for (i = 0; i < index1Length; i++)
                {
                    data32[destIdx++] = (uint)(UTRIE2_INDEX_2_OFFSET + this.index1[i + UTRIE2_OMITTED_BMP_INDEX_1_LENGTH]);
                }

                // write the index-2 array values for supplementary code points,
                // shifted right by INDEX_SHIFT, after adding dataMove
                for (i = 0; i < this.index2Length - index2Offset; i++)
                {
                    data32[destIdx++] = (uint)((dataMove + this.index2[index2Offset + i]) >> UTRIE2_INDEX_SHIFT);
                }
            }

            // write 16-bit data values
            for (i = 0; i < this.dataLength; i++)
            {
                data32[destIdx++] = this.data[i];
            }

            return new UnicodeTrie(data32, this.highStart, this.errorValue);
        }

        private uint Get(int c, bool fromLSCP)
        {
            if (c is < 0 or > 0x10ffff)
            {
                return this.errorValue;
            }

            int i2;
            int block;

            if (c >= this.highStart && (!U_IS_LEAD(c) || fromLSCP))
            {
                return this.data[this.dataLength - UTRIE2_DATA_GRANULARITY];
            }

            if (U_IS_LEAD(c) && fromLSCP)
            {
                i2 = UTRIE2_LSCP_INDEX_2_OFFSET - (0xd800 >> UTRIE2_SHIFT_2) + (c >> UTRIE2_SHIFT_2);
            }
            else
            {
                i2 = this.index1[c >> UTRIE2_SHIFT_1] + ((c >> UTRIE2_SHIFT_2) & UTRIE2_INDEX_2_MASK);
            }

            block = this.index2[i2];
            return this.data[block + (c & UTRIE2_DATA_MASK)];
        }

        private int GetDataBlock(int c, bool forLSCP)
        {
            int i2 = this.GetIndex2Block(c, forLSCP);

            if (i2 < 0)
            {
                throw new IndexOutOfRangeException(nameof(i2));
            }

            i2 += (c >> UTRIE2_SHIFT_2) & UTRIE2_INDEX_2_MASK;

            int oldBlock = this.index2[i2];
            if (this.IsWritableBlock(oldBlock))
            {
                return oldBlock;
            }

            // allocate a new data block
            int newBlock = this.AllocDataBlock(oldBlock);

            if (newBlock < 0)
            {
                throw new IndexOutOfRangeException(nameof(newBlock));
            }

            this.SetIndex2Entry(i2, newBlock);
            return newBlock;
        }

        private int GetIndex2Block(int c, bool forLSCP)
        {
            if (U_IS_LEAD(c) && forLSCP)
            {
                return UTRIE2_LSCP_INDEX_2_OFFSET;
            }

            int i1 = c >> UTRIE2_SHIFT_1;
            int i2 = this.index1[i1];
            if (i2 == this.index2NullOffset)
            {
                i2 = this.AllocIndex2Block();
                if (i2 < 0)
                {
                    throw new IndexOutOfRangeException(nameof(i2));
                }

                this.index1[i1] = i2;
            }

            return i2;
        }

        /// <summary>
        /// Is this code point a lead surrogate (U+d800..U+dbff)?
        /// </summary>
        /// <param name="c">The code point.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool U_IS_LEAD(int c) => (c & 0xfffffc00) == 0xd800;

        private bool IsWritableBlock(int block)
              => block != this.dataNullOffset && this.map[block >> UTRIE2_SHIFT_2] == 1;

        private bool IsInNullBlock(int c, bool forLSCP)
        {
            int i2, block;

            if (U_IS_LEAD(c) && forLSCP)
            {
                i2 = UTRIE2_LSCP_INDEX_2_OFFSET - (0xd800 >> UTRIE2_SHIFT_2) + (c >> UTRIE2_SHIFT_2);
            }
            else
            {
                i2 = this.index1[c >> UTRIE2_SHIFT_1] + ((c >> UTRIE2_SHIFT_2) & UTRIE2_INDEX_2_MASK);
            }

            block = this.index2[i2];
            return block == this.dataNullOffset;
        }

        private void SetIndex2Entry(int i2, int block)
        {
            int oldBlock;

            // increment first, in case block==oldBlock!
            ++this.map[block >> UTRIE2_SHIFT_2];

            oldBlock = this.index2[i2];

            if (--this.map[oldBlock >> UTRIE2_SHIFT_2] == 0)
            {
                this.ReleaseDataBlock(oldBlock);
            }

            this.index2[i2] = block;
        }

        // call when the block's reference counter reaches 0
        private void ReleaseDataBlock(int block)
        {
            // put this block at the front of the free-block chain
            this.map[block >> UTRIE2_SHIFT_2] = -this.firstFreeBlock;
            this.firstFreeBlock = block;
        }

        private int AllocDataBlock(int copyBlock)
        {
            int newBlock, newTop;

            if (this.firstFreeBlock != 0)
            {
                // get the first free block
                newBlock = this.firstFreeBlock;
                this.firstFreeBlock = -this.map[newBlock >> UTRIE2_SHIFT_2];
            }
            else
            {
                // get a new block from the high end
                newBlock = this.dataLength;
                newTop = newBlock + UTRIE2_DATA_BLOCK_LENGTH;
                if (newTop > this.dataCapacity)
                {
                    // out of memory in the data array.
                    int capacity;
                    uint[] newData;

                    if (this.dataCapacity < UNEWTRIE2_MEDIUM_DATA_LENGTH)
                    {
                        capacity = UNEWTRIE2_MEDIUM_DATA_LENGTH;
                    }
                    else if (this.dataCapacity < UNEWTRIE2_MAX_DATA_LENGTH)
                    {
                        capacity = UNEWTRIE2_MAX_DATA_LENGTH;
                    }
                    else
                    {
                        // Should never occur.
                        // Either UNEWTRIE2_MAX_DATA_LENGTH is incorrect,
                        // or the code writes more values than should be possible.
                        throw new InvalidOperationException(nameof(capacity));
                    }

                    newData = new uint[capacity];

                    Array.Copy(this.data, newData, this.dataLength);
                    this.data = newData;
                    this.dataCapacity = capacity;
                }

                this.dataLength = newTop;
            }

            Array.Copy(this.data, copyBlock, this.data, newBlock, UTRIE2_DATA_BLOCK_LENGTH);
            this.map[newBlock >> UTRIE2_SHIFT_2] = 0;
            return newBlock;
        }

        private int AllocIndex2Block()
        {
            int newBlock, newTop;

            newBlock = this.index2Length;
            newTop = newBlock + UTRIE2_INDEX_2_BLOCK_LENGTH;
            if (newTop > this.index2.Length)
            {
                // Should never occur.
                // Either UTRIE2_MAX_BUILD_TIME_INDEX_LENGTH is incorrect,
                // or the code writes more values than should be possible.
                throw new InvalidOperationException(nameof(newTop));
            }

            this.index2Length = newTop;
            Array.Copy(this.index2, this.index2NullOffset, this.index2, newBlock, UTRIE2_INDEX_2_BLOCK_LENGTH);

            return newBlock;
        }

        private int FindSameIndex2Block(int index2Length, int otherBlock)
        {
            // ensure that we do not even partially get past index2Length
            index2Length -= UTRIE2_INDEX_2_BLOCK_LENGTH;

            for (int block = 0; block <= index2Length; ++block)
            {
                if (Equal(this.index2, block, otherBlock, UTRIE2_INDEX_2_BLOCK_LENGTH))
                {
                    return block;
                }
            }

            return -1;
        }

        private int FindSameDataBlock(int dataLength, int otherBlock, int blockLength)
        {
            // ensure that we do not even partially get past dataLength
            dataLength -= blockLength;

            for (int block = 0; block <= dataLength; block += UTRIE2_DATA_GRANULARITY)
            {
                if (Equal(this.data, block, otherBlock, blockLength))
                {
                    return block;
                }
            }

            return -1;
        }

        // Find the start of the last range in the trie by enumerating backward.
        // Indexes for supplementary code points higher than this will be omitted.
        private int FindHighStart(uint highValue)
        {
            uint[] data32;

            uint value, initialValue;
            int c, prev;
            int i1, i2, j, i2Block, prevI2Block, index2NullOffset, block, prevBlock, nullBlock;

            data32 = this.data;
            initialValue = this.initialValue;

            index2NullOffset = this.index2NullOffset;
            nullBlock = this.dataNullOffset;

            /* set variables for previous range */
            if (highValue == initialValue)
            {
                prevI2Block = index2NullOffset;
                prevBlock = nullBlock;
            }
            else
            {
                prevI2Block = -1;
                prevBlock = -1;
            }

            prev = 0x110000;

            // enumerate index-2 blocks
            i1 = UNEWTRIE2_INDEX_1_LENGTH;
            c = prev;
            while (c > 0)
            {
                i2Block = this.index1[--i1];
                if (i2Block == prevI2Block)
                {
                    // the index-2 block is the same as
                    // the previous one, and filled with highValue
                    c -= UTRIE2_CP_PER_INDEX_1_ENTRY;
                    continue;
                }

                prevI2Block = i2Block;
                if (i2Block == index2NullOffset)
                {
                    // this is the null index-2 block
                    if (highValue != initialValue)
                    {
                        return c;
                    }

                    c -= UTRIE2_CP_PER_INDEX_1_ENTRY;
                }
                else
                {
                    // enumerate data blocks for one index-2 block
                    for (i2 = UTRIE2_INDEX_2_BLOCK_LENGTH; i2 > 0;)
                    {
                        block = this.index2[i2Block + --i2];
                        if (block == prevBlock)
                        {
                            // the block is the same as the previous one, and filled with highValue
                            c -= UTRIE2_DATA_BLOCK_LENGTH;
                            continue;
                        }

                        prevBlock = block;
                        if (block == nullBlock)
                        {
                            // this is the null data block
                            if (highValue != initialValue)
                            {
                                return c;
                            }

                            c -= UTRIE2_DATA_BLOCK_LENGTH;
                        }
                        else
                        {
                            for (j = UTRIE2_DATA_BLOCK_LENGTH; j > 0;)
                            {
                                value = data32[block + --j];
                                if (value != highValue)
                                {
                                    return c;
                                }

                                --c;
                            }
                        }
                    }
                }
            }

            // deliver last range
            return 0;
        }

        // initialValue is ignored if overwrite=TRUE
        private void FillBlock(int block, int start, int limit, uint value, uint initialValue, bool overwrite)
        {
            int pLimit = block + limit;
            block += start;
            if (overwrite)
            {
                while (block < pLimit)
                {
                    this.data[block++] = value;
                }
            }
            else
            {
                while (block < pLimit)
                {
                    if (this.data[block] == initialValue)
                    {
                        this.data[block] = value;
                    }

                    ++block;
                }
            }
        }

        private void WriteBlock(int block, uint value)
        {
            int limit = block + UTRIE2_DATA_BLOCK_LENGTH;
            while (block < limit)
            {
                this.data[block++] = value;
            }
        }

        private void CompactTrie()
        {
            // find highStart and round it up
            uint highValue = this.Get(0x10ffff);
            int localHighStart = this.FindHighStart(highValue);
            localHighStart = (localHighStart + (UTRIE2_CP_PER_INDEX_1_ENTRY - 1)) & ~(UTRIE2_CP_PER_INDEX_1_ENTRY - 1);
            if (localHighStart == 0x110000)
            {
                highValue = this.errorValue;
            }

            // Set highStart only after Get(trie, highStart).
            // Otherwise Get(highStart) would try to read the highValue.
            this.highStart = localHighStart;

            if (localHighStart < 0x110000)
            {
                // Blank out [highStart..10ffff] to release associated data blocks.
                int suppHighStart = this.highStart <= 0x10000 ? 0x10000 : this.highStart;
                this.SetRange(suppHighStart, 0x10ffff, this.initialValue, true);
            }

            this.CompactData();
            if (this.highStart > 0x10000)
            {
                this.CompactIndex2();
            }

            // Store the highValue in the data array and round up the dataLength.
            // Must be done after compactData() because that assumes that dataLength
            // is a multiple of UTRIE2_DATA_BLOCK_LENGTH.
            this.data[this.dataLength++] = highValue;
            while ((this.dataLength & (UTRIE2_DATA_GRANULARITY - 1)) != 0)
            {
                this.data[this.dataLength++] = this.initialValue;
            }

            this.isCompacted = true;
        }

        // Compact a build-time trie.
        //
        // The compaction
        // - removes blocks that are identical with earlier ones
        // - overlaps adjacent blocks as much as possible (if overlap==TRUE)
        // - moves blocks in steps of the data granularity
        // - moves and overlaps blocks that overlap with multiple values in the overlap region
        //
        // It does not
        // - try to move and overlap blocks that are not already adjacent
        private void CompactData()
        {
            int start, newStart, movedStart;
            int blockLength, overlap;
            int i, mapIndex, blockCount;

            // do not compact linear-ASCII data
            newStart = UTRIE2_DATA_START_OFFSET;
            for (start = 0, i = 0; start < newStart; start += UTRIE2_DATA_BLOCK_LENGTH, ++i)
            {
                this.map[i] = start;
            }

            // Start with a block length of 64 for 2-byte UTF-8,
            // then switch to UTRIE2_DATA_BLOCK_LENGTH.
            blockLength = 64;
            blockCount = blockLength >> UTRIE2_SHIFT_2;
            for (start = newStart; start < this.dataLength;)
            {
                // start: index of first entry of current block
                // newStart: index where the current block is to be moved
                //           (right after current end of already-compacted data)
                if (start == UNEWTRIE2_DATA_0800_OFFSET)
                {
                    blockLength = UTRIE2_DATA_BLOCK_LENGTH;
                    blockCount = 1;
                }

                // skip blocks that are not used
                if (this.map[start >> UTRIE2_SHIFT_2] <= 0)
                {
                    // advance start to the next block
                    start += blockLength;

                    // leave newStart with the previous block!
                    continue;
                }

                // search for an identical block
                if ((movedStart = this.FindSameDataBlock(newStart, start, blockLength)) >= 0)
                {
                    // found an identical block, set the other block's index value for the current block
                    for (i = blockCount, mapIndex = start >> UTRIE2_SHIFT_2; i > 0; --i)
                    {
                        this.map[mapIndex++] = movedStart;
                        movedStart += UTRIE2_DATA_BLOCK_LENGTH;
                    }

                    // advance start to the next block
                    start += blockLength;

                    // leave newStart with the previous block!
                    continue;
                }

                // see if the beginning of this block can be overlapped with the end of the previous block
                // look for maximum overlap (modulo granularity) with the previous, adjacent block
                overlap = blockLength - UTRIE2_DATA_GRANULARITY;
                while (overlap > 0 && !Equal(this.data, newStart - overlap, start, overlap))
                {
                    overlap -= UTRIE2_DATA_GRANULARITY;
                }

                if (overlap > 0 || newStart < start)
                {
                    // some overlap, or just move the whole block
                    movedStart = newStart - overlap;
                    for (i = blockCount, mapIndex = start >> UTRIE2_SHIFT_2; i > 0; --i)
                    {
                        this.map[mapIndex++] = movedStart;
                        movedStart += UTRIE2_DATA_BLOCK_LENGTH;
                    }

                    // move the non-overlapping indexes to their new positions
                    start += overlap;
                    for (i = blockLength - overlap; i > 0; --i)
                    {
                        this.data[newStart++] = this.data[start++];
                    }
                }
                else
                {
                    // no overlap && newStart==start
                    for (i = blockCount, mapIndex = start >> UTRIE2_SHIFT_2; i > 0; --i)
                    {
                        this.map[mapIndex++] = start;
                        start += UTRIE2_DATA_BLOCK_LENGTH;
                    }

                    newStart = start;
                }
            }

            // now adjust the index-2 table
            for (i = 0; i < this.index2Length; ++i)
            {
                if (i == UNEWTRIE2_INDEX_GAP_OFFSET)
                {
                    // Gap indexes are invalid (-1). Skip over the gap.
                    i += UNEWTRIE2_INDEX_GAP_LENGTH;
                }

                this.index2[i] = this.map[this.index2[i] >> UTRIE2_SHIFT_2];
            }

            this.dataNullOffset = this.map[this.dataNullOffset >> UTRIE2_SHIFT_2];

            // ensure dataLength alignment
            while ((newStart & (UTRIE2_DATA_GRANULARITY - 1)) != 0)
            {
                this.data[newStart++] = this.initialValue;
            }

            this.dataLength = newStart;
        }

        private void CompactIndex2()
        {
            int i, start, newStart, movedStart, overlap;

            // do not compact linear-BMP index-2 blocks
            newStart = UTRIE2_INDEX_2_BMP_LENGTH;
            for (start = 0, i = 0; start < newStart; start += UTRIE2_INDEX_2_BLOCK_LENGTH, ++i)
            {
                this.map[i] = start;
            }

            // Reduce the index table gap to what will be needed at runtime.
            newStart += UTRIE2_UTF8_2B_INDEX_2_LENGTH + ((this.highStart - 0x10000) >> UTRIE2_SHIFT_1);

            for (start = UNEWTRIE2_INDEX_2_NULL_OFFSET; start < this.index2Length;)
            {
                // start: index of first entry of current block
                // newStart: index where the current block is to be moved
                //           (right after current end of already-compacted data)
                //
                // search for an identical block
                if ((movedStart = this.FindSameIndex2Block(newStart, start)) >= 0)
                {
                    // found an identical block, set the other block's index value for the current block
                    this.map[start >> UTRIE2_SHIFT_1_2] = movedStart;

                    // advance start to the next block
                    start += UTRIE2_INDEX_2_BLOCK_LENGTH;

                    // leave newStart with the previous block!
                    continue;
                }

                // see if the beginning of this block can be overlapped with the end of the previous block
                // look for maximum overlap with the previous, adjacent block
                for (overlap = UTRIE2_INDEX_2_BLOCK_LENGTH - 1;
                    overlap > 0 && !Equal(this.index2, newStart - overlap, start, overlap);
                    --overlap)
                {
                }

                if (overlap > 0 || newStart < start)
                {
                    // some overlap, or just move the whole block
                    this.map[start >> UTRIE2_SHIFT_1_2] = newStart - overlap;

                    // move the non-overlapping indexes to their new positions
                    start += overlap;
                    for (i = UTRIE2_INDEX_2_BLOCK_LENGTH - overlap; i > 0; --i)
                    {
                        this.index2[newStart++] = this.index2[start++];
                    }
                }
                else
                {
                    // no overlap && newStart==start
                    this.map[start >> UTRIE2_SHIFT_1_2] = start;
                    start += UTRIE2_INDEX_2_BLOCK_LENGTH;
                    newStart = start;
                }
            }

            // now adjust the index-1 table
            for (i = 0; i < UNEWTRIE2_INDEX_1_LENGTH; ++i)
            {
                this.index1[i] = this.map[this.index1[i] >> UTRIE2_SHIFT_1_2];
            }

            this.index2NullOffset = this.map[this.index2NullOffset >> UTRIE2_SHIFT_1_2];

            // Ensure data table alignment:
            // Needs to be granularity-aligned for 16-bit trie
            // (so that dataMove will be down-shiftable),
            // and 2-aligned for uint32_t data.
            while ((newStart & ((UTRIE2_DATA_GRANULARITY - 1) | 1)) != 0)
            {
                // Arbitrary value: 0x3fffc not possible for real data.
                this.index2[newStart++] = 0xffff << UTRIE2_INDEX_SHIFT;
            }

            this.index2Length = newStart;
        }

        private static bool Equal(uint[] a, int s, int t, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a[s + i] != a[t + i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Equal(int[] a, int s, int t, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a[s + i] != a[t + i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

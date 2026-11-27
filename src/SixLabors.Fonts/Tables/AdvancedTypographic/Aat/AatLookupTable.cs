// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Maps glyph IDs to values in an Apple Advanced Typography lookup table.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html"/>
/// </summary>
internal sealed class AatLookupTable
{
    private readonly int firstGlyph;
    private readonly uint[] values;
    private readonly LookupRange[] ranges;

    /// <summary>
    /// Initializes a new instance of the <see cref="AatLookupTable"/> class.
    /// </summary>
    /// <param name="firstGlyph">The first glyph ID in the consecutive value array.</param>
    /// <param name="values">The values for consecutive glyph IDs.</param>
    /// <param name="ranges">The ranges in glyph ID order.</param>
    private AatLookupTable(int firstGlyph, uint[] values, LookupRange[] ranges)
    {
        this.firstGlyph = firstGlyph;
        this.values = values;
        this.ranges = ranges;
    }

    /// <summary>
    /// Reads a classic state table's class array. Its values are one byte wide.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="offset">The offset of the first byte in the class array header.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <returns>The class lookup, or <see langword="null"/> if the array is invalid.</returns>
    public static AatLookupTable? LoadClassicClasses(BigEndianBinaryReader reader, long offset, long end)
    {
        // +--------+---------------------+--------------------------------------------+
        // | Type   | Name                | Description                                |
        // +--------+---------------------+--------------------------------------------+
        // | uint16 | firstGlyph          | First glyph covered by the class array.    |
        // | uint16 | glyphCount          | Number of consecutive glyphs in the array. |
        // | uint8  | classes[glyphCount] | Class for each covered glyph.              |
        // +--------+---------------------+--------------------------------------------+
        if (!AatStateTable.Contains(offset, 4, end))
        {
            return null;
        }

        reader.Seek(offset, SeekOrigin.Begin);
        ushort first = reader.ReadUInt16();
        ushort count = reader.ReadUInt16();
        long remaining = end - offset - 4;
        uint[]? values = ReadValues(reader, 1, count, end, ref remaining);
        return values is not null ? new(first, values, []) : null;
    }

    /// <summary>
    /// Loads an extended lookup bounded by the owning subtable. The caller supplies
    /// the value width for formats whose size is determined by the surrounding table.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="offset">The offset of the lookup header.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="valueSize">The number of bytes per lookup value.</param>
    /// <returns>The lookup, or <see langword="null"/> if it cannot be read.</returns>
    public static AatLookupTable? Load(BigEndianBinaryReader reader, long offset, long end, int glyphCount, int valueSize)
    {
        // Lookup entries can refer to the same bytes. A budget of twice the
        // available bytes permits overlap and caps the total bytes read.
        long remaining = 2 * (end - offset);
        return Load(reader, offset, end, end, glyphCount, valueSize, ref remaining);
    }

    /// <summary>
    /// Loads one lookup within a shared decoding budget. The next lookup bounds
    /// the implicit format 0 array, whose header has no count of its own.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="offset">The offset of the lookup header.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="nextLookup">The first byte after the format 0 value array.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="valueSize">The number of bytes per lookup value.</param>
    /// <param name="remaining">The number of bytes that the lookup parser can still read.</param>
    /// <returns>The lookup, or <see langword="null"/> if it cannot be read within the limits.</returns>
    public static AatLookupTable? Load(BigEndianBinaryReader reader, long offset, long end, long nextLookup, int glyphCount, int valueSize, ref long remaining)
    {
        if (!AatStateTable.Contains(offset, 2, end))
        {
            return null;
        }

        reader.Seek(offset, SeekOrigin.Begin);
        ushort format = reader.ReadUInt16();
        switch (format)
        {
            case 0:
                // Format 0 stores one value per glyph immediately after the format word.
                // Some Apple fonts author only the prefix of glyphs a
                // machine can see, even though maxp covers later outputs.
                // The next section bounds the encoded prefix.
                int available = (int)Math.Min(glyphCount, (nextLookup - offset - 2) / valueSize);
                uint[]? full = ReadValues(reader, valueSize, available, nextLookup, ref remaining);
                return full is not null ? new(0, full, []) : null;
            case 2:
            case 4:
            case 6:
                return LoadSorted(reader, offset, end, valueSize, format, ref remaining);
            case 8:
            case 10:
                // +--------+------------+----------------------------------------------+
                // | Type   | Name       | Description                                  |
                // +--------+------------+----------------------------------------------+
                // | uint16 | format     | 8 or 10.                                     |
                // | uint16 | valueSize  | Bytes per value. Present only for format 10. |
                // | uint16 | firstGlyph | First glyph covered.                         |
                // | uint16 | glyphCount | Number of values following the header.       |
                // +--------+------------+----------------------------------------------+
                if (!AatStateTable.Contains(offset, format == 8 ? 6 : 8, end))
                {
                    return null;
                }

                if (format == 10)
                {
                    valueSize = reader.ReadUInt16();
                }

                ushort first = reader.ReadUInt16();
                ushort count = reader.ReadUInt16();
                uint[]? trimmed = ReadValues(reader, valueSize, count, end, ref remaining);
                return trimmed is not null ? new(first, trimmed, []) : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets a signed or unsigned value in its original bit representation.
    /// </summary>
    /// <param name="glyph">The glyph ID.</param>
    /// <param name="value">The value bits for the glyph.</param>
    /// <returns><see langword="true"/> if the lookup contains the glyph.</returns>
    public bool TryGetValue(ushort glyph, out uint value)
    {
        if (this.ranges.Length == 0)
        {
            int index = glyph - this.firstGlyph;
            if ((uint)index < (uint)this.values.Length)
            {
                value = this.values[index];
                return true;
            }
        }
        else
        {
            int lo = 0;
            int hi = this.ranges.Length - 1;
            while (lo <= hi)
            {
                // Halve the distance between the bounds to find the middle range.
                int mid = lo + ((hi - lo) >> 1);
                ref readonly LookupRange range = ref this.ranges[mid];
                if (glyph < range.First)
                {
                    hi = mid - 1;
                }
                else if (glyph > range.Last)
                {
                    lo = mid + 1;
                }
                else
                {
                    // Formats 2 and 6 use one constant value per record.
                    // Format 4 uses the glyph's distance into the segment array.
                    value = range.Values is null ? range.Value : range.Values[glyph - range.First];
                    return true;
                }
            }
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Loads the ordered segments or individual glyph records of formats 2, 4, and 6.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="offset">The offset of the lookup header.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="valueSize">The number of bytes per lookup value.</param>
    /// <param name="format">The format of the sorted lookup.</param>
    /// <param name="remaining">The number of bytes that the lookup parser can still read.</param>
    /// <returns>The lookup, or <see langword="null"/> if it cannot be read within the limits.</returns>
    private static AatLookupTable? LoadSorted(BigEndianBinaryReader reader, long offset, long end, int valueSize, ushort format, ref long remaining)
    {
        // Binary search header following the format word.
        // +--------+---------------+-------------------------------------------------+
        // | Type   | Name          | Description                                     |
        // +--------+---------------+-------------------------------------------------+
        // | uint16 | unitSize      | Size of a lookup record, including any padding. |
        // | uint16 | nUnits        | Number of lookup records.                       |
        // | uint16 | searchRange   | Search accelerator for the original AAT API.    |
        // | uint16 | entrySelector | Search accelerator for the original AAT API.    |
        // | uint16 | rangeShift    | Search accelerator for the original AAT API.    |
        // +--------+---------------+-------------------------------------------------+
        const int recordsOffset = 12;
        if (!AatStateTable.Contains(offset, recordsOffset, end))
        {
            return null;
        }

        ushort unitSize = reader.ReadUInt16();
        ushort count = reader.ReadUInt16();
        int keySize = format == 6 ? 2 : 4;
        long bytes = (long)count * unitSize;
        if (bytes > remaining || unitSize < keySize + (format == 4 ? 2 : valueSize) ||
            !AatStateTable.Contains(offset + recordsOffset, bytes, end))
        {
            return null;
        }

        remaining -= bytes;

        // +--------+------------+---------------------------------------------------------+
        // | Type   | Name       | Description                                             |
        // +--------+------------+---------------------------------------------------------+
        // | uint16 | lastGlyph  | Last glyph in a segment, or the only glyph in format 6. |
        // | uint16 | firstGlyph | First glyph in a segment. Omitted in format 6.          |
        // | value  | value      | Constant value, or an Offset16 in format 4.             |
        // +--------+------------+---------------------------------------------------------+
        LookupRange[] ranges = new LookupRange[count];
        int previous = -1;
        int loaded = 0;
        for (int i = 0; i < count; i++)
        {
            reader.Seek(offset + recordsOffset + ((long)i * unitSize), SeekOrigin.Begin);
            ushort last = reader.ReadUInt16();
            ushort first = format == 6 ? last : reader.ReadUInt16();
            if (first == ushort.MaxValue)
            {
                // A 0xFFFF first glyph marks a dummy record at the end of the search table.
                break;
            }

            if (first <= previous || last < first)
            {
                return null;
            }

            // Explicit ranges can address intermediate AAT glyph IDs outside
            // maxp. Later subtables replace them before metrics are resolved.
            previous = last;
            if (format == 4)
            {
                // This offset is relative to the lookup header, including the format word.
                long valueOffset = offset + reader.ReadOffset16();
                if (!AatStateTable.Contains(valueOffset, ((long)last - first + 1) * valueSize, end))
                {
                    return null;
                }

                reader.Seek(valueOffset, SeekOrigin.Begin);
                uint[]? segment = ReadValues(reader, valueSize, last - first + 1, end, ref remaining);
                if (segment is null)
                {
                    return null;
                }

                ranges[loaded++] = new(first, last, 0, segment);
            }
            else
            {
                uint value = valueSize == 2 ? reader.ReadUInt16() : reader.ReadUInt32();
                ranges[loaded++] = new(first, last, value, null);
            }
        }

        if (loaded != ranges.Length)
        {
            Array.Resize(ref ranges, loaded);
        }

        return new(0, [], ranges);
    }

    /// <summary>
    /// Reads a packed array whose values are one to four bytes wide.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="size">The number of bytes per value.</param>
    /// <param name="count">The number of values to read.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="remaining">The number of bytes that the lookup parser can still read.</param>
    /// <returns>The values, or <see langword="null"/> if the array exceeds the limits or has an invalid value size.</returns>
    private static uint[]? ReadValues(BigEndianBinaryReader reader, int size, int count, long end, ref long remaining)
    {
        long offset = reader.BaseStream.Position - reader.StartOfStream;
        long bytes = (long)size * count;
        if (size is < 1 or > 4 || bytes > remaining || !AatStateTable.Contains(offset, bytes, end))
        {
            return null;
        }

        remaining -= bytes;
        if (size == 4)
        {
            return reader.ReadUInt32Array(count);
        }

        uint[] result = new uint[count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = size switch
            {
                1 => reader.ReadUInt8(),
                2 => reader.ReadUInt16(),
                _ => reader.ReadUInt24()
            };
        }

        return result;
    }

    /// <summary>
    /// One glyph range and its constant or per-glyph values.
    /// </summary>
    private readonly struct LookupRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookupRange"/> struct.
        /// </summary>
        /// <param name="first">The first glyph ID in the range.</param>
        /// <param name="last">The last glyph ID in the range.</param>
        /// <param name="value">The value shared by all glyphs in the range when no array is present.</param>
        /// <param name="values">The values for individual glyphs, or <see langword="null"/> for a shared value.</param>
        public LookupRange(ushort first, ushort last, uint value, uint[]? values)
        {
            this.First = first;
            this.Last = last;
            this.Value = value;
            this.Values = values;
        }

        /// <summary>
        /// Gets the first glyph ID.
        /// </summary>
        public ushort First { get; }

        /// <summary>
        /// Gets the last glyph ID.
        /// </summary>
        public ushort Last { get; }

        /// <summary>
        /// Gets the constant value when the range has no per-glyph array.
        /// </summary>
        public uint Value { get; }

        /// <summary>
        /// Gets the optional array of values indexed from the first glyph.
        /// </summary>
        public uint[]? Values { get; }
    }
}

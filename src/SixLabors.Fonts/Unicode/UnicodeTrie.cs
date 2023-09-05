// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static SixLabors.Fonts.Unicode.UnicodeTrieBuilder;

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// A read-only Trie, holding 32 bit data values.
/// A UnicodeTrie is a highly optimized data structure for mapping from Unicode
/// code points(values ranging from 0 to 0x10ffff) to a 32 bit value.
/// </summary>
internal sealed class UnicodeTrie
{
    private readonly uint[] data;
    private readonly int highStart;
    private readonly uint errorValue;

    public UnicodeTrie(ReadOnlySpan<byte> rawData)
    {
        UnicodeTrieHeader header = MemoryMarshal.Read<UnicodeTrieHeader>(rawData);

        if (!BitConverter.IsLittleEndian)
        {
            header.HighStart = BinaryPrimitives.ReverseEndianness(header.HighStart);
            header.ErrorValue = BinaryPrimitives.ReverseEndianness(header.ErrorValue);
            header.DataLength = BinaryPrimitives.ReverseEndianness(header.DataLength);
        }

        int length = header.DataLength;
        uint[] data = new uint[length / sizeof(uint)];
        rawData.Slice(rawData.Length - length).CopyTo(MemoryMarshal.AsBytes(data.AsSpan()));

        if (!BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = BinaryPrimitives.ReverseEndianness(data[i]);
            }
        }

        this.highStart = header.HighStart;
        this.errorValue = header.ErrorValue;
        this.data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnicodeTrie"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the compressed data.</param>
    public UnicodeTrie(Stream stream)
    {
        // Read the header info
        using (var br = new BinaryReader(stream, Encoding.UTF8, true))
        {
            this.highStart = br.ReadInt32();
            this.errorValue = br.ReadUInt32();
            this.data = new uint[br.ReadInt32() / sizeof(uint)];
        }

        // Read the data in compressed format.
        using (var br = new BinaryReader(stream, Encoding.UTF8, true))
        {
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] = br.ReadUInt32();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnicodeTrie"/> class.
    /// </summary>
    /// <param name="data">The uncompressed trie data.</param>
    /// <param name="highStart">The start of the last range which ends at U+10ffff.</param>
    /// <param name="errorValue">The value for out-of-range code points and illegal UTF-8.</param>
    public UnicodeTrie(uint[] data, int highStart, uint errorValue)
    {
        this.data = data;
        this.highStart = highStart;
        this.errorValue = errorValue;
    }

    /// <summary>
    /// Get the value for a code point as stored in the trie.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The <see cref="uint"/> value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Get(uint codePoint)
    {
        uint index;
        ref uint dataBase = ref MemoryMarshal.GetReference(this.data.AsSpan());

        if (codePoint is < 0x0d800 or (> 0x0dbff and <= 0x0ffff))
        {
            // Ordinary BMP code point, excluding leading surrogates.
            // BMP uses a single level lookup.  BMP index starts at offset 0 in the Trie2 index.
            // 16 bit data is stored in the index array itself.
            index = this.data[codePoint >> UTRIE2_SHIFT_2];
            index = (index << UTRIE2_INDEX_SHIFT) + (codePoint & UTRIE2_DATA_MASK);
            return Unsafe.Add(ref dataBase, (nint)index);
        }

        if (codePoint <= 0xffff)
        {
            // Lead Surrogate Code Point.  A Separate index section is stored for
            // lead surrogate code units and code points.
            //   The main index has the code unit data.
            //   For this function, we need the code point data.
            // Note: this expression could be refactored for slightly improved efficiency, but
            //       surrogate code points will be so rare in practice that it's not worth it.
            index = this.data[UTRIE2_LSCP_INDEX_2_OFFSET + ((codePoint - 0xd800) >> UTRIE2_SHIFT_2)];
            index = (index << UTRIE2_INDEX_SHIFT) + (codePoint & UTRIE2_DATA_MASK);
            return Unsafe.Add(ref dataBase, (nint)index);
        }

        if (codePoint < this.highStart)
        {
            // Supplemental code point, use two-level lookup.
            index = UTRIE2_INDEX_1_OFFSET - UTRIE2_OMITTED_BMP_INDEX_1_LENGTH + (codePoint >> UTRIE2_SHIFT_1);
            index = this.data[index];
            index += (codePoint >> UTRIE2_SHIFT_2) & UTRIE2_INDEX_2_MASK;
            index = this.data[index];
            index = (index << UTRIE2_INDEX_SHIFT) + (codePoint & UTRIE2_DATA_MASK);
            return Unsafe.Add(ref dataBase, (nint)index);
        }

        if (codePoint <= 0x10ffff)
        {
            return Unsafe.Add(ref dataBase, (nint)(this.data.Length - UTRIE2_DATA_GRANULARITY));
        }

        // Fall through.  The code point is outside of the legal range of 0..0x10ffff.
        return this.errorValue;
    }

    /// <summary>
    /// Saves the <see cref="UnicodeTrie"/> to the stream in a compressed format.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    public void Save(Stream stream)
    {
        // Write the header info
        using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            bw.Write(this.highStart);
            bw.Write(this.errorValue);
            bw.Write(this.data.Length * sizeof(uint));
        }

        // Write the data.
        using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            for (int i = 0; i < this.data.Length; i++)
            {
                bw.Write(this.data[i]);
            }
        }
    }

    private struct UnicodeTrieHeader
    {
        public int HighStart;
        public uint ErrorValue;
        public int DataLength;
    }
}

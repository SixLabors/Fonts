// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Stores class-pair adjustments from an Apple 'kern' format 3 subtable.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kern.html"/>
/// </summary>
internal sealed class Format3SubTable : KerningSubTable
{
    private readonly int glyphCount;
    private readonly int leftClassCount;
    private readonly int rightClassCount;
    private readonly short[] values;
    private readonly byte[] classesAndIndices;

    /// <summary>
    /// Initializes a new instance of the <see cref="Format3SubTable"/> class.
    /// </summary>
    /// <param name="glyphCount">The number of glyphs in each class array.</param>
    /// <param name="leftClassCount">The number of rows in the class-pair matrix.</param>
    /// <param name="rightClassCount">The number of columns in the class-pair matrix.</param>
    /// <param name="values">The kerning values selected by the matrix.</param>
    /// <param name="classesAndIndices">The left classes, right classes, and matrix, in file order.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    private Format3SubTable(int glyphCount, int leftClassCount, int rightClassCount, short[] values, byte[] classesAndIndices, in KerningCoverage coverage)
        : base(coverage)
    {
        this.glyphCount = glyphCount;
        this.leftClassCount = leftClassCount;
        this.rightClassCount = rightClassCount;
        this.values = values;
        this.classesAndIndices = classesAndIndices;
    }

    /// <summary>
    /// Loads an Apple 'kern' format 3 subtable.
    /// </summary>
    /// <param name="reader">The reader positioned after the shared subtable header.</param>
    /// <param name="remainingLength">The bytes from the reader position to the end of the subtable.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <returns>The subtable, or <see langword="null"/> if its data is truncated.</returns>
    public static Format3SubTable? Load(BigEndianBinaryReader reader, long remainingLength, in KerningCoverage coverage)
    {
        // Format 3 header:
        // uint16 glyphCount
        // uint8  kernValueCount, leftClassCount, rightClassCount, flags
        const int headerSize = sizeof(ushort) + (4 * sizeof(byte));
        long available = Math.Min(remainingLength, reader.BaseStream.Length - reader.BaseStream.Position);
        if (available < headerSize)
        {
            return null;
        }

        int glyphCount = reader.ReadUInt16();
        int valueCount = reader.ReadByte();
        int leftClassCount = reader.ReadByte();
        int rightClassCount = reader.ReadByte();
        _ = reader.ReadByte(); // Reserved flags.

        // The header is followed by signed values, two byte arrays indexed by
        // glyph ID, and a row-major matrix of byte indices into the values.
        int classAndIndexCount = (2 * glyphCount) + (leftClassCount * rightClassCount);
        if ((valueCount * sizeof(short)) + classAndIndexCount > available - headerSize)
        {
            return null;
        }

        short[] values = reader.ReadInt16Array(valueCount);
        byte[] classesAndIndices = reader.ReadBytes(classAndIndexCount);
        return new Format3SubTable(glyphCount, leftClassCount, rightClassCount, values, classesAndIndices, coverage);
    }

    /// <inheritdoc/>
    protected override bool TryGetOffset(ushort index1, ushort index2, out short offset)
    {
        offset = 0;
        if (index1 >= this.glyphCount || index2 >= this.glyphCount)
        {
            return false;
        }

        int leftClass = this.classesAndIndices[index1];
        int rightClass = this.classesAndIndices[this.glyphCount + index2];
        if (leftClass >= this.leftClassCount || rightClass >= this.rightClassCount)
        {
            return false;
        }

        // The matrix starts after both glyph class arrays. Each row covers
        // every right-hand class, and each cell selects a signed adjustment.
        int valueIndex = this.classesAndIndices[(2 * this.glyphCount) + (leftClass * this.rightClassCount) + rightClass];
        if (valueIndex >= this.values.Length)
        {
            return false;
        }

        offset = this.values[valueIndex];
        return true;
    }
}

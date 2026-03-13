// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Represents a delta-set row in an <see cref="ItemVariationData"/> subtable.
/// Each delta set contains per-region delta adjustment values.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats"/>
/// </summary>
internal class DeltaSet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeltaSet"/> class by reading delta values from the binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the delta set data.</param>
    /// <param name="wordDeltas">The number of deltas encoded using the larger (word) type.</param>
    /// <param name="longWords">Whether word deltas are 32-bit (int32) instead of 16-bit (int16).</param>
    /// <param name="regionIndexCount">The total number of region indices (and thus the total number of deltas).</param>
    public DeltaSet(BigEndianBinaryReader reader, int wordDeltas, bool longWords, ushort regionIndexCount)
    {
        this.ShortDeltas = new int[wordDeltas];
        for (int i = 0; i < wordDeltas; i++)
        {
            this.ShortDeltas[i] = longWords ? reader.ReadInt32() : reader.ReadInt16();
        }

        int remaining = regionIndexCount - wordDeltas;
        this.RegionDeltas = new short[remaining];
        for (int i = 0; i < remaining; i++)
        {
            this.RegionDeltas[i] = longWords ? reader.ReadInt16() : reader.ReadSByte();
        }

        this.Deltas = new int[this.RegionDeltas.Length + this.ShortDeltas.Length];
        int offset = 0;

        for (int i = 0; i < this.ShortDeltas.Length; i++)
        {
            this.Deltas[offset++] = this.ShortDeltas[i];
        }

        for (int i = 0; i < this.RegionDeltas.Length; i++)
        {
            this.Deltas[offset++] = this.RegionDeltas[i];
        }
    }

    /// <summary>
    /// Gets the remaining deltas encoded using the smaller (short) type.
    /// </summary>
    public short[] RegionDeltas { get; }

    /// <summary>
    /// Gets the initial deltas encoded using the larger (word) type.
    /// </summary>
    public int[] ShortDeltas { get; }

    /// <summary>
    /// Gets the combined array of all deltas (word deltas followed by region deltas), one per region.
    /// </summary>
    public int[] Deltas { get; }
}

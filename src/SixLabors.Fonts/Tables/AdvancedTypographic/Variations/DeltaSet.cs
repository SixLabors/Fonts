// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

internal class DeltaSet
{
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

    public short[] RegionDeltas { get; }

    public int[] ShortDeltas { get; }

    public int[] Deltas { get; }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal sealed class VarColorLine
{
    public VarColorLine(Extend extend, VarColorStop[] stops)
    {
        this.Extend = extend;
        this.Stops = stops;
    }

    public Extend Extend { get; }

    public VarColorStop[] Stops { get; }

    public int Count => this.Stops.Length;

    public static VarColorLine Load(BigEndianBinaryReader reader)
    {
        Extend extend = reader.ReadByte<Extend>();
        ushort numStops = reader.ReadUInt16();

        VarColorStop[] stops = new VarColorStop[numStops];
        for (int i = 0; i < numStops; i++)
        {
            stops[i] = VarColorStop.Load(reader);
        }

        return new VarColorLine(extend, stops);
    }
}

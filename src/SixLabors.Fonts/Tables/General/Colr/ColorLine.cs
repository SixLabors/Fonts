// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal sealed class ColorLine
{
    public ColorLine(Extend extend, ColorStop[] stops)
    {
        this.Extend = extend;
        this.Stops = stops;
    }

    public Extend Extend { get; }

    public ColorStop[] Stops { get; }

    public int Count => this.Stops.Length;

    public static ColorLine Load(BigEndianBinaryReader reader)
    {
        Extend extend = reader.ReadByte<Extend>();
        ushort numStops = reader.ReadUInt16();

        ColorStop[] stops = new ColorStop[numStops];
        for (int i = 0; i < numStops; i++)
        {
            stops[i] = ColorStop.Load(reader);
        }

        return new ColorLine(extend, stops);
    }
}

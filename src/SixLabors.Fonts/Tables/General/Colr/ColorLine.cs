// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v1 ColorLine, which defines a sequence of color stops and an extend mode for gradient paints.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal sealed class ColorLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorLine"/> class.
    /// </summary>
    /// <param name="extend">The extend mode that determines how the gradient behaves outside the defined stop range.</param>
    /// <param name="stops">The array of color stops defining the gradient.</param>
    public ColorLine(Extend extend, ColorStop[] stops)
    {
        this.Extend = extend;
        this.Stops = stops;
    }

    /// <summary>
    /// Gets the extend mode that determines how the gradient behaves outside the defined stop range.
    /// </summary>
    public Extend Extend { get; }

    /// <summary>
    /// Gets the array of color stops defining the gradient.
    /// </summary>
    public ColorStop[] Stops { get; }

    /// <summary>
    /// Gets the number of color stops.
    /// </summary>
    public int Count => this.Stops.Length;

    /// <summary>
    /// Loads a <see cref="ColorLine"/> from the given reader at the current position.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The loaded <see cref="ColorLine"/>.</returns>
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

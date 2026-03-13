// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a variation-aware COLR v1 ColorLine, which defines a sequence of variable color stops
/// and an extend mode for gradient paints in variable fonts.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal sealed class VarColorLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VarColorLine"/> class.
    /// </summary>
    /// <param name="extend">The extend mode that determines how the gradient behaves outside the defined stop range.</param>
    /// <param name="stops">The array of variable color stops defining the gradient.</param>
    public VarColorLine(Extend extend, VarColorStop[] stops)
    {
        this.Extend = extend;
        this.Stops = stops;
    }

    /// <summary>
    /// Gets the extend mode that determines how the gradient behaves outside the defined stop range.
    /// </summary>
    public Extend Extend { get; }

    /// <summary>
    /// Gets the array of variable color stops defining the gradient.
    /// </summary>
    public VarColorStop[] Stops { get; }

    /// <summary>
    /// Gets the number of color stops.
    /// </summary>
    public int Count => this.Stops.Length;

    /// <summary>
    /// Loads a <see cref="VarColorLine"/> from the given reader at the current position.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The loaded <see cref="VarColorLine"/>.</returns>
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

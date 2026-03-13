// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v1 color stop within a <see cref="ColorLine"/>, defining a position, palette color, and alpha value.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal readonly struct ColorStop
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorStop"/> struct.
    /// </summary>
    /// <param name="stopOffset">The position of this color stop along the gradient, as an F2DOT14 value.</param>
    /// <param name="paletteIndex">The index into the CPAL palette for this stop's color.</param>
    /// <param name="alpha">The alpha value for this stop, as an F2DOT14 value.</param>
    public ColorStop(float stopOffset, ushort paletteIndex, float alpha)
    {
        this.StopOffset = stopOffset;
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
    }

    /// <summary>
    /// Gets the position of this color stop along the gradient, as an F2DOT14 value.
    /// </summary>
    public float StopOffset { get; }

    /// <summary>
    /// Gets the index into the CPAL palette for this stop's color.
    /// </summary>
    public ushort PaletteIndex { get; }

    /// <summary>
    /// Gets the alpha multiplier for this stop, as an F2DOT14 value.
    /// </summary>
    public float Alpha { get; }

    /// <summary>
    /// Loads a <see cref="ColorStop"/> from the given reader at the current position.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The loaded <see cref="ColorStop"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorStop Load(BigEndianBinaryReader reader)
        => new(reader.ReadF2Dot14(), reader.ReadUInt16(), reader.ReadF2Dot14());
}

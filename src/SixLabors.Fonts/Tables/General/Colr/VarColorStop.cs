// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a variation-aware COLR v1 color stop within a <see cref="VarColorLine"/>,
/// defining a position, palette color, alpha value, and a variation index base for font variations.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal readonly struct VarColorStop
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VarColorStop"/> struct.
    /// </summary>
    /// <param name="stopOffset">The position of this color stop along the gradient, as an F2DOT14 value.</param>
    /// <param name="paletteIndex">The index into the CPAL palette for this stop's color.</param>
    /// <param name="alpha">The alpha value for this stop, as an F2DOT14 value.</param>
    /// <param name="varIndexBase">The base index into the ItemVariationStore delta sets.</param>
    public VarColorStop(float stopOffset, ushort paletteIndex, float alpha, uint varIndexBase)
    {
        this.StopOffset = stopOffset;
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
        this.VarIndexBase = varIndexBase;
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
    /// Gets the base index into the ItemVariationStore delta sets for this stop's variation data.
    /// </summary>
    public uint VarIndexBase { get; }

    /// <summary>
    /// Loads a <see cref="VarColorStop"/> from the given reader at the current position.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The loaded <see cref="VarColorStop"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VarColorStop Load(BigEndianBinaryReader reader)
        => new(reader.ReadF2Dot14(), reader.ReadUInt16(), reader.ReadF2Dot14(), reader.ReadUInt32());
}

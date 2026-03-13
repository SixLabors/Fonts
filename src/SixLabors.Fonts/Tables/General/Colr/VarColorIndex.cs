// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a variation-aware color index in COLR v1, consisting of a palette index,
/// alpha value, and a variation index base for font variations.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal readonly struct VarColorIndex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VarColorIndex"/> struct.
    /// </summary>
    /// <param name="paletteIndex">The index into the CPAL palette.</param>
    /// <param name="alpha">The alpha multiplier as an F2DOT14 value.</param>
    /// <param name="varIndexBase">The base index into the ItemVariationStore delta sets.</param>
    public VarColorIndex(ushort paletteIndex, float alpha, uint varIndexBase)
    {
        this.PaletteIndex = paletteIndex;
        this.Alpha = alpha;
        this.VarIndexBase = varIndexBase;
    }

    /// <summary>
    /// Gets the index into the CPAL palette.
    /// </summary>
    public ushort PaletteIndex { get; }

    /// <summary>
    /// Gets the alpha multiplier as an F2DOT14 value.
    /// </summary>
    public float Alpha { get; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets for this color's variation data.
    /// </summary>
    public uint VarIndexBase { get; }

    /// <summary>
    /// Loads a <see cref="VarColorIndex"/> from the given reader at the current position.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The loaded <see cref="VarColorIndex"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VarColorIndex Load(BigEndianBinaryReader reader)
        => new(reader.ReadUInt16(), reader.ReadF2Dot14(), reader.ReadUInt32());
}

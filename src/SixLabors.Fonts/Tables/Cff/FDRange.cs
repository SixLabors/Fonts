// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents an element in an font dictionary array.
/// </summary>
internal readonly struct FDRange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FDRange"/> struct for FDSelect format 3.
    /// </summary>
    /// <param name="first">The first glyph index in the range.</param>
    /// <param name="fontDictionary">The font dictionary index for glyphs in this range.</param>
    public FDRange(ushort first, byte fontDictionary)
    {
        this.First = first;
        this.FontDictionary = fontDictionary;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FDRange"/> struct for FDSelect format 4.
    /// </summary>
    /// <param name="first">The first glyph index in the range.</param>
    /// <param name="fontDictionary">The font dictionary index for glyphs in this range.</param>
    public FDRange(uint first, ushort fontDictionary)
    {
        this.First = first;
        this.FontDictionary = fontDictionary;
    }

    /// <summary>
    /// Gets the first glyph index in range.
    /// </summary>
    public uint First { get; }

    /// <summary>
    /// Gets the font dictionary index for all glyphs in range.
    /// </summary>
    public ushort FontDictionary { get; }

    /// <inheritdoc/>
    public override string ToString() => $"First {this.First}, Dictionary {this.FontDictionary}.";
}

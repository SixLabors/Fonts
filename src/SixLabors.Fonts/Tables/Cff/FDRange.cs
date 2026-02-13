// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents an element in an font dictionary array.
/// </summary>
internal readonly struct FDRange
{
    public FDRange(ushort first, byte fontDictionary)
    {
        this.First = first;
        this.FontDictionary = fontDictionary;
    }

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

    public override string ToString() => $"First {this.First}, Dictionary {this.FontDictionary}.";
}

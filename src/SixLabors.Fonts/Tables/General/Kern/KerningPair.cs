// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Represents a kerning pair entry in the OpenType 'kern' table, mapping a pair of
/// glyph indices to a kerning offset value.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"/>
/// </summary>
internal readonly struct KerningPair : IComparable<KerningPair>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KerningPair"/> struct.
    /// </summary>
    /// <param name="left">The glyph index for the left-hand glyph in the kerning pair.</param>
    /// <param name="right">The glyph index for the right-hand glyph in the kerning pair.</param>
    /// <param name="offset">The kerning offset value in font design units.</param>
    internal KerningPair(ushort left, ushort right, short offset)
    {
        this.Left = left;
        this.Right = right;
        this.Offset = offset;
        this.Key = CalculateKey(left, right);
    }

    /// <summary>
    /// Gets the composite key derived from the left and right glyph indices, used for fast lookup.
    /// </summary>
    public uint Key { get; }

    /// <summary>
    /// Gets the glyph index for the left-hand glyph in the kerning pair.
    /// </summary>
    public ushort Left { get; }

    /// <summary>
    /// Gets the glyph index for the right-hand glyph in the kerning pair.
    /// </summary>
    public ushort Right { get; }

    /// <summary>
    /// Gets the kerning offset value in font design units.
    /// Positive values move glyphs apart; negative values move them closer together.
    /// </summary>
    public short Offset { get; }

    /// <summary>
    /// Calculates a composite lookup key from a pair of glyph indices.
    /// </summary>
    /// <param name="left">The left glyph index.</param>
    /// <param name="right">The right glyph index.</param>
    /// <returns>A 32-bit key combining both glyph indices.</returns>
    public static uint CalculateKey(ushort left, ushort right)
    {
        uint value = (uint)(left << 16);
        return value + right;
    }

    /// <summary>
    /// Reads a <see cref="KerningPair"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the kerning pair data.</param>
    /// <returns>The parsed <see cref="KerningPair"/>.</returns>
    public static KerningPair Read(BigEndianBinaryReader reader)

         // Type   | Field | Description
         // -------|-------|-------------------------------
         // uint16 | left  | The glyph index for the left-hand glyph in the kerning pair.
         // uint16 | right | The glyph index for the right-hand glyph in the kerning pair.
         // FWORD  | value | The kerning value for the above pair, in FUnits.If this value is greater than zero, the characters will be moved apart.If this value is less than zero, the character will be moved closer together.
         => new KerningPair(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadFWORD());

    /// <summary>
    /// Compares this kerning pair to another based on the composite key.
    /// </summary>
    /// <param name="other">The other kerning pair to compare to.</param>
    /// <returns>A value indicating the relative order of the kerning pairs.</returns>
    public int CompareTo(KerningPair other)
        => this.Key.CompareTo(other.Key);
}

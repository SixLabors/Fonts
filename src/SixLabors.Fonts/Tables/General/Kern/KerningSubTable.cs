// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Represents a kerning subtable in the OpenType 'kern' table.
/// Each subtable contains kerning data in a specific format.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"/>
/// </summary>
internal abstract class KerningSubTable
{
    /// <summary>
    /// The coverage flags describing the properties of this subtable.
    /// </summary>
    private readonly KerningCoverage coverage;

    /// <summary>
    /// Initializes a new instance of the <see cref="KerningSubTable"/> class.
    /// </summary>
    /// <param name="coverage">The coverage flags for this subtable.</param>
    public KerningSubTable(KerningCoverage coverage)
        => this.coverage = coverage;

    /// <summary>
    /// Loads a <see cref="KerningSubTable"/> from the specified binary reader.
    /// Returns <see langword="null"/> if the subtable format is not supported.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the subtable header.</param>
    /// <returns>The loaded <see cref="KerningSubTable"/>, or <see langword="null"/> for unsupported formats.</returns>
    public static KerningSubTable? Load(BigEndianBinaryReader reader)
    {
        // Kerning subtables will share the same header format.
        // This header is used to identify the format of the subtable and the kind of information it contains:
        // +--------+----------+----------------------------------------------------------+
        // | Type   | Field    | Description                                              |
        // +========+==========+==========================================================+
        // | uint16 | version  | Kern subtable version number                             |
        // +--------+----------+----------------------------------------------------------+
        // | uint16 | length   | Length of the subtable, in bytes(including this header). |
        // +--------+----------+----------------------------------------------------------+
        // | uint16 | coverage | What type of information is contained in this table.     |
        // +--------+----------+----------------------------------------------------------+
        ushort subVersion = reader.ReadUInt16();
        ushort length = reader.ReadUInt16();
        KerningCoverage coverage = KerningCoverage.Read(reader);
        if (coverage.Format == 0)
        {
            return Format0SubTable.Load(reader, coverage);
        }
        else
        {
            // we don't support versions other than 'Format 0' same as Windows
            return null;
        }
    }

    /// <summary>
    /// Attempts to get the kerning offset for the specified pair of glyph indices.
    /// </summary>
    /// <param name="index1">The glyph index of the first (left) glyph.</param>
    /// <param name="index2">The glyph index of the second (right) glyph.</param>
    /// <param name="offset">When this method returns, contains the kerning offset if found.</param>
    /// <returns><see langword="true"/> if a kerning value was found; otherwise, <see langword="false"/>.</returns>
    protected abstract bool TryGetOffset(ushort index1, ushort index2, out short offset);

    /// <summary>
    /// Attempts to apply the kerning offset for the specified glyph pair to the result vector.
    /// The offset is applied to the X component for horizontal kerning or the Y component for vertical kerning.
    /// </summary>
    /// <param name="index1">The glyph index of the first (left) glyph.</param>
    /// <param name="index2">The glyph index of the second (right) glyph.</param>
    /// <param name="result">The vector to which the kerning offset is applied.</param>
    /// <returns><see langword="true"/> if a kerning offset was applied; otherwise, <see langword="false"/>.</returns>
    public bool TryApplyOffset(ushort index1, ushort index2, ref Vector2 result)
    {
        if (this.TryGetOffset(index1, index2, out short offset))
        {
            if (this.coverage.Horizontal)
            {
                // apply to X
                if (this.coverage.OverrideAccumulator)
                {
                    result.X = offset;
                }
                else
                {
                    result.X += offset;
                }
            }
            else
            {
                // apply to Y
                if (this.coverage.OverrideAccumulator)
                {
                    result.Y = offset;
                }
                else
                {
                    result.Y += offset;
                }
            }

            return true;
        }

        return false;
    }
}

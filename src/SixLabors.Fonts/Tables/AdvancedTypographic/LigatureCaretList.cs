// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The Ligature Caret List table (LigCaretList) provides caret positioning data for ligature glyphs,
/// enabling text processing clients to correctly position carets within ligatures for selection and cursor movement.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table"/>
/// </summary>
internal sealed class LigatureCaretList
{
    /// <summary>
    /// Gets or sets the array of ligature glyph tables, one per covered glyph, in Coverage Index order.
    /// </summary>
    public LigatureGlyph[]? LigatureGlyphs { get; internal set; }

    /// <summary>
    /// Gets or sets the coverage table that defines which glyphs have ligature caret data.
    /// </summary>
    public CoverageTable? CoverageTable { get; internal set; }

    /// <summary>
    /// Loads the <see cref="LigatureCaretList"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the GDEF table to the LigCaretList table.</param>
    /// <returns>The <see cref="LigatureCaretList"/>.</returns>
    public static LigatureCaretList Load(BigEndianBinaryReader reader, long offset)
    {
        // Ligature Caret list
        // Type      | Name                           | Description
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | coverageOffset                 | Offset to Coverage table - from beginning of LigCaretList table.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | ligGlyphCount                  | Number of ligature glyphs.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | ligGlyphOffsets[ligGlyphCount] | Array of offsets to LigGlyph tables, from beginning of LigCaretList table —in Coverage Index order.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        reader.Seek(offset, SeekOrigin.Begin);

        ushort coverageOffset = reader.ReadOffset16();
        ushort ligGlyphCount = reader.ReadUInt16();

        using Buffer<ushort> ligGlyphOffsetsBuffer = new(ligGlyphCount);
        Span<ushort> ligGlyphOffsets = ligGlyphOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(ligGlyphOffsets);

        LigatureCaretList ligatureCaretList = new()
        {
            CoverageTable = CoverageTable.Load(reader, offset + coverageOffset),
            LigatureGlyphs = new LigatureGlyph[ligGlyphCount]
        };

        for (int i = 0; i < ligatureCaretList.LigatureGlyphs.Length; i++)
        {
            ligatureCaretList.LigatureGlyphs[i] = LigatureGlyph.Load(reader, offset + ligGlyphOffsets[i]);
        }

        return ligatureCaretList;
    }
}

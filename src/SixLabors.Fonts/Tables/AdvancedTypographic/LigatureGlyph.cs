// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A LigatureGlyph table contains the number of carets and the offsets to their caret value tables
/// for a single ligature glyph.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-glyph-table"/>
/// </summary>
internal sealed class LigatureGlyph
{
    /// <summary>
    /// Gets or sets the array of offsets to caret value tables for this ligature glyph.
    /// </summary>
    public ushort[]? CaretValueOffsets { get; internal set; }

    /// <summary>
    /// Loads the <see cref="LigatureGlyph"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the LigGlyph table.</param>
    /// <returns>The <see cref="LigatureGlyph"/>.</returns>
    public static LigatureGlyph Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);

        ushort caretCount = reader.ReadUInt16();
        return new LigatureGlyph()
        {
            CaretValueOffsets = reader.ReadUInt16Array(caretCount)
        };
    }
}

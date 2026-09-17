// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Format 6 is a trimmed table mapping subtable that maps a contiguous range of 16-bit character codes to glyph indices.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping"/>
/// </summary>
internal sealed class Format6SubTable : CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Format6SubTable"/> class.
    /// </summary>
    /// <param name="language">The language code for Macintosh platform subtables.</param>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="firstCode">The first character code of the range.</param>
    /// <param name="glyphIds">The array of glyph indices for the character codes in the range.</param>
    public Format6SubTable(ushort language, PlatformIDs platform, ushort encoding, ushort firstCode, ushort[] glyphIds)
        : base(platform, encoding, 6)
    {
        this.Language = language;
        this.FirstCode = firstCode;
        this.GlyphIds = glyphIds;
    }

    /// <summary>
    /// Gets the language code for Macintosh platform subtables.
    /// </summary>
    public ushort Language { get; }

    /// <summary>
    /// Gets the first character code of the range.
    /// </summary>
    public ushort FirstCode { get; }

    /// <summary>
    /// Gets the array of glyph indices for the character codes in the range.
    /// </summary>
    public ushort[] GlyphIds { get; }

    /// <inheritdoc/>
    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int index = codePoint.Value - this.FirstCode;
        if (index < 0 || index >= this.GlyphIds.Length)
        {
            glyphId = 0;
            return false;
        }

        glyphId = this.GlyphIds[index];
        return glyphId != 0;
    }

    /// <inheritdoc/>
    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        for (int i = 0; i < this.GlyphIds.Length; i++)
        {
            if (this.GlyphIds[i] == glyphId)
            {
                codePoint = new CodePoint(this.FirstCode + i);
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <inheritdoc/>
    public override IEnumerable<int> GetAvailableCodePoints()
        => Enumerable.Range(this.FirstCode, this.GlyphIds.Length);

    /// <summary>
    /// Loads one or more <see cref="Format6SubTable"/> instances from the specified encoding records and reader.
    /// </summary>
    /// <param name="encodings">The encoding records that share this subtable.</param>
    /// <param name="reader">The binary reader positioned after the format field.</param>
    /// <returns>An enumerable of <see cref="Format6SubTable"/> instances, one per encoding record.</returns>
    public static IEnumerable<Format6SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 6:
        // Type   | Name                     | Description
        // -------|--------------------------|------------------------------------------------------------------------
        // uint16 | format                   | Format number is set to 6.
        // uint16 | length                   | This is the length in bytes of the subtable.
        // uint16 | language                 | Please see "Note on the language field in 'cmap' subtables" in this document.
        // uint16 | firstCode                | First character code of subrange.
        // uint16 | entryCount               | Number of character codes in subrange.
        // uint16 | glyphIdArray[entryCount] | Array of glyph index values for character codes in the range.
        // format has already been read by this point skip it
        ushort length = reader.ReadUInt16();
        ushort language = reader.ReadUInt16();
        ushort firstCode = reader.ReadUInt16();
        ushort entryCount = reader.ReadUInt16();
        ushort[] glyphIds = reader.ReadUInt16Array(entryCount);

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format6SubTable(language, encoding.PlatformID, encoding.EncodingID, firstCode, glyphIds);
        }
    }
}

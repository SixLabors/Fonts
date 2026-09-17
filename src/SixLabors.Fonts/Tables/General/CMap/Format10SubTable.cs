// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Format 10 is a trimmed array subtable that maps a contiguous range of 32-bit character codes to glyph indices.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array"/>
/// </summary>
internal sealed class Format10SubTable : CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Format10SubTable"/> class.
    /// </summary>
    /// <param name="language">The language code for this subtable.</param>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="startCharCode">The first character code of the range.</param>
    /// <param name="glyphIds">The array of glyph indices for the character codes in the range.</param>
    public Format10SubTable(uint language, PlatformIDs platform, ushort encoding, uint startCharCode, ushort[] glyphIds)
        : base(platform, encoding, 10)
    {
        this.Language = language;
        this.StartCharCode = startCharCode;
        this.GlyphIds = glyphIds;
    }

    /// <summary>
    /// Gets the language code for this subtable.
    /// </summary>
    public uint Language { get; }

    /// <summary>
    /// Gets the first character code of the range.
    /// </summary>
    public uint StartCharCode { get; }

    /// <summary>
    /// Gets the array of glyph indices for the character codes in the range.
    /// </summary>
    public ushort[] GlyphIds { get; }

    /// <inheritdoc/>
    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        long index = codePoint.Value - this.StartCharCode;
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
                codePoint = new CodePoint((int)(this.StartCharCode + i));
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <inheritdoc/>
    public override IEnumerable<int> GetAvailableCodePoints()
        => Enumerable.Range((int)this.StartCharCode, this.GlyphIds.Length);

    /// <summary>
    /// Loads one or more <see cref="Format10SubTable"/> instances from the specified encoding records and reader.
    /// </summary>
    /// <param name="encodings">The encoding records that share this subtable.</param>
    /// <param name="reader">The binary reader positioned after the format field.</param>
    /// <returns>An enumerable of <see cref="Format10SubTable"/> instances, one per encoding record.</returns>
    public static IEnumerable<Format10SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 10:
        // Type   | Name                   | Description
        // -------|------------------------|-----------------------------------------------------------
        // uint16 | format                 | Subtable format; set to 10.
        // uint16 | reserved               | Reserved; set to 0.
        // uint32 | length                 | Byte length of this subtable (including the header).
        // uint32 | language               | Please see "Note on the language field in 'cmap' subtables" in this document.
        // uint32 | startCharCode          | First character code covered.
        // uint32 | numChars               | Number of character codes covered.
        // uint16 | glyphIdArray[numChars] | Array of glyph indices for the character codes covered.
        // format has already been read by this point skip it
        ushort reserved = reader.ReadUInt16();
        uint length = reader.ReadUInt32();
        uint language = reader.ReadUInt32();
        uint startCharCode = reader.ReadUInt32();
        uint numChars = reader.ReadUInt32();
        ushort[] glyphIds = reader.ReadUInt16Array((int)numChars);

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format10SubTable(language, encoding.PlatformID, encoding.EncodingID, startCharCode, glyphIds);
        }
    }
}

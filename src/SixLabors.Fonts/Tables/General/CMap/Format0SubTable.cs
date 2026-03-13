// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Format 0 is a simple byte encoding subtable that maps character codes 0–255 to glyph indices.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table"/>
/// </summary>
internal sealed class Format0SubTable : CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Format0SubTable"/> class.
    /// </summary>
    /// <param name="language">The language code for Macintosh platform subtables.</param>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="glyphIds">The array of glyph indices indexed by character code.</param>
    public Format0SubTable(ushort language, PlatformIDs platform, ushort encoding, byte[] glyphIds)
        : base(platform, encoding, 0)
    {
        this.Language = language;
        this.GlyphIds = glyphIds;
    }

    /// <summary>
    /// Gets the language code for Macintosh platform subtables.
    /// </summary>
    public ushort Language { get; }

    /// <summary>
    /// Gets the array of glyph indices indexed by character code.
    /// </summary>
    public byte[] GlyphIds { get; }

    /// <inheritdoc/>
    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int b = codePoint.Value;
        if (b >= this.GlyphIds.Length)
        {
            glyphId = 0;
            return false;
        }

        glyphId = this.GlyphIds[b];
        return true;
    }

    /// <inheritdoc/>
    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        for (int i = 0; i < this.GlyphIds.Length; i++)
        {
            if (this.GlyphIds[i] == glyphId)
            {
                codePoint = new CodePoint(i);
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <inheritdoc/>
    public override IEnumerable<int> GetAvailableCodePoints()
        => Enumerable.Range(0, this.GlyphIds.Length);

    /// <summary>
    /// Loads one or more <see cref="Format0SubTable"/> instances from the specified encoding records and reader.
    /// </summary>
    /// <param name="encodings">The encoding records that share this subtable.</param>
    /// <param name="reader">The binary reader positioned after the format field.</param>
    /// <returns>An enumerable of <see cref="Format0SubTable"/> instances, one per encoding record.</returns>
    public static IEnumerable<Format0SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // format has already been read by this point skip it
        ushort length = reader.ReadUInt16();
        ushort language = reader.ReadUInt16();
        int glyphsCount = length - 6;

        // char 'A' == 65 thus glyph = glyphIds[65];
        byte[] glyphIds = reader.ReadBytes(glyphsCount);

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format0SubTable(language, encoding.PlatformID, encoding.EncodingID, glyphIds);
        }
    }
}

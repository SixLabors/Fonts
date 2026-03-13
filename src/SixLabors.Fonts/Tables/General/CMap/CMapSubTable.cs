// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Base class for all 'cmap' subtables that map character codes to glyph indices.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"/>
/// </summary>
internal abstract class CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CMapSubTable"/> class.
    /// </summary>
    public CMapSubTable()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CMapSubTable"/> class.
    /// </summary>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="format">The subtable format number.</param>
    public CMapSubTable(PlatformIDs platform, ushort encoding, ushort format)
    {
        this.Platform = platform;
        this.Encoding = encoding;
        this.Format = format;
    }

    /// <summary>
    /// Gets the subtable format number.
    /// </summary>
    public ushort Format { get; }

    /// <summary>
    /// Gets the platform identifier.
    /// </summary>
    public PlatformIDs Platform { get; }

    /// <summary>
    /// Gets the platform-specific encoding identifier.
    /// </summary>
    public ushort Encoding { get; }

    /// <summary>
    /// Tries to get the glyph identifier for the given code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="glyphId">When this method returns, contains the glyph identifier if found; otherwise, 0.</param>
    /// <returns><see langword="true"/> if the glyph identifier was found; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId);

    /// <summary>
    /// Tries to get the code point for the given glyph identifier.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">When this method returns, contains the code point if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the code point was found; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint);

    /// <summary>
    /// Gets the collection of all available code points in this subtable.
    /// </summary>
    /// <returns>An enumerable of available code point values.</returns>
    public abstract IEnumerable<int> GetAvailableCodePoints();
}

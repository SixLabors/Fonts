// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the character to glyph index mapping table, which maps character codes to glyph indices.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"/>
/// </summary>
internal sealed class CMapTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "cmap";

    /// <summary>
    /// The format 14 subtables for Unicode variation sequences.
    /// </summary>
    private readonly Format14SubTable[] format14SubTables = Array.Empty<Format14SubTable>();

    /// <summary>
    /// Cached codepoints available in the font.
    /// </summary>
    private CodePoint[]? codepoints;

    /// <summary>
    /// Initializes a new instance of the <see cref="CMapTable"/> class.
    /// </summary>
    /// <param name="tables">The collection of CMap subtables.</param>
    public CMapTable(IEnumerable<CMapSubTable> tables)
    {
        this.Tables = tables.OrderBy(t => GetPreferredPlatformOrder(t.Platform)).ToArray();
        this.format14SubTables = this.Tables.OfType<Format14SubTable>().ToArray();
    }

    /// <summary>
    /// Gets the subtables ordered by preferred platform.
    /// </summary>
    internal CMapSubTable[] Tables { get; }

    /// <summary>
    /// Gets the preferred platform ordering for subtable selection.
    /// Windows is preferred, followed by Unicode, then Macintosh.
    /// </summary>
    /// <param name="platform">The platform identifier.</param>
    /// <returns>The sort order value (lower is more preferred).</returns>
    private static int GetPreferredPlatformOrder(PlatformIDs platform)
        => platform switch
        {
            PlatformIDs.Windows => 0,
            PlatformIDs.Unicode => 1,
            PlatformIDs.Macintosh => 2,
            _ => int.MaxValue
        };

    /// <summary>
    /// Tries to get the glyph ID for the given code point, optionally considering the next code point
    /// for Unicode Variation Sequence (UVS) matching.
    /// </summary>
    /// <param name="codePoint">The code point to look up.</param>
    /// <param name="nextCodePoint">The optional next code point for UVS matching.</param>
    /// <param name="glyphId">When this method returns, contains the glyph ID if found.</param>
    /// <param name="skipNextCodePoint">When this method returns, indicates whether the next code point was consumed as part of a UVS.</param>
    /// <returns><see langword="true"/> if a glyph was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out ushort glyphId, out bool skipNextCodePoint)
    {
        skipNextCodePoint = false;
        if (this.TryGetGlyphId(codePoint, out glyphId))
        {
            // If there is a second codepoint, we are asked whether this is an UVS sequence
            // - If true, return a glyph Id.
            // - Otherwise, return 0.
            if (nextCodePoint != null && this.format14SubTables.Length > 0)
            {
                foreach (Format14SubTable? cmap14 in this.format14SubTables)
                {
                    ushort pairGlyphId = cmap14.CharacterPairToGlyphId(codePoint, glyphId, nextCodePoint.Value);
                    if (pairGlyphId > 0)
                    {
                        glyphId = pairGlyphId;
                        skipNextCodePoint = true;
                        return true;
                    }
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to get the glyph ID for the given code point by searching all subtables.
    /// </summary>
    /// <param name="codePoint">The code point to look up.</param>
    /// <param name="glyphId">When this method returns, contains the glyph ID if found.</param>
    /// <returns><see langword="true"/> if a non-zero glyph ID was found; otherwise, <see langword="false"/>.</returns>
    private bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        foreach (CMapSubTable t in this.Tables)
        {
            // Keep looking until we have an index that's not the fallback.
            // Regardless of the encoding scheme, character codes that do
            // not correspond to any glyph in the font should be mapped to glyph index 0.
            // The glyph at this location must be a special glyph representing a missing character, commonly known as .notdef.
            if (t.TryGetGlyphId(codePoint, out glyphId) && glyphId > 0)
            {
                return true;
            }
        }

        glyphId = 0;
        return false;
    }

    /// <summary>
    /// Tries to get the code point for the given glyph ID via reverse lookup.
    /// </summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="codePoint">When this method returns, contains the code point if found.</param>
    /// <returns><see langword="true"/> if a code point was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        foreach (CMapSubTable t in this.Tables)
        {
            if (t.TryGetCodePoint(glyphId, out codePoint))
            {
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <summary>
    /// Gets the unicode codepoints for which a glyph exists in the font.
    /// </summary>
    /// <returns>The <see cref="IReadOnlyList{CodePoint}"/>.</returns>
    public IReadOnlyList<CodePoint> GetAvailableCodePoints()
    {
        if (this.codepoints is not null)
        {
            return this.codepoints;
        }

        HashSet<int> values = new();

        foreach (int v in this.Tables.SelectMany(subtable => subtable.GetAvailableCodePoints()))
        {
            values.Add(v);
        }

        return this.codepoints = values.OrderBy(v => v).Select(v => new CodePoint(v)).ToArray();
    }

    /// <summary>
    /// Loads the <see cref="CMapTable"/> from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="CMapTable"/>.</returns>
    public static CMapTable Load(FontReader reader)
    {
        using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
        return Load(binaryReader);
    }

    /// <summary>
    /// Loads the <see cref="CMapTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="CMapTable"/>.</returns>
    public static CMapTable Load(BigEndianBinaryReader reader)
    {
        ushort version = reader.ReadUInt16();
        ushort numTables = reader.ReadUInt16();

        var encodings = new EncodingRecord[numTables];
        for (int i = 0; i < numTables; i++)
        {
            encodings[i] = EncodingRecord.Read(reader);
        }

        // foreach encoding we move forward looking for the subtables
        var tables = new List<CMapSubTable>(numTables);
        foreach (IGrouping<uint, EncodingRecord> encoding in encodings.GroupBy(x => x.Offset))
        {
            long offset = encoding.Key;
            reader.Seek(offset, SeekOrigin.Begin);

            // Subtable format.
            switch (reader.ReadUInt16())
            {
                case 0:
                    tables.AddRange(Format0SubTable.Load(encoding, reader));
                    break;
                case 4:
                    tables.AddRange(Format4SubTable.Load(encoding, reader));
                    break;
                case 12:
                    tables.AddRange(Format12SubTable.Load(encoding, reader));
                    break;
                case 14:
                    tables.AddRange(Format14SubTable.Load(encoding, reader, offset));
                    break;
            }
        }

        return new CMapTable(tables);
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
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
    /// The Windows platform's symbol encoding identifier. A font declaring it
    /// does not map Unicode: it maps its own character codes, and the codes it
    /// uses live in <see cref="SymbolPageStart"/>'s private use page.
    /// </summary>
    private const ushort SymbolEncodingId = 0;

    /// <summary>
    /// The first character of the private use page a symbol font maps into.
    /// Such a font addresses its glyphs as U+F000 upwards while the text that
    /// uses it holds the character codes those shadow, so a lookup that misses
    /// is retried one page higher.
    /// </summary>
    private const int SymbolPageStart = 0xF000;

    /// <summary>
    /// The highest character code a symbol font's page can shadow. The page is
    /// a single byte wide, U+F000 to U+F0FF, so only a character that fits in a
    /// byte has a counterpart there.
    /// </summary>
    private const int SymbolPageLastShadowed = 0xFF;

    /// <summary>
    /// The format 14 subtables for Unicode variation sequences.
    /// </summary>
    private readonly Format14SubTable[] format14SubTables = Array.Empty<Format14SubTable>();

    /// <summary>
    /// The one subtable characters map through.
    /// </summary>
    private readonly CMapSubTable? characterMap;

    /// <summary>
    /// Whether <see cref="characterMap"/> uses the symbol encoding.
    /// </summary>
    private readonly bool isSymbolic;

    /// <summary>
    /// The legacy font-page marker controlling symbolic character remapping.
    /// </summary>
    private ushort symbolFontPage;

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
        this.characterMap = SelectCharacterMap(this.Tables, out bool symbolic);
        this.isSymbolic = symbolic;
    }

    /// <summary>
    /// Gets a value indicating whether the font declares Unicode variation sequences.
    /// When it does not, glyph lookup never needs the following codepoint, so callers
    /// can skip decoding it entirely.
    /// </summary>
    public bool HasVariationSequences => this.format14SubTables.Length > 0;

    /// <summary>
    /// Gets the subtables ordered by preferred platform.
    /// </summary>
    internal CMapSubTable[] Tables { get; }

    /// <summary>
    /// Sets the legacy font-page marker used by a symbolic character map.
    /// </summary>
    /// <param name="fontPage">The font-page marker, or zero for ordinary symbol remapping.</param>
    public void SetSymbolFontPage(ushort fontPage)
        => this.symbolFontPage = this.isSymbolic ? fontPage : (ushort)0;

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
    /// Chooses the one subtable that maps characters to glyphs, in the order the
    /// specification's implementations agree on: the Windows symbol encoding
    /// first, then the 32-bit Unicode encodings, then the 16-bit ones, and the
    /// Macintosh encoding only when a font offers nothing else. A font that
    /// carries several is not consulted for more than one of them - a character
    /// the chosen subtable does not map is unmapped, not a reason to consult a
    /// less preferred encoding, which would resolve characters through a table
    /// the font does not intend for them.
    /// </summary>
    /// <param name="tables">The subtables the font declares.</param>
    /// <param name="symbolic">Whether the chosen subtable uses the symbol encoding.</param>
    /// <returns>The subtable to map characters through, or <see langword="null"/> when the font declares none.</returns>
    private static CMapSubTable? SelectCharacterMap(CMapSubTable[] tables, out bool symbolic)
    {
        symbolic = false;
        foreach (CMapSubTable table in tables)
        {
            if (table.Platform == PlatformIDs.Windows && table.Encoding == SymbolEncodingId)
            {
                symbolic = true;
                return table;
            }
        }

        // Widest coverage first: an encoding that reaches beyond the basic
        // multilingual plane is preferred to one that cannot, and a Unicode
        // encoding to the Macintosh one, whose codes are bytes in a legacy
        // character set rather than characters.
        ReadOnlySpan<(PlatformIDs Platform, ushort Encoding)> preference =
        [
            (PlatformIDs.Windows, 10),   // Windows, full Unicode
            (PlatformIDs.Unicode, 6),    // Unicode 13.0 and later, full
            (PlatformIDs.Unicode, 4),    // Unicode 2.0 and later, full
            (PlatformIDs.Windows, 1),    // Windows, basic multilingual plane
            (PlatformIDs.Unicode, 3),    // Unicode 2.0 and later, plane zero
            (PlatformIDs.Unicode, 2),    // Unicode, ISO/IEC 10646
            (PlatformIDs.Unicode, 1),    // Unicode 1.1
            (PlatformIDs.Unicode, 0),    // Unicode 1.0
            (PlatformIDs.Macintosh, 0),  // Macintosh, single byte codes
        ];

        foreach ((PlatformIDs platform, ushort encoding) in preference)
        {
            foreach (CMapSubTable table in tables)
            {
                if (table.Platform == platform && table.Encoding == encoding && table is not Format14SubTable)
                {
                    return table;
                }
            }
        }

        return null;
    }

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
    /// <returns><see langword="true"/> if a glyph ID was found; otherwise, <see langword="false"/>.</returns>
    private bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        glyphId = 0;
        if (this.characterMap is null)
        {
            return false;
        }

        if (this.characterMap.TryGetGlyphId(codePoint, out glyphId) && glyphId > 0)
        {
            return true;
        }

        if (this.isSymbolic && this.symbolFontPage == 0 && codePoint.Value <= SymbolPageLastShadowed)
        {
            // An ordinary symbol font shadows one byte of character codes in its
            // U+F000 private-use page.
            CodePoint shadowed = new(SymbolPageStart + codePoint.Value);
            if (this.characterMap.TryGetGlyphId(shadowed, out glyphId) && glyphId > 0)
            {
                return true;
            }
        }

        if (this.isSymbolic && this.symbolFontPage != 0)
        {
            ushort mappedCodePoint = ArabicLegacyEncodingData.GetMappedCodePoint(this.symbolFontPage, codePoint.Value);
            if (mappedCodePoint != 0 && this.characterMap.TryGetGlyphId(new CodePoint(mappedCodePoint), out glyphId) && glyphId > 0)
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
    /// <returns>A read-only memory region containing the available codepoints.</returns>
    public ReadOnlyMemory<CodePoint> GetAvailableCodePoints()
    {
        if (this.codepoints is not null)
        {
            return this.codepoints;
        }

        HashSet<int> values = new();
        if (this.characterMap is not null)
        {
            // Only the subtable characters map through: a codepoint another
            // subtable lists is not one this font resolves.
            foreach (int v in this.characterMap.GetAvailableCodePoints())
            {
                values.Add(v);
            }

            if (this.isSymbolic && this.symbolFontPage == 0)
            {
                // An ordinary symbol font's page is reachable through the byte
                // values it shadows as well as through the page itself.
                foreach (int v in this.characterMap.GetAvailableCodePoints())
                {
                    if (v >= SymbolPageStart && v <= SymbolPageStart + SymbolPageLastShadowed)
                    {
                        values.Add(v - SymbolPageStart);
                    }
                }
            }
            else if (this.isSymbolic)
            {
                ReadOnlySpan<byte> mappings = ArabicLegacyEncodingData.GetMappings(this.symbolFontPage);
                for (int offset = 0; offset < mappings.Length; offset += ArabicLegacyEncodingData.MappingEntrySize)
                {
                    ushort mappedCodePoint = BinaryPrimitives.ReadUInt16LittleEndian(
                        mappings.Slice(offset + ArabicLegacyEncodingData.MappedCodePointOffset, sizeof(ushort)));

                    if (this.characterMap.TryGetGlyphId(new CodePoint(mappedCodePoint), out ushort glyphId) && glyphId > 0)
                    {
                        ushort codePoint = BinaryPrimitives.ReadUInt16LittleEndian(mappings.Slice(offset, sizeof(ushort)));
                        values.Add(codePoint);
                    }
                }
            }
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

        EncodingRecord[] encodings = new EncodingRecord[numTables];
        for (int i = 0; i < numTables; i++)
        {
            encodings[i] = EncodingRecord.Read(reader);
        }

        // foreach encoding we move forward looking for the subtables
        List<CMapSubTable> tables = new(numTables);
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
                case 13:
                    tables.AddRange(Format13SubTable.Load(encoding, reader));
                    break;
                case 14:
                    tables.AddRange(Format14SubTable.Load(encoding, reader, offset));
                    break;
            }
        }

        return new CMapTable(tables);
    }
}

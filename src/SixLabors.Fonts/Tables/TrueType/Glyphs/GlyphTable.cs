// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using SixLabors.Fonts.Tables.Woff;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Represents the 'glyf' table containing TrueType glyph outline data.
/// Each glyph is lazily loaded and cached on first access.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"/>
/// </summary>
internal class GlyphTable : Table
{
    /// <summary>
    /// The table tag name.
    /// </summary>
    internal const string TableName = "glyf";
    private readonly GlyphLoader[] loaders;
    private readonly ConcurrentDictionary<int, GlyphVector> glyphCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphTable"/> class.
    /// </summary>
    /// <param name="glyphLoaders">The array of glyph loaders, one per glyph.</param>
    public GlyphTable(GlyphLoader[] glyphLoaders)
    {
        this.loaders = glyphLoaders;
        this.glyphCache = new(Environment.ProcessorCount, glyphLoaders.Length);
    }

    /// <summary>
    /// Gets the number of glyphs in this table.
    /// </summary>
    public int GlyphCount => this.loaders.Length;

    /// <summary>
    /// Gets the <see cref="GlyphVector"/> for the glyph at the specified index.
    /// </summary>
    /// <param name="index">The zero-based glyph index.</param>
    /// <returns>The <see cref="GlyphVector"/>, or an empty vector if the index is out of range.</returns>
    // TODO: Make this non-virtual
    internal virtual GlyphVector GetGlyph(int index)
    {
        if (index < 0 || index >= this.loaders.Length)
        {
            return GlyphVector.Empty();
        }

        return this.glyphCache.GetOrAdd(index, i => this.loaders[i].CreateGlyph(this));
    }

    /// <summary>
    /// Loads the 'glyf' table from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="GlyphTable"/>.</returns>
    public static GlyphTable Load(FontReader reader)
    {
        uint[] locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;

        // Use an empty bounds instance as the fallback.
        // We will substitute this with the advance width/height to determine bounds instead when rendering/measuring.
        Bounds fallbackEmptyBounds = Bounds.Empty;

        using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
        return Load(binaryReader, reader.TableFormat, locations, in fallbackEmptyBounds);
    }

    /// <summary>
    /// Loads the 'glyf' table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the table.</param>
    /// <param name="format">The table format (e.g. WOFF2 vs standard).</param>
    /// <param name="locations">The glyph offset array from the 'loca' table.</param>
    /// <param name="fallbackEmptyBounds">The fallback bounds for empty glyphs.</param>
    /// <returns>The <see cref="GlyphTable"/>.</returns>
    public static GlyphTable Load(
        BigEndianBinaryReader reader,
        TableFormat format,
        uint[] locations,
        in Bounds fallbackEmptyBounds)
    {
        EmptyGlyphLoader empty = new(fallbackEmptyBounds);
        int entryCount = locations.Length;
        int glyphCount = entryCount - 1; // last entry is a placeholder to the end of the table
        GlyphLoader[] glyphs = new GlyphLoader[glyphCount];

        // Special case for WOFF2 format where all glyphs need to be read in one go.
        if (format is TableFormat.Woff2)
        {
            return new GlyphTable(Woff2Utils.LoadAllGlyphs(reader, empty));
        }

        for (int i = 0; i < glyphCount; i++)
        {
            if (locations[i] == locations[i + 1])
            {
                // This is an empty glyph;
                glyphs[i] = empty;
            }
            else
            {
                // Move to start of glyph.
                reader.Seek(locations[i], SeekOrigin.Begin);
                glyphs[i] = GlyphLoader.Load(reader);
            }
        }

        return new GlyphTable(glyphs);
    }
}

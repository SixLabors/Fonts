// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.Woff;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

internal class GlyphTable : Table
{
    internal const string TableName = "glyf";
    private readonly GlyphLoader[] loaders;
    private readonly ConcurrentDictionary<int, GlyphVector> glyphCache;

    public GlyphTable(GlyphLoader[] glyphLoaders)
    {
        this.loaders = glyphLoaders;
        this.glyphCache = new(Environment.ProcessorCount, glyphLoaders.Length);
    }

    public int GlyphCount => this.loaders.Length;

    // TODO: Make this non-virtual
    internal virtual GlyphVector GetGlyph(int index)
    {
        if (index < 0 || index >= this.loaders.Length)
        {
            return default;
        }

        return this.glyphCache.GetOrAdd(index, i => this.loaders[i].CreateGlyph(this));
    }

    public static GlyphTable Load(FontReader reader)
    {
        uint[] locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;

        FVarTable? fvar = reader.TryGetTable<FVarTable>();
        AVarTable? avar = reader.TryGetTable<AVarTable>();
        GVarTable? gvar = reader.TryGetTable<GVarTable>();
        HVarTable? hvar = reader.TryGetTable<HVarTable>();
        GlyphVariationProcessor? glyphVariationProcessor = fvar is null || hvar is null ? null : new GlyphVariationProcessor(hvar!.ItemVariationStore, fvar, avar, gvar);

        // Use an empty bounds instance as the fallback.
        // We will substitute this with the advance width/height to determine bounds instead when rendering/measuring.
        Bounds fallbackEmptyBounds = Bounds.Empty;

        using BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName);
        return Load(binaryReader, reader.TableFormat, locations, in fallbackEmptyBounds);
    }

    public static GlyphTable Load(BigEndianBinaryReader reader, TableFormat format, uint[] locations, in Bounds fallbackEmptyBounds)
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

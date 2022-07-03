// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.Woff;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    internal class GlyphTable : Table
    {
        internal const string TableName = "glyf";
        private readonly GlyphLoader[] loaders;

        public GlyphTable(GlyphLoader[] glyphLoaders)
            => this.loaders = glyphLoaders;

        public int GlyphCount => this.loaders.Length;

        // TODO: Make this non-virtual
        internal virtual GlyphVector GetGlyph(int index)
            => this.loaders[index].CreateGlyph(this);

        public static GlyphTable Load(FontReader reader)
        {
            uint[] locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;
            Bounds fallbackEmptyBounds = reader.GetTable<HeadTable>().Bounds;

            FVarTable? fvar = reader.TryGetTable<FVarTable>();
            AVarTable? avar = reader.TryGetTable<AVarTable>();
            GVarTable? gvar = reader.TryGetTable<GVarTable>();

            
            //GlyphVariationProcessor? glyphVariationProcessor = fvar is null ? null : new GlyphVariationProcessor(itemStore, fvar, avar, gvar);
            
            using (BigEndianBinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader, reader.TableFormat, locations, fallbackEmptyBounds);
            }
        }

        public static GlyphTable Load(BigEndianBinaryReader reader, TableFormat format, uint[] locations, in Bounds fallbackEmptyBounds)
        {
            var empty = new EmptyGlyphLoader(fallbackEmptyBounds);
            int entryCount = locations.Length;
            int glyphCount = entryCount - 1; // last entry is a placeholder to the end of the table
            var glyphs = new GlyphLoader[glyphCount];

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
}

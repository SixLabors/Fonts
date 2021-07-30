// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class GlyphTable : Table
    {
        private const string TableName = TableNames.Glyph;
        private readonly GlyphLoader[] loaders;

        public GlyphTable(GlyphLoader[] glyphLoaders)
            => this.loaders = glyphLoaders;

        public int GlyphCount => this.loaders.Length;

        internal virtual GlyphVector GetGlyph(int index)
            => this.loaders[index].CreateGlyph(this);

        public static GlyphTable Load(FontReader reader)
        {
            uint[] locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;
            Bounds fallbackEmptyBounds = reader.GetTable<HeadTable>().Bounds;

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
                    // this is an empty glyphs;
                    glyphs[i] = empty;
                }
                else
                {
                    // Move to start of glyph.
                    reader.Seek(locations[i], System.IO.SeekOrigin.Begin);

                    glyphs[i] = GlyphLoader.Load(reader);
                }
            }

            return new GlyphTable(glyphs);
        }
    }
}

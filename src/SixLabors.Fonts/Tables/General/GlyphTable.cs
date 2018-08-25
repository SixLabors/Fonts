// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class GlyphTable : Table
    {
        private const string TableName = "glyf";
        private readonly GlyphLoader[] loaders;

        public int GlyphCount => this.loaders.Length;

        public GlyphTable(GlyphLoader[] glyphLoaders)
        {
            this.loaders = glyphLoaders;
        }

        internal virtual GlyphVector GetGlyph(int index)
        {
            return this.loaders[index].CreateGlyph(this);
        }

        public static GlyphTable Load(FontReader reader)
        {
            uint[] locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;
            Bounds fallbackEmptyBounds = reader.GetTable<HeadTable>().Bounds;

            using (BinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader, locations, fallbackEmptyBounds);
            }
        }

        public static GlyphTable Load(BinaryReader reader, uint[] locations, in Bounds fallbackEmptyBounds)
        {
            var empty = new EmptyGlyphLoader(fallbackEmptyBounds);
            int entryCount = locations.Length;
            int glyphCount = entryCount - 1; // last entry is a placeholder to the end of the table
            var glyphs = new GlyphLoader[glyphCount];

            for (int i = 0; i < glyphCount; i++)
            {
                if (locations[i] == locations[i + 1])
                {
                    // this is an empty glyphs;
                    glyphs[i] = empty;
                }
                else
                {
                    // move to start of glyph
                    reader.Seek(locations[i], System.IO.SeekOrigin.Begin);

                    glyphs[i] = GlyphLoader.Load(reader);
                }
            }

            return new GlyphTable(glyphs);
        }
    }
}
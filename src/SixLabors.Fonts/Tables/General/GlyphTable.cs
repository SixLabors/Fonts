using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class GlyphTable : Table
    {
        private const string TableName = "glyf";
        private GlyphLoader[] loaders;
        private Glyph[] glyphs;

        public GlyphTable(GlyphLoader[] glyphLoaders)
        {
            this.loaders = glyphLoaders;
            this.glyphs = new Glyph[glyphLoaders.Length];
        }

        internal Glyph GetGlyph(int index)
        {
            if (this.glyphs[index] == null)
            {
                this.glyphs[index] = this.loaders[index].CreateGlyph(this);
            }

            return this.glyphs[index];
        }

        public static GlyphTable Load(FontReader reader)
        {
            var locations = reader.GetTable<IndexLocationTable>().GlyphOffsets;
            return Load(reader.GetReaderAtTablePosition(TableName), locations);
        }

        public static GlyphTable Load(BinaryReader reader, uint[] locations)
        {
            var start = reader.BaseStream.Position;
            var empty = new Glyphs.EmptyGlyphLoader();
            var entryCount = locations.Length;
            var glyphCount = entryCount - 1; // last entry is a placeholder to the end of the table
            Glyphs.GlyphLoader[] glyphs = new Glyphs.GlyphLoader[glyphCount];
            for (var i = 0; i < glyphCount; i++)
            {
                if (locations[i] == locations[i + 1])
                {
                    // this is an empty glyphs;
                    glyphs[i] = empty;
                }
                else
                {
                    // move to start of glyph
                    var position = start + locations[i];
                    reader.Seek(position, System.IO.SeekOrigin.Begin);

                    glyphs[i] = GlyphLoader.Load(reader);
                }
            }

            return new GlyphTable(glyphs);
        }
    }
}
using System;
using System.IO;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    public class Font : FontDescription
    {
        private readonly CMapTable cmap;
        private readonly GlyphTable glyphs;
        private readonly OS2Table os2;
        private readonly HorizontalMetricsTable horizontalMetrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontDescription" /> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="cmap">The cmap.</param>
        /// <param name="glyphs">The glyphs.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="horizontalMetrics">The horizontal metrics.</param>
        internal Font(NameTable nameTable, CMapTable cmap, GlyphTable glyphs, OS2Table os2, HorizontalMetricsTable horizontalMetrics)
            : base(nameTable)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
        }

        internal ushort GetGlyphIndex(char character)
        {
            return this.cmap.GetGlyphId(character);
        }

        internal Glyph GetGlyph(char character)
        {
            var idx = this.GetGlyphIndex(character);
            return this.GetGlyph(idx);
        }

        internal Glyph GetGlyph(ushort index)
        {
            return this.glyphs.GetGlyph(index);
        }

        internal int GetAdvancedWidth(ushort index)
        {
            return this.horizontalMetrics.GetAdvancedWidth(index);
        }

        internal int GetAdvancedWidth(ushort first, ushort second)
        {
            var advance = this.horizontalMetrics.GetAdvancedWidth(first);

            // TODO combin data from the kern table to offset this width
            return advance;
        }

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static new Font Load(Stream stream)
        {
            var reader = new FontReader(stream);

            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recomended order
            reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
            reader.GetTable<HoizontalHeadTable>(); // hhea
            reader.GetTable<MaximumProfileTable>(); // maxp
            var os2 = reader.GetTable<OS2Table>(); // OS/2
            var horizontalMetrics = reader.GetTable<HorizontalMetricsTable>(); // hmtx
            // LTSH - Linear threshold data
            // VDMX - Vertical device metrics
            // hdmx - Horizontal device metrics
            var cmap = reader.GetTable<CMapTable>(); // cmap
            // fpgm - Font Program
            // prep - Control Value Program
            // cvt  - Control Value Table
            reader.GetTable<IndexLocationTable>(); // loca
            var glyphs = reader.GetTable<GlyphTable>(); // glyf
            // kern - Kerning
            var nameTable = reader.GetTable<NameTable>(); // name
            // post - PostScript information
            // gasp - Grid-fitting/Scan-conversion (optional table)
            // PCLT - PCL 5 data
            // DSIG - Digital signature
            return new Font(nameTable, cmap, glyphs, os2, horizontalMetrics);
        }
    }
}

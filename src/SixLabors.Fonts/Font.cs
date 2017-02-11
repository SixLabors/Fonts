using System;
using System.IO;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using System.Numerics;

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
        private Glyph[] glyphCache;
        private readonly HeadTable head;

        private KerningTable kerning;

        public int LineHeight { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontDescription" /> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="cmap">The cmap.</param>
        /// <param name="glyphs">The glyphs.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="horizontalMetrics">The horizontal metrics.</param>
        internal Font(NameTable nameTable, CMapTable cmap, GlyphTable glyphs, OS2Table os2, HorizontalMetricsTable horizontalMetrics, HeadTable head, KerningTable kern)
            : base(nameTable)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.head = head;
            glyphCache = new Glyph[this.glyphs.GlyphCount];

            // https://www.microsoft.com/typography/otspec/recom.htm#tad
            this.LineHeight = os2.TypoAscender - os2.TypoDescender + os2.TypoLineGap;
            this.EmSize = this.head.UnitsPerEm;
            this.kerning = kern;
        }

        public ushort EmSize { get; }

        internal ushort GetGlyphIndex(char character)
        {
            return this.cmap.GetGlyphId(character);
        }

        public Glyph GetGlyph(char character)
        {
            var idx = this.GetGlyphIndex(character);
            if (glyphCache[idx] == null)
            {
                var advanceWidth = this.horizontalMetrics.GetAdvancedWidth(idx);
                var lsb = this.horizontalMetrics.GetLeftSideBearing(idx);
                var vector = this.glyphs.GetGlyph(idx);
                glyphCache[idx] = new Glyph(vector.ControlPoints, vector.OnCurves, vector.EndPoints, vector.Bounds, advanceWidth, this.EmSize, idx);
            }

            return glyphCache[idx];
        }

        public Vector2 GetOffset(Glyph glyph, Glyph previousGlyph)
        {
            // we also want to wire int sub/super script offsetting into here too
            if (previousGlyph == null)
            {
                return Vector2.Zero;
            }
            
            // once we wire in the kerning caclulations this will return real data
            return this.kerning.GetOffset(previousGlyph.Index, glyph.Index);
        }

#if FILESYSTEM
        /// <summary>
        /// Reads a <see cref="Font"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="Font"/>.</returns>
        public static Font LoadFont(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                var reader = new FontReader(fs);
                return LoadFont(reader);
            }
        }
#endif

        /// <summary>
        /// Reads a <see cref="Font"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="Font"/>.</returns>
        public static Font LoadFont(Stream stream)
        {
            var reader = new FontReader(stream);
            return LoadFont(reader);
        }

        internal static Font LoadFont(FontReader reader)
        {
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recomended order
            var head = reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
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
            var kern = reader.GetTable<KerningTable>(); // kern - Kerning
            var nameTable = reader.GetTable<NameTable>(); // name
            // post - PostScript information
            // gasp - Grid-fitting/Scan-conversion (optional table)
            // PCLT - PCL 5 data
            // DSIG - Digital signature
            return new Font(nameTable, cmap, glyphs, os2, horizontalMetrics, head, kern);
        }
    }
}

// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    internal class FontInstance : IFontInstance
    {
        private readonly CMapTable cmap;
        private readonly GlyphTable glyphs;
        private readonly HeadTable head;
        private readonly OS2Table os2;
        private readonly HorizontalMetricsTable horizontalMetrics;
        private readonly GlyphInstance[] glyphCache;
        private readonly KerningTable kerning;

        /// <summary>
        /// Gets the height of the line.
        /// </summary>
        /// <value>
        /// The height of the line.
        /// </value>
        public int LineHeight { get; }

        /// <summary>
        /// Gets the ascender.
        /// </summary>
        public short Ascender { get; }

        /// <summary>
        /// Gets the descender.
        /// </summary>
        public short Descender { get; }

        /// <summary>
        /// Gets the line gap.
        /// </summary>
        public short LineGap { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontInstance"/> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="cmap">The cmap.</param>
        /// <param name="glyphs">The glyphs.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="horizontalMetrics">The horizontal metrics.</param>
        /// <param name="head">The head.</param>
        /// <param name="kern">The kern.</param>
        internal FontInstance(NameTable nameTable, CMapTable cmap, GlyphTable glyphs, OS2Table os2, HorizontalMetricsTable horizontalMetrics, HeadTable head, KerningTable kern)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.head = head;
            this.glyphCache = new GlyphInstance[this.glyphs.GlyphCount];

            // https://www.microsoft.com/typography/otspec/recom.htm#tad
            this.LineHeight = os2.TypoAscender - os2.TypoDescender + os2.TypoLineGap;
            this.Ascender = os2.TypoAscender;
            this.Descender = os2.TypoDescender;
            this.LineGap = os2.TypoLineGap;
            this.EmSize = this.head.UnitsPerEm;
            this.kerning = kern;
            this.Description = new FontDescription(nameTable, os2, head);
        }

        /// <summary>
        /// Gets the size of the em.
        /// </summary>
        /// <value>
        /// The size of the em.
        /// </value>
        public ushort EmSize { get; }

        public FontDescription Description { get; }

        internal ushort GetGlyphIndex(int codePoint)
        {
            if (codePoint > ushort.MaxValue)
            {
                throw new NotImplementedException("cmap table doesn't support 32-bit characters yet.");
            }

            return this.cmap.GetGlyphId(codePoint);
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="codePoint">The code point of the character.</param>
        /// <returns>the glyph for a known character.</returns>
        GlyphInstance IFontInstance.GetGlyph(int codePoint)
        {
            ushort idx = this.GetGlyphIndex(codePoint);
            if (this.glyphCache[idx] is null)
            {
                ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(idx);
                short lsb = this.horizontalMetrics.GetLeftSideBearing(idx);
                GlyphVector vector = this.glyphs.GetGlyph(idx);
                this.glyphCache[idx] = new GlyphInstance(this, vector.ControlPoints, vector.OnCurves, vector.EndPoints, vector.Bounds, advanceWidth, lsb, this.EmSize, idx);
            }

            return this.glyphCache[idx];
        }

        /// <summary>
        /// Gets the amount the <paramref name="glyph"/> should be ofset if it was proceeded by the <paramref name="previousGlyph"/>.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <param name="previousGlyph">The previous glyph.</param>
        /// <returns>A <see cref="Vector2"/> represting the offset that should be applied to the <paramref name="glyph"/>. </returns>
        Vector2 IFontInstance.GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph)
        {
            // we also want to wire int sub/super script offsetting into here too
            if (previousGlyph is null)
            {
                return Vector2.Zero;
            }

            // once we wire in the kerning calculations this will return real data
            return this.kerning.GetOffset(previousGlyph.Index, glyph.Index);
        }

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance LoadFont(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                var reader = new FontReader(fs);
                return LoadFont(reader);
            }
        }

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance LoadFont(Stream stream)
        {
            FontReader reader = new FontReader(stream);
            return LoadFont(reader);
        }

        internal static FontInstance LoadFont(FontReader reader)
        {
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recomended order
            HeadTable head = reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
            reader.GetTable<HoizontalHeadTable>(); // hhea
            reader.GetTable<MaximumProfileTable>(); // maxp
            OS2Table os2 = reader.GetTable<OS2Table>(); // OS/2
            HorizontalMetricsTable horizontalMetrics = reader.GetTable<HorizontalMetricsTable>(); // hmtx

            // LTSH - Linear threshold data
            // VDMX - Vertical device metrics
            // hdmx - Horizontal device metrics
            CMapTable cmap = reader.GetTable<CMapTable>(); // cmap

            // fpgm - Font Program
            // prep - Control Value Program
            // cvt  - Control Value Table
            reader.GetTable<IndexLocationTable>(); // loca
            GlyphTable glyphs = reader.GetTable<GlyphTable>(); // glyf
            KerningTable kern = reader.GetTable<KerningTable>(); // kern - Kerning
            NameTable nameTable = reader.GetTable<NameTable>(); // name

            // post - PostScript information
            // gasp - Grid-fitting/Scan-conversion (optional table)
            // PCLT - PCL 5 data
            // DSIG - Digital signature
            return new FontInstance(nameTable, cmap, glyphs, os2, horizontalMetrics, head, kern);
        }
    }
}

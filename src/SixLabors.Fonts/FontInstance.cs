// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    public class FontInstance : IFontInstance
    {
        private readonly CMapTable cmap;
        private readonly GlyphTable glyphs;
        private readonly HeadTable head;
        private readonly OS2Table os2;
        private readonly HorizontalMetricsTable horizontalMetrics;
        private readonly GlyphInstance[] glyphCache;
        private readonly GlyphInstance[][]? colorGlyphCache;
        private readonly KerningTable kerning;
        private readonly ColrTable? colrTable;
        private readonly CpalTable? cpalTable;

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
        /// <param name="colrTable">The COLR table</param>
        /// <param name="cpalTable">The CPAL table</param>
        internal FontInstance(NameTable nameTable, CMapTable cmap, GlyphTable glyphs, OS2Table os2, HorizontalMetricsTable horizontalMetrics, HeadTable head, KerningTable kern, ColrTable? colrTable, CpalTable? cpalTable)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.head = head;
            this.glyphCache = new GlyphInstance[this.glyphs.GlyphCount];
            if (!(colrTable is null))
            {
                this.colorGlyphCache = new GlyphInstance[this.glyphs.GlyphCount][];
            }

            // https://www.microsoft.com/typography/otspec/recom.htm#tad
            this.LineHeight = os2.TypoAscender - os2.TypoDescender + os2.TypoLineGap;
            this.Ascender = os2.TypoAscender;
            this.Descender = os2.TypoDescender;
            this.LineGap = os2.TypoLineGap;
            this.EmSize = this.head.UnitsPerEm;
            this.kerning = kern;
            this.colrTable = colrTable;
            this.cpalTable = cpalTable;
            this.Description = new FontDescription(nameTable, os2, head);
        }

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
        /// Gets the size of the em.
        /// </summary>
        /// <value>
        /// The size of the em.
        /// </value>
        public ushort EmSize { get; }

        /// <inheritdoc/>
        public FontDescription Description { get; }

        internal bool TryGetGlyphIndex(int codePoint, out ushort glyphId)
        {
            return this.cmap.TryGetGlyphId(codePoint, out glyphId);
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="codePoint">The code point of the character.</param>
        /// <returns>the glyph for a known character.</returns>
        GlyphInstance IFontInstance.GetGlyph(int codePoint)
        {
            var foundGlyph = this.TryGetGlyphIndex(codePoint, out var idx);
            if (!foundGlyph)
            {
                idx = 0;
            }

            if (this.glyphCache[idx] is null)
            {
                this.glyphCache[idx] = this.CreateInstance(idx, foundGlyph ? GlyphType.Standard : GlyphType.Fallback);
            }

            return this.glyphCache[idx];
        }

        private GlyphInstance CreateInstance(ushort idx, GlyphType glyphType, ushort palleteIndex = 0)
        {
            ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(idx);
            short lsb = this.horizontalMetrics.GetLeftSideBearing(idx);
            GlyphVector vector = this.glyphs.GetGlyph(idx);
            GlyphColor? color = null;
            if (glyphType == GlyphType.ColrLayer)
            {
                // 0xFFFF is special index meaning use foreground color and thus leave unset
                if (palleteIndex != 0xFFFF)
                {
                    color = this.cpalTable?.GetGlyphColor(0, palleteIndex);
                }
            }

            return new GlyphInstance(this, vector, advanceWidth, lsb, this.EmSize, idx, glyphType, color);
        }

        internal bool TryGetColoredVectors(ushort idx, [NotNullWhen(true)] out GlyphInstance[]? vectors)
        {
            if (this.colrTable == null || this.colorGlyphCache == null)
            {
                vectors = null;
                return false;
            }

            vectors = this.colorGlyphCache[idx];
            if (vectors is null)
            {
                var indexes = this.colrTable.GetLayers(idx);
                if (indexes.Length > 0)
                {
                    vectors = new GlyphInstance[indexes.Length];
                    for (var i = 0; i < indexes.Length; i++)
                    {
                        var layer = indexes[i];

                        vectors[i] = this.CreateInstance(layer.GlyphId, GlyphType.ColrLayer, layer.PalletteIndex);
                    }
                }

                vectors = vectors ?? Array.Empty<GlyphInstance>();
                this.colorGlyphCache[idx] = vectors;
            }

            return vectors.Length > 0;
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
        /// <param name="path">The file path.</param>
        /// <param name="offset">Position in the stream to read the font from.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance LoadFont(string path, long offset)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                fs.Position = offset;
                return LoadFont(fs);
            }
        }

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance LoadFont(Stream stream)
        {
            var reader = new FontReader(stream);
            return LoadFont(reader);
        }

        internal static FontInstance LoadFont(FontReader reader)
        {
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recomended order
            HeadTable head = reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
            reader.GetTable<HorizontalHeadTable>(); // hhea
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

            ColrTable? colrTable = reader.TryGetTable<ColrTable>(); // colr
            CpalTable? cpalTable;
            if (colrTable != null)
            {
                cpalTable = reader.GetTable<CpalTable>(); // CPAL - required if COLR is provided
            }
            else
            {
                cpalTable = reader.TryGetTable<CpalTable>(); // colr
            }

            // post - PostScript information
            // gasp - Grid-fitting/Scan-conversion (optional table)
            // PCLT - PCL 5 data
            // DSIG - Digital signature
            return new FontInstance(nameTable, cmap, glyphs, os2, horizontalMetrics, head, kern, colrTable, cpalTable);
        }

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance[] LoadFontCollection(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return LoadFontCollection(fs);
            }
        }

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FontInstance[] LoadFontCollection(Stream stream)
        {
            long startPos = stream.Position;
            var reader = new BinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var fonts = new FontInstance[(int)ttcHeader.NumFonts];

            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                fonts[i] = FontInstance.LoadFont(stream);
            }

            return fonts;
        }
    }
}

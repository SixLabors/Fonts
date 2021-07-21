// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Unicode;

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
        private readonly GlyphMetrics[] glyphCache;
        private readonly GlyphMetrics[][]? colorGlyphCache;
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
        /// <param name="horizontalHeadTable">The horizontal head table.</param>
        /// <param name="horizontalMetrics">The horizontal metrics.</param>
        /// <param name="head">The head.</param>
        /// <param name="kern">The kern.</param>
        /// <param name="colrTable">The COLR table</param>
        /// <param name="cpalTable">The CPAL table</param>
        internal FontInstance(
            NameTable nameTable,
            CMapTable cmap,
            GlyphTable glyphs,
            OS2Table os2,
            HorizontalHeadTable horizontalHeadTable,
            HorizontalMetricsTable horizontalMetrics,
            HeadTable head,
            KerningTable kern,
            ColrTable? colrTable,
            CpalTable? cpalTable)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.head = head;
            this.glyphCache = new GlyphMetrics[this.glyphs.GlyphCount];
            if (!(colrTable is null))
            {
                this.colorGlyphCache = new GlyphMetrics[this.glyphs.GlyphCount][];
            }

            // https://www.microsoft.com/typography/otspec/recom.htm#tad
            // We use the same approach as FreeType for calculating the the global  ascender, descender,  and
            // height of  OpenType fonts for consistency.
            //
            // 1.If the OS/ 2 table exists and the fsSelection bit 7 is set (USE_TYPO_METRICS), trust the font
            //   and use the Typo* metrics.
            // 2.Otherwise, use the HorizontalHeadTable "hhea" table's metrics.
            // 3.If they are zero and the OS/ 2 table exists,
            //    - Use the OS/ 2 table's sTypo* metrics if they are non-zero.
            //    - Otherwise, use the OS / 2 table's usWin* metrics.
            bool useTypoMetrics = os2.FontStyle.HasFlag(OS2Table.FontStyleSelection.USE_TYPO_METRICS);
            if (useTypoMetrics)
            {
                this.Ascender = os2.TypoAscender;
                this.Descender = os2.TypoDescender;
                this.LineGap = os2.TypoLineGap;
            }
            else
            {
                this.Ascender = horizontalHeadTable.Ascender;
                this.Descender = horizontalHeadTable.Descender;
                this.LineGap = horizontalHeadTable.LineGap;
            }

            if (this.Ascender == 0 || this.Descender == 0)
            {
                if (os2.TypoAscender != 0 || os2.TypoDescender != 0)
                {
                    this.Ascender = os2.TypoAscender;
                    this.Descender = os2.TypoDescender;
                }
                else
                {
                    this.Ascender = (short)os2.WinAscent;
                    this.Descender = (short)os2.WinAscent;
                }

                this.LineGap = os2.TypoLineGap;
            }

            this.LineHeight = this.Ascender - this.Descender + this.LineGap;
            this.EmSize = this.head.UnitsPerEm;
            this.kerning = kern;
            this.colrTable = colrTable;
            this.cpalTable = cpalTable;
            this.Description = new FontDescription(nameTable, os2, head);
        }

        /// <summary>
        /// Gets the height of the line.
        /// </summary>
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
        public ushort EmSize { get; }

        /// <inheritdoc/>
        public FontDescription Description { get; }

        internal bool TryGetGlyphIndex(CodePoint codePoint, out ushort glyphId)
            => this.cmap.TryGetGlyphId(codePoint, out glyphId);

        /// <summary>
        /// Gets the glyph metrics for the codepoint.
        /// </summary>
        /// <param name="codePoint">The code point of the character.</param>
        /// <returns>The glyph metrics for a known character.</returns>
        GlyphMetrics IFontInstance.GetGlyph(CodePoint codePoint)
        {
            bool foundGlyph = this.TryGetGlyphIndex(codePoint, out ushort idx);
            if (!foundGlyph)
            {
                idx = 0;
            }

            if (this.glyphCache[idx] is null)
            {
                this.glyphCache[idx] = this.CreateGlyphMetrics(codePoint, idx, foundGlyph ? GlyphType.Standard : GlyphType.Fallback);
            }

            return this.glyphCache[idx];
        }

        private GlyphMetrics CreateGlyphMetrics(CodePoint codePoint, ushort idx, GlyphType glyphType, ushort palleteIndex = 0)
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

            return new GlyphMetrics(this, codePoint, vector, advanceWidth, lsb, this.EmSize, idx, glyphType, color);
        }

        internal bool TryGetColoredVectors(CodePoint codePoint, ushort idx, [NotNullWhen(true)] out GlyphMetrics[]? vectors)
        {
            if (this.colrTable == null || this.colorGlyphCache == null)
            {
                vectors = null;
                return false;
            }

            vectors = this.colorGlyphCache[idx];
            if (vectors is null)
            {
                Span<LayerRecord> indexes = this.colrTable.GetLayers(idx);
                if (indexes.Length > 0)
                {
                    vectors = new GlyphMetrics[indexes.Length];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        LayerRecord? layer = indexes[i];

                        vectors[i] = this.CreateGlyphMetrics(codePoint, layer.GlyphId, GlyphType.ColrLayer, layer.PalletteIndex);
                    }
                }

                vectors ??= Array.Empty<GlyphMetrics>();
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
        Vector2 IFontInstance.GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
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
            HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>(); // hhea
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
            return new FontInstance(
                nameTable,
                cmap,
                glyphs,
                os2,
                hhea,
                horizontalMetrics,
                head,
                kern,
                colrTable,
                cpalTable);
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
            var reader = new BigEndianBinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var fonts = new FontInstance[(int)ttcHeader.NumFonts];

            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                fonts[i] = LoadFont(stream);
            }

            return fonts;
        }
    }
}

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
    /// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
    /// </summary>
    public class FontMetrics : IFontMetrics
    {
        private readonly CMapTable cmap;
        private readonly GlyphTable glyphs;
        private readonly HeadTable head;
        private readonly OS2Table os2;
        private readonly HorizontalMetricsTable horizontalMetrics;
        private readonly VerticalMetricsTable? verticalMetricsTable;
        private readonly GlyphMetrics[] glyphCache;
        private readonly GlyphMetrics[][]? colorGlyphCache;
        private readonly KerningTable kerning;
        private readonly ColrTable? colrTable;
        private readonly CpalTable? cpalTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontMetrics"/> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="cmap">The cmap table.</param>
        /// <param name="glyphs">The glyph table.</param>
        /// <param name="os2">The os2 table.</param>
        /// <param name="horizontalHeadTable">The horizontal head table.</param>
        /// <param name="horizontalMetrics">The horizontal metrics table.</param>
        /// <param name="verticalHeadTable">The vertical head table.</param>
        /// <param name="verticalMetrics">The vertical metrics table.</param>
        /// <param name="head">The head table.</param>
        /// <param name="kern">The kerning table.</param>
        /// <param name="colrTable">The COLR table</param>
        /// <param name="cpalTable">The CPAL table</param>
        internal FontMetrics(
            NameTable nameTable,
            CMapTable cmap,
            GlyphTable glyphs,
            OS2Table os2,
            HorizontalHeadTable horizontalHeadTable,
            HorizontalMetricsTable horizontalMetrics,
            VerticalHeadTable? verticalHeadTable,
            VerticalMetricsTable? verticalMetrics,
            HeadTable head,
            KerningTable kern,
            ColrTable? colrTable,
            CpalTable? cpalTable)
        {
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.verticalMetricsTable = verticalMetrics;
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
                this.LineHeight = (short)(this.Ascender - this.Descender + this.LineGap);
            }
            else
            {
                this.Ascender = horizontalHeadTable.Ascender;
                this.Descender = horizontalHeadTable.Descender;
                this.LineGap = horizontalHeadTable.LineGap;
                this.LineHeight = (short)(this.Ascender - this.Descender + this.LineGap);
            }

            if (this.Ascender == 0 || this.Descender == 0)
            {
                if (os2.TypoAscender != 0 || os2.TypoDescender != 0)
                {
                    this.Ascender = os2.TypoAscender;
                    this.Descender = os2.TypoDescender;
                    this.LineGap = os2.TypoLineGap;
                    this.LineHeight = (short)(this.Ascender - this.Descender + this.LineGap);
                }
                else
                {
                    this.Ascender = (short)os2.WinAscent;
                    this.Descender = (short)-os2.WinDescent;
                    this.LineHeight = (short)(this.Ascender - this.Descender);
                }
            }

            this.UnitsPerEm = this.head.UnitsPerEm;
            this.AdvanceWidthMax = (short)horizontalHeadTable.AdvanceWidthMax;
            this.AdvanceHeightMax = verticalHeadTable == null ? this.LineHeight : verticalHeadTable.AdvanceHeightMax;

            this.kerning = kern;
            this.colrTable = colrTable;
            this.cpalTable = cpalTable;
            this.Description = new FontDescription(nameTable, os2, head);
        }

        /// <inheritdoc/>
        public FontDescription Description { get; }

        /// <inheritdoc/>
        public ushort UnitsPerEm { get; }

        /// <inheritdoc/>
        public short Ascender { get; }

        /// <inheritdoc/>
        public short Descender { get; }

        /// <inheritdoc/>
        public short AdvanceWidthMax { get; }

        /// <inheritdoc/>
        public short AdvanceHeightMax { get; }

        /// <inheritdoc/>
        public short LineGap { get; }

        /// <inheritdoc/>
        public short LineHeight { get; }

        /// <inheritdoc/>
        public GlyphMetrics GetGlyphMetrics(CodePoint codePoint)
        {
            // TODO: Check this. It looks like we could potentially return the metrics
            // for the glyph at position zero when a matching codepoint cannot be found.
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

        /// <inheritdoc/>
        Vector2 IFontMetrics.GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
        {
            // We also want to wire int sub/super script offsetting into here too
            if (previousGlyph is null)
            {
                return Vector2.Zero;
            }

            // Once we wire in the kerning calculations this will return real data
            return this.kerning.GetOffset(previousGlyph.Index, glyph.Index);
        }

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FontMetrics LoadFont(string path)
        {
            using FileStream fs = File.OpenRead(path);
            var reader = new FontReader(fs);
            return LoadFont(reader);
        }

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="offset">Position in the stream to read the font from.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FontMetrics LoadFont(string path, long offset)
        {
            using FileStream fs = File.OpenRead(path);
            fs.Position = offset;
            return LoadFont(fs);
        }

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FontMetrics LoadFont(Stream stream)
        {
            var reader = new FontReader(stream);
            return LoadFont(reader);
        }

        internal static FontMetrics LoadFont(FontReader reader)
        {
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recomended order
            HeadTable head = reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
            HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>(); // hhea
            reader.GetTable<MaximumProfileTable>(); // maxp
            OS2Table os2 = reader.GetTable<OS2Table>(); // OS/2

            // LTSH - Linear threshold data
            // VDMX - Vertical device metrics
            // hdmx - Horizontal device metrics
            HorizontalMetricsTable horizontalMetrics = reader.GetTable<HorizontalMetricsTable>(); // hmtx

            VerticalHeadTable? vhea = reader.TryGetTable<VerticalHeadTable>();

            // Vertical metrics are optional. Supported by AAF fonts.
            VerticalMetricsTable? verticalMetrics = null;
            if (vhea != null)
            {
                verticalMetrics = reader.GetTable<VerticalMetricsTable>();
            }

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
            return new FontMetrics(
                nameTable,
                cmap,
                glyphs,
                os2,
                hhea,
                horizontalMetrics,
                vhea,
                verticalMetrics,
                head,
                kern,
                colrTable,
                cpalTable);
        }

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FontMetrics[] LoadFontCollection(string path)
        {
            using FileStream fs = File.OpenRead(path);
            return LoadFontCollection(fs);
        }

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FontMetrics[] LoadFontCollection(Stream stream)
        {
            long startPos = stream.Position;
            var reader = new BigEndianBinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var fonts = new FontMetrics[(int)ttcHeader.NumFonts];

            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                fonts[i] = LoadFont(stream);
            }

            return fonts;
        }

        internal bool TryGetGlyphIndex(CodePoint codePoint, out ushort glyphId)
            => this.cmap.TryGetGlyphId(codePoint, out glyphId);

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

        private GlyphMetrics CreateGlyphMetrics(CodePoint codePoint, ushort idx, GlyphType glyphType, ushort palleteIndex = 0)
        {
            ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(idx);
            short lsb = this.horizontalMetrics.GetLeftSideBearing(idx);

            ushort advancedHeight = 0;
            short tsb = 0;
            if (this.verticalMetricsTable != null)
            {
                advancedHeight = this.verticalMetricsTable.GetAdvancedHeight(idx);
                tsb = this.verticalMetricsTable.GetTopSideBearing(idx);
            }

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

            return new GlyphMetrics(
                this,
                codePoint,
                vector,
                advanceWidth,
                advancedHeight,
                lsb,
                tsb,
                this.UnitsPerEm,
                idx,
                glyphType,
                color);
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
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
        private readonly GlyphMetrics[][] glyphCache2;
        private readonly GlyphMetrics[][]? colorGlyphCache;
        private readonly KerningTable kerning;
        private readonly GSubTable? gSubTable;
        private readonly GPosTable? gPosTable;
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
        /// <param name="gSubTable">The glyph substitution table.</param>
        /// <param name="gPosTable">The glyph positioning table.</param>
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
            GSubTable? gSubTable,
            GPosTable? gPosTable,
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
            this.glyphCache2 = new GlyphMetrics[this.glyphs.GlyphCount][];
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

            // 72 * UnitsPerEm means 1pt = 1px
            this.ScaleFactor = this.UnitsPerEm * 72F;
            this.AdvanceWidthMax = (short)horizontalHeadTable.AdvanceWidthMax;
            this.AdvanceHeightMax = verticalHeadTable == null ? this.LineHeight : verticalHeadTable.AdvanceHeightMax;

            this.kerning = kern;
            this.gSubTable = gSubTable;
            this.gPosTable = gPosTable;
            this.colrTable = colrTable;
            this.cpalTable = cpalTable;
            this.Description = new FontDescription(nameTable, os2, head);
        }

        /// <inheritdoc/>
        public FontDescription Description { get; }

        /// <inheritdoc/>
        public ushort UnitsPerEm { get; }

        /// <inheritdoc/>
        public float ScaleFactor { get; }

        /// <inheritdoc/>
        public short Ascender { get; }

        /// <inheritdoc/>
        public short Descender { get; }

        /// <inheritdoc/>
        public short LineGap { get; }

        /// <inheritdoc/>
        public short LineHeight { get; }

        /// <inheritdoc/>
        public short AdvanceWidthMax { get; }

        /// <inheritdoc/>
        public short AdvanceHeightMax { get; }

        /// <inheritdoc/>
        public bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out int glyphId, out bool skipNextCodePoint)
        {
            if (this.cmap.TryGetGlyphId(codePoint, nextCodePoint, out ushort id, out skipNextCodePoint))
            {
                glyphId = id;
                return true;
            }

            glyphId = -1;
            return false;
        }

        /// <inheritdoc/>
        public GlyphMetrics GetGlyphMetrics(CodePoint codePoint)
        {
            bool foundGlyph = this.TryGetGlyphId(codePoint, out ushort idx);
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
        public IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, int glyphId, ColorFontSupport support)
        {
            GlyphType glyphType = GlyphType.Standard;
            if (glyphId < 0)
            {
                // A glyph was not found in this face for the previously matched
                // codepoint. Set to fallback.
                glyphId = 0;
                glyphType = GlyphType.Fallback;
            }

            if (support == ColorFontSupport.MicrosoftColrFormat
                && this.TryGetColoredVectors(codePoint, (ushort)glyphId, out GlyphMetrics[]? metrics))
            {
                return metrics;
            }

            if (this.glyphCache2[glyphId] is null)
            {
                this.glyphCache2[glyphId] = new[]
                {
                    this.CreateGlyphMetrics(
                    codePoint,
                    (ushort)glyphId,
                    glyphType)
                };
            }

            return this.glyphCache2[glyphId];
        }

        /// <inheritdoc/>
        public void ApplySubstitution(GlyphSubstitutionCollection collection)
        {
            if (this.gSubTable != null)
            {
                for (ushort index = 0; index < collection.Count; index++)
                {
                    this.gSubTable.ApplySubstitution(collection, index, collection.Count - index);
                }
            }
        }

        /// <inheritdoc/>
        public void UpdatePositions(GlyphPositioningCollection collection)
        {
            if (this.gPosTable != null)
            {
                for (ushort index = 0; index < collection.Count; index++)
                {
                    this.gPosTable.UpdatePositions(this, collection, index, collection.Count - index);
                }
            }

            // TODO: We should fall back to using the kerning table here.
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
            // recommended order
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

            // Advanced Typographics instructions.
            GSubTable? gSub = reader.TryGetTable<GSubTable>();
            GPosTable? gPos = reader.TryGetTable<GPosTable>();

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
                gSub,
                gPos,
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

        // TODO: This has to go.
        internal bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
            => this.cmap.TryGetGlyphId(codePoint, out glyphId);

        internal bool TryGetColoredVectors(CodePoint codePoint, ushort glyphId, [NotNullWhen(true)] out GlyphMetrics[]? vectors)
        {
            if (this.colrTable == null || this.colorGlyphCache == null)
            {
                vectors = null;
                return false;
            }

            vectors = this.colorGlyphCache[glyphId];
            if (vectors is null)
            {
                Span<LayerRecord> indexes = this.colrTable.GetLayers(glyphId);
                if (indexes.Length > 0)
                {
                    vectors = new GlyphMetrics[indexes.Length];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        LayerRecord? layer = indexes[i];

                        vectors[i] = this.CreateGlyphMetrics(codePoint, layer.GlyphId, GlyphType.ColrLayer, layer.PaletteIndex);
                    }
                }

                vectors ??= Array.Empty<GlyphMetrics>();
                this.colorGlyphCache[glyphId] = vectors;
            }

            return vectors.Length > 0;
        }

        private GlyphMetrics CreateGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            GlyphType glyphType,
            ushort palleteIndex = 0)
        {
            ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(glyphId);
            short lsb = this.horizontalMetrics.GetLeftSideBearing(glyphId);

            // Provide a default for the advance height. This is overwritten for vertical fonts.
            ushort advancedHeight = (ushort)(this.Ascender - this.Descender);
            short tsb = 0;
            if (this.verticalMetricsTable != null)
            {
                advancedHeight = this.verticalMetricsTable.GetAdvancedHeight(glyphId);
                tsb = this.verticalMetricsTable.GetTopSideBearing(glyphId);
            }

            GlyphVector vector = this.glyphs.GetGlyph(glyphId);
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
                glyphId,
                glyphType,
                color);
        }
    }
}

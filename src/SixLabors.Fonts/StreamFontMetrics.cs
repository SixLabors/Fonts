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
using SixLabors.Fonts.Tables.Hinting;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// <para>
    /// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
    /// </para>
    /// <para>The font source is a stream.</para>
    /// </summary>
    internal class StreamFontMetrics : FontMetrics
    {
        private readonly MaximumProfileTable maximumProfileTable;
        private readonly CMapTable cmap;
        private readonly GlyphTable glyphs;
        private readonly HeadTable head;
        private readonly OS2Table os2;
        private readonly HorizontalMetricsTable horizontalMetrics;
        private readonly VerticalMetricsTable? verticalMetricsTable;
        private readonly GlyphMetrics[][] glyphCache;
        private readonly GlyphMetrics[][]? colorGlyphCache;
        private readonly KerningTable kerningTable;
        private readonly GSubTable? gSubTable;
        private readonly GPosTable? gPosTable;
        private readonly ColrTable? colrTable;
        private readonly CpalTable? cpalTable;
        private readonly GlyphDefinitionTable? glyphDefinitionTable;
        private readonly FpgmTable? fpgm;
        private readonly CvtTable? cvt;
        private readonly PrepTable? prep;

        [ThreadStatic]
        private Hinting.Interpreter? interpreter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="maximumProfileTable">The maximum profile table.</param>
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
        /// <param name="fpgm">The font program table.</param>
        /// <param name="cvt">The control value table.</param>
        /// <param name="prep">The control value program table.</param>
        /// <param name="glyphDefinitionTable">The glyph definition table.</param>
        internal StreamFontMetrics(
            NameTable nameTable,
            MaximumProfileTable maximumProfileTable,
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
            CpalTable? cpalTable,
            FpgmTable? fpgm,
            CvtTable? cvt,
            PrepTable? prep,
            GlyphDefinitionTable? glyphDefinitionTable)
        {
            this.maximumProfileTable = maximumProfileTable;
            this.cmap = cmap;
            this.os2 = os2;
            this.glyphs = glyphs;
            this.horizontalMetrics = horizontalMetrics;
            this.verticalMetricsTable = verticalMetrics;
            this.head = head;
            this.glyphCache = new GlyphMetrics[this.glyphs.GlyphCount][];
            if (colrTable is not null)
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

            this.kerningTable = kern;
            this.gSubTable = gSubTable;
            this.gPosTable = gPosTable;
            this.colrTable = colrTable;
            this.cpalTable = cpalTable;
            this.fpgm = fpgm;
            this.cvt = cvt;
            this.prep = prep;
            this.glyphDefinitionTable = glyphDefinitionTable;
            this.Description = new FontDescription(nameTable, os2, head);
        }

        /// <inheritdoc/>
        public override FontDescription Description { get; }

        /// <inheritdoc/>
        public override ushort UnitsPerEm { get; }

        /// <inheritdoc/>
        public override float ScaleFactor { get; }

        /// <inheritdoc/>
        public override short Ascender { get; }

        /// <inheritdoc/>
        public override short Descender { get; }

        /// <inheritdoc/>
        public override short LineGap { get; }

        /// <inheritdoc/>
        public override short LineHeight { get; }

        /// <inheritdoc/>
        public override short AdvanceWidthMax { get; }

        /// <inheritdoc/>
        public override short AdvanceHeightMax { get; }

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
            => this.TryGetGlyphId(codePoint, null, out glyphId, out bool _);

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out ushort glyphId, out bool skipNextCodePoint)
            => this.cmap.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);

        /// <inheritdoc/>
        internal override bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
        {
            glyphClass = null;
            if (this.glyphDefinitionTable is not null && this.glyphDefinitionTable.TryGetGlyphClass(glyphId, out glyphClass))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        internal override bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
        {
            markAttachmentClass = null;
            if (this.glyphDefinitionTable is not null && this.glyphDefinitionTable.TryGetMarkAttachmentClass(glyphId, out markAttachmentClass))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ColorFontSupport support)
        {
            this.TryGetGlyphId(codePoint, out ushort glyphId);
            return this.GetGlyphMetrics(codePoint, glyphId, support);
        }

        /// <inheritdoc/>
        internal override IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ushort glyphId, ColorFontSupport support)
        {
            GlyphType glyphType = GlyphType.Standard;
            if (glyphId == 0)
            {
                // A glyph was not found in this face for the previously matched
                // codepoint. Set to fallback.
                glyphType = GlyphType.Fallback;
            }

            if (support == ColorFontSupport.MicrosoftColrFormat
                && this.TryGetColoredVectors(codePoint, glyphId, out GlyphMetrics[]? metrics))
            {
                return metrics;
            }

            if (this.glyphCache[glyphId] is null)
            {
                this.glyphCache[glyphId] = new[]
                {
                    this.CreateGlyphMetrics(
                    codePoint,
                    glyphId,
                    glyphType)
                };
            }

            return this.glyphCache[glyphId];
        }

        /// <inheritdoc/>
        internal override void ApplySubstitution(GlyphSubstitutionCollection collection)
        {
            if (this.gSubTable != null)
            {
                this.gSubTable.ApplySubstitution(this, collection);
            }
        }

        /// <inheritdoc/>
        internal override void UpdatePositions(GlyphPositioningCollection collection)
        {
            bool updated = false;
            if (this.gPosTable != null)
            {
                updated = this.gPosTable.TryUpdatePositions(this, collection);
            }

            if (!updated && this.kerningTable != null)
            {
                for (ushort index = 1; index < collection.Count; index++)
                {
                    this.kerningTable.UpdatePositions(this, collection, (ushort)(index - 1), index);
                }
            }
        }

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
        public static StreamFontMetrics LoadFont(string path)
        {
            using FileStream fs = File.OpenRead(path);
            var reader = new FontReader(fs);
            return LoadFont(reader);
        }

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="offset">Position in the stream to read the font from.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
        public static StreamFontMetrics LoadFont(string path, long offset)
        {
            using FileStream fs = File.OpenRead(path);
            fs.Position = offset;
            return LoadFont(fs);
        }

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
        public static StreamFontMetrics LoadFont(Stream stream)
        {
            var reader = new FontReader(stream);
            return LoadFont(reader);
        }

        internal static StreamFontMetrics LoadFont(FontReader reader)
        {
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // recommended order
            HeadTable head = reader.GetTable<HeadTable>(); // head - not saving but loading in suggested order
            HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>(); // hhea
            MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>(); // maxp
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

            FpgmTable? fpgm = reader.TryGetTable<FpgmTable>(); // fpgm - Font Program
            PrepTable? prep = reader.TryGetTable<PrepTable>(); // prep -  Control Value Program
            CvtTable? cvt = reader.TryGetTable<CvtTable>(); // cvt  - Control Value Table

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
            GlyphDefinitionTable? glyphDefinitionTable = reader.TryGetTable<GlyphDefinitionTable>();

            // post - PostScript information
            // gasp - Grid-fitting/Scan-conversion (optional table)
            // PCLT - PCL 5 data
            // DSIG - Digital signature
            return new StreamFontMetrics(
                nameTable,
                maxp,
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
                cpalTable,
                fpgm,
                cvt,
                prep,
                glyphDefinitionTable);
        }

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
        public static StreamFontMetrics[] LoadFontCollection(string path)
        {
            using FileStream fs = File.OpenRead(path);
            return LoadFontCollection(fs);
        }

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
        public static StreamFontMetrics[] LoadFontCollection(Stream stream)
        {
            long startPos = stream.Position;
            var reader = new BigEndianBinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var fonts = new StreamFontMetrics[(int)ttcHeader.NumFonts];

            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                fonts[i] = LoadFont(stream);
            }

            return fonts;
        }

        internal GlyphVector ApplyHinting(GlyphVector glyphVector, float pixelSize, ushort glyphIndex)
        {
            if (this.interpreter == null)
            {
                this.interpreter = new Hinting.Interpreter(
                    this.maximumProfileTable.MaxStackElements,
                    this.maximumProfileTable.MaxStorage,
                    this.maximumProfileTable.MaxFunctionDefs,
                    this.maximumProfileTable.MaxInstructionDefs,
                    this.maximumProfileTable.MaxTwilightPoints);

                if (this.fpgm != null)
                {
                    this.interpreter.InitializeFunctionDefs(this.fpgm.Instructions);
                }
            }

            float scale = pixelSize / this.UnitsPerEm;
            this.interpreter.SetControlValueTable(this.cvt?.ControlValues, scale, pixelSize, this.prep?.Instructions);

            Bounds bounds = glyphVector.GetBounds();
            short leftSideBearing = this.horizontalMetrics.GetLeftSideBearing(glyphIndex);
            ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(glyphIndex);
            short topSideBearing = this.verticalMetricsTable?.GetTopSideBearing(glyphIndex) ?? (short)(this.Ascender - bounds.Max.Y);
            ushort advanceHeight = this.verticalMetricsTable?.GetAdvancedHeight(glyphIndex) ?? (ushort)this.LineHeight;

            var pp1 = new Vector2(bounds.Min.X - (leftSideBearing * scale), 0);
            var pp2 = new Vector2(pp1.X + (advanceWidth * scale), 0);
            var pp3 = new Vector2(0, bounds.Max.Y + (topSideBearing * scale));
            var pp4 = new Vector2(0, pp3.Y - (advanceHeight * scale));

            GlyphVector.Hint(ref glyphVector, this.interpreter, pp1, pp2, pp3, pp4);

            return glyphVector;
        }

        private bool TryGetColoredVectors(CodePoint codePoint, ushort glyphId, [NotNullWhen(true)] out GlyphMetrics[]? vectors)
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
            GlyphVector vector = this.glyphs.GetGlyph(glyphId);
            Bounds bounds = vector.GetBounds();
            ushort advanceWidth = this.horizontalMetrics.GetAdvancedWidth(glyphId);
            short lsb = this.horizontalMetrics.GetLeftSideBearing(glyphId);

            // Provide a default for the advance height. This is overwritten for vertical fonts.
            ushort advancedHeight = (ushort)(this.Ascender - this.Descender);
            short tsb = (short)(this.Ascender - bounds.Max.Y);
            if (this.verticalMetricsTable != null)
            {
                advancedHeight = this.verticalMetricsTable.GetAdvancedHeight(glyphId);
                tsb = this.verticalMetricsTable.GetTopSideBearing(glyphId);
            }

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

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Tables.Hinting;
using SixLabors.Fonts.Tables.TrueType;
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
        private readonly TrueTypeFontTables? trueTypeFontTables;
        private readonly CompactFontTables? compactFontTables;
        private readonly OutlineType outlineType;

        // https://docs.microsoft.com/en-us/typography/opentype/spec/otff#font-tables
        private readonly GlyphMetrics[][] glyphCache;
        private readonly GlyphMetrics[][]? colorGlyphCache;
        private readonly FontDescription description;
        private ushort unitsPerEm;
        private float scaleFactor;
        private short ascender;
        private short descender;
        private short lineGap;
        private short lineHeight;
        private short advanceWidthMax;
        private short advanceHeightMax;
        private short subscriptXSize;
        private short subscriptYSize;
        private short subscriptXOffset;
        private short subscriptYOffset;
        private short superscriptXSize;
        private short superscriptYSize;
        private short superscriptXOffset;
        private short superscriptYOffset;
        private short strikeoutSize;
        private short strikeoutPosition;
        private short underlinePosition;
        private short underlineThickness;
        private float italicAngle;

        [ThreadStatic]
        private Hinting.Interpreter? interpreter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
        /// </summary>
        /// <param name="tables">The True Type font tables.</param>
        internal StreamFontMetrics(TrueTypeFontTables tables)
        {
            this.trueTypeFontTables = tables;
            this.outlineType = OutlineType.TrueType;
            this.description = new FontDescription(tables.Name, tables.Os2, tables.Head);
            this.glyphCache = new GlyphMetrics[tables.Glyf.GlyphCount][];
            if (tables.Colr is not null)
            {
                this.glyphCache = new GlyphMetrics[tables.Glyf.GlyphCount][];
            }

            this.Initialize(ref tables);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
        /// </summary>
        /// <param name="tables">The Compact Fton tables.</param>
        internal StreamFontMetrics(CompactFontTables tables)
        {
            this.compactFontTables = tables;
            this.outlineType = OutlineType.CFF;
            this.description = new FontDescription(tables.Name, tables.Os2, tables.Head);

            // TODO: Glyphcaches.
            this.glyphCache = new GlyphMetrics[tables.Cff.Cff1FontSet._fonts[0]._glyphs.Length][];

            this.Initialize(ref tables);
        }

        public HeadTable.HeadFlags HeadFlags { get; private set; }

        /// <inheritdoc/>
        public override FontDescription Description => this.description;

        /// <inheritdoc/>
        public override ushort UnitsPerEm => this.unitsPerEm;

        /// <inheritdoc/>
        public override float ScaleFactor => this.scaleFactor;

        /// <inheritdoc/>
        public override short Ascender => this.ascender;

        /// <inheritdoc/>
        public override short Descender => this.descender;

        /// <inheritdoc/>
        public override short LineGap => this.lineGap;

        /// <inheritdoc/>
        public override short LineHeight => this.lineHeight;

        /// <inheritdoc/>
        public override short AdvanceWidthMax => this.advanceWidthMax;

        /// <inheritdoc/>
        public override short AdvanceHeightMax => this.advanceHeightMax;

        /// <inheritdoc/>
        public override short SubscriptXSize => this.subscriptXSize;

        /// <inheritdoc/>
        public override short SubscriptYSize => this.subscriptYSize;

        /// <inheritdoc/>
        public override short SubscriptXOffset => this.subscriptXOffset;

        /// <inheritdoc/>
        public override short SubscriptYOffset => this.subscriptYOffset;

        /// <inheritdoc/>
        public override short SuperscriptXSize => this.superscriptXSize;

        /// <inheritdoc/>
        public override short SuperscriptYSize => this.superscriptYSize;

        /// <inheritdoc/>
        public override short SuperscriptXOffset => this.superscriptXOffset;

        /// <inheritdoc/>
        public override short SuperscriptYOffset => this.superscriptYOffset;

        /// <inheritdoc/>
        public override short StrikeoutSize => this.strikeoutSize;

        /// <inheritdoc/>
        public override short StrikeoutPosition => this.strikeoutPosition;

        /// <inheritdoc/>
        public override short UnderlinePosition => this.underlinePosition;

        /// <inheritdoc/>
        public override short UnderlineThickness => this.underlineThickness;

        /// <inheritdoc/>
        public override float ItalicAngle => this.italicAngle;

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
            => this.TryGetGlyphId(codePoint, null, out glyphId, out bool _);

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out ushort glyphId, out bool skipNextCodePoint)
        {
            CMapTable cmap = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Value.Cmap
                : this.compactFontTables!.Value.Cmap;

            return cmap.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);
        }

        /// <inheritdoc/>
        internal override bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
        {
            GlyphDefinitionTable? gdef = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Value.Gdef
                : this.compactFontTables!.Value.Gdef;

            glyphClass = null;
            return gdef is not null && gdef.TryGetGlyphClass(glyphId, out glyphClass);
        }

        /// <inheritdoc/>
        internal override bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
        {
            GlyphDefinitionTable? gdef = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Value.Gdef
                : this.compactFontTables!.Value.Gdef;

            markAttachmentClass = null;
            return gdef is not null && gdef.TryGetMarkAttachmentClass(glyphId, out markAttachmentClass);
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
                && this.TryGetColoredMetrics(codePoint, glyphId, out GlyphMetrics[]? metrics))
            {
                return metrics;
            }

            // We overwrite the cache entry for this type should the attributes change.
            GlyphMetrics[]? cached = this.glyphCache[glyphId];
            if (cached is null)
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
            GSubTable? gsub = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Value.GSub
                : this.compactFontTables!.Value.GSub;

            gsub?.ApplySubstitution(this, collection);
        }

        /// <inheritdoc/>
        internal override void UpdatePositions(GlyphPositioningCollection collection)
        {
            bool isTTF = this.outlineType == OutlineType.TrueType;
            GPosTable? gpos = isTTF
                ? this.trueTypeFontTables!.Value.GPos
                : this.compactFontTables!.Value.GPos;

            bool kerned = false;
            KerningMode kerningMode = collection.TextOptions.KerningMode;
            gpos?.TryUpdatePositions(this, collection, out kerned);

            if (!kerned && kerningMode != KerningMode.None)
            {
                KerningTable? kern = isTTF
                    ? this.trueTypeFontTables!.Value.Kern
                    : this.compactFontTables!.Value.Kern;

                if (kern != null)
                {
                    for (ushort index = 1; index < collection.Count; index++)
                    {
                        kern.UpdatePositions(this, collection, (ushort)(index - 1), index);
                    }
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
            if (reader.OutlineType == OutlineType.TrueType)
            {
                return LoadTrueTypeFont(reader);
            }
            else
            {
                return LoadCompactFont(reader);
            }
        }

        private void Initialize<T>(ref T tables)
            where T : IFontTables
        {
            HeadTable head = tables.Head;
            HorizontalHeadTable hhea = tables.Hhea;
            VerticalHeadTable? vhea = tables.Vhea;
            OS2Table os2 = tables.Os2;
            PostTable post = tables.Post;

            this.HeadFlags = head.Flags;

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
                this.ascender = os2.TypoAscender;
                this.descender = os2.TypoDescender;
                this.lineGap = os2.TypoLineGap;
                this.lineHeight = (short)(this.ascender - this.descender + this.lineGap);
            }
            else
            {
                this.ascender = hhea.Ascender;
                this.descender = hhea.Descender;
                this.lineGap = hhea.LineGap;
                this.lineHeight = (short)(this.ascender - this.descender + this.lineGap);
            }

            if (this.ascender == 0 || this.descender == 0)
            {
                if (os2.TypoAscender != 0 || os2.TypoDescender != 0)
                {
                    this.ascender = os2.TypoAscender;
                    this.descender = os2.TypoDescender;
                    this.lineGap = os2.TypoLineGap;
                    this.lineHeight = (short)(this.ascender - this.descender + this.lineGap);
                }
                else
                {
                    this.ascender = (short)os2.WinAscent;
                    this.descender = (short)-os2.WinDescent;
                    this.lineHeight = (short)(this.ascender - this.descender);
                }
            }

            this.unitsPerEm = head.UnitsPerEm;

            // 72 * UnitsPerEm means 1pt = 1px
            this.scaleFactor = this.unitsPerEm * 72F;
            this.advanceWidthMax = (short)hhea.AdvanceWidthMax;
            this.advanceHeightMax = vhea == null ? this.LineHeight : vhea.AdvanceHeightMax;

            this.subscriptXSize = os2.SubscriptXSize;
            this.subscriptYSize = os2.SubscriptYSize;
            this.subscriptXOffset = os2.SubscriptXOffset;
            this.subscriptYOffset = os2.SubscriptYOffset;
            this.superscriptXSize = os2.SuperscriptXSize;
            this.superscriptYSize = os2.SuperscriptYSize;
            this.superscriptXOffset = os2.SuperscriptXOffset;
            this.superscriptYOffset = os2.SuperscriptYOffset;
            this.strikeoutSize = os2.StrikeoutSize;
            this.strikeoutPosition = os2.StrikeoutPosition;

            this.underlinePosition = post.UnderlinePosition;
            this.underlineThickness = post.UnderlineThickness;
            this.italicAngle = post.ItalicAngle;
        }

        private static StreamFontMetrics LoadTrueTypeFont(FontReader reader)
        {
            // Load using recommended order for best performance.
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // 'head', 'hhea', 'maxp', OS/2, 'hmtx', LTSH, VDMX, 'hdmx', 'cmap', 'fpgm', 'prep', 'cvt ', 'loca', 'glyf', 'kern', 'name', 'post', 'gasp', PCLT, DSIG
            TrueTypeFontTables tables = default;
            tables.Head = reader.GetTable<HeadTable>();
            tables.Hhea = reader.GetTable<HorizontalHeadTable>();
            tables.Maxp = reader.GetTable<MaximumProfileTable>();
            tables.Os2 = reader.GetTable<OS2Table>();
            tables.Htmx = reader.GetTable<HorizontalMetricsTable>();
            tables.Cmap = reader.GetTable<CMapTable>();
            tables.Fpgm = reader.TryGetTable<FpgmTable>();
            tables.Prep = reader.TryGetTable<PrepTable>();
            tables.Cvt = reader.TryGetTable<CvtTable>();
            tables.Loca = reader.GetTable<IndexLocationTable>();
            tables.Glyf = reader.GetTable<GlyphTable>();
            tables.Kern = reader.TryGetTable<KerningTable>();
            tables.Name = reader.GetTable<NameTable>();
            tables.Post = reader.GetTable<PostTable>();

            tables.Vhea = reader.TryGetTable<VerticalHeadTable>();
            tables.Vmtx = reader.TryGetTable<VerticalMetricsTable>();

            tables.Gdef = reader.TryGetTable<GlyphDefinitionTable>();
            tables.GSub = reader.TryGetTable<GSubTable>();
            tables.GPos = reader.TryGetTable<GPosTable>();

            tables.Colr = reader.TryGetTable<ColrTable>();
            tables.Cpal = reader.TryGetTable<CpalTable>();

            return new StreamFontMetrics(tables);
        }

        private static StreamFontMetrics LoadCompactFont(FontReader reader)
        {
            // Load using recommended order for best performance.
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // 'head', 'hhea', 'maxp', OS/2, 'name', 'cmap', 'post', 'CFF '
            CompactFontTables tables = default;
            tables.Head = reader.GetTable<HeadTable>();
            tables.Hhea = reader.GetTable<HorizontalHeadTable>();
            tables.Maxp = reader.GetTable<MaximumProfileTable>();
            tables.Os2 = reader.GetTable<OS2Table>();
            tables.Name = reader.GetTable<NameTable>();
            tables.Cmap = reader.GetTable<CMapTable>();
            tables.Post = reader.GetTable<PostTable>();
            tables.Cff = reader.GetTable<CffTable>(); // TODO: CFF2, VORG

            tables.Htmx = reader.GetTable<HorizontalMetricsTable>();
            tables.Vhea = reader.TryGetTable<VerticalHeadTable>();
            tables.Vmtx = reader.TryGetTable<VerticalMetricsTable>();
            tables.Kern = reader.TryGetTable<KerningTable>();

            tables.Gdef = reader.TryGetTable<GlyphDefinitionTable>();
            tables.GSub = reader.TryGetTable<GSubTable>();
            tables.GPos = reader.TryGetTable<GPosTable>();

            tables.Colr = reader.TryGetTable<ColrTable>();
            tables.Cpal = reader.TryGetTable<CpalTable>();

            return new StreamFontMetrics(tables);
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

        internal void ApplyHinting(HintingMode hintingMode, GlyphMetrics metrics, ref GlyphVector glyphVector, Vector2 scaleXY, float scaledPPEM)
        {
            if (hintingMode == HintingMode.None)
            {
                return;
            }

            bool isTTF = this.outlineType == OutlineType.TrueType;
            if (this.interpreter == null)
            {
                MaximumProfileTable maxp = isTTF
                    ? this.trueTypeFontTables!.Value.Maxp
                    : this.compactFontTables!.Value.Maxp;

                this.interpreter = new Hinting.Interpreter(
                    maxp.MaxStackElements,
                    maxp.MaxStorage,
                    maxp.MaxFunctionDefs,
                    maxp.MaxInstructionDefs,
                    maxp.MaxTwilightPoints);

                FpgmTable? fpgm = isTTF
                    ? this.trueTypeFontTables!.Value.Fpgm
                    : null;

                if (fpgm != null)
                {
                    this.interpreter.InitializeFunctionDefs(fpgm.Instructions);
                }
            }

            CvtTable? cvt = null;
            PrepTable? prep = null;
            if (isTTF)
            {
                cvt = this.trueTypeFontTables!.Value.Cvt;
                prep = this.trueTypeFontTables!.Value.Prep;
            }

            float scaleFactor = scaledPPEM / this.UnitsPerEm;
            this.interpreter.SetControlValueTable(cvt?.ControlValues, scaleFactor, scaledPPEM, prep?.Instructions);

            Bounds bounds = glyphVector.GetBounds();

            var pp1 = new Vector2(bounds.Min.X - (metrics.LeftSideBearing * scaleXY.X), 0);
            var pp2 = new Vector2(pp1.X + (metrics.AdvanceWidth * scaleXY.X), 0);
            var pp3 = new Vector2(0, bounds.Max.Y + (metrics.TopSideBearing * scaleXY.Y));
            var pp4 = new Vector2(0, pp3.Y - (metrics.AdvanceHeight * scaleXY.Y));

            GlyphVector.Hint(hintingMode, ref glyphVector, this.interpreter, pp1, pp2, pp3, pp4);
        }

        private bool TryGetColoredMetrics(CodePoint codePoint, ushort glyphId, [NotNullWhen(true)] out GlyphMetrics[]? metrics)
        {
            ColrTable? colr = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Value.Colr
                : this.compactFontTables!.Value.Colr;

            if (colr == null || this.colorGlyphCache == null)
            {
                metrics = null;
                return false;
            }

            // We overwrite the cache entry for this type should the attributes change.
            metrics = this.colorGlyphCache[glyphId];
            if (metrics is null)
            {
                Span<LayerRecord> indexes = colr.GetLayers(glyphId);
                if (indexes.Length > 0)
                {
                    metrics = new GlyphMetrics[indexes.Length];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        LayerRecord? layer = indexes[i];

                        metrics[i] = this.CreateGlyphMetrics(codePoint, layer.GlyphId, GlyphType.ColrLayer, layer.PaletteIndex);
                    }
                }

                metrics ??= Array.Empty<GlyphMetrics>();
                this.colorGlyphCache[glyphId] = metrics;
            }

            return metrics.Length > 0;
        }

        private GlyphMetrics CreateGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            GlyphType glyphType,
            ushort palleteIndex = 0)
        {
            bool isTTF = this.outlineType == OutlineType.TrueType;
            if (!isTTF)
            {
                // TODO: Implement
                throw new NotImplementedException("TTF Only!!");
            }

            GlyphTable glyf = this.trueTypeFontTables!.Value.Glyf;
            HorizontalMetricsTable htmx = isTTF
                ? this.trueTypeFontTables!.Value.Htmx
                : this.compactFontTables!.Value.Htmx;
            VerticalMetricsTable? vtmx = isTTF
                ? this.trueTypeFontTables!.Value.Vmtx
                : this.compactFontTables!.Value.Vmtx;

            GlyphVector vector = glyf.GetGlyph(glyphId);
            Bounds bounds = vector.GetBounds();
            ushort advanceWidth = htmx.GetAdvancedWidth(glyphId);
            short lsb = htmx.GetLeftSideBearing(glyphId);

            // Provide a default for the advance height. This is overwritten for vertical fonts.
            ushort advancedHeight = (ushort)(this.Ascender - this.Descender);
            short tsb = (short)(this.Ascender - bounds.Max.Y);
            if (vtmx != null)
            {
                advancedHeight = vtmx.GetAdvancedHeight(glyphId);
                tsb = vtmx.GetTopSideBearing(glyphId);
            }

            GlyphColor? color = null;
            if (glyphType == GlyphType.ColrLayer)
            {
                // 0xFFFF is special index meaning use foreground color and thus leave unset
                if (palleteIndex != 0xFFFF)
                {
                    CpalTable? cpal = isTTF
                        ? this.trueTypeFontTables!.Value.Cpal
                        : this.compactFontTables!.Value.Cpal;

                    color = cpal?.GetGlyphColor(0, palleteIndex);
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

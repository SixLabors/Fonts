// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Post;
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
    internal partial class StreamFontMetrics : FontMetrics
    {
        private readonly TrueTypeFontTables? trueTypeFontTables;
        private readonly CompactFontTables? compactFontTables;
        private readonly OutlineType outlineType;

        // https://docs.microsoft.com/en-us/typography/opentype/spec/otff#font-tables
        private readonly ConcurrentDictionary<ushort, GlyphMetrics[]> glyphCache;
        private readonly ConcurrentDictionary<ushort, GlyphMetrics[]>? colorGlyphCache;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
        /// </summary>
        /// <param name="tables">The True Type font tables.</param>
        internal StreamFontMetrics(TrueTypeFontTables tables)
        {
            this.trueTypeFontTables = tables;
            this.outlineType = OutlineType.TrueType;
            this.description = new FontDescription(tables.Name, tables.Os2, tables.Head);
            this.glyphCache = new();
            if (tables.Colr is not null)
            {
                this.colorGlyphCache = new();
            }

            this.Initialize(tables);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
        /// </summary>
        /// <param name="tables">The Compact Font tables.</param>
        internal StreamFontMetrics(CompactFontTables tables)
        {
            this.compactFontTables = tables;
            this.outlineType = OutlineType.CFF;
            this.description = new FontDescription(tables.Name, tables.Os2, tables.Head);
            this.glyphCache = new();
            if (tables.Colr is not null)
            {
                this.colorGlyphCache = new();
            }

            this.Initialize(tables);
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
                ? this.trueTypeFontTables!.Cmap
                : this.compactFontTables!.Cmap;

            return cmap.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);
        }

        /// <inheritdoc/>
        internal override bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
        {
            GlyphDefinitionTable? gdef = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Gdef
                : this.compactFontTables!.Gdef;

            glyphClass = null;
            return gdef is not null && gdef.TryGetGlyphClass(glyphId, out glyphClass);
        }

        /// <inheritdoc/>
        internal override bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
        {
            GlyphDefinitionTable? gdef = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Gdef
                : this.compactFontTables!.Gdef;

            markAttachmentClass = null;
            return gdef is not null && gdef.TryGetMarkAttachmentClass(glyphId, out markAttachmentClass);
        }

        /// <inheritdoc/>
        public override bool TryGetGlyphMetrics(
            CodePoint codePoint,
            TextAttributes textAttributes,
            TextDecorations textDecorations,
            ColorFontSupport support,
            [NotNullWhen(true)] out IReadOnlyList<GlyphMetrics>? metrics)
        {
            // We return metrics for the special glyph representing a missing character, commonly known as .notdef.
            this.TryGetGlyphId(codePoint, out ushort glyphId);
            metrics = this.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, support);
            return metrics.Any();
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<GlyphMetrics> GetGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            TextAttributes textAttributes,
            TextDecorations textDecorations,
            ColorFontSupport support)
        {
            GlyphType glyphType = GlyphType.Standard;
            if (glyphId == 0)
            {
                // A glyph was not found in this face for the previously matched
                // codepoint. Set to fallback.
                glyphType = GlyphType.Fallback;
            }

            if (support == ColorFontSupport.MicrosoftColrFormat
                && this.TryGetColoredMetrics(codePoint, glyphId, textAttributes, textDecorations, out GlyphMetrics[]? metrics))
            {
                return metrics;
            }

            // We overwrite the cache entry for this type should the attributes change.
            return this.glyphCache.GetOrAdd(
                  glyphId,
                  id => new[]
                  {
                    this.CreateGlyphMetrics(
                    codePoint,
                    id,
                    glyphType,
                    textAttributes,
                    textDecorations)
                  });
        }

        /// <inheritdoc />
        internal override IReadOnlyList<CodePoint> GetAvailableCodePoints()
        {
            CMapTable cmap = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Cmap
                : this.compactFontTables!.Cmap;

            return cmap.GetAvailableCodePoints();
        }

        /// <inheritdoc/>
        internal override void ApplySubstitution(GlyphSubstitutionCollection collection)
        {
            GSubTable? gsub = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.GSub
                : this.compactFontTables!.GSub;

            gsub?.ApplySubstitution(this, collection);
        }

        /// <inheritdoc/>
        internal override bool TryGetKerningOffset(ushort previousId, ushort currentId, out Vector2 vector)
        {
            bool isTTF = this.outlineType == OutlineType.TrueType;
            KerningTable? kern = isTTF
                ? this.trueTypeFontTables!.Kern
                : this.compactFontTables!.Kern;

            if (kern is null)
            {
                vector = default;
                return false;
            }

            return kern.TryGetKerningOffset(previousId, currentId, out vector);
        }

        /// <inheritdoc/>
        internal override void UpdatePositions(GlyphPositioningCollection collection)
        {
            bool isTTF = this.outlineType == OutlineType.TrueType;
            GPosTable? gpos = isTTF
                ? this.trueTypeFontTables!.GPos
                : this.compactFontTables!.GPos;

            bool kerned = false;
            KerningMode kerningMode = collection.TextOptions.KerningMode;

            gpos?.TryUpdatePositions(this, collection, out kerned);

            if (!kerned && kerningMode != KerningMode.None)
            {
                KerningTable? kern = isTTF
                    ? this.trueTypeFontTables!.Kern
                    : this.compactFontTables!.Kern;

                if (kern?.Count > 0)
                {
                    // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
                    int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
                    for (int index = 1; index < collection.Count; index++)
                    {
                        if (index >= maxCount)
                        {
                            break;
                        }

                        kern.UpdatePositions(this, collection, index - 1, index);
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

        private void Initialize<T>(T tables)
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

        private bool TryGetColoredMetrics(
            CodePoint codePoint,
            ushort glyphId,
            TextAttributes textAttributes,
            TextDecorations textDecorations,
            [NotNullWhen(true)] out GlyphMetrics[]? metrics)
        {
            ColrTable? colr = this.outlineType == OutlineType.TrueType
                ? this.trueTypeFontTables!.Colr
                : this.compactFontTables!.Colr;

            if (colr == null || this.colorGlyphCache == null)
            {
                metrics = null;
                return false;
            }

            // We overwrite the cache entry for this type should the attributes change.
            metrics = this.colorGlyphCache.GetOrAdd(glyphId, id =>
            {
                GlyphMetrics[] m = Array.Empty<GlyphMetrics>();
                Span<LayerRecord> indexes = colr.GetLayers(id);
                if (indexes.Length > 0)
                {
                    m = new GlyphMetrics[indexes.Length];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        LayerRecord layer = indexes[i];
                        m[i] = this.CreateGlyphMetrics(codePoint, layer.GlyphId, GlyphType.ColrLayer, textAttributes, textDecorations, layer.PaletteIndex);
                    }
                }

                return m;
            });

            return metrics.Length > 0;
        }

        private GlyphMetrics CreateGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            GlyphType glyphType,
            TextAttributes textAttributes,
            TextDecorations textDecorations,
            ushort palleteIndex = 0)
            => this.outlineType switch
            {
                OutlineType.TrueType => this.CreateTrueTypeGlyphMetrics(codePoint, glyphId, glyphType, textAttributes, textDecorations, palleteIndex),
                OutlineType.CFF => this.CreateCffGlyphMetrics(codePoint, glyphId, glyphType, textAttributes, textDecorations, palleteIndex),
                _ => throw new NotSupportedException(),
            };
    }
}

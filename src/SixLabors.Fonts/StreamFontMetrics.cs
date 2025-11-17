// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

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
    private readonly ConcurrentDictionary<(int CodePoint, ushort Id, TextAttributes Attributes, ColorFontSupport ColorSupport, bool IsVerticalLayout), GlyphMetrics> glyphCache;
    private readonly ConcurrentDictionary<(int CodePoint, int NextCodePoint), (bool Success, ushort GlyphId, bool SkipNextCodePoint)> glyphIdCache;
    private readonly ConcurrentDictionary<ushort, (bool Success, CodePoint CodePoint)> codePointCache;
    private readonly FontDescription description;
    private readonly HorizontalMetrics horizontalMetrics;
    private readonly VerticalMetrics verticalMetrics;
    private ushort unitsPerEm;
    private float scaleFactor;
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
        this.glyphIdCache = new();
        this.codePointCache = new();
        this.glyphCache = new();

        (HorizontalMetrics HorizontalMetrics, VerticalMetrics VerticalMetrics) metrics = this.Initialize(tables);
        this.horizontalMetrics = metrics.HorizontalMetrics;
        this.verticalMetrics = metrics.VerticalMetrics;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamFontMetrics"/> class.
    /// </summary>
    /// <param name="tables">The Compact Font tables.</param>
    /// <param name="glyphVariationProcessor">Processor which handles glyph variations.</param>
    internal StreamFontMetrics(CompactFontTables tables, GlyphVariationProcessor? glyphVariationProcessor = null)
    {
        this.compactFontTables = tables;
        this.outlineType = OutlineType.CFF;
        this.description = new FontDescription(tables.Name, tables.Os2, tables.Head);
        this.GlyphVariationProcessor = glyphVariationProcessor;
        this.glyphIdCache = new();
        this.codePointCache = new();
        this.glyphCache = new();

        (HorizontalMetrics HorizontalMetrics, VerticalMetrics VerticalMetrics) metrics = this.Initialize(tables);
        this.horizontalMetrics = metrics.HorizontalMetrics;
        this.verticalMetrics = metrics.VerticalMetrics;
    }

    public HeadTable.HeadFlags HeadFlags { get; private set; }

    public GlyphVariationProcessor? GlyphVariationProcessor { get; private set; }

    /// <inheritdoc/>
    public override FontDescription Description => this.description;

    /// <inheritdoc/>
    public override ushort UnitsPerEm => this.unitsPerEm;

    /// <inheritdoc/>
    public override float ScaleFactor => this.scaleFactor;

    /// <inheritdoc/>
    public override HorizontalMetrics HorizontalMetrics => this.horizontalMetrics;

    /// <inheritdoc/>
    public override VerticalMetrics VerticalMetrics => this.verticalMetrics;

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

        (bool success, ushort id, bool skip) = this.glyphIdCache.GetOrAdd(
                       (codePoint.Value, nextCodePoint?.Value ?? -1),
                       static (_, arg) =>
                       {
                           bool success = arg.cmap.TryGetGlyphId(arg.codePoint, arg.nextCodePoint, out ushort id, out bool skip);
                           return (success, id, skip);
                       },
                       (cmap, codePoint, nextCodePoint));

        glyphId = id;
        skipNextCodePoint = skip;
        return success;
    }

    /// <inheritdoc/>
    internal override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        CMapTable cmap = this.outlineType == OutlineType.TrueType
            ? this.trueTypeFontTables!.Cmap
            : this.compactFontTables!.Cmap;

        (bool success, CodePoint value) = this.codePointCache.GetOrAdd(
            glyphId,
            static (glyphId, arg) =>
            {
                bool success = arg.TryGetCodePoint(glyphId, out CodePoint codePoint);
                return (success, codePoint);
            },
            cmap);

        codePoint = value;
        return success;
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
    public override bool TryGetVariationAxes(out VariationAxis[]? variationAxes)
    {
        if (this.trueTypeFontTables?.Fvar == null)
        {
            variationAxes = Array.Empty<VariationAxis>();
            return false;
        }

        FVarTable? fvar = this.trueTypeFontTables?.Fvar;
        Tables.General.Name.NameTable? names = this.trueTypeFontTables?.Name;
        variationAxes = new VariationAxis[fvar!.Axes.Length];
        for (int i = 0; i < fvar.Axes.Length; i++)
        {
            VariationAxisRecord axis = fvar.Axes[i];
            string name = names != null ? names.GetNameById(CultureInfo.InvariantCulture, axis.AxisNameId) : string.Empty;
            variationAxes[i] = new VariationAxis()
            {
                Tag = axis.Tag,
                Min = axis.MinValue,
                Max = axis.MaxValue,
                Default = axis.DefaultValue,
                Name = name
            };
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool TryGetGlyphMetrics(
        CodePoint codePoint,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out GlyphMetrics? metrics)
    {
        // We return metrics for the special glyph representing a missing character, commonly known as .notdef.
        this.TryGetGlyphId(codePoint, out ushort glyphId);
        metrics = this.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, layoutMode, support);
        return metrics != null;
    }

    /// <inheritdoc/>
    internal override GlyphMetrics GetGlyphMetrics(
        CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support)

        // We overwrite the cache entry for this type should the attributes change.
        => this.glyphCache.GetOrAdd(
            CreateCacheKey(in codePoint, glyphId, textAttributes, support, layoutMode),
            static (key, arg) =>

            arg.Item3.CreateGlyphMetrics(
                in arg.codePoint,
                key.Id,
                key.Id == 0 ? GlyphType.Fallback : GlyphType.Standard,
                key.Attributes,
                arg.textDecorations,
                key.ColorSupport,
                key.IsVerticalLayout),
            (textDecorations, codePoint, this));

    /// <inheritdoc />
    public override IReadOnlyList<CodePoint> GetAvailableCodePoints()
    {
        CMapTable cmap = this.outlineType == OutlineType.TrueType
            ? this.trueTypeFontTables!.Cmap
            : this.compactFontTables!.Cmap;

        return cmap.GetAvailableCodePoints();
    }

    /// <inheritdoc/>
    internal override bool TryGetGSubTable([NotNullWhen(true)] out GSubTable? gSubTable)
    {
        gSubTable = this.outlineType == OutlineType.TrueType
            ? this.trueTypeFontTables!.GSub
            : this.compactFontTables!.GSub;

        return gSubTable is not null;
    }

    /// <inheritdoc/>
    internal override void ApplySubstitution(GlyphSubstitutionCollection collection)
    {
        if (this.TryGetGSubTable(out GSubTable? gSubTable))
        {
            gSubTable.ApplySubstitution(this, collection);
        }
    }

    /// <inheritdoc/>
    internal override bool TryGetKerningOffset(ushort currentId, ushort nextId, out Vector2 vector)
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

        return kern.TryGetKerningOffset(currentId, nextId, out vector);
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

        // TODO: I don't think we should disable kerning here.
        if (!kerned && kerningMode != KerningMode.None)
        {
            KerningTable? kern = isTTF
                ? this.trueTypeFontTables!.Kern
                : this.compactFontTables!.Kern;

            if (kern?.Count > 0)
            {
                // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
                int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
                for (int index = 0; index < collection.Count - 1; index++)
                {
                    if (index >= maxCount)
                    {
                        break;
                    }

                    kern.UpdatePositions(this, collection, index, index + 1);
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
        using FontReader reader = new(fs);
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
        using FontReader reader = new(stream);
        return LoadFont(reader);
    }

    internal static StreamFontMetrics LoadFont(FontReader reader)
    {
        if (reader.OutlineType == OutlineType.TrueType)
        {
            return LoadTrueTypeFont(reader);
        }

        return LoadCompactFont(reader);
    }

    private (HorizontalMetrics HorizontalMetrics, VerticalMetrics VerticalMetrics) Initialize<T>(T tables)
        where T : IFontTables
    {
        HeadTable head = tables.Head;
        HorizontalHeadTable hhea = tables.Hhea;
        VerticalHeadTable? vhea = tables.Vhea;
        OS2Table os2 = tables.Os2;
        PostTable post = tables.Post;

        this.HeadFlags = head.Flags;
        this.unitsPerEm = head.UnitsPerEm;
        this.scaleFactor = this.unitsPerEm * 72F; // 72 * UnitsPerEm means 1pt = 1px
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

        HorizontalMetrics horizontalMetrics = InitializeHorizontalMetrics(hhea, vhea, os2);
        VerticalMetrics verticalMetrics = InitializeVerticalMetrics(horizontalMetrics, vhea);
        return (horizontalMetrics, verticalMetrics);
    }

    private static HorizontalMetrics InitializeHorizontalMetrics(HorizontalHeadTable hhea, VerticalHeadTable? vhea, OS2Table os2)
    {
        short ascender;
        short descender;
        short lineGap;
        short lineHeight;
        short advanceWidthMax;
        short advanceHeightMax;

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
        bool useTypoMetrics = (os2.FontStyle & OS2Table.FontStyleSelection.USE_TYPO_METRICS) == OS2Table.FontStyleSelection.USE_TYPO_METRICS;
        if (useTypoMetrics)
        {
            ascender = os2.TypoAscender;
            descender = os2.TypoDescender;
            lineGap = os2.TypoLineGap;
            lineHeight = (short)(ascender - descender + lineGap);
        }
        else
        {
            ascender = hhea.Ascender;
            descender = hhea.Descender;
            lineGap = hhea.LineGap;
            lineHeight = (short)(ascender - descender + lineGap);
        }

        if (ascender == 0 || descender == 0)
        {
            if (os2.TypoAscender != 0 || os2.TypoDescender != 0)
            {
                ascender = os2.TypoAscender;
                descender = os2.TypoDescender;
                lineGap = os2.TypoLineGap;
                lineHeight = (short)(ascender - descender + lineGap);
            }
            else
            {
                ascender = (short)os2.WinAscent;
                descender = (short)-os2.WinDescent;
                lineHeight = (short)(ascender - descender);
            }
        }

        advanceWidthMax = (short)hhea.AdvanceWidthMax;
        advanceHeightMax = vhea == null ? lineHeight : vhea.AdvanceHeightMax;

        return new()
        {
            Ascender = ascender,
            Descender = descender,
            LineGap = lineGap,
            LineHeight = lineHeight,
            AdvanceWidthMax = advanceWidthMax,
            AdvanceHeightMax = advanceHeightMax
        };
    }

    private static VerticalMetrics InitializeVerticalMetrics(HorizontalMetrics metrics, VerticalHeadTable? vhea)
    {
        VerticalMetrics verticalMetrics = new()
        {
            Ascender = metrics.Ascender,
            Descender = metrics.Descender,
            LineGap = metrics.LineGap,
            LineHeight = metrics.LineHeight,
            AdvanceWidthMax = metrics.AdvanceWidthMax,
            AdvanceHeightMax = metrics.AdvanceHeightMax,
            Synthesized = true
        };

        if (vhea is null)
        {
            return verticalMetrics;
        }

        short ascender = vhea.Ascender;

        // Always negative due to the grid orientation.
        short descender = (short)(vhea.Descender > 0 ? -vhea.Descender : vhea.Descender);
        short lineGap = vhea.LineGap;
        short lineHeight = (short)(ascender - descender + lineGap);

        verticalMetrics.Ascender = ascender;
        verticalMetrics.Descender = descender;
        verticalMetrics.LineGap = lineGap;
        verticalMetrics.LineHeight = lineHeight;
        verticalMetrics.Synthesized = false;

        return verticalMetrics;
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
        BigEndianBinaryReader reader = new(stream, true);
        TtcHeader ttcHeader = TtcHeader.Read(reader);
        StreamFontMetrics[] fonts = new StreamFontMetrics[(int)ttcHeader.NumFonts];

        for (int i = 0; i < ttcHeader.NumFonts; ++i)
        {
            stream.Position = startPos + ttcHeader.OffsetTable[i];
            fonts[i] = LoadFont(stream);
        }

        return fonts;
    }

    private static (int CodePoint, ushort Id, TextAttributes Attributes, ColorFontSupport ColorSupport, bool IsVerticalLayout) CreateCacheKey(
        in CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        ColorFontSupport colorSupport,
        LayoutMode layoutMode)
        => (codePoint.Value, glyphId, textAttributes, colorSupport, AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode));

    private GlyphMetrics CreateGlyphMetrics(
        in CodePoint codePoint,
        ushort glyphId,
        GlyphType glyphType,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        ColorFontSupport colorSupport,
        bool isVerticalLayout,
        ushort paletteIndex = 0)
        => this.outlineType switch
        {
            OutlineType.TrueType => this.CreateTrueTypeGlyphMetrics(in codePoint, glyphId, glyphType, textAttributes, textDecorations, colorSupport, isVerticalLayout, paletteIndex),
            OutlineType.CFF => this.CreateCffGlyphMetrics(in codePoint, glyphId, glyphType, textAttributes, textDecorations, colorSupport, isVerticalLayout, paletteIndex),
            _ => throw new NotSupportedException(),
        };
}

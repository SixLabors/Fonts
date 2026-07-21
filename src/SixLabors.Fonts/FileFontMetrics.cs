// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// <para>
/// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
/// </para>
/// <para>The font source is a filesystem path.</para>
/// </summary>
internal sealed class FileFontMetrics : FontMetrics
{
    private readonly Lazy<StreamFontMetrics> fontMetrics;
    private readonly FontSource source;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileFontMetrics"/> class.
    /// </summary>
    /// <param name="path">The filesystem path to the font.</param>
    public FileFontMetrics(string path)
        : this(path, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileFontMetrics"/> class.
    /// </summary>
    /// <param name="path">The filesystem path to the font.</param>
    /// <param name="offset">The offset of the font within the file.</param>
    private FileFontMetrics(string path, long offset)
        : this(FontDescription.LoadDescription(path), path, offset)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileFontMetrics"/> class.
    /// </summary>
    /// <param name="description">The font description.</param>
    /// <param name="path">The filesystem path to the font.</param>
    /// <param name="offset">The offset of the font within the file.</param>
    private FileFontMetrics(FontDescription description, string path, long offset)
    {
        this.Description = description;
        this.Path = path;
        this.source = FontSource.Create(path, offset);
        this.fontMetrics = new Lazy<StreamFontMetrics>(this.LoadFont, true);
    }

    /// <inheritdoc cref="FontMetrics.Description"/>
    public override FontDescription Description { get; }

    /// <summary>
    /// Gets the filesystem path to the font face source.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the underlying <see cref="StreamFontMetrics"/> that this file-backed instance delegates to.
    /// </summary>
    public StreamFontMetrics StreamFontMetrics => this.fontMetrics.Value;

    /// <inheritdoc />
    public override ushort UnitsPerEm => this.fontMetrics.Value.UnitsPerEm;

    /// <inheritdoc />
    public override float ScaleFactor => this.fontMetrics.Value.ScaleFactor;

    /// <inheritdoc/>
    public override HorizontalMetrics HorizontalMetrics => this.fontMetrics.Value.HorizontalMetrics;

    /// <inheritdoc/>
    public override VerticalMetrics VerticalMetrics => this.fontMetrics.Value.VerticalMetrics;

    /// <inheritdoc/>
    public override short SubscriptXSize => this.fontMetrics.Value.SubscriptXSize;

    /// <inheritdoc/>
    public override short SubscriptYSize => this.fontMetrics.Value.SubscriptYSize;

    /// <inheritdoc/>
    public override short SubscriptXOffset => this.fontMetrics.Value.SubscriptXOffset;

    /// <inheritdoc/>
    public override short SubscriptYOffset => this.fontMetrics.Value.SubscriptYOffset;

    /// <inheritdoc/>
    public override short SuperscriptXSize => this.fontMetrics.Value.SuperscriptXSize;

    /// <inheritdoc/>
    public override short SuperscriptYSize => this.fontMetrics.Value.SuperscriptYSize;

    /// <inheritdoc/>
    public override short SuperscriptXOffset => this.fontMetrics.Value.SuperscriptXOffset;

    /// <inheritdoc/>
    public override short SuperscriptYOffset => this.fontMetrics.Value.SuperscriptYOffset;

    /// <inheritdoc/>
    public override short StrikeoutSize => this.fontMetrics.Value.StrikeoutSize;

    /// <inheritdoc/>
    public override short XHeight => this.fontMetrics.Value.XHeight;

    /// <inheritdoc/>
    public override short CapHeight => this.fontMetrics.Value.CapHeight;

    /// <inheritdoc/>
    public override short StrikeoutPosition => this.fontMetrics.Value.StrikeoutPosition;

    /// <inheritdoc/>
    public override short UnderlinePosition => this.fontMetrics.Value.UnderlinePosition;

    /// <inheritdoc/>
    public override short UnderlineThickness => this.fontMetrics.Value.UnderlineThickness;

    /// <inheritdoc/>
    public override float ItalicAngle => this.fontMetrics.Value.ItalicAngle;

    /// <inheritdoc/>
    public override bool TryGetTableData(Tag tag, out ReadOnlyMemory<byte> table)
        => this.source.TryGetTableData(tag, out table);

    /// <inheritdoc/>
    public override Stream OpenStream()
        => this.source.OpenStream();

    /// <inheritdoc/>
    internal override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
        => this.fontMetrics.Value.TryGetGlyphId(codePoint, out glyphId);

    /// <inheritdoc/>
    internal override bool TryGetGlyphId(
        CodePoint codePoint,
        CodePoint? nextCodePoint,
        out ushort glyphId,
        out bool skipNextCodePoint)
        => this.fontMetrics.Value.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);

    /// <inheritdoc/>
    internal override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
        => this.fontMetrics.Value.TryGetCodePoint(glyphId, out codePoint);

    /// <inheritdoc/>
    internal override bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
        => this.fontMetrics.Value.TryGetGlyphClass(glyphId, out glyphClass);

    /// <inheritdoc/>
    internal override bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
        => this.fontMetrics.Value.TryGetMarkAttachmentClass(glyphId, out markAttachmentClass);

    /// <inheritdoc/>
    public override bool TryGetVariationAxes(out ReadOnlyMemory<VariationAxis> variationAxes)
        => this.fontMetrics.Value.TryGetVariationAxes(out variationAxes);

    /// <inheritdoc/>
    internal override bool IsInMarkFilteringSet(ushort markGlyphSetIndex, ushort glyphId)
        => this.fontMetrics.Value.IsInMarkFilteringSet(markGlyphSetIndex, glyphId);

    /// <inheritdoc />
    public override bool TryGetGlyphMetrics(
        CodePoint codePoint,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out FontGlyphMetrics? metrics)
          => this.fontMetrics.Value.TryGetGlyphMetrics(codePoint, textAttributes, textDecorations, layoutMode, support, out metrics);

    /// <inheritdoc />
    public override bool TryGetGlyphMetrics(
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out FontGlyphMetrics? metrics)
        => this.fontMetrics.Value.TryGetGlyphMetrics(glyphId, textAttributes, textDecorations, layoutMode, support, out metrics);

    /// <inheritdoc />
    internal override FontGlyphMetrics GetGlyphMetrics(
        CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support)
        => this.fontMetrics.Value.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, layoutMode, support);

    /// <inheritdoc />
    public override ReadOnlyMemory<CodePoint> GetAvailableCodePoints()
        => this.fontMetrics.Value.GetAvailableCodePoints();

    /// <inheritdoc/>
    internal override bool TryGetGSubTable([NotNullWhen(true)] out GSubTable? gSubTable)
        => this.fontMetrics.Value.TryGetGSubTable(out gSubTable);

    /// <inheritdoc/>
    internal override bool TryGetBaselineCoordinate(Tag baselineTag, bool isVerticalLayout, out short coordinate)
        => this.fontMetrics.Value.TryGetBaselineCoordinate(baselineTag, isVerticalLayout, out coordinate);

    /// <inheritdoc/>
    internal override void ApplySubstitution(GlyphSubstitutionCollection collection)
        => this.fontMetrics.Value.ApplySubstitution(collection);

    /// <inheritdoc/>
    internal override bool TryGetKerningOffset(ushort currentId, ushort nextId, out Vector2 vector)
        => this.fontMetrics.Value.TryGetKerningOffset(currentId, nextId, out vector);

    /// <inheritdoc/>
    internal override void UpdatePositions(GlyphPositioningCollection collection)
        => this.fontMetrics.Value.UpdatePositions(collection);

    /// <inheritdoc/>
    internal override float GetGDefVariationDelta(uint packedVariationIndex)
        => this.fontMetrics.Value.GetGDefVariationDelta(packedVariationIndex);

    /// <inheritdoc/>
    internal override ReadOnlySpan<float> GetNormalizedCoordinates()
        => this.fontMetrics.Value.GetNormalizedCoordinates();

    /// <summary>
    /// Reads a font collection from the specified filesystem path.
    /// </summary>
    /// <param name="path">The filesystem path to the font collection.</param>
    /// <returns>A read-only memory region containing the font metrics.</returns>
    public static ReadOnlyMemory<FileFontMetrics> LoadFontCollection(string path)
    {
        using FileStream fs = File.OpenRead(path);
        long startPos = fs.Position;
        using BigEndianBinaryReader reader = new(fs, true);
        TtcHeader ttcHeader = TtcHeader.Read(reader);
        FileFontMetrics[] fonts = new FileFontMetrics[(int)ttcHeader.NumFonts];

        for (int i = 0; i < ttcHeader.NumFonts; ++i)
        {
            fs.Position = startPos + ttcHeader.OffsetTable[i];
            FontDescription description = FontDescription.LoadDescription(fs);
            fonts[i] = new FileFontMetrics(description, path, ttcHeader.OffsetTable[i]);
        }

        return fonts;
    }

    private StreamFontMetrics LoadFont()
    {
        using Stream stream = this.OpenStream();
        return StreamFontMetrics.LoadFont(stream, this.source);
    }
}

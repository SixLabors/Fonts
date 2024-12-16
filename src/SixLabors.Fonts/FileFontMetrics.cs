// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
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

    public FileFontMetrics(string path)
        : this(path, 0)
    {
    }

    public FileFontMetrics(string path, long offset)
        : this(FontDescription.LoadDescription(path), path, offset)
    {
    }

    internal FileFontMetrics(FontDescription description, string path, long offset)
    {
        this.Description = description;
        this.Path = path;
        this.fontMetrics = new Lazy<StreamFontMetrics>(() => StreamFontMetrics.LoadFont(path, offset), true);
    }

    /// <inheritdoc cref="FontMetrics.Description"/>
    public override FontDescription Description { get; }

    /// <summary>
    /// Gets the filesystem path to the font face source.
    /// </summary>
    public string Path { get; }

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
    public override short StrikeoutPosition => this.fontMetrics.Value.StrikeoutPosition;

    /// <inheritdoc/>
    public override short UnderlinePosition => this.fontMetrics.Value.UnderlinePosition;

    /// <inheritdoc/>
    public override short UnderlineThickness => this.fontMetrics.Value.UnderlineThickness;

    /// <inheritdoc/>
    public override float ItalicAngle => this.fontMetrics.Value.ItalicAngle;

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

    /// <inheritdoc />
    public override bool TryGetGlyphMetrics(
        CodePoint codePoint,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out IReadOnlyList<GlyphMetrics>? metrics)
          => this.fontMetrics.Value.TryGetGlyphMetrics(codePoint, textAttributes, textDecorations, layoutMode, support, out metrics);

    /// <inheritdoc />
    internal override IReadOnlyList<GlyphMetrics> GetGlyphMetrics(
        CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support)
        => this.fontMetrics.Value.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, layoutMode, support);

    /// <inheritdoc />
    public override IReadOnlyList<CodePoint> GetAvailableCodePoints()
        => this.fontMetrics.Value.GetAvailableCodePoints();

    /// <inheritdoc/>
    internal override bool TryGetGSubTable([NotNullWhen(true)] out GSubTable? gSubTable)
        => this.fontMetrics.Value.TryGetGSubTable(out gSubTable);

    /// <inheritdoc/>
    internal override void ApplySubstitution(GlyphSubstitutionCollection collection)
        => this.fontMetrics.Value.ApplySubstitution(collection);

    /// <inheritdoc/>
    internal override bool TryGetKerningOffset(ushort currentId, ushort nextId, out Vector2 vector)
        => this.fontMetrics.Value.TryGetKerningOffset(currentId, nextId, out vector);

    /// <inheritdoc/>
    internal override void UpdatePositions(GlyphPositioningCollection collection)
        => this.fontMetrics.Value.UpdatePositions(collection);

    /// <summary>
    /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
    public static FileFontMetrics[] LoadFontCollection(string path)
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
}

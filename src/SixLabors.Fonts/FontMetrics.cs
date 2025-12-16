// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
/// </summary>
public abstract class FontMetrics
{
    internal FontMetrics()
    {
    }

    /// <summary>
    /// Gets the basic description of the face.
    /// </summary>
    public abstract FontDescription Description { get; }

    /// <summary>
    /// Gets the number of font units per EM square for this face.
    /// </summary>
    public abstract ushort UnitsPerEm { get; }

    /// <summary>
    /// Gets the scale factor that is applied to all glyphs in this face.
    /// Calculated as 72 * <see cref="UnitsPerEm"/> so that 1pt = 1px.
    /// </summary>
    public abstract float ScaleFactor { get; }

    /// <summary>
    /// Gets the metrics specific to horizontal text.
    /// </summary>
    public abstract HorizontalMetrics HorizontalMetrics { get; }

    /// <summary>
    /// Gets the metrics specific to vertical text.
    /// </summary>
    public abstract VerticalMetrics VerticalMetrics { get; }

    /// <summary>
    /// Gets the recommended horizontal size in font design units for subscripts for this font.
    /// </summary>
    public abstract short SubscriptXSize { get; }

    /// <summary>
    /// Gets the recommended vertical size in font design units for subscripts for this font.
    /// </summary>
    public abstract short SubscriptYSize { get; }

    /// <summary>
    /// Gets the recommended horizontal offset in font design units for subscripts for this font.
    /// </summary>
    public abstract short SubscriptXOffset { get; }

    /// <summary>
    /// Gets the recommended vertical offset in font design units for subscripts for this font.
    /// </summary>
    public abstract short SubscriptYOffset { get; }

    /// <summary>
    /// Gets the recommended horizontal size in font design units for superscripts for this font.
    /// </summary>
    public abstract short SuperscriptXSize { get; }

    /// <summary>
    /// Gets the recommended vertical size in font design units for superscripts for this font.
    /// </summary>
    public abstract short SuperscriptYSize { get; }

    /// <summary>
    /// Gets the recommended horizontal offset in font design units for superscripts for this font.
    /// </summary>
    public abstract short SuperscriptXOffset { get; }

    /// <summary>
    /// Gets the recommended vertical offset in font design units for superscripts for this font.
    /// </summary>
    public abstract short SuperscriptYOffset { get; }

    /// <summary>
    /// Gets thickness of the strikeout stroke in font design units.
    /// </summary>
    public abstract short StrikeoutSize { get; }

    /// <summary>
    /// Gets the position of the top of the strikeout stroke relative to the baseline in font design units.
    /// </summary>
    public abstract short StrikeoutPosition { get; }

    /// <summary>
    /// Gets the suggested distance of the top of the underline from the baseline (negative values indicate below baseline).
    /// </summary>
    public abstract short UnderlinePosition { get; }

    /// <summary>
    /// Gets the suggested values for the underline thickness. In general, the underline thickness should match the thickness of
    /// the underscore character (U+005F LOW LINE), and should also match the strikeout thickness, which is specified in the OS/2 table.
    /// </summary>
    public abstract short UnderlineThickness { get; }

    /// <summary>
    /// Gets the italic angle in counter-clockwise degrees from the vertical. Zero for upright text, negative for text that leans to the right (forward).
    /// </summary>
    public abstract float ItalicAngle { get; }

    /// <summary>
    /// Gets the specified glyph id matching the codepoint.
    /// </summary>
    /// <param name="codePoint">The codepoint.</param>
    /// <param name="glyphId">
    /// When this method returns, contains the glyph id associated with the specified codepoint,
    /// if the codepoint is found; otherwise, <value>0</value>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains a glyph for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    internal abstract bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId);

    /// <summary>
    /// Gets the specified glyph id matching the codepoint pair.
    /// </summary>
    /// <param name="codePoint">The codepoint.</param>
    /// <param name="nextCodePoint">The next codepoint. Can be null.</param>
    /// <param name="glyphId">
    /// When this method returns, contains the glyph id associated with the specified codepoint,
    /// if the codepoint is found; otherwise, <value>0</value>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <param name="skipNextCodePoint">
    /// When this method return, contains a value indicating whether the next codepoint should be skipped.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains a glyph for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    internal abstract bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out ushort glyphId, out bool skipNextCodePoint);

    /// <summary>
    /// Gets the specified glyph id matching the codepoint.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">
    /// When this method returns, contains the codepoint associated with the specified glyph id,
    /// if the glyph id is found; otherwise, <value>default</value>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains a codepoint for the specified glyph id; otherwise, <see langword="false"/>.
    /// </returns>
    internal abstract bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint);

    /// <summary>
    /// Tries to get the glyph class for a given glyph id.
    /// The font needs to have a GDEF table defined.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="glyphClass">The glyph class.</param>
    /// <returns>true, if the glyph class could be retrieved.</returns>
    internal abstract bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass);

    /// <summary>
    /// Tries to get the mark attachment class for a given glyph id.
    /// The font needs to have a GDEF table defined.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="markAttachmentClass">The mark attachment class.</param>
    /// <returns>true, if the mark attachment class could be retrieved.</returns>
    internal abstract bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass);

    /// <summary>
    /// Returns a value indicating whether the specified glyph is in the given mark filtering set.
    /// The font needs to have a GDEF table defined.
    /// </summary>
    /// <param name="markGlyphSetIndex">The mark glyph set index.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>
    /// true, if the glyph is in the mark filtering set.
    /// </returns>
    internal abstract bool IsInMarkFilteringSet(ushort markGlyphSetIndex, ushort glyphId);

    /// <summary>
    /// Gets the glyph metrics for a given code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to get the glyph for.</param>
    /// <param name="textAttributes">The text attributes applied to the glyph.</param>
    /// <param name="textDecorations">The text decorations applied to the glyph.</param>
    /// <param name="layoutMode">The layout mode applied to the glyph.</param>
    /// <param name="support">Options for enabling color font support during layout and rendering.</param>
    /// <param name="metrics">
    /// When this method returns, contains the metrics for the given codepoint and color support if the metrics
    /// are found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyph metrics for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool TryGetGlyphMetrics(
        CodePoint codePoint,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out GlyphMetrics? metrics);

    /// <summary>
    /// Gets the unicode codepoints for which a glyph exists in the font.
    /// </summary>
    /// <returns>The <see cref="IReadOnlyList{CodePoint}"/>.</returns>
    public abstract IReadOnlyList<CodePoint> GetAvailableCodePoints();

    /// <summary>
    /// Gets the glyph metrics for a given code point and glyph id.
    /// </summary>
    /// <param name="codePoint">The Unicode codepoint.</param>
    /// <param name="glyphId">
    /// The previously matched or substituted glyph id for the codepoint in the face.
    /// If this value equals <value>0</value> the default fallback metrics are returned.
    /// </param>
    /// <param name="textAttributes">The text attributes applied to the glyph.</param>
    /// <param name="textDecorations">The text decorations applied to the glyph.</param>
    /// <param name="layoutMode">The layout mode applied to the glyph.</param>
    /// <param name="colorSupport">Options for enabling color font support during layout and rendering.</param>
    /// <returns>The <see cref="IEnumerable{GlyphMetrics}"/>.</returns>
    internal abstract GlyphMetrics GetGlyphMetrics(
        CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport colorSupport);

    /// <summary>
    /// Tries to get the GSUB table.
    /// </summary>
    /// <param name="gSubTable">The GSUB table.</param>
    /// <returns>true, if the glyph class could be retrieved.</returns>
    internal abstract bool TryGetGSubTable([NotNullWhen(true)] out GSubTable? gSubTable);

    /// <summary>
    /// Applies any available substitutions to the collection of glyphs.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    internal abstract void ApplySubstitution(GlyphSubstitutionCollection collection);

    /// <summary>
    /// Gets the amount, in font units, the <paramref name="currentId"/> glyph should be offset if it is followed by
    /// the <paramref name="nextId"/> glyph.
    /// </summary>
    /// <param name="currentId">The current glyph id.</param>
    /// <param name="nextId">The next glyph id.</param>
    /// <param name="vector">
    /// When this method returns, contains the offset, in font units, that should be applied to the
    /// <paramref name="currentId"/> glyph, if the offset is found; otherwise the default vector value.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains and offset for the glyph combination; otherwise, <see langword="false"/>.
    /// </returns>
    internal abstract bool TryGetKerningOffset(ushort currentId, ushort nextId, out Vector2 vector);

    /// <summary>
    /// Applies any available positioning updates to the collection of glyphs.
    /// </summary>
    /// <param name="collection">The glyph positioning collection.</param>
    internal abstract void UpdatePositions(GlyphPositioningCollection collection);
}

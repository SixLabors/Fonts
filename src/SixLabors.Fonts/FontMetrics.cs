// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
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
        /// Gets the typographic ascender of the face, expressed in font units.
        /// </summary>
        public abstract short Ascender { get; }

        /// <summary>
        /// Gets the typographic descender of the face, expressed in font units.
        /// </summary>
        public abstract short Descender { get; }

        /// <summary>
        /// Gets the typographic line gap of the face, expressed in font units.
        /// This field should be combined with the <see cref="Ascender"/> and <see cref="Descender"/>
        /// values to determine default line spacing.
        /// </summary>
        public abstract short LineGap { get; }

        /// <summary>
        /// Gets the typographic line spacing of the face, expressed in font units.
        /// </summary>
        public abstract short LineHeight { get; }

        /// <summary>
        /// Gets the maximum advance width, in font units, for all glyphs in this face.
        /// </summary>
        public abstract short AdvanceWidthMax { get; }

        /// <summary>
        /// Gets the maximum advance height, in font units, for all glyphs in this
        /// face.This is only relevant for vertical layouts, and is set to <see cref="LineHeight"/> for
        /// fonts that do not provide vertical metrics.
        /// </summary>
        public abstract short AdvanceHeightMax { get; }

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
        /// Gets the glyph metrics for a given code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point to get the glyph for.</param>
        /// <param name="support">Options for enabling color font support during layout and rendering.</param>
        /// <returns>The glyph metrics to find.</returns>
        public abstract IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ColorFontSupport support);

        /// <summary>
        /// Gets the unicode codepoints for which a glyph exists in the font.
        /// </summary>
        /// <returns>An enumerable containing all available codepoints.</returns>
        internal abstract IEnumerable<CodePoint> GetAvailableCodePoints();

        /// <summary>
        /// Gets the glyph metrics for a given code point and glyph id.
        /// </summary>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="glyphId">
        /// The previously matched or substituted glyph id for the codepoint in the face.
        /// If this value equals <value>0</value> the default fallback metrics are returned.
        /// </param>
        /// <param name="support">Options for enabling color font support during layout and rendering.</param>
        /// <returns>The <see cref="IEnumerable{GlyphMetrics}"/>.</returns>
        internal abstract IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ushort glyphId, ColorFontSupport support);

        /// <summary>
        /// Applies any available substitutions to the collection of glyphs.
        /// </summary>
        /// <param name="collection">The glyph substitution collection.</param>
        internal abstract void ApplySubstitution(GlyphSubstitutionCollection collection);

        /// <summary>
        /// Applies any available positioning updates to the collection of glyphs.
        /// </summary>
        /// <param name="collection">The glyph positioning collection.</param>
        internal abstract void UpdatePositions(GlyphPositioningCollection collection);
    }
}

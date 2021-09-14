// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
    /// </summary>
    public interface IFontMetrics
    {
        /// <summary>
        /// Gets the basic description of the face.
        /// </summary>
        FontDescription Description { get; }

        /// <summary>
        /// Gets the number of font units per EM square for this face.
        /// </summary>
        ushort UnitsPerEm { get; }

        /// <summary>
        /// Gets the scale factor that is applied to all glyphs in this face.
        /// Calculated as 72 * <see cref="UnitsPerEm"/> so that 1pt = 1px.
        /// </summary>
        float ScaleFactor { get; }

        /// <summary>
        /// Gets the typographic ascender of the face, expressed in font units.
        /// </summary>
        short Ascender { get; }

        /// <summary>
        /// Gets the typographic descender of the face, expressed in font units.
        /// </summary>
        short Descender { get; }

        /// <summary>
        /// Gets the typographic line gap of the face, expressed in font units.
        /// This field should be combined with the <see cref="Ascender"/> and <see cref="Descender"/>
        /// values to determine default line spacing.
        /// </summary>
        short LineGap { get; }

        /// <summary>
        /// Gets the typographic line spacing of the face, expressed in font units.
        /// </summary>
        short LineHeight { get; }

        /// <summary>
        /// Gets the maximum advance width, in font units, for all glyphs in this face.
        /// </summary>
        short AdvanceWidthMax { get; }

        /// <summary>
        /// Gets the maximum advance height, in font units, for all glyphs in this
        /// face.This is only relevant for vertical layouts, and is set to <see cref="LineHeight"/> for
        /// fonts that do not provide vertical metrics.
        /// </summary>
        short AdvanceHeightMax { get; }

        /// <summary>
        /// Gets the specified glyph id matching the codepoint pair.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        /// <param name="nextCodePoint">The next codepoint. Can be null.</param>
        /// <param name="glyphId">
        /// When this method returns, contains the glyph id associated with the specified codepoint,
        /// if the codepoint is found; otherwise, <value>-1</value>.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <param name="skipNextCodePoint">
        /// When this method return, contains a value indicating whether the next codepoint should be skipped.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the face contains a glyph for the specified codepoint; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryGetGlyphId(CodePoint codePoint, CodePoint? nextCodePoint, out int glyphId, out bool skipNextCodePoint);

        /// <summary>
        /// Gets the glyph metrics for a given code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point to get the glyph for.</param>
        /// <returns>The glyph metrics to find.</returns>
        GlyphMetrics GetGlyphMetrics(CodePoint codePoint);

        /// <summary>
        /// Gets the glyph metrics for a given code point and glyph id.
        /// </summary>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="glyphId">
        /// The previously matched or substituted glyph id for the codepoint in the face.
        /// If this value is less than <value>0</value> the default fallback metrics are returned.
        /// </param>
        /// <param name="support">Options for enabling color font support during layout and rendering.</param>
        /// <returns>The <see cref="IEnumerable{GlyphMetrics}"/>.</returns>
        IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, int glyphId, ColorFontSupport support);

        /// <summary>
        /// Applies any available substitutions to the collection of glyphs.
        /// </summary>
        /// <param name="collection">The glyph substitution collection.</param>
        void ApplySubstitution(GlyphSubstitutionCollection collection);

        /// <summary>
        /// Applies any available positioning updates to the collection of glyphs.
        /// </summary>
        /// <param name="collection">The glyph positioning collection.</param>
        void UpdatePositions(GlyphPositioningCollection collection);

        /// <summary>
        /// Get the kerning offset that should be applied between 2 glyphs.
        /// </summary>
        /// <param name="glyph">The current glyph.</param>
        /// <param name="previousGlyph">The previous glyph in the rendered font.</param>
        /// <returns>The <see cref="Vector2"/> representing the offset between the 2 glyphs.</returns>
        Vector2 GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph);
    }
}

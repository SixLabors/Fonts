// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Allows the shaping of glyphs based upon typographics rulesets.
    /// </summary>
    internal interface IFontShaper : IFontMetrics
    {
        /// <summary>
        /// Gets the specified glyph class matching the id.
        /// </summary>
        /// <param name="glyphId">The glyph id.</param>
        /// <param name="glyphClass">
        /// When this method returns, contains the glyph class associated with the specified id,
        /// if the id is found; otherwise, <see langword="null"/>.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the face contains a glyph class for the specified id; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryGetGlyphClass(ushort glyphId, out GlyphClassDef? glyphClass);

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
    }
}

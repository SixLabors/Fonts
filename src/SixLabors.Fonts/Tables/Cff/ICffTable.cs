// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Defines a common interface for CFF1 and CFF2 tables.
    /// </summary>
    internal interface ICffTable
    {
        /// <summary>
        /// Gets the number of glyphs in the table.
        /// </summary>
        int GlyphCount
        {
            get;
        }

        /// <summary>
        /// Gets the item variation store.
        /// </summary>
        /// <returns>The item variation store. If CFF1, there is no variations and null will be returned instead.</returns>
        ItemVariationStore? ItemVariationStore
        {
            get;
        }

        /// <summary>
        /// Gets the glyph data at the given index.
        /// </summary>
        /// <param name="index">The glyph index.</param>
        /// <returns>The <see cref="CffGlyphData"/>.</returns>
        CffGlyphData GetGlyph(int index);
    }
}

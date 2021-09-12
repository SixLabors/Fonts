// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    internal abstract class BaseShaper
    {
        /// <summary>
        /// Assigns the substitution features to each glyph.
        /// </summary>
        /// <param name="glyphs">The glyphs collection.</param>
        public abstract void AssignFeatures(GlyphSubstitutionCollection glyphs);
    }
}

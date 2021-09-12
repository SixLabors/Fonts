// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// Default shaper, which will be applied to all glyphs.
    /// Based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/DefaultShaper.js"/>
    /// </summary>
    internal class DefaultShaper
    {
        public virtual void AssignFeatures(GlyphSubstitutionCollection glyphs)
        {
            // Add variation Features.
            AddFeature(glyphs, Tag.Parse("rvrn"));

            // Add directional features.
            AddFeature(glyphs, Tag.Parse("ltra"));
            AddFeature(glyphs, Tag.Parse("ltrm"));
            AddFeature(glyphs, Tag.Parse("rtla"));
            AddFeature(glyphs, Tag.Parse("rtlm"));

            // Add fractional features.
            AddFeature(glyphs, Tag.Parse("frac"));
            AddFeature(glyphs, Tag.Parse("numr"));
            AddFeature(glyphs, Tag.Parse("dnom"));

            // Add common features.
            AddFeature(glyphs, Tag.Parse("ccmp"));
            AddFeature(glyphs, Tag.Parse("locl"));
            AddFeature(glyphs, Tag.Parse("rlig"));
            AddFeature(glyphs, Tag.Parse("mark"));
            AddFeature(glyphs, Tag.Parse("mkmk"));

            // Add horizontal features.
            AddFeature(glyphs, Tag.Parse("calt"));
            AddFeature(glyphs, Tag.Parse("clig"));
            AddFeature(glyphs, Tag.Parse("liga"));
            AddFeature(glyphs, Tag.Parse("rclt"));
            AddFeature(glyphs, Tag.Parse("curs"));
            AddFeature(glyphs, Tag.Parse("kern"));

            // TODO: Enable contextual fractions
        }

        private static void AddFeature(GlyphSubstitutionCollection glyphs, Tag variationFeatures)
        {
            for (int i = 0; i < glyphs.Count; i++)
            {
                glyphs.AddSubstitutionFeature(i, variationFeatures);
            }
        }
    }
}

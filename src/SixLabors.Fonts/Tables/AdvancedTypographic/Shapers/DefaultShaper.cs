// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// Default shaper, which will be applied to all glyphs.
    /// Based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/DefaultShaper.js"/>
    /// </summary>
    internal class DefaultShaper : BaseShaper
    {
        /// <inheritdoc />
        public override void AssignFeatures(GlyphSubstitutionCollection collection, int index, int count)
        {
            // TODO: Perf. Tags should be static.

            // Add variation Features.
            AddFeature(collection, index, count, Tag.Parse("rvrn"));

            // Add directional features.
            AddFeature(collection, index, count, Tag.Parse("ltra"));
            AddFeature(collection, index, count, Tag.Parse("ltrm"));
            AddFeature(collection, index, count, Tag.Parse("rtla"));
            AddFeature(collection, index, count, Tag.Parse("rtlm"));

            // Add fractional features.
            AddFeature(collection, index, count, Tag.Parse("frac"));
            AddFeature(collection, index, count, Tag.Parse("numr"));
            AddFeature(collection, index, count, Tag.Parse("dnom"));

            // Add common features.
            AddFeature(collection, index, count, Tag.Parse("ccmp"));
            AddFeature(collection, index, count, Tag.Parse("locl"));
            AddFeature(collection, index, count, Tag.Parse("rlig"));
            AddFeature(collection, index, count, Tag.Parse("mark"));
            AddFeature(collection, index, count, Tag.Parse("mkmk"));

            // Add horizontal features.
            AddFeature(collection, index, count, Tag.Parse("calt"));
            AddFeature(collection, index, count, Tag.Parse("clig"));
            AddFeature(collection, index, count, Tag.Parse("liga"));
            AddFeature(collection, index, count, Tag.Parse("rclt"));
            AddFeature(collection, index, count, Tag.Parse("curs"));
            AddFeature(collection, index, count, Tag.Parse("kern"));

            // TODO: Enable contextual fractions
        }

        private static void AddFeature(GlyphSubstitutionCollection collection, int index, int count, Tag variationFeatures)
        {
            for (int i = index; i < count; i++)
            {
                collection.AddSubstitutionFeature(i, variationFeatures);
            }
        }
    }
}

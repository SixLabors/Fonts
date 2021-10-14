// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// Default shaper, which will be applied to all glyphs.
    /// Based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/DefaultShaper.js"/>
    /// </summary>
    internal class DefaultShaper : BaseShaper
    {
        private static readonly Tag RvnrTag = Tag.Parse("rvrn");

        private static readonly Tag LtraTag = Tag.Parse("ltra");

        private static readonly Tag LtrmTag = Tag.Parse("ltrm");

        private static readonly Tag RtlaTag = Tag.Parse("rtla");

        private static readonly Tag RtlmTag = Tag.Parse("rtlm");

        private static readonly Tag FracTag = Tag.Parse("frac");

        private static readonly Tag NumrTag = Tag.Parse("numr");

        private static readonly Tag DnomTag = Tag.Parse("dnom");

        private static readonly Tag CcmpTag = Tag.Parse("ccmp");

        private static readonly Tag LoclTag = Tag.Parse("locl");

        private static readonly Tag RligTag = Tag.Parse("rlig");

        private static readonly Tag MarkTag = Tag.Parse("mark");

        private static readonly Tag MkmkTag = Tag.Parse("mkmk");

        private static readonly Tag CaltTag = Tag.Parse("calt");

        private static readonly Tag CligTag = Tag.Parse("clig");

        private static readonly Tag LigaTag = Tag.Parse("liga");

        private static readonly Tag RcltTag = Tag.Parse("rclt");

        private static readonly Tag CursTag = Tag.Parse("curs");

        private static readonly Tag KernTag = Tag.Parse("kern");

        private static readonly CodePoint FractionSlash = new(0x2044);

        private static readonly CodePoint Slash = new(0x002F);

        /// <inheritdoc />
        public override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            // Add variation Features.
            AddFeature(collection, index, count, RvnrTag);

            // Add directional features.
            AddFeature(collection, index, count, LtraTag);
            AddFeature(collection, index, count, LtrmTag);
            AddFeature(collection, index, count, RtlaTag);
            AddFeature(collection, index, count, RtlmTag);

            // Add common features.
            AddFeature(collection, index, count, CcmpTag);
            AddFeature(collection, index, count, LoclTag);
            AddFeature(collection, index, count, RligTag);
            AddFeature(collection, index, count, MarkTag);
            AddFeature(collection, index, count, MkmkTag);

            // Add horizontal features.
            AddFeature(collection, index, count, CaltTag);
            AddFeature(collection, index, count, CligTag);
            AddFeature(collection, index, count, LigaTag);
            AddFeature(collection, index, count, RcltTag);
            AddFeature(collection, index, count, CursTag);
            AddFeature(collection, index, count, KernTag);

            // Enable contextual fractions.
            for (int i = 0; i < collection.Count; i++)
            {
                GlyphShapingData shapingData = collection.GetGlyphShapingData(i);
                if (shapingData.CodePoint == FractionSlash || shapingData.CodePoint == Slash)
                {
                    int start = i;
                    int end = i + 1;

                    // Apply numerator.
                    shapingData = collection.GetGlyphShapingData(start - 1);
                    while (start > 0 && CodePoint.IsDigit(shapingData.CodePoint))
                    {
                        AddFeature(collection, start - 1, 1, NumrTag);
                        AddFeature(collection, start - 1, 1, FracTag);
                        start--;
                    }

                    // Apply denominator.
                    shapingData = collection.GetGlyphShapingData(end);
                    while (end < collection.Count && CodePoint.IsDigit(shapingData.CodePoint))
                    {
                        AddFeature(collection, end, 1, DnomTag);
                        AddFeature(collection, end, 1, FracTag);
                        end++;
                    }

                    // Apply fraction slash.
                    AddFeature(collection, i, 1, FracTag);
                    i = end - 1;
                }
            }
        }

        protected static void AddFeature(IGlyphShapingCollection collection, int index, int count, Tag variationFeatures)
        {
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                collection.AddShapingFeature(i, variationFeatures);
            }
        }
    }
}

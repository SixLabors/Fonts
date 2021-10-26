// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
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

        private readonly List<Tag> stageFeatures = new();

        internal DefaultShaper() => this.MarkZeroingMode = MarkZeroingMode.PostGpos;

        protected DefaultShaper(MarkZeroingMode markZeroingMode)
            => this.MarkZeroingMode = markZeroingMode;

        /// <inheritdoc />
        public override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            // Add variation Features.
            this.AddFeature(collection, index, count, RvnrTag);

            // Add directional features.
            for (int i = index; i < count; i++)
            {
                GlyphShapingData shapingData = collection.GetGlyphShapingData(i);
                if (shapingData.Direction == TextDirection.LeftToRight)
                {
                    this.AddFeature(collection, i, 1, LtraTag);
                    this.AddFeature(collection, i, 1, LtrmTag);
                }
                else
                {
                    this.AddFeature(collection, i, 1, RtlaTag);
                    this.AddFeature(collection, i, 1, RtlmTag);
                }
            }

            // Add common features.
            this.AddFeature(collection, index, count, CcmpTag);
            this.AddFeature(collection, index, count, LoclTag);
            this.AddFeature(collection, index, count, RligTag);
            this.AddFeature(collection, index, count, MarkTag);
            this.AddFeature(collection, index, count, MkmkTag);

            // Add horizontal features.
            this.AddFeature(collection, index, count, CaltTag);
            this.AddFeature(collection, index, count, CligTag);
            this.AddFeature(collection, index, count, LigaTag);
            this.AddFeature(collection, index, count, RcltTag);
            this.AddFeature(collection, index, count, CursTag);
            this.AddFeature(collection, index, count, KernTag);

            // Enable contextual fractions.
            for (int i = index; i < count; i++)
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
                        this.AddFeature(collection, start - 1, 1, NumrTag);
                        this.AddFeature(collection, start - 1, 1, FracTag);
                        start--;
                    }

                    // Apply denominator.
                    shapingData = collection.GetGlyphShapingData(end);
                    while (end < collection.Count && CodePoint.IsDigit(shapingData.CodePoint))
                    {
                        this.AddFeature(collection, end, 1, DnomTag);
                        this.AddFeature(collection, end, 1, FracTag);
                        end++;
                    }

                    // Apply fraction slash.
                    this.AddFeature(collection, i, 1, FracTag);
                    i = end - 1;
                }
            }
        }

        protected void AddFeature(IGlyphShapingCollection collection, int index, int count, Tag variationFeatures, bool enabled = true)
        {
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                collection.AddShapingFeature(i, new TagEntry(variationFeatures, enabled));
            }

            if (!this.stageFeatures.Contains(variationFeatures))
            {
                this.stageFeatures.Add(variationFeatures);
            }
        }

        public override IEnumerable<Tag> GetShapingStageFeatures() => this.stageFeatures;
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
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
        protected static readonly Tag RvnrTag = Tag.Parse("rvrn");

        protected static readonly Tag LtraTag = Tag.Parse("ltra");

        protected static readonly Tag LtrmTag = Tag.Parse("ltrm");

        protected static readonly Tag RtlaTag = Tag.Parse("rtla");

        protected static readonly Tag RtlmTag = Tag.Parse("rtlm");

        protected static readonly Tag FracTag = Tag.Parse("frac");

        protected static readonly Tag NumrTag = Tag.Parse("numr");

        protected static readonly Tag DnomTag = Tag.Parse("dnom");

        protected static readonly Tag CcmpTag = Tag.Parse("ccmp");

        protected static readonly Tag LoclTag = Tag.Parse("locl");

        protected static readonly Tag RligTag = Tag.Parse("rlig");

        protected static readonly Tag MarkTag = Tag.Parse("mark");

        protected static readonly Tag MkmkTag = Tag.Parse("mkmk");

        protected static readonly Tag CaltTag = Tag.Parse("calt");

        protected static readonly Tag CligTag = Tag.Parse("clig");

        protected static readonly Tag LigaTag = Tag.Parse("liga");

        protected static readonly Tag RcltTag = Tag.Parse("rclt");

        protected static readonly Tag CursTag = Tag.Parse("curs");

        protected static readonly Tag KernTag = Tag.Parse("kern");

        protected static readonly Tag VertTag = Tag.Parse("vert");

        protected static readonly Tag VKernTag = Tag.Parse("vkrn");

        private static readonly CodePoint FractionSlash = new(0x2044);

        private static readonly CodePoint Slash = new(0x002F);

        private readonly HashSet<ShapingStage> shapingStages = new();

        private readonly KerningMode kerningMode;

        private readonly IReadOnlyList<Tag> featureTags;

        internal DefaultShaper(TextOptions textOptions)
            : this(MarkZeroingMode.PostGpos, textOptions)
        {
        }

        protected DefaultShaper(MarkZeroingMode markZeroingMode, TextOptions textOptions)
        {
            this.MarkZeroingMode = markZeroingMode;
            this.kerningMode = textOptions.KerningMode;
            this.featureTags = textOptions.FeatureTags;
        }

        /// <inheritdoc />
        public override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            // Add variation Features.
            this.AddFeature(collection, index, count, RvnrTag);

            // Add directional features.
            LayoutMode layoutMode = collection.TextOptions.LayoutMode;
            bool isVerticalLayout = false;
            for (int i = index; i < count; i++)
            {
                GlyphShapingData shapingData = collection.GetGlyphShapingData(i);
                isVerticalLayout |= AdvancedTypographicUtils.IsVerticalGlyph(shapingData.CodePoint, layoutMode);

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

            if (!isVerticalLayout)
            {
                // Add horizontal features.
                this.AddFeature(collection, index, count, CaltTag);
                this.AddFeature(collection, index, count, CligTag);
                this.AddFeature(collection, index, count, LigaTag);
                this.AddFeature(collection, index, count, RcltTag);
                this.AddFeature(collection, index, count, CursTag);
                this.AddFeature(collection, index, count, KernTag);
            }
            else
            {
                // We only apply `vert` feature.See:
                // https://github.com/harfbuzz/harfbuzz/commit/d71c0df2d17f4590d5611239577a6cb532c26528
                // https://lists.freedesktop.org/archives/harfbuzz/2013-August/003490.html

                // We really want to find a 'vert' feature if there's any in the font, no
                // matter which script/langsys it is listed (or not) under.
                // See various bugs referenced from:
                // https://github.com/harfbuzz/harfbuzz/issues/63
                this.AddFeature(collection, index, count, VertTag);
            }

            // User defined fractional features require special treatment.
            // https://docs.microsoft.com/en-us/typography/opentype/spec/features_fj#tag-frac
            if (this.HasFractions())
            {
                this.AssignFractionalFeatures(collection, index, count);
            }

            // Add user defined features.
            foreach (Tag feature in this.featureTags)
            {
                // We've already dealt with fractional features.
                if (feature != FracTag && feature != NumrTag && feature != DnomTag)
                {
                    this.AddFeature(collection, index, count, feature);
                }
            }
        }

        protected void AddFeature(
            IGlyphShapingCollection collection,
            int index,
            int count,
            Tag feature,
            bool enabled = true,
            Action<IGlyphShapingCollection, int, int>? preAction = null,
            Action<IGlyphShapingCollection, int, int>? postAction = null)
        {
            if (this.kerningMode == KerningMode.None)
            {
                if (feature == KernTag || feature == VKernTag)
                {
                    return;
                }
            }

            int end = index + count;
            for (int i = index; i < end; i++)
            {
                collection.AddShapingFeature(i, new TagEntry(feature, enabled));
            }

            this.shapingStages.Add(new ShapingStage(feature, preAction, postAction));
        }

        public override IEnumerable<ShapingStage> GetShapingStages() => this.shapingStages;

        private void AssignFractionalFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            // Enable contextual fractions.
            for (int i = index; i < index + count; i++)
            {
                GlyphShapingData shapingData = collection.GetGlyphShapingData(i);
                if (shapingData.CodePoint == FractionSlash || shapingData.CodePoint == Slash)
                {
                    int start = i;
                    int end = i + 1;

                    // Apply numerator.
                    if (start > 0)
                    {
                        shapingData = collection.GetGlyphShapingData(start - 1);
                        while (start > 0 && CodePoint.IsDigit(shapingData.CodePoint))
                        {
                            this.AddFeature(collection, start - 1, 1, NumrTag);
                            this.AddFeature(collection, start - 1, 1, FracTag);
                            start--;
                        }
                    }

                    // Apply denominator.
                    if (end < collection.Count)
                    {
                        shapingData = collection.GetGlyphShapingData(end);
                        while (end < collection.Count && CodePoint.IsDigit(shapingData.CodePoint))
                        {
                            this.AddFeature(collection, end, 1, DnomTag);
                            this.AddFeature(collection, end, 1, FracTag);
                            end++;
                        }
                    }

                    // Apply fraction slash.
                    this.AddFeature(collection, i, 1, FracTag);
                    i = end - 1;
                }
            }
        }

        private bool HasFractions()
        {
            bool hasNmr = false;
            bool hasDnom = false;

            // My kingdom for a binary search on IReadOnlyList
            for (int i = 0; i < this.featureTags.Count; i++)
            {
                Tag feature = this.featureTags[i];
                if (feature == FracTag)
                {
                    return true;
                }

                if (feature == DnomTag)
                {
                    hasDnom = true;
                }

                if (feature == NumrTag)
                {
                    hasNmr = true;
                }

                if (hasDnom && hasNmr)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

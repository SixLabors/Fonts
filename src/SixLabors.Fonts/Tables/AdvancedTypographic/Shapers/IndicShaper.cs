// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// The IndicShaper supports Indic scripts e.g. Devanagari, Kannada, etc.
    /// </summary>
    internal sealed class IndicShaper : DefaultShaper
    {
        private static readonly StateMachine StateMachine =
            new(IndicShapingData.StateTable, IndicShapingData.AcceptingStates, IndicShapingData.Tags);

        private static readonly Tag RphfTag = Tag.Parse("rphf");
        private static readonly Tag NuktTag = Tag.Parse("nukt");
        private static readonly Tag AkhnTag = Tag.Parse("akhn");
        private static readonly Tag PrefTag = Tag.Parse("pref");

        private static readonly Tag RkrfTag = Tag.Parse("rkrf");
        private static readonly Tag AbvfTag = Tag.Parse("abvf");
        private static readonly Tag BlwfTag = Tag.Parse("blwf");
        private static readonly Tag HalfTag = Tag.Parse("half");
        private static readonly Tag PstfTag = Tag.Parse("pstf");
        private static readonly Tag VatuTag = Tag.Parse("vatu");
        private static readonly Tag CjctTag = Tag.Parse("cjct");
        private static readonly Tag CfarTag = Tag.Parse("cfar");

        private static readonly Tag InitTag = Tag.Parse("init");
        private static readonly Tag AbvsTag = Tag.Parse("abvs");
        private static readonly Tag BlwsTag = Tag.Parse("blws");
        private static readonly Tag PresTag = Tag.Parse("pres");
        private static readonly Tag PstsTag = Tag.Parse("psts");
        private static readonly Tag HalnTag = Tag.Parse("haln");
        private static readonly Tag DistTag = Tag.Parse("dist");
        private static readonly Tag AbvmTag = Tag.Parse("abvm");
        private static readonly Tag BlwmTag = Tag.Parse("blwm");

        public IndicShaper(ScriptClass script, TextOptions textOptions)
            : base(script, MarkZeroingMode.None, textOptions)
        {
        }

        protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            this.AddFeature(collection, index, count, LoclTag, preAction: SetupSyllables);
            this.AddFeature(collection, index, count, CcmpTag);

            this.AddFeature(collection, index, count, NuktTag, preAction: InitialReorder);
            this.AddFeature(collection, index, count, AkhnTag);

            this.AddFeature(collection, index, count, RphfTag);
            this.AddFeature(collection, index, count, RkrfTag);
            this.AddFeature(collection, index, count, PrefTag);
            this.AddFeature(collection, index, count, BlwfTag);
            this.AddFeature(collection, index, count, AbvfTag);
            this.AddFeature(collection, index, count, HalfTag);
            this.AddFeature(collection, index, count, PstfTag);
            this.AddFeature(collection, index, count, VatuTag);
            this.AddFeature(collection, index, count, CjctTag);
            this.AddFeature(collection, index, count, CfarTag, postAction: FinalReorder);

            this.AddFeature(collection, index, count, InitTag);
            this.AddFeature(collection, index, count, PresTag);
            this.AddFeature(collection, index, count, AbvsTag);
            this.AddFeature(collection, index, count, BlwsTag);
            this.AddFeature(collection, index, count, PstsTag);
            this.AddFeature(collection, index, count, HalnTag);
            this.AddFeature(collection, index, count, DistTag);
            this.AddFeature(collection, index, count, AbvmTag);
            this.AddFeature(collection, index, count, BlwmTag);

            // Setup the indic config for the selected script
        }

        private static void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
        {
            Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];

            for (int i = index; i < index + count; i++)
            {
                CodePoint codePoint = collection.GetGlyphShapingData(i).CodePoint;
                values[i - index] = IndicShapingCategory(codePoint);
            }

            int syllable = 0;
            int last = 0;
            foreach (StateMatch match in StateMachine.Match(values))
            {
                if (match.StartIndex > last)
                {
                    ++syllable;
                    for (int i = last; i < match.StartIndex; i++)
                    {
                        GlyphShapingData data = collection.GetGlyphShapingData(i + index);
                        data.IndicShapingEngineInfo = new(IndicShapingData.Categories.X, IndicShapingData.Positions.End, "non_indic_cluster", syllable);
                    }
                }

                ++syllable;

                // Create shaper info.
                for (int i = match.StartIndex; i <= match.EndIndex; i++)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(i + index);
                    CodePoint codePoint = data.CodePoint;

                    data.IndicShapingEngineInfo = new(
                        (IndicShapingData.Categories)(1 << IndicShapingCategory(codePoint)),
                        (IndicShapingData.Positions)IndicShapingPosition(codePoint),
                        match.Tags[0],
                        syllable);

                    string category = UniversalShapingData.Categories[UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value)];

                    data.UniversalShapingEngineInfo = new(category, match.Tags[0], syllable);
                }

                last = match.EndIndex + 1;
            }

            if (last < count)
            {
                ++syllable;
                for (int i = last; i < count; i++)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(i + index);
                    data.IndicShapingEngineInfo = new(IndicShapingData.Categories.X, IndicShapingData.Positions.End, "non_indic_cluster", syllable);
                }
            }
        }

        private static int IndicShapingCategory(CodePoint codePoint)
            => UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> 8;

        private static int IndicShapingPosition(CodePoint codePoint)
            => 1 << (UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) & 0xFF);

        private static void InitialReorder(IGlyphShapingCollection collection, int index, int count)
        {
        }

        private static void FinalReorder(IGlyphShapingCollection collection, int index, int count)
        {
        }
    }
}

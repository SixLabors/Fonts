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

        private const int DottedCircle = 0x25cc;

        private readonly TextOptions textOptions;
        private IndicShapingData.ShapingConfiguration indicConfiguration;

        public IndicShaper(ScriptClass script, TextOptions textOptions)
            : base(script, MarkZeroingMode.None, textOptions)
        {
            this.textOptions = textOptions;

            if (IndicShapingData.IndicConfigurations.ContainsKey(script))
            {
                this.indicConfiguration = IndicShapingData.IndicConfigurations[script];
            }
            else
            {
                this.indicConfiguration = IndicShapingData.ShapingConfiguration.Default;
            }
        }

        protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            this.AddFeature(collection, index, count, LoclTag, preAction: SetupSyllables);
            this.AddFeature(collection, index, count, CcmpTag);

            this.AddFeature(collection, index, count, NuktTag, preAction: this.InitialReorder);
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

        private void InitialReorder(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            IndicShapingData.ShapingConfiguration indicConfiguration = this.indicConfiguration;
            for (int i = 0; i < count; i++)
            {
                GlyphShapingData data = substitutionCollection.GetGlyphShapingData(i + index);
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                IndicShapingEngineInfo? info = data.IndicShapingEngineInfo;
                if (info?.Position == IndicShapingData.Positions.Base_C)
                {
                    CodePoint cp = new(indicConfiguration.Virama);
                    if (fontMetrics.TryGetGlyphId(cp, out ushort id))
                    {
                        GlyphShapingData virama = new(data, false)
                        {
                            GlyphId = id,
                            CodePoint = cp
                        };

                        info.Position = this.ConsonantPosition(new(data, false), virama);
                    }
                }
            }

            int max = index + count;
            int start = index;
            int end = NextSyllable(substitutionCollection, index, max);
            while (start < max)
            {
                GlyphShapingData data = substitutionCollection.GetGlyphShapingData(start);
                IndicShapingEngineInfo? info = data.IndicShapingEngineInfo;
                string? type = info?.SyllableType;

                if (type is "symbol_cluster" or "non_indic_cluster")
                {
                    goto Increment;
                }

                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                if (info != null && type == "broken_cluster" && fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort id))
                {
                    // Insert after possible Repha.
                    int i = start;
                    GlyphShapingData current = substitutionCollection.GetGlyphShapingData(i);
                    for (i = start; i < end; i++)
                    {
                        if (current.IndicShapingEngineInfo?.Category != IndicShapingData.Categories.Repha)
                        {
                            break;
                        }

                        current = substitutionCollection.GetGlyphShapingData(i);
                    }

                    Span<ushort> glyphs = stackalloc ushort[2];
                    glyphs[0] = current.GlyphId;
                    glyphs[1] = id;

                    substitutionCollection.Replace(i, glyphs);

                    // Update shaping info for newly inserted data.
                    GlyphShapingData dotted = substitutionCollection.GetGlyphShapingData(i + 1);
                    var dottedCategory = (IndicShapingData.Categories)(1 << IndicShapingCategory(dotted.CodePoint));
                    var dottedPosition = (IndicShapingData.Positions)IndicShapingPosition(dotted.CodePoint);
                    dotted.IndicShapingEngineInfo = new(dottedCategory, dottedPosition, info.SyllableType, info.Syllable);

                    end++;
                    max++;
                }

                // 1. Find base consonant:
                //
                // The shaping engine finds the base consonant of the syllable, using the
                // following algorithm: starting from the end of the syllable, move backwards
                // until a consonant is found that does not have a below-base or post-base
                // form (post-base forms have to follow below-base forms), or that is not a
                // pre-base reordering Ra, or arrive at the first consonant. The consonant
                // stopped at will be the base.
                int baseConsonant = end;
                int limit = start;
                bool hasReph = false;

                // If the syllable starts with Ra + Halant (in a script that has Reph)
                // and has more than one consonant, Ra is excluded from candidates for
                // base consonants.
                if (fontMetrics.TryGetGSubTable(out GSubTable? gSubTable) &&
                    start + 3 <= end &&
                    indicConfiguration.RephPosition != IndicShapingData.Positions.Ra_To_Become_Reph &&
                    gSubTable.TryGetFeatureLookups(in RphfTag, this.ScriptClass, out _))
                {
                    GlyphShapingData next = substitutionCollection.GetGlyphShapingData(start + 2);
                    if ((indicConfiguration.RephMode == IndicShapingData.RephMode.Implicit && !IsJoiner(next)) ||
                        (indicConfiguration.RephMode == IndicShapingData.RephMode.Explicit && next.IndicShapingEngineInfo?.Category == IndicShapingData.Categories.ZWJ))
                    {
                        // See if it matches the 'rphf' feature.

                        // TODO: Need to implement WouldSubstitute.
                    }
                }

                Increment:
                start = end;
                end = NextSyllable(substitutionCollection, start, max);
            }
        }

        private IndicShapingData.Positions ConsonantPosition(GlyphShapingData consonant, GlyphShapingData virama)
        {
            GlyphSubstitutionCollection collection = new(this.textOptions);

            collection.AddGlyph(virama, 0);
            collection.AddGlyph(consonant, 1);
            if (this.WouldSubstitute(collection, in BlwfTag, 0))
            {
                return IndicShapingData.Positions.Below_C;
            }

            collection.Clear();
            collection.AddGlyph(consonant, 0);
            collection.AddGlyph(virama, 1);

            if (this.WouldSubstitute(collection, in BlwfTag, 0))
            {
                return IndicShapingData.Positions.Below_C;
            }

            collection.Clear();
            collection.AddGlyph(virama, 0);
            collection.AddGlyph(consonant, 1);
            if (this.WouldSubstitute(collection, in PstfTag, 0))
            {
                return IndicShapingData.Positions.Post_C;
            }

            collection.Clear();
            collection.AddGlyph(consonant, 0);
            collection.AddGlyph(virama, 1);

            if (this.WouldSubstitute(collection, in PstfTag, 0))
            {
                return IndicShapingData.Positions.Post_C;
            }

            collection.Clear();
            collection.AddGlyph(virama, 0);
            collection.AddGlyph(consonant, 1);
            if (this.WouldSubstitute(collection, in PrefTag, 0))
            {
                return IndicShapingData.Positions.Post_C;
            }

            collection.Clear();
            collection.AddGlyph(consonant, 0);
            collection.AddGlyph(virama, 1);

            if (this.WouldSubstitute(collection, in PrefTag, 0))
            {
                return IndicShapingData.Positions.Post_C;
            }

            return IndicShapingData.Positions.Base_C;
        }

        private bool WouldSubstitute(GlyphSubstitutionCollection collection, in Tag featureTag, int index)
        {
            GlyphShapingData data = collection.GetGlyphShapingData(index);
            FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;

            if (fontMetrics.TryGetGSubTable(out GSubTable? gSubTable))
            {
                SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
                int initialCount = collection.Count;
                int collectionCount = initialCount;
                int count = initialCount - index;
                int i = index;

                // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
                int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
                int maxOperationsCount = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(collection.Count);
                int currentOperations = 0;

                gSubTable.ApplyFeature(
                    fontMetrics,
                    collection,
                    ref iterator,
                    in featureTag,
                    this.ScriptClass,
                    index,
                    ref count,
                    ref i,
                    ref collectionCount,
                    maxCount,
                    maxOperationsCount,
                    ref currentOperations);

                return collectionCount != initialCount;
            }

            return false;
        }

        private static bool IsConsonant(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & IndicShapingData.ConsonantFlags) != 0;

        private static bool IsJoiner(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & IndicShapingData.JoinerFlags) != 0;

        private static bool IsHalantOrCoeng(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & IndicShapingData.HalantOrCoengFlags) != 0;

        private static int NextSyllable(IGlyphShapingCollection collection, int index, int count)
        {
            if (index >= count)
            {
                return index;
            }

            int? syllable = collection.GetGlyphShapingData(index).IndicShapingEngineInfo?.Syllable;
            while (++index < count)
            {
                if (collection.GetGlyphShapingData(index).IndicShapingEngineInfo?.Syllable != syllable)
                {
                    break;
                }
            }

            return index;
        }

        private static void FinalReorder(IGlyphShapingCollection collection, int index, int count)
        {
        }
    }
}

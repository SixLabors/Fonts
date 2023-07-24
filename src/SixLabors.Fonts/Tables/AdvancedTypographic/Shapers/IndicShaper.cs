// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// The IndicShaper supports Indic scripts e.g. Devanagari, Kannada, etc.
    /// </summary>
    internal sealed class IndicShaper : DefaultShaper
    {
        private static readonly StateMachine StateMachine =
            new(StateTable, AcceptingStates, Tags);

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
        private ShapingConfiguration indicConfiguration;

        public IndicShaper(ScriptClass script, TextOptions textOptions)
            : base(script, MarkZeroingMode.None, textOptions)
        {
            this.textOptions = textOptions;

            if (IndicConfigurations.ContainsKey(script))
            {
                this.indicConfiguration = IndicConfigurations[script];
            }
            else
            {
                this.indicConfiguration = ShapingConfiguration.Default;
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
                CodePoint codePoint = collection[i].CodePoint;
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
                        GlyphShapingData data = collection[i + index];
                        data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
                    }
                }

                ++syllable;

                // Create shaper info.
                for (int i = match.StartIndex; i <= match.EndIndex; i++)
                {
                    GlyphShapingData data = collection[i + index];
                    CodePoint codePoint = data.CodePoint;

                    data.IndicShapingEngineInfo = new(
                        (Categories)(1 << IndicShapingCategory(codePoint)),
                        (Positions)IndicShapingPosition(codePoint),
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
                    GlyphShapingData data = collection[i + index];
                    data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
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

            // Create a reusable temporary substitution collection and buffer to allow checking whether
            // certain combinations will be substituted.
            GlyphSubstitutionCollection tempCollection = new(this.textOptions);
            Span<GlyphShapingData> tempBuffer = new GlyphShapingData[3];

            ShapingConfiguration indicConfiguration = this.indicConfiguration;
            for (int i = 0; i < count; i++)
            {
                GlyphShapingData data = substitutionCollection[i + index];
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                IndicShapingEngineInfo? info = data.IndicShapingEngineInfo;
                if (info?.Position == Positions.Base_C)
                {
                    CodePoint cp = new(indicConfiguration.Virama);
                    if (fontMetrics.TryGetGlyphId(cp, out ushort id))
                    {
                        GlyphShapingData virama = new(data, false)
                        {
                            GlyphId = id,
                            CodePoint = cp
                        };

                        tempBuffer[2] = virama;
                        tempBuffer[1] = data;
                        tempBuffer[0] = virama;

                        info.Position = this.ConsonantPosition(tempCollection, tempBuffer);
                    }
                }
            }

            int max = index + count;
            int start = index;
            int end = NextSyllable(substitutionCollection, index, max);
            while (start < max)
            {
                GlyphShapingData data = substitutionCollection[start];
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
                    GlyphShapingData current = substitutionCollection[i];
                    for (i = start; i < end; i++)
                    {
                        if (current.IndicShapingEngineInfo?.Category != Categories.Repha)
                        {
                            break;
                        }

                        current = substitutionCollection[i];
                    }

                    Span<ushort> glyphs = stackalloc ushort[2];
                    glyphs[0] = current.GlyphId;
                    glyphs[1] = id;

                    substitutionCollection.Replace(i, glyphs);

                    // Update shaping info for newly inserted data.
                    GlyphShapingData dotted = substitutionCollection[i + 1];
                    var dottedCategory = (Categories)(1 << IndicShapingCategory(dotted.CodePoint));
                    var dottedPosition = (Positions)IndicShapingPosition(dotted.CodePoint);
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
                    indicConfiguration.RephPosition != Positions.Ra_To_Become_Reph &&
                    gSubTable.TryGetFeatureLookups(in RphfTag, this.ScriptClass, out _))
                {
                    GlyphShapingData next = substitutionCollection[start + 2];
                    if ((indicConfiguration.RephMode == RephMode.Implicit && !IsJoiner(next)) ||
                        (indicConfiguration.RephMode == RephMode.Explicit && next.IndicShapingEngineInfo?.Category == Categories.ZWJ))
                    {
                        // See if it matches the 'rphf' feature.
                        tempBuffer[2] = substitutionCollection[start + 2];
                        tempBuffer[1] = substitutionCollection[start + 1];
                        tempBuffer[0] = substitutionCollection[start];

                        if ((indicConfiguration.RephMode == RephMode.Explicit && this.WouldSubstitute(tempCollection, in RphfTag, tempBuffer)) ||
                            this.WouldSubstitute(tempCollection, in RphfTag, tempBuffer.Slice(0, 2)))
                        {
                            limit += 2;
                            while (limit < end && IsJoiner(substitutionCollection[limit]))
                            {
                                limit++;
                            }

                            baseConsonant = start;
                            hasReph = true;
                        }
                    }
                }
                else if (indicConfiguration.RephMode == RephMode.Log_Repha &&
                    substitutionCollection[start].IndicShapingEngineInfo?.Category == Categories.Repha)
                {
                    limit++;
                    while (limit < end && IsJoiner(substitutionCollection[limit]))
                    {
                        limit++;
                    }

                    baseConsonant = start;
                    hasReph = true;
                }

                switch (indicConfiguration.BasePosition)
                {
                    case BasePosition.Last:

                        // Starting from the end of the syllable, move backwards
                        int i = end;
                        bool seenBelow = false;

                        do
                        {
                            IndicShapingEngineInfo? prevInfo = substitutionCollection[--i].IndicShapingEngineInfo;

                            // Until a consonant is found
                            if (IsConsonant(substitutionCollection[i]))
                            {
                                // that does not have a below-base or post-base form
                                // (post-base forms have to follow below-base forms),
                                if (prevInfo?.Position != Positions.Below_C && (prevInfo?.Position != Positions.Post_C || seenBelow))
                                {
                                    baseConsonant = i;
                                    break;
                                }

                                // or that is not a pre-base reordering Ra,
                                //
                                // IMPLEMENTATION NOTES:
                                //
                                // Our pre-base reordering Ra's are marked POS_POST_C, so will be skipped
                                // by the logic above already.
                                //

                                // or arrive at the first consonant. The consonant stopped at will
                                // be the base.
                                if (prevInfo?.Position == Positions.Below_C)
                                {
                                    seenBelow = true;
                                }

                                baseConsonant = i;
                            }
                            else if (start < i && prevInfo?.Category == Categories.ZWJ &&
                                substitutionCollection[i - 1].IndicShapingEngineInfo?.Category == Categories.H)
                            {
                                // A ZWJ after a Halant stops the base search, and requests an explicit
                                // half form.
                                // A ZWJ before a Halant, requests a subjoined form instead, and hence
                                // search continues.  This is particularly important for Bengali
                                // sequence Ra,H,Ya that should form Ya-Phalaa by subjoining Ya.
                                break;
                            }
                        }
                        while (i > limit);

                        break;
                    case BasePosition.First:
                        break;
                    default:
                        break;
                }

                Increment:
                start = end;
                end = NextSyllable(substitutionCollection, start, max);
            }
        }

        private Positions ConsonantPosition(GlyphSubstitutionCollection collection, ReadOnlySpan<GlyphShapingData> data)
        {
            if (this.WouldSubstitute(collection, in BlwfTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in BlwfTag, data.Slice(1, 2)))
            {
                return Positions.Below_C;
            }

            if (this.WouldSubstitute(collection, in PstfTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in PstfTag, data.Slice(1, 2)))
            {
                return Positions.Post_C;
            }

            if (this.WouldSubstitute(collection, in PrefTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in PrefTag, data.Slice(1, 2)))
            {
                return Positions.Post_C;
            }

            return Positions.Base_C;
        }

        private bool WouldSubstitute(GlyphSubstitutionCollection collection, in Tag featureTag, ReadOnlySpan<GlyphShapingData> buffer)
        {
            collection.Clear();
            for (int i = 0; i < buffer.Length; i++)
            {
                collection.AddGlyph(buffer[i], i);
            }

            GlyphShapingData data = buffer[0];
            FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;

            if (fontMetrics.TryGetGSubTable(out GSubTable? gSubTable))
            {
                int index = 0;
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
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & ConsonantFlags) != 0;

        private static bool IsJoiner(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & JoinerFlags) != 0;

        private static bool IsHalantOrCoeng(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & HalantOrCoengFlags) != 0;

        private static int NextSyllable(IGlyphShapingCollection collection, int index, int count)
        {
            if (index >= count)
            {
                return index;
            }

            int? syllable = collection[index].IndicShapingEngineInfo?.Syllable;
            while (++index < count)
            {
                if (collection[index].IndicShapingEngineInfo?.Syllable != syllable)
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

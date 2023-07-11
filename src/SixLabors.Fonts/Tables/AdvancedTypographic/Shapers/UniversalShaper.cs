// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// This shaper is an implementation of the Universal Shaping Engine, which
    /// uses Unicode data to shape a number of scripts without a dedicated shaping engine.
    /// <see href="https://www.microsoft.com/typography/OpenTypeDev/USE/intro.htm"/>.
    /// </summary>
    internal sealed class UniversalShaper : DefaultShaper
    {
        private static readonly StateMachine StateMachine =
            new(UniversalShapingData.StateTable, UniversalShapingData.AcceptingStates, UniversalShapingData.Tags);

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

        private static readonly Tag AbvsTag = Tag.Parse("abvs");
        private static readonly Tag BlwsTag = Tag.Parse("blws");
        private static readonly Tag PresTag = Tag.Parse("pres");
        private static readonly Tag PstsTag = Tag.Parse("psts");
        private static readonly Tag DistTag = Tag.Parse("dist");
        private static readonly Tag AbvmTag = Tag.Parse("abvm");
        private static readonly Tag BlwmTag = Tag.Parse("blwm");

        private const int DottedCircle = 0x25cc;

        public UniversalShaper(TextOptions textOptions)
           : base(MarkZeroingMode.PreGPos, textOptions)
        {
        }

        /// <inheritdoc/>
        protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            // Default glyph pre-processing group
            this.AddFeature(collection, index, count, LoclTag, preAction: SetupSyllables);
            this.AddFeature(collection, index, count, CcmpTag);
            this.AddFeature(collection, index, count, NuktTag);
            this.AddFeature(collection, index, count, AkhnTag);

            // Reordering group
            this.AddFeature(collection, index, count, RphfTag, true, ClearSubstitutionFlags, RecordRhpf);
            this.AddFeature(collection, index, count, PrefTag, true, ClearSubstitutionFlags, RecordPref);

            // Orthographic unit shaping group
            this.AddFeature(collection, index, count, RkrfTag);
            this.AddFeature(collection, index, count, AbvfTag);
            this.AddFeature(collection, index, count, BlwfTag);
            this.AddFeature(collection, index, count, HalfTag);
            this.AddFeature(collection, index, count, PstfTag);
            this.AddFeature(collection, index, count, VatuTag);
            this.AddFeature(collection, index, count, CjctTag, postAction: Reorder);

            // Standard topographic presentation and positional feature application
            this.AddFeature(collection, index, count, AbvsTag);
            this.AddFeature(collection, index, count, BlwsTag);
            this.AddFeature(collection, index, count, PresTag);
            this.AddFeature(collection, index, count, PstsTag);
            this.AddFeature(collection, index, count, DistTag);
            this.AddFeature(collection, index, count, AbvmTag);
            this.AddFeature(collection, index, count, BlwmTag);
        }

        /// <inheritdoc/>
        protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
            => DecomposeSplitVowels(collection, index, count);

        private static void DecomposeSplitVowels(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            int end = index + count;
            for (int i = end - 1; i >= 0; i--)
            {
                GlyphShapingData data = substitutionCollection.GetGlyphShapingData(i);
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                if (UniversalShapingData.Decompositions.TryGetValue(data.CodePoint.Value, out int[]? decompositions) && decompositions != null)
                {
                    Span<ushort> ids = stackalloc ushort[decompositions.Length];
                    for (int j = 0; j < decompositions.Length; j++)
                    {
                        // Font should always contain the decomposed glyph.
                        fontMetrics.TryGetGlyphId(new CodePoint(decompositions[j]), out ushort id);

                        ids[j] = id;
                    }

                    substitutionCollection.Replace(i, ids);
                }
            }
        }

        private static void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
        {
            Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];
            for (int i = index; i < index + count; i++)
            {
                CodePoint codePoint = collection.GetGlyphShapingData(i).CodePoint;
                values[i - index] = UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value);
            }

            int syllable = 0;
            foreach (StateMatch match in StateMachine.Match(values))
            {
                ++syllable;

                // Create shaper info
                for (int i = match.StartIndex; i <= match.EndIndex; i++)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(i + index);
                    CodePoint codePoint = data.CodePoint;
                    string category = UniversalShapingData.Categories[UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value)];

                    data.UniversalShapingEngineInfo = new(category, match.Tags[0], syllable);
                }

                // Assign rphf feature
                int limit = collection.GetGlyphShapingData(match.StartIndex).UniversalShapingEngineInfo!.Category == "R"
                    ? 1
                    : Math.Min(3, match.EndIndex - match.StartIndex);

                for (int i = match.StartIndex; i < match.StartIndex + limit; i++)
                {
                    collection.AddShapingFeature(i + index, new TagEntry(RcltTag, true));
                }
            }
        }

        private static void ClearSubstitutionFlags(IGlyphShapingCollection collection, int index, int count)
        {
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                GlyphShapingData data = collection.GetGlyphShapingData(i);
                data.IsSubstituted = false;
            }
        }

        private static void RecordRhpf(IGlyphShapingCollection collection, int index, int count)
        {
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                GlyphShapingData data = collection.GetGlyphShapingData(i);
                if (data.IsSubstituted && data.Features.Any(x => x.Tag == RphfTag))
                {
                    // Mark a substituted repha.
                    if (data.UniversalShapingEngineInfo != null)
                    {
                        data.UniversalShapingEngineInfo.Category = "R";
                    }
                }
            }
        }

        private static void RecordPref(IGlyphShapingCollection collection, int index, int count)
        {
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                GlyphShapingData data = collection.GetGlyphShapingData(i);
                if (data.IsSubstituted)
                {
                    // Mark a substituted pref as VPre, as they behave the same way.
                    if (data.UniversalShapingEngineInfo != null)
                    {
                        data.UniversalShapingEngineInfo.Category = "VPre";
                    }
                }
            }
        }

        private static void Reorder(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            int max = index + count;
            int start = index;
            int end = NextSyllable(substitutionCollection, index, max);
            while (start < max)
            {
                GlyphShapingData data = substitutionCollection.GetGlyphShapingData(start);
                UniversalShapingEngineInfo? info = data.UniversalShapingEngineInfo;
                string? type = info?.SyllableType;

                // Only a few syllable types need reordering.
                if (type is not "virama_terminated_cluster" and not "standard_cluster" and not "broken_cluster")
                {
                    goto Increment;
                }

                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                if (type == "broken_cluster" && fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort id))
                {
                    // Insert after possible Repha.
                    int i = start;
                    GlyphShapingData current = substitutionCollection.GetGlyphShapingData(i);
                    for (i = start; i < end; i++)
                    {
                        if (current.UniversalShapingEngineInfo?.Category != "R")
                        {
                            break;
                        }

                        current = substitutionCollection.GetGlyphShapingData(i);
                    }

                    Span<ushort> glyphs = stackalloc ushort[2];
                    glyphs[0] = current.GlyphId;
                    glyphs[1] = id;

                    substitutionCollection.Replace(i, glyphs);
                    end++;
                    max++;
                }

                // Move things forward
                if (info?.Category == "R" && end - start > 1)
                {
                    // Got a repha. Reorder it to after first base, before first halant.
                    for (int i = start + 1; i < end; i++)
                    {
                        GlyphShapingData current = substitutionCollection.GetGlyphShapingData(i);
                        info = current.UniversalShapingEngineInfo;
                        if (IsBase(info) || IsHalant(current))
                        {
                            // If we hit a halant, move before it; otherwise it's a base: move to it's
                            // place, and shift things in between backward.
                            if (IsHalant(current))
                            {
                                i--;
                            }

                            substitutionCollection.MoveGlyph(start, i);
                            break;
                        }
                    }
                }

                // Move things back
                for (int i = start, j = end; i < end; i++)
                {
                    GlyphShapingData current = substitutionCollection.GetGlyphShapingData(i);
                    info = current.UniversalShapingEngineInfo;

                    if (IsBase(info) || IsHalant(current))
                    {
                        // If we hit a halant, move after it; otherwise move to the beginning, and
                        // shift things in between forward.
                        if (IsHalant(current))
                        {
                            j = i + 1;
                        }
                        else
                        {
                            j = i;
                        }
                    }
                    else if ((info?.Category == "VPre" || info?.Category == "VMPre")
                        && current.LigatureComponent <= 0 // Only move the first component of a MultipleSubst
                        && j < i)
                    {
                        substitutionCollection.MoveGlyph(i, j);
                    }
                }

                Increment:
                start = end;
                end = NextSyllable(substitutionCollection, start, max);
            }
        }

        private static int NextSyllable(IGlyphShapingCollection collection, int index, int count)
        {
            if (index >= count)
            {
                return index;
            }

            int? syllable = collection.GetGlyphShapingData(index).UniversalShapingEngineInfo?.Syllable;
            while (++index < count)
            {
                if (collection.GetGlyphShapingData(index).UniversalShapingEngineInfo?.Syllable != syllable)
                {
                    break;
                }
            }

            return index;
        }

        private static bool IsHalant(GlyphShapingData data)
            => (data.UniversalShapingEngineInfo?.Category is "H" or "HVM" or "IS") && data.LigatureId == 0;

        private static bool IsBase(UniversalShapingEngineInfo? info)
            => info?.Category is "B" or "GB";
    }
}

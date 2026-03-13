// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// This shaper is an implementation of the Universal Shaping Engine, which
/// uses Unicode data to shape a number of scripts without a dedicated shaping engine.
/// <see href="https://www.microsoft.com/typography/OpenTypeDev/USE/intro.htm"/>.
/// </summary>
internal sealed class UniversalShaper : DefaultShaper
{
    /// <summary>The state machine for Universal Shaping Engine syllable identification.</summary>
    private static readonly StateMachine StateMachine =
        new(UniversalShapingData.StateTable, UniversalShapingData.AcceptingStates, UniversalShapingData.Tags);

    /// <summary>The 'rphf' (reph forms) feature tag.</summary>
    private static readonly Tag RphfTag = Tag.Parse("rphf");

    /// <summary>The 'nukt' (nukta forms) feature tag.</summary>
    private static readonly Tag NuktTag = Tag.Parse("nukt");

    /// <summary>The 'akhn' (akhands) feature tag.</summary>
    private static readonly Tag AkhnTag = Tag.Parse("akhn");

    /// <summary>The 'pref' (pre-base forms) feature tag.</summary>
    private static readonly Tag PrefTag = Tag.Parse("pref");

    /// <summary>The 'rkrf' (rakar forms) feature tag.</summary>
    private static readonly Tag RkrfTag = Tag.Parse("rkrf");

    /// <summary>The 'abvf' (above-base forms) feature tag.</summary>
    private static readonly Tag AbvfTag = Tag.Parse("abvf");

    /// <summary>The 'blwf' (below-base forms) feature tag.</summary>
    private static readonly Tag BlwfTag = Tag.Parse("blwf");

    /// <summary>The 'half' (half forms) feature tag.</summary>
    private static readonly Tag HalfTag = Tag.Parse("half");

    /// <summary>The 'pstf' (post-base forms) feature tag.</summary>
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    /// <summary>The 'vatu' (vattu variants) feature tag.</summary>
    private static readonly Tag VatuTag = Tag.Parse("vatu");

    /// <summary>The 'cjct' (conjunct forms) feature tag.</summary>
    private static readonly Tag CjctTag = Tag.Parse("cjct");

    /// <summary>The 'abvs' (above-base substitutions) feature tag.</summary>
    private static readonly Tag AbvsTag = Tag.Parse("abvs");

    /// <summary>The 'blws' (below-base substitutions) feature tag.</summary>
    private static readonly Tag BlwsTag = Tag.Parse("blws");

    /// <summary>The 'pres' (pre-base substitutions) feature tag.</summary>
    private static readonly Tag PresTag = Tag.Parse("pres");

    /// <summary>The 'psts' (post-base substitutions) feature tag.</summary>
    private static readonly Tag PstsTag = Tag.Parse("psts");

    /// <summary>The 'dist' (distances) feature tag.</summary>
    private static readonly Tag DistTag = Tag.Parse("dist");

    /// <summary>The 'abvm' (above-base mark positioning) feature tag.</summary>
    private static readonly Tag AbvmTag = Tag.Parse("abvm");

    /// <summary>The 'blwm' (below-base mark positioning) feature tag.</summary>
    private static readonly Tag BlwmTag = Tag.Parse("blwm");

    /// <summary>Dotted circle code point (U+25CC) used as a placeholder base.</summary>
    private const int DottedCircle = 0x25cc;

    /// <summary>The font metrics used for glyph lookups.</summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>Whether any broken clusters were detected during syllable setup.</summary>
    private bool hasBrokenClusters;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public UniversalShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
       : base(script, MarkZeroingMode.PreGPos, textOptions)
        => this.fontMetrics = fontMetrics;

    /// <inheritdoc/>
    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        // Default glyph pre-processing group
        this.AddFeature(collection, index, count, LoclTag, preAction: this.SetupSyllables);
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
        this.AddFeature(collection, index, count, CjctTag, postAction: this.Reorder);

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
        => this.DecomposeSplitVowels(collection, index, count);

    /// <summary>
    /// Decomposes split vowels into their constituent parts if supported by the font.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void DecomposeSplitVowels(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        FontMetrics fontMetrics = this.fontMetrics;
        Span<ushort> buffer = stackalloc ushort[16];
        int end = index + count;
        for (int i = end - 1; i >= index; i--)
        {
            GlyphShapingData data = substitutionCollection[i];
            if (UniversalShapingData.Decompositions.TryGetValue(data.CodePoint.Value, out int[]? decompositions) && decompositions != null)
            {
                Span<ushort> ids = buffer[..decompositions.Length];
                bool shouldDecompose = true;
                for (int j = 0; j < decompositions.Length; j++)
                {
                    if (!fontMetrics.TryGetGlyphId(new CodePoint(decompositions[j]), out ushort id))
                    {
                        shouldDecompose = false;
                        break;
                    }

                    ids[j] = id;
                }

                if (shouldDecompose)
                {
                    substitutionCollection.Replace(i, ids, FeatureTags.GlyphCompositionDecomposition);
                    for (int j = 0; j < decompositions.Length; j++)
                    {
                        substitutionCollection[i + j].CodePoint = new(decompositions[j]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Identifies syllables using the Universal Shaping Engine state machine and assigns shaping info to each glyph.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        this.hasBrokenClusters = false;

        Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];
        for (int i = index; i < index + count; i++)
        {
            CodePoint codePoint = substitutionCollection[i].CodePoint;
            values[i - index] = UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value);
        }

        int syllable = 0;
        foreach (StateMatch match in StateMachine.Match(values))
        {
            ++syllable;

            // Create shaper info
            for (int i = match.StartIndex; i <= match.EndIndex; i++)
            {
                GlyphShapingData data = substitutionCollection[i + index];
                CodePoint codePoint = data.CodePoint;
                string category = UniversalShapingData.Categories[UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value)];

                string syllableType = match.Tags[0];

                if (syllableType == "broken_cluster")
                {
                    this.hasBrokenClusters = true;
                }

                data.UniversalShapingEngineInfo = new(category, syllableType, syllable);
            }

            // Assign rphf feature
            int limit = substitutionCollection[match.StartIndex + index].UniversalShapingEngineInfo!.Category == "R"
                ? 1
                : Math.Min(3, match.EndIndex - match.StartIndex);

            for (int i = match.StartIndex; i < match.StartIndex + limit; i++)
            {
                substitutionCollection.AddShapingFeature(i + index, new TagEntry(RcltTag, true));
            }
        }
    }

    /// <summary>
    /// Clears substitution flags on all glyphs in the range, preparing for the next substitution pass.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void ClearSubstitutionFlags(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            GlyphShapingData data = substitutionCollection[i];
            data.IsSubstituted = false;
        }
    }

    /// <summary>
    /// Records glyphs substituted by the 'rphf' feature by marking their category as repha ("R").
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void RecordRhpf(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            GlyphShapingData data = substitutionCollection[i];
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

    /// <summary>
    /// Records glyphs substituted by the 'pref' feature by marking their category as pre-base vowel ("VPre").
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void RecordPref(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            GlyphShapingData data = substitutionCollection[i];
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

    /// <summary>
    /// Reorders glyphs within syllables, handling repha movement, pre-base vowel movement,
    /// and dotted circle insertion for broken clusters.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void Reorder(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        FontMetrics fontMetrics = this.fontMetrics;
        int max = index + count;
        int start = index;
        int end = NextSyllable(substitutionCollection, index, max);

        if (this.hasBrokenClusters)
        {
            if (fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort circleId))
            {
                Span<ushort> glyphs = stackalloc ushort[2];
                while (start < max)
                {
                    GlyphShapingData data = substitutionCollection[start];
                    UniversalShapingEngineInfo? info = data.UniversalShapingEngineInfo;
                    string? type = info?.SyllableType;

                    if (type == "broken_cluster")
                    {
                        // Insert after possible Repha.
                        int i = start;
                        for (i = start; i < end; i++)
                        {
                            if (substitutionCollection[i].UniversalShapingEngineInfo?.Category != "R")
                            {
                                break;
                            }
                        }

                        GlyphShapingData current = substitutionCollection[i];
                        UniversalShapingEngineInfo currentInfo = current.UniversalShapingEngineInfo!;
                        glyphs[0] = current.GlyphId;
                        glyphs[1] = circleId;

                        substitutionCollection.Replace(i, glyphs, FeatureTags.GlyphCompositionDecomposition);

                        // Update shaping info for newly inserted data.
                        GlyphShapingData dotted = substitutionCollection[i + 1];
                        dotted.UniversalShapingEngineInfo!.Category = "B";
                        dotted.UniversalShapingEngineInfo.SyllableType = currentInfo.SyllableType;
                        dotted.UniversalShapingEngineInfo.Syllable = currentInfo.Syllable;

                        end++;
                        max++;
                    }

                    start = end;
                    end = NextSyllable(substitutionCollection, start, max);
                }

                start = index;
                end = NextSyllable(substitutionCollection, index, max);
            }
        }

        while (start < max)
        {
            GlyphShapingData data = substitutionCollection[start];
            UniversalShapingEngineInfo? info = data.UniversalShapingEngineInfo;
            string? type = info?.SyllableType;

            // Only a few syllable types need reordering.
            if (type is not "virama_terminated_cluster" and not "standard_cluster" and not "broken_cluster")
            {
                // TODO: Check this. Harfbuzz seems to test more categories and returns.
                goto Increment;
            }

            // Move things forward
            if (info?.Category == "R" && end - start > 1)
            {
                // Got a repha. Reorder it to after first base, before first halant.
                for (int i = start + 1; i < end; i++)
                {
                    GlyphShapingData current = substitutionCollection[i];
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
            for (int i = start, j = start; i < end; i++)
            {
                GlyphShapingData current = substitutionCollection[i];
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

    /// <summary>
    /// Finds the start index of the next syllable in the collection.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="index">The current index.</param>
    /// <param name="count">The maximum index bound.</param>
    /// <returns>The start index of the next syllable.</returns>
    private static int NextSyllable(GlyphSubstitutionCollection collection, int index, int count)
    {
        if (index >= count)
        {
            return index;
        }

        int? syllable = collection[index].UniversalShapingEngineInfo?.Syllable;
        while (++index < count)
        {
            if (collection[index].UniversalShapingEngineInfo?.Syllable != syllable)
            {
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// Determines whether the glyph is a halant, halant-like, or invisible stacker character.
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a halant or equivalent.</returns>
    private static bool IsHalant(GlyphShapingData data)
        => (data.UniversalShapingEngineInfo?.Category is "H" or "HVM" or "IS") && !data.IsLigated;

    /// <summary>
    /// Determines whether the shaping info represents a base consonant or generic base.
    /// </summary>
    /// <param name="info">The universal shaping engine info.</param>
    /// <returns><see langword="true"/> if the glyph is a base.</returns>
    private static bool IsBase(UniversalShapingEngineInfo? info)
        => info?.Category is "B" or "GB";
}

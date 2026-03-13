// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Shaper for the Myanmar script. Handles syllable identification, reordering,
/// and application of Myanmar-specific OpenType features.
/// </summary>
internal sealed class MyanmarShaper : DefaultShaper
{
    /// <summary>The state machine for Myanmar syllable identification.</summary>
    private static readonly StateMachine StateMachine =
        new(
            Unicode.Resources.MyanmarShapingData.StateTable,
            Unicode.Resources.MyanmarShapingData.AcceptingStates,
            Unicode.Resources.MyanmarShapingData.Tags);

    /// <summary>Maps Myanmar shaping category codes to compact DFA symbol indices.</summary>
    private static readonly int[] CategoryToSymbolId = BuildCategoryToSymbolId();

    /// <summary>The 'rphf' (reph forms) feature tag.</summary>
    private static readonly Tag RphfTag = Tag.Parse("rphf");

    /// <summary>The 'pref' (pre-base forms) feature tag.</summary>
    private static readonly Tag PrefTag = Tag.Parse("pref");

    /// <summary>The 'blwf' (below-base forms) feature tag.</summary>
    private static readonly Tag BlwfTag = Tag.Parse("blwf");

    /// <summary>The 'pstf' (post-base forms) feature tag.</summary>
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    /// <summary>The 'pres' (pre-base substitutions) feature tag.</summary>
    private static readonly Tag PresTag = Tag.Parse("pres");

    /// <summary>The 'abvs' (above-base substitutions) feature tag.</summary>
    private static readonly Tag AbvsTag = Tag.Parse("abvs");

    /// <summary>The 'blws' (below-base substitutions) feature tag.</summary>
    private static readonly Tag BlwsTag = Tag.Parse("blws");

    /// <summary>The 'psts' (post-base substitutions) feature tag.</summary>
    private static readonly Tag PstsTag = Tag.Parse("psts");

    /// <summary>Dotted circle code point (U+25CC) used as a placeholder base.</summary>
    private const int DottedCircle = 0x25cc;

    /// <summary>The text options.</summary>
    private readonly TextOptions textOptions;

    /// <summary>The font metrics used for glyph lookups.</summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>Whether any broken clusters were detected during syllable setup.</summary>
    private bool hasBrokenClusters;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyanmarShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public MyanmarShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
       : base(script, MarkZeroingMode.PreGPos, textOptions)
    {
        this.textOptions = textOptions;
        this.fontMetrics = fontMetrics;
    }

    /// <inheritdoc />
    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        this.AddFeature(collection, index, count, LoclTag, preAction: this.SetupSyllables);
        this.AddFeature(collection, index, count, CcmpTag);

        this.AddFeature(collection, index, count, RphfTag, preAction: this.InitialReorder);
        this.AddFeature(collection, index, count, PrefTag);
        this.AddFeature(collection, index, count, BlwfTag);
        this.AddFeature(collection, index, count, PstfTag);

        this.AddFeature(collection, index, count, PresTag);
        this.AddFeature(collection, index, count, AbvsTag);
        this.AddFeature(collection, index, count, BlwsTag);
        this.AddFeature(collection, index, count, PstsTag);
    }

    /// <summary>
    /// Identifies Myanmar syllables using the state machine and assigns shaping info to each glyph.
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
            // Convert HarfBuzz-style Myanmar shaping categories into the compact
            // DFA symbol indices used by the generated state machine.
            //
            // HarfBuzz category codes (C=1, V=2, MR=36, VBlw=21, etc.) are sparse
            // and can be larger than the alphabet size of the DFA. Our state
            // machine expects its input alphabet to be dense 0..N-1, matching the
            // sequential IDs assigned in GenerateMyanmarShapingData.
            //
            // CategoryToSymbolId[(int)my] performs this mapping, ensuring that
            // every codepoint is presented to the DFA using the correct compact
            // symbol index.
            CodePoint codePoint = substitutionCollection[i].CodePoint;
            MyanmarCategories my = (MyanmarCategories)IndicShapingCategory(codePoint);
            values[i - index] = CategoryToSymbolId[(int)my];
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
                    GlyphShapingData data = substitutionCollection[i + index];
                    data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
                }
            }

            ++syllable;

            // Create shaper info.
            for (int i = match.StartIndex; i <= match.EndIndex; i++)
            {
                GlyphShapingData data = substitutionCollection[i + index];
                CodePoint codePoint = data.CodePoint;

                string syllableType = match.Tags[0];

                if (syllableType == "broken_cluster")
                {
                    this.hasBrokenClusters = true;
                }

                data.IndicShapingEngineInfo = new(
                    (Categories)IndicShapingCategory(codePoint),
                    (Positions)IndicShapingPosition(codePoint),
                    syllableType,
                    syllable);
            }

            last = match.EndIndex + 1;
        }

        if (last < count)
        {
            ++syllable;
            for (int i = last; i < count; i++)
            {
                GlyphShapingData data = substitutionCollection[i + index];
                data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
            }
        }
    }

    /// <summary>
    /// Performs the initial reordering pass for Myanmar consonant syllables, including
    /// dotted circle insertion for broken clusters.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void InitialReorder(IGlyphShapingCollection collection, int index, int count)
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
                    IndicShapingEngineInfo? dataInfo = data.IndicShapingEngineInfo;
                    string? type = dataInfo?.SyllableType;

                    if (type == "broken_cluster")
                    {
                        // Insert after possible Repha.
                        int i = start;
                        for (i = start; i < end; i++)
                        {
                            if (substitutionCollection[i].IndicShapingEngineInfo?.Category != Categories.Repha)
                            {
                                break;
                            }
                        }

                        GlyphShapingData current = substitutionCollection[i];
                        glyphs[0] = current.GlyphId;
                        glyphs[1] = circleId;

                        substitutionCollection.Replace(i, glyphs, FeatureTags.GlyphCompositionDecomposition);

                        // Update shaping info for newly inserted data.
                        GlyphShapingData dotted = substitutionCollection[i + 1];
                        dotted.IndicShapingEngineInfo!.Category = Categories.Dotted_Circle;

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
            IndicShapingEngineInfo? dataInfo = data.IndicShapingEngineInfo;
            string? type = dataInfo?.SyllableType;

            switch (type)
            {
                // We already inserted dotted-circles, so just call the consonant_syllable.
                case "broken_cluster":
                case "consonant_syllable":
                    ReorderConsonantSyllable(substitutionCollection, start, end);
                    break;
                default:
                    break;
            }

            start = end;
            end = NextSyllable(substitutionCollection, start, max);
        }
    }

    /// <summary>
    /// Reorders glyphs within a single Myanmar consonant syllable according to the Myanmar shaping spec.
    /// </summary>
    /// <param name="substitutionCollection">The glyph substitution collection.</param>
    /// <param name="start">The start index of the syllable.</param>
    /// <param name="end">The exclusive end index of the syllable.</param>
    private static void ReorderConsonantSyllable(GlyphSubstitutionCollection substitutionCollection, int start, int end)
    {
        int basePosition = end;
        bool hasReph = false;
        {
            int limit = start;
            if (start + 3 <= end &&
                substitutionCollection[start].IndicShapingEngineInfo?.MyanmarCategory == MyanmarCategories.Ra &&
                substitutionCollection[start + 1].IndicShapingEngineInfo?.MyanmarCategory == MyanmarCategories.As &&
                substitutionCollection[start + 2].IndicShapingEngineInfo?.MyanmarCategory == MyanmarCategories.H)
            {
                limit += 3;
                basePosition = start;
                hasReph = true;
            }

            {
                if (!hasReph)
                {
                    basePosition = limit;
                }

                for (int i = limit; i < end; i++)
                {
                    if (IsConsonant(substitutionCollection[i]))
                    {
                        basePosition = i;
                        break;
                    }
                }
            }
        }

        // Reorder
        {
            int i = start;
            for (; i < start + (hasReph ? 3 : 0); i++)
            {
                substitutionCollection[i].IndicShapingEngineInfo!.Position = Positions.After_Main;
            }

            for (; i < basePosition; i++)
            {
                substitutionCollection[i].IndicShapingEngineInfo!.Position = Positions.Pre_C;
            }

            if (i < end)
            {
                substitutionCollection[i].IndicShapingEngineInfo!.Position = Positions.Base_C;
                i++;
            }

            Positions pos = Positions.After_Main;

            // The following loop may be ugly, but it implements all of Myanmar reordering!
            for (; i < end; i++)
            {
                GlyphShapingData data = substitutionCollection[i];
                IndicShapingEngineInfo info = data.IndicShapingEngineInfo!;

                // Pre-base reordering
                if (info.MyanmarCategory == MyanmarCategories.MR)
                {
                    info.Position = Positions.Pre_C;
                    continue;
                }

                // Left matra
                if (info.MyanmarCategory == MyanmarCategories.VPre)
                {
                    info.Position = Positions.Pre_M;
                    continue;
                }

                if (info.MyanmarCategory == MyanmarCategories.VS)
                {
                    info.Position = substitutionCollection[i - 1].IndicShapingEngineInfo!.Position;
                    continue;
                }

                if (pos == Positions.After_Main && info.MyanmarCategory == MyanmarCategories.VBlw)
                {
                    pos = Positions.Below_C;
                    info.Position = pos;
                    continue;
                }

                if (pos == Positions.Below_C && info.MyanmarCategory == MyanmarCategories.A)
                {
                    info.Position = Positions.Before_Sub;
                    continue;
                }

                if (pos == Positions.Below_C && info.MyanmarCategory == MyanmarCategories.VBlw)
                {
                    info.Position = pos;
                    continue;
                }

                if (pos == Positions.Below_C && info.MyanmarCategory != MyanmarCategories.A)
                {
                    pos = Positions.After_Sub;
                    info.Position = pos;
                    continue;
                }

                info.Position = pos;
            }
        }

        substitutionCollection.Sort(start, end, (a, b) =>
        {
            int pa = a.IndicShapingEngineInfo?.Position != null ? (int)a.IndicShapingEngineInfo.Position : 0;
            int pb = b.IndicShapingEngineInfo?.Position != null ? (int)b.IndicShapingEngineInfo.Position : 0;
            return pa - pb;
        });

        // Flip left-matra sequence.
        int firstLeftMatra = end;
        int lastLeftMatra = end;

        for (int i = start; i < end; i++)
        {
            if (substitutionCollection[i].IndicShapingEngineInfo?.Position == Positions.Pre_M)
            {
                if (firstLeftMatra == end)
                {
                    firstLeftMatra = i;
                }

                lastLeftMatra = i;
            }
        }

        // https://github.com/harfbuzz/harfbuzz/issues/3863
        if (firstLeftMatra < lastLeftMatra)
        {
            // No need to merge clusters, done already?
            substitutionCollection.ReverseRange(firstLeftMatra, lastLeftMatra + 1);

            // Reverse back VS, etc.
            int i = firstLeftMatra;
            for (int j = i; j <= lastLeftMatra; j++)
            {
                if (substitutionCollection[j].IndicShapingEngineInfo?.MyanmarCategory == MyanmarCategories.VPre)
                {
                    substitutionCollection.ReverseRange(i, j + 1);
                    i = j + 1;
                }
            }
        }
    }

    /// <summary>
    /// Determines whether the glyph data represents a Myanmar consonant.
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a consonant.</returns>
    private static bool IsConsonant(GlyphShapingData data)
        => data.IndicShapingEngineInfo != null && (FlagUnsafe(data.IndicShapingEngineInfo.MyanmarCategory) & MyanmarConsonantFlags) != 0;

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

    /// <summary>
    /// Gets the Indic shaping category for a code point (upper 8 bits of the shaping properties).
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The shaping category value.</returns>
    private static int IndicShapingCategory(CodePoint codePoint)
        => UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> 8;

    /// <summary>
    /// Gets the Indic shaping position for a code point (lower 8 bits as a bit flag).
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The shaping position as a bit flag.</returns>
    private static int IndicShapingPosition(CodePoint codePoint)
        => 1 << (UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) & 0xFF);

    /// <summary>
    /// Builds a lookup table mapping Myanmar shaping category codes to compact DFA symbol indices.
    /// </summary>
    /// <returns>An array mapping category codes to symbol IDs.</returns>
    private static int[] BuildCategoryToSymbolId()
    {
        // Get all enum values in declared order (important!)
        MyanmarCategories[] values = Enum.GetValues<MyanmarCategories>();

        // Determine maximum underlying numeric category so we can index safetly
        int maxCategoryValue = 0;
        foreach (MyanmarCategories v in values)
        {
            int val = (int)v;
            if (val > maxCategoryValue)
            {
                maxCategoryValue = val;
            }
        }

        // Allocate mapping table indexed by Harfbuzz category code
        int[] map = new int[maxCategoryValue + 1];

        // Assign compact DFA symbol indices 0..N-1 in enum order
        for (int symbolId = 0; symbolId < values.Length; symbolId++)
        {
            MyanmarCategories cat = values[symbolId];
            int categoryCode = (int)cat;    // Harfbuzz-style category code
            map[categoryCode] = symbolId;   // DFA symbol id
        }

        return map;
    }
}

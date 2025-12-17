// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

internal sealed class MyanmarShaper : DefaultShaper
{
    private static readonly StateMachine StateMachine =
        new(
            Unicode.Resources.MyanmarShapingData.StateTable,
            Unicode.Resources.MyanmarShapingData.AcceptingStates,
            Unicode.Resources.MyanmarShapingData.Tags);

    private static readonly int[] CategoryToSymbolId = BuildCategoryToSymbolId();

    // Basic features.
    // These features are applied in order, one at a time, after reordering, constrained to the syllable.
    private static readonly Tag RphfTag = Tag.Parse("rphf");
    private static readonly Tag PrefTag = Tag.Parse("pref");
    private static readonly Tag BlwfTag = Tag.Parse("blwf");
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    // Other features.
    // These features are applied all at once, after clearing syllables.
    private static readonly Tag PresTag = Tag.Parse("pres");
    private static readonly Tag AbvsTag = Tag.Parse("abvs");
    private static readonly Tag BlwsTag = Tag.Parse("blws");
    private static readonly Tag PstsTag = Tag.Parse("psts");

    private const int DottedCircle = 0x25cc;

    private readonly TextOptions textOptions;
    private readonly FontMetrics fontMetrics;

    private bool hasBrokenClusters;

    public MyanmarShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
       : base(script, MarkZeroingMode.PreGPos, textOptions)
    {
        this.textOptions = textOptions;
        this.fontMetrics = fontMetrics;
    }

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

        FontMetrics fontMetrics = this.fontMetrics;

        bool hasDottedCircle = fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort circleId);
        _ = fontMetrics.TryGetGSubTable(out GSubTable? gSubTable);
        int max = index + count;
        int start = index;
        int end = NextSyllable(substitutionCollection, index, max);

        if (this.hasBrokenClusters)
        {
            Span<ushort> glyphs = stackalloc ushort[2];
            while (start < max)
            {
                GlyphShapingData data = substitutionCollection[start];
                IndicShapingEngineInfo? dataInfo = data.IndicShapingEngineInfo;
                string? type = dataInfo?.SyllableType;

                if (dataInfo != null && hasDottedCircle && type == "broken_cluster")
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

    private static bool IsConsonant(GlyphShapingData data)
        => data.IndicShapingEngineInfo != null && (FlagUnsafe(data.IndicShapingEngineInfo.MyanmarCategory) & MyanmarConsonantFlags) != 0;

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

    private static int IndicShapingCategory(CodePoint codePoint)
        => UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> 8;

    private static int IndicShapingPosition(CodePoint codePoint)
        => 1 << (UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) & 0xFF);

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

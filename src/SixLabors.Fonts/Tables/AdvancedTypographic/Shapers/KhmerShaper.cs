// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Shapes Khmer syllables and assigns the script's required substitution features.
/// </summary>
/// <remarks>
/// The behavior is transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer.cc</c>, symbols <c>collect_features_khmer</c>, <c>override_features_khmer</c>, <c>setup_syllables_khmer</c>, <c>reorder_khmer</c>, <c>reorder_consonant_syllable</c>, <c>decompose_khmer</c>, and <c>compose_khmer</c>, and <c>src/hb-ot-shaper-khmer-machine.rl</c>, symbol <c>khmer_syllable_machine</c>. The syllable grammar, feature assignment rules, and split-vowel decompositions are not derivable from the Unicode Character Database.
/// </remarks>
internal sealed class KhmerShaper : DefaultShaper
{
    /// <summary>
    /// The bit shift extracting the shaping category from the packed property word.
    /// </summary>
    private const int CategoryShift = 8;

    /// <summary>
    /// The first split vowel whose leading piece is U+17C1.
    /// </summary>
    private const int FirstSplitVowel = 0x17BE;

    /// <summary>
    /// The second split vowel whose leading piece is U+17C1.
    /// </summary>
    private const int SecondSplitVowel = 0x17BF;

    /// <summary>
    /// The third split vowel whose leading piece is U+17C1.
    /// </summary>
    private const int ThirdSplitVowel = 0x17C0;

    /// <summary>
    /// The fourth split vowel whose leading piece is U+17C1.
    /// </summary>
    private const int FourthSplitVowel = 0x17C4;

    /// <summary>
    /// The fifth split vowel whose leading piece is U+17C1.
    /// </summary>
    private const int FifthSplitVowel = 0x17C5;

    /// <summary>
    /// The pre-base piece inserted before each split vowel.
    /// </summary>
    private const int SplitVowelLeadingPiece = 0x17C1;

    /// <summary>
    /// The dotted circle inserted as the missing base of a broken syllable.
    /// </summary>
    private const int DottedCircle = 0x25CC;

    /// <summary>
    /// The state machine used to identify Khmer syllables.
    /// </summary>
    private static readonly StateMachine StateMachine = new(Unicode.Resources.KhmerShapingData.StateTable, Unicode.Resources.KhmerShapingData.AcceptingStates, Unicode.Resources.KhmerShapingData.Tags);

    /// <summary>
    /// The syllable type assigned by each accepting state.
    /// </summary>
    private static readonly SyllableType[] StateSyllableTypes = SyllableTypeMap.FromMachineTags(Unicode.Resources.KhmerShapingData.Tags);

    /// <summary>
    /// The pre-base forms feature.
    /// </summary>
    private static readonly Tag PrefTag = Tag.Parse("pref");

    /// <summary>
    /// The below-base forms feature.
    /// </summary>
    private static readonly Tag BlwfTag = Tag.Parse("blwf");

    /// <summary>
    /// The above-base forms feature.
    /// </summary>
    private static readonly Tag AbvfTag = Tag.Parse("abvf");

    /// <summary>
    /// The post-base forms feature.
    /// </summary>
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    /// <summary>
    /// The conjunct form after Ro feature.
    /// </summary>
    private static readonly Tag CfarTag = Tag.Parse("cfar");

    /// <summary>
    /// The pre-base substitutions feature.
    /// </summary>
    private static readonly Tag PresTag = Tag.Parse("pres");

    /// <summary>
    /// The above-base substitutions feature.
    /// </summary>
    private static readonly Tag AbvsTag = Tag.Parse("abvs");

    /// <summary>
    /// The below-base substitutions feature.
    /// </summary>
    private static readonly Tag BlwsTag = Tag.Parse("blws");

    /// <summary>
    /// The post-base substitutions feature.
    /// </summary>
    private static readonly Tag PstsTag = Tag.Parse("psts");

    /// <summary>
    /// The font metrics used to resolve the dotted-circle glyph.
    /// </summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>
    /// The combined syllable setup and initial reordering stage action.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> setupAndReorderAction;

    /// <summary>
    /// The action that clears syllable state after the basic features.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> clearSyllablesAction;

    /// <summary>
    /// Whether the current segment contains a syllable missing its base.
    /// </summary>
    private bool hasBrokenSyllables;

    /// <summary>
    /// Initializes a new instance of the <see cref="KhmerShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics used for glyph lookup.</param>
    public KhmerShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
        : base(script, MarkZeroingMode.None, textOptions)
    {
        this.FallbackMarkPositioning = false;
        this.fontMetrics = fontMetrics;
        this.setupAndReorderAction = this.SetupAndReorder;
        this.clearSyllablesAction = ClearSyllables;
        this.NormalizationMode = NormalizationMode.ComposedDiacriticsNoShortCircuit;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The five split-vowel pairs are transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer.cc</c>, symbol <c>decompose_khmer</c>. They are not canonical decompositions and are not derivable from the Unicode Character Database.
    /// </remarks>
    public override bool TryDecompose(CodePoint codePoint, out CodePoint first, out CodePoint second)
    {
        switch (codePoint.Value)
        {
            case FirstSplitVowel:
            case SecondSplitVowel:
            case ThirdSplitVowel:
            case FourthSplitVowel:
            case FifthSplitVowel:
                first = new CodePoint(SplitVowelLeadingPiece);
                second = codePoint;
                return true;
            default:
                return base.TryDecompose(codePoint, out first, out second);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The leading-mark exclusion is transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer.cc</c>, symbol <c>compose_khmer</c>. It is shaping behavior and is not derivable from the Unicode Character Database.
    /// </remarks>
    public override bool TryCompose(CodePoint first, CodePoint second, out CodePoint composed)
    {
        if (CodePoint.IsMark(first))
        {
            composed = default;
            return false;
        }

        return base.TryCompose(first, second, out composed);
    }

    /// <inheritdoc />
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
        ShapingFeatureFlags basicFlags = ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable;

        // Both source pauses precede the first lookup group and have no lookups between them, so one stage callback preserves their ordering without manufacturing an empty feature stage.
        this.EnableFeature(buffer, index, count, LoclTag, ShapingFeatureFlags.PerSyllable, this.setupAndReorderAction, null);
        this.EnableFeature(buffer, index, count, CcmpTag, ShapingFeatureFlags.PerSyllable);

        this.AddFeature(buffer, index, count, PrefTag, basicFlags, false, null, null);
        this.AddFeature(buffer, index, count, BlwfTag, basicFlags, false, null, null);
        this.AddFeature(buffer, index, count, AbvfTag, basicFlags, false, null, null);
        this.AddFeature(buffer, index, count, PstfTag, basicFlags, false, null, null);
        this.AddFeature(buffer, index, count, CfarTag, basicFlags, false, null, this.clearSyllablesAction);

        this.EnableFeature(buffer, index, count, PresTag, ShapingFeatureFlags.ManualJoiners);
        this.EnableFeature(buffer, index, count, AbvsTag, ShapingFeatureFlags.ManualJoiners);
        this.EnableFeature(buffer, index, count, BlwsTag, ShapingFeatureFlags.ManualJoiners);
        this.EnableFeature(buffer, index, count, PstsTag, ShapingFeatureFlags.ManualJoiners);
    }

    /// <inheritdoc />
    protected override void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count, bool isVertical)
    {
        base.PlanPostprocessingFeatures(buffer, index, count, isVertical);

        this.EnableFeature(buffer, index, count, CligTag);
        this.Features.DisableFeature(LigaTag);
    }

    /// <inheritdoc />
    protected override void AssignFeatures(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Identifies syllables and performs their initial reordering before substitution lookups run.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements.</param>
    private void SetupAndReorder(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        this.SetupSyllables(buffer, index, count);
        this.Reorder(plan, buffer, index, count);
    }

    /// <summary>
    /// Assigns the syllable and category information consumed by feature matching and reordering.
    /// </summary>
    /// <remarks>
    /// The grammar is transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer-machine.rl</c>, symbol <c>khmer_syllable_machine</c>. It is not derivable from the Unicode Character Database.
    /// </remarks>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements.</param>
    private void SetupSyllables(ShapingBuffer buffer, int index, int count)
    {
        this.hasBrokenSyllables = false;

        Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];
        Span<byte> categories = count <= 64 ? stackalloc byte[count] : new byte[count];
        ReadOnlySpan<byte> categoryToSymbolIds = Unicode.Resources.KhmerShapingData.CategoryToSymbolIds;
        for (int i = 0; i < count; i++)
        {
            int category = UnicodeData.GetIndicShapingProperties((uint)buffer[index + i].CodePoint.Value) >> CategoryShift;
            categories[i] = (byte)category;
            values[i] = categoryToSymbolIds[category];
        }

        int syllable = 0;
        int last = 0;
        StateMachine.MatchEnumerator match = StateMachine.EnumerateMatches(values);
        while (match.MoveNext())
        {
            // The fallback category is a one-character rule in the source machine, so any unmatched input also receives its own syllable number.
            while (last < match.StartIndex)
            {
                syllable++;
                ref GlyphShapingData unmatched = ref buffer[index + last];
                unmatched.Syllable.IndicCategory = Categories.X;
                unmatched.Syllable.IndicPosition = Positions.End;
                unmatched.Syllable.Type = SyllableType.NonIndicCluster;
                unmatched.Syllable.Number = syllable;
                last++;
            }

            syllable++;
            SyllableType syllableType = StateSyllableTypes[match.TagState];
            this.hasBrokenSyllables |= syllableType == SyllableType.BrokenCluster;

            for (int i = match.StartIndex; i <= match.EndIndex; i++)
            {
                ref GlyphShapingData data = ref buffer[index + i];
                data.Syllable.IndicCategory = (Categories)categories[i];
                data.Syllable.IndicPosition = Positions.End;
                data.Syllable.Type = syllableType;
                data.Syllable.Number = syllable;
            }

            last = match.EndIndex + 1;
        }

        while (last < count)
        {
            syllable++;
            ref GlyphShapingData unmatched = ref buffer[index + last];
            unmatched.Syllable.IndicCategory = Categories.X;
            unmatched.Syllable.IndicPosition = Positions.End;
            unmatched.Syllable.Type = SyllableType.NonIndicCluster;
            unmatched.Syllable.Number = syllable;
            last++;
        }
    }

    /// <summary>
    /// Inserts missing bases and reorders each consonant or broken syllable.
    /// </summary>
    /// <remarks>
    /// This pass is transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer.cc</c>, symbols <c>reorder_khmer</c>, <c>reorder_syllable_khmer</c>, and <c>reorder_consonant_syllable</c>. Its feature-mask assignment and movement rules are not derivable from the Unicode Character Database.
    /// </remarks>
    /// <param name="plan">The plan whose feature masks are assigned.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements.</param>
    private void Reorder(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        int max = index + count;
        int start = index;
        int end = NextSyllable(buffer, start, max);

        if (this.hasBrokenSyllables && this.fontMetrics.TryGetGlyphId(new CodePoint(DottedCircle), out ushort circleId))
        {
            while (start < max)
            {
                if (buffer[start].Syllable.Type == SyllableType.BrokenCluster)
                {
                    buffer.InsertDottedCircle(start, circleId);
                    buffer[start].Syllable.IndicCategory = Categories.Dotted_Circle;
                    buffer[start].Syllable.IndicPosition = Positions.End;
                    end++;
                    max++;
                }

                start = end;
                end = NextSyllable(buffer, start, max);
            }

            start = index;
            end = NextSyllable(buffer, start, max);
        }

        uint postBaseMask = plan.Features.GetMask(BlwfTag) | plan.Features.GetMask(AbvfTag) | plan.Features.GetMask(PstfTag);
        uint prefMask = plan.Features.GetMask(PrefTag);
        uint cfarMask = plan.Features.GetMask(CfarTag);

        while (start < max)
        {
            SyllableType type = buffer[start].Syllable.Type;
            if (type is SyllableType.ConsonantSyllable or SyllableType.BrokenCluster)
            {
                ReorderConsonantSyllable(buffer, start, end, postBaseMask, prefMask, cfarMask);
            }

            start = end;
            end = NextSyllable(buffer, start, max);
        }
    }

    /// <summary>
    /// Assigns basic feature masks and moves pre-base pieces within one syllable.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="start">The first record in the syllable.</param>
    /// <param name="end">The exclusive end of the syllable.</param>
    /// <param name="postBaseMask">The combined below-, above-, and post-base forms mask.</param>
    /// <param name="prefMask">The pre-base forms mask.</param>
    /// <param name="cfarMask">The conjunct form after Ro mask.</param>
    private static void ReorderConsonantSyllable(ShapingBuffer buffer, int start, int end, uint postBaseMask, uint prefMask, uint cfarMask)
    {
        for (int i = start + 1; i < end; i++)
        {
            buffer.EnableShapingFeature(i, postBaseMask);
        }

        int coengCount = 0;
        for (int i = start + 1; i < end; i++)
        {
            if (buffer[i].Syllable.IndicCategory == Categories.H && coengCount <= 2 && i + 1 < end)
            {
                coengCount++;

                if (buffer[i + 1].Syllable.IndicCategory == Categories.Ra)
                {
                    buffer.EnableShapingFeature(i, prefMask);
                    buffer.EnableShapingFeature(i + 1, prefMask);

                    buffer.CombineInputStarts(start, i + 2);

                    // Move the two records independently so their order remains H, Ra at the start.
                    buffer.MoveGlyph(i, start);
                    buffer.MoveGlyph(i + 1, start + 1);

                    for (int j = i + 2; j < end; j++)
                    {
                        buffer.EnableShapingFeature(j, cfarMask);
                    }

                    coengCount = 2;
                }
            }
            else if (buffer[i].Syllable.IndicCategory == Categories.VPre)
            {
                buffer.CombineInputStarts(start, i + 1);
                buffer.MoveGlyph(i, start);
            }
        }
    }

    /// <summary>
    /// Clears syllable state once the features constrained by it have run.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements.</param>
    private static void ClearSyllables(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            buffer[i].Syllable = default;
        }
    }

    /// <summary>
    /// Finds the exclusive end of the syllable beginning at the given index.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The first record in the syllable.</param>
    /// <param name="end">The exclusive segment end.</param>
    /// <returns>The exclusive end of the syllable.</returns>
    private static int NextSyllable(ShapingBuffer buffer, int index, int end)
    {
        if (index >= end)
        {
            return index;
        }

        int syllable = buffer[index].Syllable.Number;
        while (++index < end && buffer[index].Syllable.Number == syllable)
        {
        }

        return index;
    }
}

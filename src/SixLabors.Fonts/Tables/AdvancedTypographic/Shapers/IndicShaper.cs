// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic.GSub;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// The IndicShaper supports Indic scripts e.g. Devanagari, Kannada, etc.
/// </summary>
internal sealed class IndicShaper : DefaultShaper
{
    /// <summary>
    /// Devanagari letter RRA.
    /// </summary>
    private const int DevanagariLetterRra = 0x0931;

    /// <summary>
    /// Bengali letter RRA.
    /// </summary>
    private const int BengaliLetterRra = 0x09DC;

    /// <summary>
    /// Bengali letter RHA.
    /// </summary>
    private const int BengaliLetterRha = 0x09DD;

    /// <summary>
    /// Tamil letter AU.
    /// </summary>
    private const int TamilLetterAu = 0x0B94;

    /// <summary>
    /// Bengali letter YA.
    /// </summary>
    private const int BengaliLetterYa = 0x09AF;

    /// <summary>
    /// Bengali sign Nukta.
    /// </summary>
    private const int BengaliSignNukta = 0x09BC;

    /// <summary>
    /// Bengali letter YYA.
    /// </summary>
    private const int BengaliLetterYya = 0x09DF;

    /// <summary>
    /// The bit shift extracting the shaping category from a packed Indic shaping
    /// property word; the category occupies the upper byte.
    /// </summary>
    private const int IndicCategoryShift = 8;

    /// <summary>
    /// The mask extracting the zero-based shaping position from a packed Indic
    /// shaping property word; the position occupies the lower byte.
    /// </summary>
    private const int IndicPositionMask = 0xFF;

    /// <summary>
    /// The state machine for Indic syllable identification.
    /// </summary>
    private static readonly StateMachine StateMachine =
        new(StateTable, AcceptingStates, Tags);

    /// <summary>
    /// The syllable type for each machine state, translated from the tag rows once so
    /// match handling never maps rule name strings.
    /// </summary>
    private static readonly SyllableType[] StateSyllableTypes =
        SyllableTypeMap.FromMachineTags(Tags);

    /// <summary>
    /// Maps Indic shaping category codes to compact DFA symbol indices.
    /// </summary>
    private static readonly int[] CategoryToSymbolId = BuildCategoryToSymbolId();

    /// <summary>
    /// The 'rphf' (reph forms) feature tag.
    /// </summary>
    private static readonly Tag RphfTag = Tag.Parse("rphf");

    /// <summary>
    /// The 'nukt' (nukta forms) feature tag.
    /// </summary>
    private static readonly Tag NuktTag = Tag.Parse("nukt");

    /// <summary>
    /// The 'akhn' (akhands) feature tag.
    /// </summary>
    private static readonly Tag AkhnTag = Tag.Parse("akhn");

    /// <summary>
    /// The 'pref' (pre-base forms) feature tag.
    /// </summary>
    private static readonly Tag PrefTag = Tag.Parse("pref");

    /// <summary>
    /// The 'rkrf' (rakar forms) feature tag.
    /// </summary>
    private static readonly Tag RkrfTag = Tag.Parse("rkrf");

    /// <summary>
    /// The 'abvf' (above-base forms) feature tag.
    /// </summary>
    private static readonly Tag AbvfTag = Tag.Parse("abvf");

    /// <summary>
    /// The 'blwf' (below-base forms) feature tag.
    /// </summary>
    private static readonly Tag BlwfTag = Tag.Parse("blwf");

    /// <summary>
    /// The 'half' (half forms) feature tag.
    /// </summary>
    private static readonly Tag HalfTag = Tag.Parse("half");

    /// <summary>
    /// The 'pstf' (post-base forms) feature tag.
    /// </summary>
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    /// <summary>
    /// The 'vatu' (vattu variants) feature tag.
    /// </summary>
    private static readonly Tag VatuTag = Tag.Parse("vatu");

    /// <summary>
    /// The 'cjct' (conjunct forms) feature tag.
    /// </summary>
    private static readonly Tag CjctTag = Tag.Parse("cjct");

    /// <summary>
    /// The 'cfar' (conjunct form after Ra) feature tag.
    /// </summary>
    private static readonly Tag CfarTag = Tag.Parse("cfar");

    /// <summary>
    /// The 'init' (initial forms) feature tag.
    /// </summary>
    private static readonly Tag InitTag = Tag.Parse("init");

    /// <summary>
    /// The 'abvs' (above-base substitutions) feature tag.
    /// </summary>
    private static readonly Tag AbvsTag = Tag.Parse("abvs");

    /// <summary>
    /// The 'blws' (below-base substitutions) feature tag.
    /// </summary>
    private static readonly Tag BlwsTag = Tag.Parse("blws");

    /// <summary>
    /// The 'pres' (pre-base substitutions) feature tag.
    /// </summary>
    private static readonly Tag PresTag = Tag.Parse("pres");

    /// <summary>
    /// The 'psts' (post-base substitutions) feature tag.
    /// </summary>
    private static readonly Tag PstsTag = Tag.Parse("psts");

    /// <summary>
    /// The 'haln' (halant forms) feature tag.
    /// </summary>
    private static readonly Tag HalnTag = Tag.Parse("haln");

    /// <summary>
    /// Dotted circle code point (U+25CC) used as a placeholder base.
    /// </summary>
    private const int DottedCircle = 0x25cc;

    /// <summary>
    /// The font metrics used for glyph lookups.
    /// </summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>
    /// The script-specific shaping configuration for this Indic script.
    /// </summary>
    private ShapingConfiguration indicConfiguration;

    /// <summary>
    /// Whether this font uses old-spec Indic script tags.
    /// </summary>
    private readonly bool isOldSpec;

    /// <summary>
    /// Whether feature probes disallow matching context outside the probed glyph
    /// sequence. New-spec scripts other than Malayalam match with zero context.
    /// </summary>
    private readonly bool zeroContext;

    /// <summary>
    /// Whether the substitution lookups used by Indic feature probes have been
    /// captured from the owning shaping plan.
    /// </summary>
    private bool probeLookupsResolved;

    /// <summary>
    /// The reph-form lookups used to test whether an initial Ra forms a reph.
    /// </summary>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)>? rphfProbeLookups;

    /// <summary>
    /// The pre-base-form lookups used to test consonant positioning.
    /// </summary>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)>? prefProbeLookups;

    /// <summary>
    /// The below-base-form lookups used to test consonant positioning.
    /// </summary>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)>? blwfProbeLookups;

    /// <summary>
    /// The post-base-form lookups used to test consonant positioning.
    /// </summary>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)>? pstfProbeLookups;

    /// <summary>
    /// The vattu-form lookups used to test consonant positioning.
    /// </summary>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)>? vatuProbeLookups;

    /// <summary>
    /// Whether any broken clusters were detected during syllable setup.
    /// </summary>
    private bool hasBrokenClusters;

    /// <summary>
    /// The syllable setup pause, converted to a delegate once so per-pass feature
    /// planning never allocates for the conversion.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> setupSyllablesAction;

    /// <summary>
    /// The initial reorder pause, converted to a delegate once so per-pass feature
    /// planning never allocates for the conversion.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> initialReorderAction;

    /// <summary>
    /// The final reorder pause, converted to a delegate once so per-pass feature
    /// planning never allocates for the conversion.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> finalReorderAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndicShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="unicodeScriptTag">The Unicode script tag found in the font.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public IndicShaper(ScriptClass script, Tag unicodeScriptTag, TextOptions textOptions, FontMetrics fontMetrics)
        : base(script, MarkZeroingMode.None, textOptions)
    {
        this.FallbackMarkPositioning = false;
        this.fontMetrics = fontMetrics;
        this.setupSyllablesAction = this.SetupSyllables;
        this.initialReorderAction = this.InitialReorder;
        this.finalReorderAction = this.FinalReorder;

        // Every character comes apart first, even one the font already draws whole.
        // This shaper reads a syllable by its pieces and moves them about, so a vowel
        // written as one character has to be split into the pieces that are reordered;
        // were it left whole because the font can draw it, the reordering would never
        // see it.
        this.NormalizationMode = NormalizationMode.ComposedDiacriticsNoShortCircuit;

        if (IndicConfigurations.TryGetValue(script, out ShapingConfiguration value))
        {
            this.indicConfiguration = value;
        }
        else
        {
            this.indicConfiguration = ShapingConfiguration.Default;
        }

        this.isOldSpec = this.indicConfiguration.HasOldSpec && !unicodeScriptTag.ToString().EndsWith("2", StringComparison.OrdinalIgnoreCase);
        this.zeroContext = !this.isOldSpec && script != ScriptClass.Malayalam;
    }

    /// <inheritdoc />
    protected override void PreprocessText(ShapingBuffer buffer, int index, int count)
        => VowelConstraints.Insert(buffer, this.fontMetrics, this.ScriptClass, index, count);

    /// <inheritdoc />
    public override bool TryDecompose(CodePoint codePoint, out CodePoint first, out CodePoint second)
    {
        // Indic normalization keeps these four letters atomic even though canonical
        // decomposition data contains pairs for them. Fonts expect the original
        // letters during script-specific substitution.
        if (codePoint.Value is DevanagariLetterRra or BengaliLetterRra or BengaliLetterRha or TamilLetterAu)
        {
            first = default;
            second = default;
            return false;
        }

        return base.TryDecompose(codePoint, out first, out second);
    }

    /// <inheritdoc />
    public override bool TryCompose(CodePoint first, CodePoint second, out CodePoint composed)
    {
        // A split vowel begins with a mark and must remain decomposed for reordering.
        if (CodePoint.IsMark(first))
        {
            composed = default;
            return false;
        }

        if (first.Value == BengaliLetterYa && second.Value == BengaliSignNukta)
        {
            // This excluded canonical pair is intentionally restored to Bengali YYA
            // so fonts can address the letter as one substitution input.
            composed = new CodePoint(BengaliLetterYya);
            return true;
        }

        return base.TryCompose(first, second, out composed);
    }

    /// <inheritdoc />
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
        this.EnableFeature(buffer, index, count, LoclTag, ShapingFeatureFlags.PerSyllable, this.setupSyllablesAction, null);
        this.EnableFeature(buffer, index, count, CcmpTag, ShapingFeatureFlags.PerSyllable);

        this.EnableFeature(buffer, index, count, NuktTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, this.initialReorderAction, null);
        this.EnableFeature(buffer, index, count, AkhnTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);

        this.AddFeature(buffer, index, count, RphfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.EnableFeature(buffer, index, count, RkrfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.AddFeature(buffer, index, count, PrefTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.AddFeature(buffer, index, count, BlwfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.AddFeature(buffer, index, count, AbvfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.AddFeature(buffer, index, count, HalfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.AddFeature(buffer, index, count, PstfTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.EnableFeature(buffer, index, count, VatuTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, CjctTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.AddFeature(buffer, index, count, CfarTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, this.finalReorderAction);

        this.AddFeature(buffer, index, count, InitTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable, false, null, null);
        this.EnableFeature(buffer, index, count, PresTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, AbvsTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, BlwsTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, PstsTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, HalnTag, ShapingFeatureFlags.ManualJoiners | ShapingFeatureFlags.PerSyllable);
    }

    /// <inheritdoc />
    protected override void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count)
    {
        base.PlanPostprocessingFeatures(buffer, index, count);

        // Standard ligature substitution interferes with the conjunct forms these
        // scripts build through their dedicated features, so the feature is
        // disabled for the whole plan and its lookups are never collected.
        this.Features.DisableFeature(LigaTag);
    }

    /// <summary>
    /// Identifies Indic syllables using the state machine and assigns shaping info to each glyph.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void SetupSyllables(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        this.hasBrokenClusters = false;

        Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];
        Span<ushort> shapingProps = count <= 64 ? stackalloc ushort[count] : new ushort[count];

        for (int i = index; i < index + count; i++)
        {
            // Convert HarfBuzz-style Indic shaping categories into the compact
            // DFA symbol indices used by the generated state machine.
            //
            // HarfBuzz category codes (C=1, V=2, MR=36, VBlw=21, etc.) are sparse
            // and can be larger than the alphabet size of the DFA. Our state
            // machine expects its input alphabet to be dense 0..N-1, matching the
            // sequential IDs assigned in GenerateIndicShapingDataTrie.
            //
            // The property word is fetched once per glyph and stashed: the match
            // loop below derives both the category and position lanes from it
            // rather than walking the trie again.
            CodePoint codePoint = buffer[i].CodePoint;
            ushort props = (ushort)UnicodeData.GetIndicShapingProperties((uint)codePoint.Value);
            shapingProps[i - index] = props;
            values[i - index] = CategoryToSymbolId[props >> IndicCategoryShift];
        }

        int syllable = 0;
        int last = 0;
        StateMachine.MatchEnumerator match = StateMachine.EnumerateMatches(values);
        while (match.MoveNext())
        {
            if (match.StartIndex > last)
            {
                ++syllable;
                for (int i = last; i < match.StartIndex; i++)
                {
                    ref GlyphShapingData data = ref buffer[i + index];
                    data.Syllable.IndicCategory = Categories.X;
                    data.Syllable.IndicPosition = Positions.End;
                    data.Syllable.Type = SyllableType.NonIndicCluster;
                    data.Syllable.Number = syllable;
                }
            }

            ++syllable;

            SyllableType syllableType = StateSyllableTypes[match.TagState];
            if (syllableType == SyllableType.BrokenCluster)
            {
                this.hasBrokenClusters = true;
            }

            // Create shaper info.
            for (int i = match.StartIndex; i <= match.EndIndex; i++)
            {
                ref GlyphShapingData data = ref buffer[i + index];
                ushort props = shapingProps[i];

                data.Syllable.IndicCategory = (Categories)(props >> IndicCategoryShift);
                data.Syllable.IndicPosition = (Positions)((props & IndicPositionMask) + 1);
                data.Syllable.Type = syllableType;
                data.Syllable.Number = syllable;
            }

            last = match.EndIndex + 1;
        }

        if (last < count)
        {
            ++syllable;
            for (int i = last; i < count; i++)
            {
                ref GlyphShapingData data = ref buffer[i + index];
                data.Syllable.IndicCategory = Categories.X;
                data.Syllable.IndicPosition = Positions.End;
                data.Syllable.Type = SyllableType.NonIndicCluster;
                data.Syllable.Number = syllable;
            }
        }
    }

    /// <summary>
    /// Gets the Indic shaping category for a code point (upper 8 bits of the shaping properties).
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The shaping category value.</returns>
    private static int IndicShapingCategory(CodePoint codePoint)
        => UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> IndicCategoryShift;

    /// <summary>
    /// Gets the Indic shaping position for a code point. The trie stores the position
    /// zero-based; adding one maps it onto the ordinal enum whose zero is the
    /// unassigned sentinel.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The shaping position ordinal.</returns>
    private static int IndicShapingPosition(CodePoint codePoint)
        => (UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) & IndicPositionMask) + 1;

    /// <summary>
    /// Performs the initial reordering pass for Indic syllables, including base consonant
    /// identification, reph handling, matra reordering, and feature assignment.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void InitialReorder(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        if (!this.probeLookupsResolved)
        {
            // The shaper and plan have the same lifetime. Capture these resolved
            // feature lists once because consonant classification probes them
            // repeatedly for every Indic segment shaped by the cached plan.
            this.rphfProbeLookups = plan.TryGetGSubFeatureLookups(in RphfTag, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? rphfLookups) ? rphfLookups : null;
            this.prefProbeLookups = plan.TryGetGSubFeatureLookups(in PrefTag, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? prefLookups) ? prefLookups : null;
            this.blwfProbeLookups = plan.TryGetGSubFeatureLookups(in BlwfTag, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? blwfLookups) ? blwfLookups : null;
            this.pstfProbeLookups = plan.TryGetGSubFeatureLookups(in PstfTag, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? pstfLookups) ? pstfLookups : null;
            this.vatuProbeLookups = plan.TryGetGSubFeatureLookups(in VatuTag, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? vatuLookups) ? vatuLookups : null;
            this.probeLookupsResolved = true;
        }

        // Feature assignment revisits these tags throughout every syllable. Resolve
        // their plan masks once so the reorder loops do not repeatedly search the
        // same small feature lists.
        uint rphfMask = this.Features.GetMask(RphfTag);
        uint halfMask = this.Features.GetMask(HalfTag);
        uint blwfMask = this.Features.GetMask(BlwfTag);
        uint abvfMask = this.Features.GetMask(AbvfTag);
        uint pstfMask = this.Features.GetMask(PstfTag);
        uint prefMask = this.Features.GetMask(PrefTag);
        uint cfarMask = this.Features.GetMask(CfarTag);

        // Reusable glyph id span for feature probes. Hoisted out of the syllable loop
        // because a stack allocation inside the loop body would grow the stack once
        // per syllable for the lifetime of the call.
        Span<ushort> probeGlyphs = stackalloc ushort[3];

        ShapingConfiguration indicConfiguration = this.indicConfiguration;
        FontMetrics fontMetrics = this.fontMetrics;
        CodePoint viramaPoint = new(indicConfiguration.Virama);

        if (fontMetrics.TryGetGlyphId(viramaPoint, out ushort viramaId))
        {
            for (int i = 0; i < count; i++)
            {
                ref GlyphShapingData data = ref buffer[i + index];

                if (data.Syllable.IndicPosition == Positions.Base_C)
                {
                    data.Syllable.IndicPosition = this.ConsonantPosition(viramaId, data.GlyphId);
                }
            }
        }

        int max = index + count;
        int start = index;
        int end = NextSyllable(buffer, index, max);

        if (this.hasBrokenClusters)
        {
            if (fontMetrics.TryGetGlyphId(new CodePoint(DottedCircle), out ushort circleId))
            {
                Span<ushort> glyphs = stackalloc ushort[2];
                while (start < max)
                {
                    if (buffer[start].Syllable.Type == SyllableType.BrokenCluster)
                    {
                        // Insert after possible Repha.
                        int i = start;
                        for (i = start; i < end; i++)
                        {
                            if (buffer[i].Syllable.IndicCategory != Categories.Repha)
                            {
                                break;
                            }
                        }

                        ref GlyphShapingData current = ref buffer[i];
                        glyphs[0] = circleId;
                        glyphs[1] = current.GlyphId;

                        buffer.Replace(i, glyphs, KnownFeatureTags.GlyphCompositionDecomposition);

                        // The dotted circle is now at position i (inherits original shaping info).
                        // Update it to be a dotted circle base.
                        ref GlyphShapingData dotted = ref buffer[i];
                        dotted.Syllable.IndicCategory = Categories.Dotted_Circle;
                        dotted.Syllable.IndicPosition = Positions.End;

                        // The original mark glyph is now at position i + 1 (copy of original info).
                        // Its shaping info is already correct from the copy.
                        end++;
                        max++;
                    }

                    start = end;
                    end = NextSyllable(buffer, start, max);
                }

                start = index;
                end = NextSyllable(buffer, index, max);
            }
        }

        while (start < max)
        {
            if (buffer[start].Syllable.Type is SyllableType.SymbolCluster or SyllableType.NonIndicCluster)
            {
                goto Increment;
            }

            // Kannada preserves a legacy spelling in which Ra + Halant + ZWJ
            // behaves as Ra + ZWJ + Halant. Move only the shaping records; source
            // offsets remain attached to their logical text slots.
            if (this.ScriptClass == ScriptClass.Kannada
                && start + 3 <= end
                && !buffer[start].IsLigated
                && buffer[start].Syllable.IndicCategory == Categories.Ra
                && !buffer[start + 1].IsLigated
                && buffer[start + 1].Syllable.IndicCategory == Categories.H
                && !buffer[start + 2].IsLigated
                && buffer[start + 2].Syllable.IndicCategory == Categories.ZWJ)
            {
                buffer.CombineInputStarts(start + 1, start + 3);
                buffer.MoveGlyph(start + 2, start + 1);
            }

            // 1. Find base consonant:
            //
            // The shaping engine finds the base consonant of the syllable, using the
            // following algorithm: starting from the end of the syllable, move backwards
            // until a consonant is found that does not have a below-base or post-base
            // form (post-base forms have to follow below-base forms), or that is not a
            // pre-base reordering Ra, or arrive at the first consonant. The consonant
            // stopped at will be the base.
            int basePosition = end;
            int limit = start;
            bool hasReph = false;

            // If the syllable starts with Ra + Halant (in a script that has Reph)
            // and has more than one consonant, Ra is excluded from candidates for
            // base consonants.
            if (start + 3 <= end &&
                indicConfiguration.RephPosition != Positions.Ra_To_Become_Reph &&
                this.rphfProbeLookups is not null &&
                ((indicConfiguration.RephMode == RephMode.Implicit && !IsJoiner(ref buffer[start + 2])) ||
                 (indicConfiguration.RephMode == RephMode.Explicit && buffer[start + 2].Syllable.IndicCategory == Categories.ZWJ)))
            {
                // See if it matches the 'rphf' feature.
                probeGlyphs[0] = buffer[start].GlyphId;
                probeGlyphs[1] = buffer[start + 1].GlyphId;
                probeGlyphs[2] = buffer[start + 2].GlyphId;

                if ((indicConfiguration.RephMode == RephMode.Explicit && this.WouldSubstitute(this.rphfProbeLookups, probeGlyphs)) ||
                    this.WouldSubstitute(this.rphfProbeLookups, probeGlyphs[..2]))
                {
                    limit += 2;
                    while (limit < end && IsJoiner(ref buffer[limit]))
                    {
                        limit++;
                    }

                    basePosition = start;
                    hasReph = true;
                }
            }
            else if (indicConfiguration.RephMode == RephMode.Log_Repha &&
                     buffer[start].Syllable.IndicCategory == Categories.Repha)
            {
                limit++;
                while (limit < end && IsJoiner(ref buffer[limit]))
                {
                    limit++;
                }

                basePosition = start;
                hasReph = true;
            }

            switch (indicConfiguration.BasePosition)
            {
                case BasePosition.Last:
                {
                    // Starting from the end of the syllable, move backwards
                    int i = end;
                    bool seenBelow = false;

                    do
                    {
                        ref GlyphShapingData prev = ref buffer[--i];

                        // Until a consonant is found
                        if (IsConsonant(ref prev))
                        {
                            // that does not have a below-base or post-base form
                            // (post-base forms have to follow below-base forms),
                            if (prev.Syllable.IndicPosition != Positions.Below_C && (prev.Syllable.IndicPosition != Positions.Post_C || seenBelow))
                            {
                                basePosition = i;
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
                            if (prev.Syllable.Type != SyllableType.None && prev.Syllable.IndicPosition == Positions.Below_C)
                            {
                                seenBelow = true;
                            }

                            basePosition = i;
                        }
                        else if (start < i && prev.Syllable.IndicCategory == Categories.ZWJ && prev.Syllable.Type != SyllableType.None &&
                                 buffer[i - 1].Syllable.IndicCategory == Categories.H)
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
                }

                case BasePosition.First:
                {
                    // The first consonant is always the base.
                    basePosition = start;

                    for (int i = basePosition + 1; i < end; i++)
                    {
                        ref GlyphShapingData c = ref buffer[i];
                        if (IsConsonant(ref c))
                        {
                            c.Syllable.IndicPosition = Positions.Below_C;
                        }
                    }

                    break;
                }
            }

            // If the syllable starts with Ra + Halant (in a script that has Reph)
            // and has more than one consonant, Ra is excluded from candidates for
            // base consonants.
            //
            //  Only do this for unforced Reph. (ie. not for Ra,H,ZWJ)
            if (hasReph && basePosition == start && limit - basePosition <= 2)
            {
                hasReph = false;
            }

            // 2. Decompose and reorder Matras:
            //
            // Each matra and any syllable modifier sign in the cluster are moved to the
            // appropriate position relative to the consonant(s) in the cluster. The
            // shaping engine decomposes two- or three-part matras into their constituent
            // parts before any repositioning. Matra characters are classified by which
            // consonant in a conjunct they have affinity for and are reordered to the
            // following positions:
            //
            //   o Before first half form in the syllable
            //   o After subjoined consonants
            //   o After post-form consonant
            //   o After main consonant (for above marks)
            //
            // IMPLEMENTATION NOTES:
            //
            // The normalize() routine has already decomposed matras for us, so we don't
            // need to worry about that.

            // 3.  Reorder marks to canonical order:
            //
            // Adjacent nukta and halant or nukta and vedic sign are always repositioned
            // if necessary, so that the nukta is first.
            //
            // IMPLEMENTATION NOTES:
            //
            // We don't need to do this: the normalize() routine already did this for us.

            // Reorder characters
            for (int i = start; i < basePosition; i++)
            {
                ref GlyphShapingData item = ref buffer[i];
                if (item.Syllable.Type != SyllableType.None)
                {
                    item.Syllable.IndicPosition = (Positions)Math.Min((int)Positions.Pre_C, (int)item.Syllable.IndicPosition);
                }
            }

            if (basePosition < end)
            {
                ref GlyphShapingData item = ref buffer[basePosition];
                if (item.Syllable.Type != SyllableType.None)
                {
                    item.Syllable.IndicPosition = Positions.Base_C;
                }
            }

            // Mark final consonants.  A final consonant is one appearing after a matra,
            // like in Khmer.
            for (int i = basePosition + 1; i < end; i++)
            {
                if (buffer[i].Syllable.IndicCategory == Categories.M)
                {
                    for (int j = i + 1; j < end; j++)
                    {
                        ref GlyphShapingData c = ref buffer[j];
                        if (IsConsonant(ref c))
                        {
                            c.Syllable.IndicPosition = Positions.Final_C;
                            break;
                        }
                    }

                    break;
                }
            }

            // Handle beginning Ra
            if (hasReph)
            {
                ref GlyphShapingData c = ref buffer[start];
                if (c.Syllable.Type != SyllableType.None)
                {
                    c.Syllable.IndicPosition = Positions.Ra_To_Become_Reph;
                }
            }

            // For old-style Indic script tags, move the first post-base Halant after
            // the last consonant. Kannada alone blocks the move when another halant
            // already terminates the sequence; the other old-style scripts move it
            // unconditionally.
            if (this.isOldSpec)
            {
                bool disallowDoubleHalants = this.ScriptClass == ScriptClass.Kannada;
                for (int i = basePosition + 1; i < end; i++)
                {
                    if (buffer[i].Syllable.IndicCategory == Categories.H)
                    {
                        int j;
                        for (j = end - 1; j > i; j--)
                        {
                            ref GlyphShapingData c = ref buffer[j];
                            if (IsConsonant(ref c) || (disallowDoubleHalants && c.Syllable.IndicCategory == Categories.H))
                            {
                                break;
                            }
                        }

                        if (j > i)
                        {
                            // The old-spec sequence is one shaping input range even
                            // when Kannada retains a final halant to avoid doubling it.
                            buffer.CombineInputStarts(i, j + 1);
                            if (buffer[j].Syllable.IndicCategory != Categories.H)
                            {
                                buffer.MoveGlyph(i, j);
                            }
                        }

                        break;
                    }
                }
            }

            // Attach misc marks to previous char to move with them.
            Positions lastPosition = Positions.Start;
            for (int i = start; i < end; i++)
            {
                ref GlyphShapingData item = ref buffer[i];
                if (item.Syllable.Type != SyllableType.None)
                {
                    Categories category = item.Syllable.IndicCategory;
                    if ((FlagUnsafe(category) & (JoinerFlags | Flag(Categories.N) | Flag(Categories.RS) | Flag(Categories.CM) | (HalantFlags & FlagUnsafe(category)))) != 0)
                    {
                        item.Syllable.IndicPosition = lastPosition;
                        if (category == Categories.H && item.Syllable.IndicPosition == Positions.Pre_M)
                        {
                            // Uniscribe doesn't move the Halant with Left Matra.
                            // TEST: U+092B,U+093F,U+094DE
                            // We follow.  This is important for the Sinhala
                            // U+0DDA split matra since it decomposes to U+0DD9,U+0DCA
                            // where U+0DD9 is a left matra and U+0DCA is the virama.
                            // We don't want to move the virama with the left matra.
                            // TEST: U+0D9A,U+0DDA
                            for (int j = i; j > start; j--)
                            {
                                // An unassigned record reads as position zero, matching
                                // the previous null semantics: keep scanning.
                                Positions pos = buffer[j - 1].Syllable.IndicPosition;
                                if (pos is not 0 and not Positions.Pre_M)
                                {
                                    item.Syllable.IndicPosition = pos;
                                    break;
                                }
                            }
                        }
                    }
                    else if (item.Syllable.IndicPosition != Positions.SMVD)
                    {
                        // If an MPst follows an SM, update the SM's position to match
                        // so they move together during reordering.
                        if (category == Categories.MPst
                            && i > start
                            && buffer[i - 1].Syllable.IndicCategory == Categories.SM)
                        {
                            buffer[i - 1].Syllable.IndicPosition = item.Syllable.IndicPosition;
                        }

                        lastPosition = item.Syllable.IndicPosition;
                    }
                }
            }

            // For post-base consonants let them own anything before them
            // since the last consonant or matra.
            int last = basePosition;
            for (int i = basePosition + 1; i < end; i++)
            {
                ref GlyphShapingData current = ref buffer[i];
                if (current.Syllable.Type != SyllableType.None)
                {
                    if (IsConsonant(ref current))
                    {
                        for (int j = last + 1; j < i; j++)
                        {
                            ref GlyphShapingData between = ref buffer[j];
                            if (between.Syllable.Type != SyllableType.None && between.Syllable.IndicPosition < Positions.SMVD)
                            {
                                between.Syllable.IndicPosition = current.Syllable.IndicPosition;
                            }
                        }

                        last = i;
                    }
                    else if ((FlagUnsafe(current.Syllable.IndicCategory) & (Flag(Categories.M) | Flag(Categories.MPst))) != 0)
                    {
                        last = i;
                    }
                }
            }

            buffer.Sort(start, end, (a, b) =>
            {
                int pa = (int)a.Syllable.IndicPosition;
                int pb = (int)b.Syllable.IndicPosition;
                return pa - pb;
            });

            // Stable position sorting groups every pre-base matra at the front but
            // leaves adjacent split-matra pieces in logical order. Reverse the full
            // pre-base range, then reverse each matra-led piece back independently;
            // this reverses the pieces without reversing the marks within a piece.
            int firstLeftMatra = end;
            int lastLeftMatra = end;
            basePosition = end;
            for (int i = start; i < end; i++)
            {
                if (buffer[i].Syllable.IndicPosition == Positions.Base_C)
                {
                    basePosition = i;
                    break;
                }

                if (buffer[i].Syllable.IndicPosition == Positions.Pre_M)
                {
                    if (firstLeftMatra == end)
                    {
                        firstLeftMatra = i;
                    }

                    lastLeftMatra = i;
                }
            }

            if (firstLeftMatra < lastLeftMatra)
            {
                buffer.ReverseRange(firstLeftMatra, lastLeftMatra + 1);

                uint matraFlags = Flag(Categories.M) | Flag(Categories.MPst);
                int pieceStart = firstLeftMatra;
                for (int i = pieceStart; i <= lastLeftMatra; i++)
                {
                    if ((FlagUnsafe(buffer[i].Syllable.IndicCategory) & matraFlags) != 0)
                    {
                        buffer.ReverseRange(pieceStart, i + 1);
                        pieceStart = i + 1;
                    }
                }
            }

            // Setup features now.

            // Reph.
            for (int i = start; i < end; i++)
            {
                if (buffer[i].Syllable.IndicPosition != Positions.Ra_To_Become_Reph)
                {
                    break;
                }

                buffer.EnableShapingFeature(i, rphfMask);
            }

            // Pre-base
            bool blwf = !this.isOldSpec && indicConfiguration.BlwfMode == BlwfMode.Pre_And_Post;
            for (int i = start; i < basePosition; i++)
            {
                buffer.EnableShapingFeature(i, halfMask);
                if (blwf)
                {
                    buffer.EnableShapingFeature(i, blwfMask);
                }
            }

            // Post-base
            for (int i = basePosition + 1; i < end; i++)
            {
                buffer.EnableShapingFeature(i, abvfMask);
                buffer.EnableShapingFeature(i, pstfMask);
                buffer.EnableShapingFeature(i, blwfMask);
            }

            if (this.isOldSpec && this.ScriptClass == ScriptClass.Devanagari)
            {
                // Old-spec eye-lash Ra needs special handling.
                // From the spec:
                //
                // "The feature 'below-base form' is applied to consonants
                // having below-base forms and following the base consonant.
                // The exception is vattu, which may appear below half forms
                // as well as below the base glyph. The feature 'below-base
                // form' will be applied to all such occurrences of Ra as well."
                //
                // Test case: U+0924,U+094D,U+0930,U+094d,U+0915
                // with Sanskrit 2003 font.
                //
                // Ra + Halant immediately before the base receives the below-base
                // feature. Earlier pairs also receive it unless ZWJ follows, because
                // that explicit joiner requests the eyelash form instead.
                for (int i = start; i + 1 < basePosition; i++)
                {
                    if (buffer[i].Syllable.IndicCategory == Categories.Ra
                        && buffer[i + 1].Syllable.IndicCategory == Categories.H
                        && (i + 2 == basePosition || buffer[i + 2].Syllable.IndicCategory != Categories.ZWJ))
                    {
                        buffer.EnableShapingFeature(i, blwfMask);
                        buffer.EnableShapingFeature(i + 1, blwfMask);
                    }
                }
            }

            const int prefLen = 2;
            if (basePosition + prefLen < end &&
                this.prefProbeLookups is not null)
            {
                // Find a Halant,Ra sequence and mark it for pre-base reordering processing.
                for (int i = basePosition + 1; i + prefLen - 1 < end; i++)
                {
                    probeGlyphs[0] = buffer[i].GlyphId;
                    probeGlyphs[1] = buffer[i + 1].GlyphId;
                    if (this.WouldSubstitute(this.prefProbeLookups, probeGlyphs[..2]))
                    {
                        for (int j = 0; j < prefLen; j++)
                        {
                            buffer.EnableShapingFeature(i++, prefMask);
                        }

                        // Mark the subsequent stuff with 'cfar'.  Used in Khmer.
                        // Read the feature spec.
                        // This allows distinguishing the following cases with MS Khmer fonts:
                        // U+1784,U+17D2,U+179A,U+17D2,U+1782
                        // U+1784,U+17D2,U+1782,U+17D2,U+179A
                        if (plan.TryGetGSubFeatureLookups(in CfarTag, out _))
                        {
                            while (i < end)
                            {
                                buffer.EnableShapingFeature(i, cfarMask);
                                i++;
                            }
                        }

                        break;
                    }
                }
            }

            // Apply ZWJ/ZWNJ effects
            for (int i = start + 1; i < end; i++)
            {
                ref GlyphShapingData current = ref buffer[i];
                if (IsJoiner(ref current))
                {
                    bool nonJoiner = current.Syllable.IndicCategory == Categories.ZWNJ;
                    int j = i;

                    do
                    {
                        j--;

                        // ZWJ/ZWNJ should disable CJCT.  They do that by simply
                        // being there, since we don't skip them for the CJCT
                        // feature (ie. F_MANUAL_ZWJ)

                        // A ZWNJ disables HALF.
                        if (nonJoiner)
                        {
                            buffer.DisableShapingFeature(j, halfMask);
                        }
                    }
                    while (j > start && !IsConsonant(ref buffer[j]));
                }
            }

            Increment:
            start = end;
            end = NextSyllable(buffer, start, max);
        }
    }

    /// <summary>
    /// Determines the positional class of a consonant by testing whether the
    /// virama-consonant and consonant-virama pairs would be substituted by the
    /// below-base, vattu, post-base, or pre-base forming features.
    /// </summary>
    /// <param name="virama">The virama glyph id.</param>
    /// <param name="consonant">The consonant glyph id.</param>
    /// <returns>The consonant's positional class.</returns>
    private Positions ConsonantPosition(ushort virama, ushort consonant)
    {
        Span<ushort> glyphs = stackalloc ushort[3];
        glyphs[0] = virama;
        glyphs[1] = consonant;
        glyphs[2] = virama;

        if (this.WouldSubstitute(this.blwfProbeLookups, glyphs[..2]) ||
            this.WouldSubstitute(this.blwfProbeLookups, glyphs.Slice(1, 2)) ||
            this.WouldSubstitute(this.vatuProbeLookups, glyphs[..2]) ||
            this.WouldSubstitute(this.vatuProbeLookups, glyphs.Slice(1, 2)))
        {
            return Positions.Below_C;
        }

        if (this.WouldSubstitute(this.pstfProbeLookups, glyphs[..2]) ||
            this.WouldSubstitute(this.pstfProbeLookups, glyphs.Slice(1, 2)))
        {
            return Positions.Post_C;
        }

        if (this.WouldSubstitute(this.prefProbeLookups, glyphs[..2]) ||
            this.WouldSubstitute(this.prefProbeLookups, glyphs.Slice(1, 2)))
        {
            return Positions.Post_C;
        }

        return Positions.Base_C;
    }

    /// <summary>
    /// Tests whether any lookup for a shaping feature would substitute the given
    /// glyph sequence without running the substitution.
    /// </summary>
    /// <param name="lookups">The feature's resolved substitution lookups.</param>
    /// <param name="glyphs">The glyph id sequence to test.</param>
    /// <returns><see langword="true"/> if a substitution would occur.</returns>
    private bool WouldSubstitute(List<(Tag Feature, ushort Index, LookupTable LookupTable)>? lookups, ReadOnlySpan<ushort> glyphs)
    {
        if (lookups is null)
        {
            return false;
        }

        for (int i = 0; i < lookups.Count; i++)
        {
            if (lookups[i].LookupTable.WouldApply(glyphs, this.zeroContext))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the glyph data represents an Indic consonant.
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a consonant.</returns>
    private static bool IsConsonant(ref GlyphShapingData data)
        => (FlagUnsafe(data.Syllable.IndicCategory) & ConsonantFlags) != 0;

    /// <summary>
    /// Determines whether the glyph data represents a joiner (ZWJ or ZWNJ).
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a joiner.</returns>
    private static bool IsJoiner(ref GlyphShapingData data)
        => (FlagUnsafe(data.Syllable.IndicCategory) & JoinerFlags) != 0;

    /// <summary>
    /// Determines whether the glyph data represents a halant or coeng character.
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a halant or coeng.</returns>
    private static bool IsHalant(ref GlyphShapingData data)
        => (FlagUnsafe(data.Syllable.IndicCategory) & HalantFlags) != 0;

    /// <summary>
    /// Finds the start index of the next syllable in the buffer.
    /// </summary>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="index">The current index.</param>
    /// <param name="count">The maximum index bound.</param>
    /// <returns>The start index of the next syllable.</returns>
    private static int NextSyllable(ShapingBuffer buffer, int index, int count)
    {
        if (index >= count)
        {
            return index;
        }

        int? syllable = buffer[index].Syllable.Number;
        while (++index < count)
        {
            if (buffer[index].Syllable.Number != syllable)
            {
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// Performs the final reordering pass for Indic syllables, repositioning reph,
    /// pre-base consonants, and pre-base matras after basic shaping.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void FinalReorder(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        int max = index + count;
        int start = index;
        int end = NextSyllable(buffer, index, max);
        FontMetrics fontMetrics = this.fontMetrics;
        uint prefMask = this.Features.GetMask(PrefTag);
        uint initMask = this.Features.GetMask(InitTag);

        while (start < max)
        {
            // 4. Final reordering:
            //
            // After the localized forms and basic shaping forms GSUB features have been
            // applied (see below), the shaping engine performs some final glyph
            // reordering before applying all the remaining font features to the entire
            // cluster.
            bool tryPref = this.prefProbeLookups is not null;

            // Find base consonant again.
            int basePosition = start;
            for (; basePosition < end; basePosition++)
            {
                if (buffer[basePosition].Syllable.IndicPosition >= Positions.Base_C)
                {
                    if (tryPref && basePosition + 1 < end)
                    {
                        for (int i = basePosition + 1; i < end; i++)
                        {
                            ref GlyphShapingData current = ref buffer[i];
                            if ((current.FeatureMask & prefMask) != 0)
                            {
                                // A pre-base candidate that did not finish as a
                                // ligature did not form. Treat the following glyph as
                                // the base so later matra movement respects the block.
                                if (!(current.IsSubstituted && current.IsLigated && !current.IsDecomposed))
                                {
                                    basePosition = i;
                                    while (basePosition < end && IsHalant(ref buffer[basePosition]))
                                    {
                                        basePosition++;
                                    }

                                    ref GlyphShapingData newBase = ref buffer[basePosition];
                                    if (newBase.Syllable.Type != SyllableType.None)
                                    {
                                        newBase.Syllable.IndicPosition = Positions.Base_C;
                                        tryPref = false;
                                    }
                                }

                                break;
                            }
                        }
                    }

                    // For Malayalam, skip over unformed below- (but NOT post-) forms.
                    if (this.ScriptClass == ScriptClass.Malayalam)
                    {
                        for (int i = basePosition + 1; i < end; i++)
                        {
                            while (i < end && IsJoiner(ref buffer[i]))
                            {
                                i++;
                            }

                            if (i == end || !IsHalant(ref buffer[i]))
                            {
                                break;
                            }

                            i++; // Skip halant.
                            while (i < end && IsJoiner(ref buffer[i]))
                            {
                                i++;
                            }

                            if (i < end)
                            {
                                ref GlyphShapingData current = ref buffer[i];
                                if (IsConsonant(ref current) && current.Syllable.IndicPosition == Positions.Below_C)
                                {
                                    basePosition = i;
                                    ref GlyphShapingData newBase = ref buffer[basePosition];
                                    if (newBase.Syllable.Type != SyllableType.None)
                                    {
                                        newBase.Syllable.IndicPosition = Positions.Base_C;
                                    }
                                }
                            }
                        }
                    }

                    if (start < basePosition && buffer[basePosition].Syllable.IndicPosition > Positions.Base_C)
                    {
                        basePosition--;
                    }

                    break;
                }
            }

            if (basePosition == end && start < basePosition && buffer[basePosition - 1].Syllable.IndicCategory == Categories.ZWJ)
            {
                basePosition--;
            }

            if (basePosition < end)
            {
                while (start < basePosition && (FlagUnsafe(buffer[basePosition].Syllable.IndicCategory) & (Flag(Categories.N) | HalantFlags)) != 0)
                {
                    basePosition--;
                }
            }

            // o Reorder matras:
            //
            // If a pre-base matra character had been reordered before applying basic
            // features, the glyph can be moved closer to the main consonant based on
            // whether half-forms had been formed. Actual position for the matra is
            // defined as "after last standalone halant glyph, after initial matra
            // position and before the main consonant". A halant followed by ZWJ is
            // not a valid destination, so the search continues toward the original
            // matra position. Halant followed by ZWNJ terminates the syllable in the
            // state machine and needs no special handling here.
            //
            // Otherwise there can't be any pre-base matra characters.
            if (start + 1 < end && start < basePosition)
            {
                // If we lost track of base, alas, position before last thingy.
                int newPos = basePosition == end ? basePosition - 2 : basePosition - 1;

                // Malayalam / Tamil do not have "half" forms or explicit virama forms.
                // The glyphs formed by 'half' are Chillus or ligated explicit viramas.
                // We want to position matra after them.
                if (this.ScriptClass is not ScriptClass.Malayalam and not ScriptClass.Tamil)
                {
                    bool searchAgain;
                    do
                    {
                        searchAgain = false;

                        // Post-base matras also delimit the search even though their
                        // category is distinct from ordinary matras.
                        uint destinationFlags = Flag(Categories.M) | Flag(Categories.MPst) | HalantFlags;
                        while (newPos > start && (FlagUnsafe(buffer[newPos].Syllable.IndicCategory) & destinationFlags) == 0)
                        {
                            newPos--;
                        }

                        ref GlyphShapingData current = ref buffer[newPos];
                        if (IsHalant(ref current) && current.Syllable.IndicPosition != Positions.Pre_M)
                        {
                            // A ZWJ preserves the half-form request and prevents this
                            // halant from pulling the matra inward. Continue searching
                            // before the halant instead of moving past the joiner.
                            if (newPos + 1 < end
                                && buffer[newPos + 1].Syllable.IndicCategory == Categories.ZWJ
                                && newPos > start)
                            {
                                newPos--;
                                searchAgain = true;
                            }
                        }
                        else
                        {
                            // No standalone halant was found, or this halant belongs
                            // to the pre-base matra itself, so retain the initial order.
                            newPos = start;
                        }
                    }
                    while (searchAgain);
                }

                if (start < newPos && buffer[newPos].Syllable.IndicPosition != Positions.Pre_M)
                {
                    // Now go see if there's actually any matras...
                    for (int i = newPos; i > start; i--)
                    {
                        if (buffer[i - 1].Syllable.IndicPosition == Positions.Pre_M)
                        {
                            int oldPos = i - 1;
                            if (oldPos < basePosition && basePosition <= newPos)
                            {
                                // Shouldn't actually happen.
                                basePosition--;
                            }

                            buffer.MoveGlyph(oldPos, newPos);
                            buffer.CombineInputStarts(newPos, Math.Min(end, basePosition + 1));
                            newPos--;
                        }
                    }
                }
                else
                {
                    for (int i = start; i < basePosition; i++)
                    {
                        if (buffer[i].Syllable.IndicPosition == Positions.Pre_M)
                        {
                            buffer.CombineInputStarts(i, Math.Min(end, basePosition + 1));
                            break;
                        }
                    }
                }
            }

            // o Reorder reph:
            //
            // Reph’s original position is always at the beginning of the syllable,
            // (i.e. it is not reordered at the character reordering stage). However,
            // it will be reordered according to the basic-forms shaping results.
            // Possible positions for reph, depending on the script, are; after main,
            // before post-base consonant forms, and after post-base consonant forms.

            // Two cases:
            //
            // - If repha is encoded as a sequence of characters (Ra,H or Ra,H,ZWJ), then
            //   we should only move it if the sequence ligated to the repha form.
            //
            // - If repha is encoded separately and in the logical position, we should only
            //   move it if it did NOT ligate.  If it ligated, it's probably the font trying
            //   to make it work without the reordering.
            ref GlyphShapingData original = ref buffer[start];
            if (start + 1 < end &&
                original.Syllable.IndicPosition == Positions.Ra_To_Become_Reph &&
                (original.Syllable.IndicCategory == Categories.Repha != (original.IsLigated && !original.IsDecomposed)))
            {
                int newRephPos = start;
                Positions rephPos = this.indicConfiguration.RephPosition;
                bool found = false;

                // 1. If reph should be positioned after post-base consonant forms,
                //    proceed to step 5.
                if (rephPos != Positions.After_Post)
                {
                    // 2. If the reph repositioning class is not after post-base: target
                    //    position is after the first explicit halant glyph between the
                    //    first post-reph consonant and last main consonant. If ZWJ or ZWNJ
                    //    are following this halant, position is moved after it. If such
                    //    position is found, this is the target position. Otherwise,
                    //    proceed to the next step.
                    //
                    //    Note: in old-implementation fonts, where classifications were
                    //    fixed in shaping engine, there was no case where reph position
                    //    will be found on this step.
                    newRephPos = start + 1;
                    while (newRephPos < basePosition && !IsHalant(ref buffer[newRephPos]))
                    {
                        newRephPos++;
                    }

                    if (newRephPos < basePosition && IsHalant(ref buffer[newRephPos]))
                    {
                        // ->If ZWJ or ZWNJ are following this halant, position is moved after it.
                        if (newRephPos + 1 < basePosition && IsJoiner(ref buffer[newRephPos + 1]))
                        {
                            newRephPos++;
                        }

                        found = true;
                    }

                    // 3. If reph should be repositioned after the main consonant: find the
                    //    first consonant not ligated with main, or find the first
                    //    consonant that is not a potential pre-base reordering Ra.
                    if (!found && rephPos == Positions.After_Main)
                    {
                        newRephPos = basePosition;
                        while (newRephPos + 1 < end && buffer[newRephPos + 1].Syllable.IndicPosition <= Positions.After_Main)
                        {
                            newRephPos++;
                        }

                        found = newRephPos < end;
                    }

                    // 4. If reph should be positioned before post-base consonant, find
                    //    first post-base classified consonant not ligated with main. If no
                    //    consonant is found, the target position should be before the
                    //    first matra, syllable modifier sign or vedic sign.
                    //
                    // This is our take on what step 4 is trying to say (and failing, BADLY).
                    if (!found && rephPos == Positions.After_Sub)
                    {
                        newRephPos = basePosition;
                        while (newRephPos + 1 < end
                            && buffer[newRephPos + 1].Syllable.IndicPosition is not Positions.Post_C and not Positions.After_Post and not Positions.SMVD)
                        {
                            newRephPos++;
                        }

                        found = newRephPos < end;
                    }
                }

                // 5. If no consonant is found in steps 3 or 4, move reph to a position
                //    immediately before the first post-base matra, syllable modifier
                //    sign or vedic sign that has a reordering class after the intended
                //    reph position. For example, if the reordering position for reph
                //    is post-main, it will skip above-base matras that also have a
                //    post-main position.
                if (!found)
                {
                    // Copied from step 2.
                    newRephPos = start + 1;
                    while (newRephPos < basePosition && !IsHalant(ref buffer[newRephPos]))
                    {
                        newRephPos++;
                    }

                    if (newRephPos < basePosition && IsHalant(ref buffer[newRephPos]))
                    {
                        // ->If ZWJ or ZWNJ are following this halant, position is moved after it.
                        if (newRephPos + 1 < basePosition && IsJoiner(ref buffer[newRephPos + 1]))
                        {
                            newRephPos++;
                        }

                        found = true;
                    }
                }

                // 6. Otherwise, reorder reph to the end of the syllable.
                if (!found)
                {
                    newRephPos = end - 1;
                    while (newRephPos > start && buffer[newRephPos].Syllable.IndicPosition == Positions.SMVD)
                    {
                        newRephPos--;
                    }

                    // If the Reph is to be ending up after a Matra,Halant sequence,
                    // position it before that Halant so it can interact with the Matra.
                    // However, if it's a plain Consonant,Halant we shouldn't do that.
                    // Uniscribe doesn't do this.
                    // TEST: U+0930,U+094D,U+0915,U+094B,U+094D
                    if (IsHalant(ref buffer[newRephPos]))
                    {
                        for (int i = basePosition + 1; i < newRephPos; i++)
                        {
                            if ((FlagUnsafe(buffer[i].Syllable.IndicCategory) & Flag(Categories.M)) != 0)
                            {
                                newRephPos--;
                            }
                        }
                    }
                }

                if (newRephPos != start)
                {
                    buffer.CombineInputStarts(start, newRephPos + 1);
                    buffer.MoveGlyph(start, newRephPos);
                }

                if (start < basePosition && basePosition <= newRephPos)
                {
                    basePosition--;
                }
            }

            // o Reorder pre-base reordering consonants:
            //
            // If a pre-base reordering consonant is found, reorder it according to
            // the following rules:
            if (tryPref && basePosition + 1 < end)
            {
                for (int i = basePosition + 1; i < end; i++)
                {
                    ref GlyphShapingData current = ref buffer[i];
                    if ((current.FeatureMask & prefMask) != 0)
                    {
                        // 1. Only reorder a glyph produced by substitution during application
                        //    of the <pref> feature. (Note that a font may shape a Ra consonant with
                        //    the feature generally but block it in certain contexts.)

                        // Note: We just check that something got substituted.  We don't check that
                        // the <pref> feature actually did it...
                        //
                        // Reorder pref only if it ligated.
                        if (current.IsLigated && !current.IsDecomposed)
                        {
                            // 2. Try to find a target position the same way as for pre-base matra.
                            //    If it is found, reorder pre-base consonant glyph.
                            //
                            // 3. If position is not found, reorder immediately before main
                            //    consonant.
                            int newPos = basePosition;

                            // Malayalam / Tamil do not have "half" forms or explicit virama forms.
                            // The glyphs formed by 'half' are Chillus or ligated explicit viramas.
                            // We want to position matra after them.
                            if (this.ScriptClass is not ScriptClass.Malayalam and not ScriptClass.Tamil)
                            {
                                while (newPos > start && (FlagUnsafe(buffer[newPos - 1].Syllable.IndicCategory) & (Flag(Categories.M) | HalantFlags)) == 0)
                                {
                                    newPos--;
                                }

                                // TODO: Remove once we have Kmher shaper.
                                // In Khmer coeng model, a H,Ra can go *after* matras.  If it goes after a
                                // split matra, it should be reordered to *before* the left part of such matra.
                                if (newPos > start && buffer[newPos - 1].Syllable.IndicCategory == Categories.M)
                                {
                                    int oldPos = i;
                                    for (int j = basePosition + 1; j < oldPos; j++)
                                    {
                                        if (buffer[j].Syllable.IndicCategory == Categories.M)
                                        {
                                            newPos--;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (newPos > start && IsHalant(ref buffer[newPos - 1]))
                            {
                                // -> If ZWJ or ZWNJ follow this halant, position is moved after it.
                                if (newPos < end && IsJoiner(ref buffer[newPos]))
                                {
                                    newPos++;
                                }
                            }

                            buffer.CombineInputStarts(newPos, i + 1);
                            buffer.MoveGlyph(i, newPos);

                            if (newPos <= basePosition && basePosition < i)
                            {
                                basePosition++;
                            }
                        }

                        break;
                    }
                }
            }

            // Apply 'init' to a left matra only at the start of a word. Letters,
            // marks, format controls, and the non-public character categories all
            // continue the preceding word for this feature.
            bool isInitialMatra = buffer[start].Syllable.IndicPosition == Positions.Pre_M;
            bool isWordStart = start == 0;
            if (isInitialMatra && !isWordStart)
            {
                UnicodeCategory previousCategory = CodePoint.GetGeneralCategory(buffer[start - 1].CodePoint);
                isWordStart = previousCategory is not (
                    UnicodeCategory.Format
                    or UnicodeCategory.OtherNotAssigned
                    or UnicodeCategory.PrivateUse
                    or UnicodeCategory.Surrogate
                    or UnicodeCategory.LowercaseLetter
                    or UnicodeCategory.ModifierLetter
                    or UnicodeCategory.OtherLetter
                    or UnicodeCategory.TitlecaseLetter
                    or UnicodeCategory.UppercaseLetter
                    or UnicodeCategory.SpacingCombiningMark
                    or UnicodeCategory.EnclosingMark
                    or UnicodeCategory.NonSpacingMark);
            }

            if (isInitialMatra && isWordStart)
            {
                buffer.EnableShapingFeature(start, initMask);
            }

            start = end;
            end = NextSyllable(buffer, start, max);
        }
    }

    /// <summary>
    /// Builds a lookup table mapping Indic shaping category codes to compact DFA symbol indices.
    /// </summary>
    /// <returns>An array mapping category codes to symbol IDs.</returns>
    private static int[] BuildCategoryToSymbolId()
    {
        // Get all enum values in declared order (important!)
        Categories[] values = Enum.GetValues<Categories>();

        // Determine maximum underlying numeric category so we can index safetly
        int maxCategoryValue = 0;
        foreach (Categories v in values)
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
            Categories cat = values[symbolId];
            int categoryCode = (int)cat;    // Harfbuzz-style category code
            map[categoryCode] = symbolId;   // DFA symbol id
        }

        return map;
    }
}

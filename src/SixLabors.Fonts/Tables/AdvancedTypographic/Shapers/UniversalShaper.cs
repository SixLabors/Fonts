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
    /// <summary>
    /// The generated category name table, captured once: the generated property
    /// allocates a fresh array on every access.
    /// </summary>
    private static readonly string[] CategoryNames = UniversalShapingData.Categories;

    /// <summary>
    /// Symbol indices for the categories compared during reordering, resolved once from
    /// the generated table so per-glyph category tests are integer comparisons. The
    /// symbol index is the value the state machine consumes, so glyphs never
    /// materialize category name strings.
    /// </summary>
    private static readonly int CategoryB = Array.IndexOf(CategoryNames, "B");

    /// <summary>
    /// The symbol index for characters omitted from syllable-machine input.
    /// </summary>
    private static readonly int CategoryCGJ = Array.IndexOf(CategoryNames, "CGJ");

    private static readonly int CategoryH = Array.IndexOf(CategoryNames, "H");

    private static readonly int CategoryHVM = Array.IndexOf(CategoryNames, "HVM");

    private static readonly int CategoryIS = Array.IndexOf(CategoryNames, "IS");

    private static readonly int CategoryR = Array.IndexOf(CategoryNames, "R");

    private static readonly int CategoryVPre = Array.IndexOf(CategoryNames, "VPre");

    private static readonly int CategoryVMPre = Array.IndexOf(CategoryNames, "VMPre");

    /// <summary>
    /// The symbol index for a non-joiner whose inclusion depends on the following character.
    /// </summary>
    private static readonly int CategoryZwnj = Array.IndexOf(CategoryNames, "ZWNJ");

    /// <summary>
    /// The state machine for Universal Shaping Engine syllable identification.
    /// </summary>
    private static readonly StateMachine StateMachine =
        new(UniversalShapingData.StateTable, UniversalShapingData.AcceptingStates, UniversalShapingData.Tags);

    /// <summary>
    /// The syllable type for each machine state, translated from the tag rows once so
    /// match handling never maps rule name strings.
    /// </summary>
    private static readonly SyllableType[] StateSyllableTypes =
        SyllableTypeMap.FromMachineTags(UniversalShapingData.Tags);

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
    /// Whether any broken clusters were detected during syllable setup.
    /// </summary>
    private bool hasBrokenClusters;

    /// <summary>
    /// The syllable setup pause, converted to a delegate once so per-pass feature
    /// planning never allocates for the conversion.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> setupSyllablesAction;

    /// <summary>
    /// The reorder pause, converted to a delegate once so per-pass feature
    /// planning never allocates for the conversion.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> reorderAction;

    /// <summary>
    /// The pause separating topographical substitutions from later presentation substitutions.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> pauseAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public UniversalShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
       : base(script, MarkZeroingMode.PreGPos, textOptions)
    {
        this.FallbackMarkPositioning = false;
        this.fontMetrics = fontMetrics;
        this.setupSyllablesAction = this.SetupSyllables;
        this.reorderAction = this.Reorder;
        this.pauseAction = Pause;

        // Every character comes apart first, even one the font already draws whole.
        // This shaper divides a run into syllables and moves their pieces about, so a
        // character standing for several pieces must be split before the division can
        // see them.
        this.NormalizationMode = NormalizationMode.ComposedDiacriticsNoShortCircuit;
    }

    /// <inheritdoc/>
    protected override void PreprocessText(ShapingBuffer buffer, int index, int count)
        => VowelConstraints.Insert(buffer, this.fontMetrics, this.ScriptClass, index, count);

    /// <inheritdoc/>
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
        // These language and composition features establish the glyph forms consumed
        // by every later USE stage. Applying them per syllable prevents a contextual
        // lookup from crossing an orthographic-unit boundary.
        this.EnableFeature(buffer, index, count, LoclTag, ShapingFeatureFlags.PerSyllable, this.setupSyllablesAction, null);
        this.EnableFeature(buffer, index, count, CcmpTag, ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, NuktTag, ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, AkhnTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);

        // Reordering group. The repha feature varies per glyph: syllable setup
        // enables it on each syllable's leading glyphs only, so a repha forms
        // there and nowhere else.
        this.AddFeature(buffer, index, count, RphfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable, false, ClearSubstitutionFlags, RecordRhpf);
        this.EnableFeature(buffer, index, count, PrefTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable, ClearSubstitutionFlags, RecordPref);

        // These substitutions form the internal pieces of an orthographic unit.
        // Reordering runs only after the complete group because it depends on which
        // repha, pre-base, half, below-base, and post-base forms actually substituted.
        this.EnableFeature(buffer, index, count, RkrfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, AbvfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, BlwfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, HalfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, PstfTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, VatuTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable);
        this.EnableFeature(buffer, index, count, CjctTag, ShapingFeatureFlags.ManualZwj | ShapingFeatureFlags.PerSyllable, null, this.reorderAction);

        // Each syllable receives exactly one topographical form. These features
        // use varying masks and therefore remain disabled until mask setup.
        this.AddFeature(buffer, index, count, ArabicJoining.IsolTag, false, null, null);
        this.AddFeature(buffer, index, count, ArabicJoining.InitTag, false, null, null);
        this.AddFeature(buffer, index, count, ArabicJoining.MediTag, false, null, null);
        this.AddFeature(buffer, index, count, ArabicJoining.FinaTag, false, null, this.pauseAction);

        // The empty pause after the topographical group forces these presentation
        // features into a later lookup stage; combining the groups would change
        // feature ordering in fonts that implement both.
        this.EnableFeature(buffer, index, count, AbvsTag, ShapingFeatureFlags.ManualZwj);
        this.EnableFeature(buffer, index, count, BlwsTag, ShapingFeatureFlags.ManualZwj);
        this.EnableFeature(buffer, index, count, HalnTag, ShapingFeatureFlags.ManualZwj);
        this.EnableFeature(buffer, index, count, PresTag, ShapingFeatureFlags.ManualZwj);
        this.EnableFeature(buffer, index, count, PstsTag, ShapingFeatureFlags.ManualZwj);
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(ShapingBuffer buffer, int index, int count)
    {
        // Several of the scripts this engine shapes are cursive, and their
        // characters take their form from the ones around them.
        if (ArabicJoining.Joins(this.ScriptClass))
        {
            ArabicJoining.Apply(buffer, index, count, this.ScriptClass, this.Features);
        }
    }

    /// <inheritdoc />
    public override bool TryCompose(CodePoint first, CodePoint second, out CodePoint composed)
    {
        // A split vowel begins with a mark. Joining that mark back onto the next
        // part would undo the decomposition required by the shaping stages.
        if (CodePoint.IsMark(first))
        {
            composed = default;
            return false;
        }

        return base.TryCompose(first, second, out composed);
    }

    /// <summary>
    /// Identifies syllables using the Universal Shaping Engine state machine and assigns shaping info to each glyph.
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

        // Most shaping segments fit on the stack. Larger segments use temporary
        // managed storage rather than risking unbounded stack growth; both spans are
        // reused in place for the filtered machine input.
        Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];
        Span<int> sourceIndices = count <= 64 ? stackalloc int[count] : new int[count];
        for (int i = index; i < index + count; i++)
        {
            CodePoint codePoint = buffer[i].CodePoint;
            int sourceIndex = i - index;
            int category = UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint.Value);

            // Every record retains its category even when it is omitted from machine
            // input, because feature setup and reordering still inspect that record.
            values[sourceIndex] = category;
            buffer[i].Syllable.UseCategory = category;
        }

        // The machine omits CGJ-class records. It also omits a non-joiner immediately
        // before a Unicode mark, looking through any CGJ-class records. Retain each
        // consumed record's original position so matches can be projected across the
        // omitted records exactly as the source machine does.
        int machineCount = 0;
        for (int i = 0; i < count; i++)
        {
            int category = values[i];
            if (category == CategoryCGJ)
            {
                // A combining-grapheme-joiner category affects mark behavior but is
                // transparent to the syllable grammar.
                continue;
            }

            if (category == CategoryZwnj)
            {
                // A non-joiner immediately before a mark is transparent to syllable
                // recognition. Transparent CGJ-class records between the pair do not
                // break that relationship; the first other character does.
                for (int next = i + 1; next < count; next++)
                {
                    if (values[next] == CategoryCGJ)
                    {
                        continue;
                    }

                    if (CodePoint.IsMark(buffer[index + next].CodePoint))
                    {
                        category = CategoryCGJ;
                    }

                    break;
                }

                if (category == CategoryCGJ)
                {
                    continue;
                }
            }

            // Compact accepted categories into the front of the existing spans. The
            // source map projects each machine match back over any omitted records.
            values[machineCount] = category;
            sourceIndices[machineCount] = i;
            machineCount++;
        }

        int syllable = 0;
        uint rphfMask = this.Features.GetMask(RphfTag);
        StateMachine.MatchEnumerator match = StateMachine.EnumerateMatches(values[..machineCount]);
        while (match.MoveNext())
        {
            // A nonzero number distinguishes adjacent syllables even when they share
            // the same type; callbacks use the number to recover exact boundaries.
            ++syllable;

            // The next accepted category starts the next original range. Using it as
            // the exclusive end assigns transparent records between matches to the
            // syllable preceding them, preserving a complete projection.
            int originalStart = sourceIndices[match.StartIndex];
            int originalEnd = match.EndIndex + 1 < machineCount ? sourceIndices[match.EndIndex + 1] : count;

            // Create shaper info. The symbol index is stored directly: it is the value
            // the state machine consumes and the key into the generated name table.
            SyllableType syllableType = StateSyllableTypes[match.TagState];
            if (syllableType == SyllableType.BrokenCluster)
            {
                this.hasBrokenClusters = true;
            }

            for (int i = originalStart; i < originalEnd; i++)
            {
                ref GlyphShapingData data = ref buffer[i + index];
                data.Syllable.Type = syllableType;
                data.Syllable.Number = syllable;
            }

            // Enable the repha feature on the syllable's leading glyphs only: a
            // repha can form there and nowhere else, so the feature stays off for
            // the rest of the syllable. An explicit repha needs only its own record;
            // other syllables expose at most the first three candidates.
            int limit = buffer[originalStart + index].Syllable.UseCategory == CategoryR
                ? 1
                : Math.Min(3, originalEnd - originalStart);

            for (int i = originalStart; i < originalStart + limit; i++)
            {
                buffer.EnableShapingFeature(i + index, rphfMask);
            }
        }

        if (!ArabicJoining.Joins(this.ScriptClass))
        {
            // Cursive scripts already received character-level joining forms from
            // ArabicJoining. Other USE scripts derive forms from adjacent syllables.
            this.SetupTopographicalMasks(buffer, index, count);
        }
    }

    /// <summary>
    /// Clears substitution flags on all glyphs in the range, preparing for the next substitution pass.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void ClearSubstitutionFlags(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            data.IsSubstituted = false;
        }
    }

    /// <summary>
    /// Records glyphs substituted by the 'rphf' feature by marking their category as repha ("R").
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void RecordRhpf(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        uint rphfMask = plan.Features.GetMask(RphfTag);
        if (rphfMask == 0)
        {
            return;
        }

        int end = index + count;
        int start = index;
        while (start < end)
        {
            int syllableEnd = NextSyllable(buffer, start, end);

            // Only the leading mask-enabled region can form repha. Once a record has
            // no repha mask, later records in this syllable are not candidates.
            for (int i = start; i < syllableEnd && (buffer[i].FeatureMask & rphfMask) != 0; i++)
            {
                if (buffer[i].IsSubstituted)
                {
                    // Reordering consumes categories rather than feature history, so
                    // translate the first successful substitution into repha state.
                    buffer[i].Syllable.UseCategory = CategoryR;
                    break;
                }
            }

            start = syllableEnd;
        }
    }

    /// <summary>
    /// Records glyphs substituted by the 'pref' feature by marking their category as pre-base vowel ("VPre").
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void RecordPref(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        int end = index + count;
        int start = index;
        while (start < end)
        {
            int syllableEnd = NextSyllable(buffer, start, end);

            // The first successful pre-base-form substitution is the item later
            // moved to the syllable's pre-base position. Other substitutions in the
            // syllable retain their original categories.
            for (int i = start; i < syllableEnd; i++)
            {
                if (buffer[i].IsSubstituted)
                {
                    buffer[i].Syllable.UseCategory = CategoryVPre;
                    break;
                }
            }

            start = syllableEnd;
        }
    }

    /// <summary>
    /// Reorders glyphs within syllables, handling repha movement, pre-base vowel movement,
    /// and dotted circle insertion for broken clusters.
    /// </summary>
    /// <param name="plan">The plan whose segment is being shaped.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void Reorder(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        FontMetrics fontMetrics = this.fontMetrics;
        int max = index + count;
        int start = index;
        int end = NextSyllable(buffer, index, max);

        // The generated span is indexed by the same category symbols stored on each
        // record. Keeping it as static data avoids constructing a runtime lookup.
        ReadOnlySpan<bool> postBaseCategories = UniversalShapingData.PostBaseCategories;

        if (this.hasBrokenClusters)
        {
            if (fontMetrics.TryGetGlyphId(new CodePoint(DottedCircle), out ushort circleId))
            {
                // Insert one base into each broken syllable. The bounds grow as
                // records are inserted, so both the current syllable end and the
                // overall end advance with each insertion.
                while (start < max)
                {
                    if (buffer[start].Syllable.Type == SyllableType.BrokenCluster)
                    {
                        // A leading repha must remain the first record even when its
                        // syllable needs a synthetic base.
                        int i = start;
                        for (i = start; i < end; i++)
                        {
                            ref GlyphShapingData candidate = ref buffer[i];
                            if (candidate.Syllable.Type == SyllableType.None || candidate.Syllable.UseCategory != CategoryR)
                            {
                                break;
                            }
                        }

                        buffer.InsertDottedCircle(i, circleId);

                        // The inserted record inherits the syllable and feature masks
                        // of the following record, while its category identifies it as a base.
                        buffer[i].Syllable.UseCategory = CategoryB;

                        end++;
                        max++;
                    }

                    start = end;
                    end = NextSyllable(buffer, start, max);
                }

                // The insertion walk ends at the buffer boundary. Reset the cursors
                // before applying the independent per-syllable reorder pass.
                start = index;
                end = NextSyllable(buffer, index, max);
            }
        }

        while (start < max)
        {
            ref GlyphShapingData data = ref buffer[start];

            // Only a few syllable types need reordering.
            if (data.Syllable.Type is not SyllableType.ViramaTerminatedCluster
                and not SyllableType.SakotTerminatedCluster
                and not SyllableType.StandardCluster
                and not SyllableType.SymbolCluster
                and not SyllableType.BrokenCluster)
            {
                goto Increment;
            }

            // A leading repha moves towards the end but must remain before the first
            // post-base form or halant. If neither exists, it becomes the last record.
            if (data.Syllable.UseCategory == CategoryR && end - start > 1)
            {
                for (int i = start + 1; i < end; i++)
                {
                    ref GlyphShapingData current = ref buffer[i];
                    bool isPostBase = postBaseCategories[current.Syllable.UseCategory] || IsHalant(ref current);
                    if (isPostBase || i == end - 1)
                    {
                        // The target is the slot before a post-base item, but the last
                        // slot itself when the scan simply reached the syllable end.
                        if (isPostBase)
                        {
                            i--;
                        }

                        buffer.CombineInputStarts(start, i + 1);
                        buffer.MoveGlyph(start, i);
                        break;
                    }
                }
            }

            // Pre-base vowels and modifiers move to the current insertion point. A
            // halant advances that point past itself, so a pre-base item never crosses
            // the stacker it belongs behind.
            for (int i = start, j = start; i < end; i++)
            {
                ref GlyphShapingData current = ref buffer[i];

                if (IsHalant(ref current))
                {
                    j = i + 1;
                }

                // A multiple substitution gives every emitted glyph a component
                // index. Moving only its first component preserves their order.
                else if (current.Syllable.Type != SyllableType.None
                    && (current.Syllable.UseCategory == CategoryVPre || current.Syllable.UseCategory == CategoryVMPre)
                    && current.LigatureComponent <= 0
                    && j < i)
                {
                    buffer.CombineInputStarts(j, i + 1);
                    buffer.MoveGlyph(i, j);
                }
            }

            Increment:
            start = end;
            end = NextSyllable(buffer, start, max);
        }

        // Later features are not constrained to syllables, so release the state
        // once all syllable-local substitutions and reordering have completed.
        for (int i = index; i < max; i++)
        {
            buffer[i].Syllable = default;
        }
    }

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

        int syllable = buffer[index].Syllable.Number;
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
    /// Determines whether the glyph is a halant, halant-like, or invisible stacker character.
    /// </summary>
    /// <param name="data">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a halant or equivalent.</returns>
    private static bool IsHalant(ref GlyphShapingData data)
        => data.Syllable.Type != SyllableType.None
        && (data.Syllable.UseCategory == CategoryH || data.Syllable.UseCategory == CategoryHVM || data.Syllable.UseCategory == CategoryIS)
        && !data.IsLigated;

    /// <summary>
    /// Assigns joining forms by adjacent syllable for scripts without cursive joining data.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private void SetupTopographicalMasks(ShapingBuffer buffer, int index, int count)
    {
        uint isolMask = this.Features.GetMask(ArabicJoining.IsolTag);
        uint initMask = this.Features.GetMask(ArabicJoining.InitTag);
        uint mediMask = this.Features.GetMask(ArabicJoining.MediTag);
        uint finaMask = this.Features.GetMask(ArabicJoining.FinaTag);
        uint allMasks = isolMask | initMask | mediMask | finaMask;

        // The previous syllable is revised only when a following joinable syllable
        // proves it is not isolated or final. Retain its range and selected form
        // until that decision can be made.
        int end = index + count;
        int lastStart = index;
        uint lastMask = 0;
        for (int start = index; start < end;)
        {
            int syllableEnd = NextSyllable(buffer, start, end);
            SyllableType syllableType = buffer[start].Syllable.Type;
            if (syllableType is SyllableType.HieroglyphCluster or SyllableType.NonCluster)
            {
                // These syllables never join and terminate any chain from the
                // preceding syllable.
                lastMask = 0;
            }
            else
            {
                bool joinsPrevious = lastMask == finaMask || lastMask == isolMask;
                if (joinsPrevious)
                {
                    // A previous final becomes medial when the chain continues; a
                    // previous isolated syllable becomes initial.
                    uint previousMask = lastMask == finaMask ? mediMask : initMask;
                    SetTopographicalMask(buffer, lastStart, start, allMasks, previousMask);
                }

                // A continuing syllable is final for now. A new chain begins as
                // isolated; either form may be revised by the next syllable.
                lastMask = joinsPrevious ? finaMask : isolMask;
                SetTopographicalMask(buffer, start, syllableEnd, allMasks, lastMask);
            }

            lastStart = start;
            start = syllableEnd;
        }
    }

    /// <summary>
    /// Replaces the topographical feature mask over a range.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="start">The inclusive range start.</param>
    /// <param name="end">The exclusive range end.</param>
    /// <param name="allMasks">The union of all topographical masks.</param>
    /// <param name="mask">The mask selected for the range.</param>
    private static void SetTopographicalMask(ShapingBuffer buffer, int start, int end, uint allMasks, uint mask)
    {
        for (int i = start; i < end; i++)
        {
            // The forms are mutually exclusive. Clear the complete form group before
            // enabling the one selected for this syllable.
            buffer.DisableShapingFeature(i, allMasks);
            buffer.EnableShapingFeature(i, mask);
        }
    }

    /// <summary>
    /// Separates topographical substitutions from later presentation substitutions.
    /// </summary>
    /// <param name="plan">The shaping plan.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    private static void Pause(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        // The feature planner treats a callback as a stage boundary even when the
        // callback has no buffer work. Keeping this method empty is therefore the
        // behavior: it separates topographical and presentation lookups.
    }
}

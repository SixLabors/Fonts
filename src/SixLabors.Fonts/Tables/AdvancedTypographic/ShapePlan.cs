// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The prebuilt shaping plan for one (font, script, options) identity: the shaper,
/// its stage list, and per layout table the pause-delimited stage groups with their
/// merged lookup lists. Structure that does not depend on the text is resolved once
/// here; per-glyph feature assignment and mask values remain per-pass state. Plans
/// are immutable after construction apart from the lazily built positioning groups,
/// and are only shared within a single buffer, which is exclusively owned.
/// </summary>
internal sealed class ShapePlan
{
    /// <summary>
    /// The substitution table the plan resolved against, or <see langword="null"/>
    /// when the font has none; retained for pause-time feature queries that fall
    /// outside the resolved set.
    /// </summary>
    private readonly GSubTable? gsubTable;

    /// <summary>
    /// The features resolved while building the substitution groups, kept as a flat
    /// list because a plan resolves a few dozen at most. Pause-time queries from the
    /// complex shapers read these instead of re-resolving per syllable.
    /// </summary>
    private readonly List<(Tag Feature, List<(Tag Feature, ushort Index, GSub.LookupTable LookupTable)>? Lookups)> resolvedGsubFeatures = new();

    /// <summary>
    /// The lazily built substitution stage groups. Shapers register their stages
    /// while planning a segment, not at construction, so groups can only be built
    /// after the first plan invocation; a stage count change afterwards triggers a
    /// rebuild so conditionally registered stages are never missed.
    /// </summary>
    private List<ShapePlanStageGroup<GSub.LookupTable>>? gsubStageGroups;

    /// <summary>
    /// The lazily built positioning stage groups. Positioning tables are only
    /// reachable from the positioning pass, so the first positioning use builds
    /// them.
    /// </summary>
    private List<ShapePlanStageGroup<GPos.LookupTable>>? gposStageGroups;

    /// <summary>
    /// The stage count the groups were built over. A later plan invocation that
    /// grows the stage list invalidates both tables' groups.
    /// </summary>
    private int builtStageCount = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShapePlan"/> class. Use
    /// <see cref="Build"/>, which also resolves the substitution groups.
    /// </summary>
    /// <param name="fontMetrics">The font the plan binds to.</param>
    /// <param name="script">The script class the plan shapes.</param>
    /// <param name="scriptTag">The resolved OpenType script tag.</param>
    /// <param name="shaper">The shaper constructed for the plan.</param>
    /// <param name="stages">The shaper's stage list.</param>
    /// <param name="languageTags">The language system candidates the plan resolved under.</param>
    /// <param name="gsubTable">The substitution table, or <see langword="null"/>.</param>
    private ShapePlan(
        FontMetrics fontMetrics,
        ScriptClass script,
        Tag scriptTag,
        BaseShaper shaper,
        List<ShapingStage> stages,
        Tag[] languageTags,
        GSubTable? gsubTable)
    {
        this.FontMetrics = fontMetrics;
        this.Script = script;
        this.ScriptTag = scriptTag;
        this.Shaper = shaper;
        this.Stages = stages;
        this.LanguageTags = languageTags;
        this.gsubTable = gsubTable;
    }

    /// <summary>
    /// Gets the font the plan binds to.
    /// </summary>
    public FontMetrics FontMetrics { get; }

    /// <summary>
    /// Gets the script class the plan shapes.
    /// </summary>
    public ScriptClass Script { get; }

    /// <summary>
    /// Gets the resolved OpenType script tag.
    /// </summary>
    public Tag ScriptTag { get; }

    /// <summary>
    /// Gets the shaper constructed for the plan. Its per-segment state is reassigned
    /// at every pause invocation, so reusing the instance across segments and passes
    /// is safe within the owning buffer.
    /// </summary>
    public BaseShaper Shaper { get; }

    /// <summary>
    /// Gets the shaper's live stage list; group boundaries index into it. Shapers
    /// append stages while planning, so the list is only complete after a plan
    /// invocation and additions are deduplicated by feature tag.
    /// </summary>
    public List<ShapingStage> Stages { get; }

    /// <summary>
    /// Gets the language system candidates the plan resolved under.
    /// </summary>
    public Tag[] LanguageTags { get; }

    /// <summary>
    /// Gets the plan's feature classification and bit assignment, shared with the
    /// shaper that created it. Global features share one reserved bit; varying
    /// features gain distinct bits in registration order, append-only, so plans of
    /// the same identity assign identical layouts.
    /// </summary>
    public ShapePlanFeatures Features => this.Shaper.Features;

    /// <summary>
    /// Gets a value indicating whether the plan may be cached. A substitution table
    /// carrying feature variations resolves against live variation coordinates, so
    /// its plans must be rebuilt per operation.
    /// </summary>
    public bool IsCacheable => this.gsubTable?.FeatureVariations is null;

    /// <summary>
    /// Builds the plan for the given identity: constructs the shaper and captures
    /// its live stage list. Stage groups are built lazily by the first
    /// <see cref="GetOrBuildGSubStageGroups"/> call after a plan invocation has
    /// registered the shaper's stages.
    /// </summary>
    /// <param name="fontMetrics">The font the plan binds to.</param>
    /// <param name="script">The script class the plan shapes.</param>
    /// <param name="scriptTag">The resolved OpenType script tag.</param>
    /// <param name="textOptions">The options whose values the shaper captures.</param>
    /// <param name="featureTags">The additional feature tags for the plan's shaping runs.</param>
    /// <param name="languageTags">The language system candidates to resolve under.</param>
    /// <returns>The built <see cref="ShapePlan"/>.</returns>
    public static ShapePlan Build(FontMetrics fontMetrics, ScriptClass script, Tag scriptTag, TextOptions textOptions, IReadOnlyList<Tag> featureTags, Tag[] languageTags)
    {
        BaseShaper shaper = ShaperFactory.Create(script, scriptTag, fontMetrics, textOptions, languageTags);

        // A TextRun list replaces the whole-text feature list. Assign the already
        // resolved list before planning so GSUB and GPOS use the same features.
        shaper.FeatureTags = featureTags;
        List<ShapingStage> stages = shaper.GetShapingStages();

        _ = fontMetrics.TryGetGSubTable(out GSubTable? gsubTable);
        return new ShapePlan(fontMetrics, script, scriptTag, shaper, stages, languageTags, gsubTable);
    }

    /// <summary>
    /// Gets the substitution stage groups, building them from the live stage list
    /// on first use and rebuilding both tables' groups if a later plan invocation
    /// registered additional stages.
    /// </summary>
    /// <returns>The substitution stage groups.</returns>
    public List<ShapePlanStageGroup<GSub.LookupTable>> GetOrBuildGSubStageGroups()
    {
        // The build body lives in a separate method because its resolver captures
        // state: keeping the lambda out of this method keeps the steady-state hit
        // path free of closure allocation, which would otherwise occur on every
        // call at method entry regardless of the early return.
        if (this.gsubStageGroups is not null && this.Stages.Count == this.builtStageCount)
        {
            return this.gsubStageGroups;
        }

        return this.BuildGSubStageGroups();
    }

    /// <summary>
    /// Gets the positioning stage groups, building them on first use over the same
    /// stage boundaries as the substitution groups. A positioning table carrying
    /// feature variations resolves against live variation coordinates, so its
    /// groups are rebuilt on every call instead of stored.
    /// </summary>
    /// <param name="gposTable">The positioning table to resolve against.</param>
    /// <returns>The positioning stage groups.</returns>
    public List<ShapePlanStageGroup<GPos.LookupTable>> GetOrBuildGPosStageGroups(GPosTable gposTable)
    {
        // Ensure the group boundaries reflect the current stage list before the
        // positioning groups are built or served. The build body lives in a
        // separate method for the same closure-allocation reason as the
        // substitution side.
        _ = this.GetOrBuildGSubStageGroups();

        if (this.gposStageGroups is not null && gposTable.FeatureVariations is null)
        {
            return this.gposStageGroups;
        }

        return this.BuildGPosStageGroups(gposTable);
    }

    /// <summary>
    /// Builds the substitution stage groups from the live stage list, invalidating
    /// everything built over the old boundaries first: the positioning groups and
    /// the resolved feature snapshot both index the same partition.
    /// </summary>
    /// <returns>The substitution stage groups.</returns>
    private List<ShapePlanStageGroup<GSub.LookupTable>> BuildGSubStageGroups()
    {
        this.builtStageCount = this.Stages.Count;
        this.gposStageGroups = null;
        this.resolvedGsubFeatures.Clear();

        List<ShapePlanStageGroup<GSub.LookupTable>> groups = new();
        GSubTable? gsubTable = this.gsubTable;
        FontMetrics fontMetrics = this.FontMetrics;
        ScriptClass script = this.Script;
        Tag[] languageTags = this.LanguageTags;
        this.BuildStageGroups(
            this.Stages,
            groups,
            (in Tag featureTag, out List<(Tag Feature, ushort Index, GSub.LookupTable LookupTable)>? lookups) =>
            {
                if (gsubTable is null)
                {
                    lookups = null;
                    return false;
                }

                bool found = gsubTable.TryGetFeatureLookups(fontMetrics, in featureTag, script, languageTags, out lookups);
                this.resolvedGsubFeatures.Add((featureTag, lookups));
                return found;
            });

        this.gsubStageGroups = groups;
        return groups;
    }

    /// <summary>
    /// Builds the positioning stage groups over the current stage boundaries and
    /// stores them unless the table's resolution depends on live variation
    /// coordinates.
    /// </summary>
    /// <param name="gposTable">The positioning table to resolve against.</param>
    /// <returns>The positioning stage groups.</returns>
    private List<ShapePlanStageGroup<GPos.LookupTable>> BuildGPosStageGroups(GPosTable gposTable)
    {
        List<ShapePlanStageGroup<GPos.LookupTable>> groups = new();
        FontMetrics fontMetrics = this.FontMetrics;
        ScriptClass script = this.Script;
        Tag[] languageTags = this.LanguageTags;
        this.BuildStageGroups(
            this.Stages,
            groups,
            (in Tag featureTag, out List<(Tag Feature, ushort Index, GPos.LookupTable LookupTable)>? lookups) =>
                gposTable.TryGetFeatureLookups(fontMetrics, in featureTag, script, languageTags, out lookups));

        if (gposTable.FeatureVariations is null)
        {
            this.gposStageGroups = groups;
        }

        return groups;
    }

    /// <summary>
    /// Gets the resolved substitution lookups for a feature, serving pause-time
    /// queries from the complex shapers. Features the plan resolved while building
    /// its groups answer from the plan; anything else falls through to the table so
    /// behavior never depends on the plan's coverage.
    /// </summary>
    /// <param name="featureTag">The feature tag to query.</param>
    /// <param name="lookups">When this method returns, contains the feature's lookups if any.</param>
    /// <returns><see langword="true"/> if the feature resolves to at least one lookup.</returns>
    public bool TryGetGSubFeatureLookups(in Tag featureTag, [NotNullWhen(true)] out List<(Tag Feature, ushort Index, GSub.LookupTable LookupTable)>? lookups)
    {
        List<(Tag Feature, List<(Tag Feature, ushort Index, GSub.LookupTable LookupTable)>? Lookups)> resolved = this.resolvedGsubFeatures;
        for (int i = 0; i < resolved.Count; i++)
        {
            if (resolved[i].Feature == featureTag)
            {
                lookups = resolved[i].Lookups;
                return lookups is not null && lookups.Count > 0;
            }
        }

        if (this.gsubTable is null)
        {
            lookups = null;
            return false;
        }

        return this.gsubTable.TryGetFeatureLookups(this.FontMetrics, in featureTag, this.Script, this.LanguageTags, out lookups);
    }

    /// <summary>
    /// Computes the pause-delimited stage groups over the stage list and resolves
    /// each group's features into a merged lookup list sorted by lookup index. A
    /// lookup registered by several of a group's features gains their combined
    /// plan-assigned mask instead of a second entry.
    /// </summary>
    /// <typeparam name="TLookup">The layout table's lookup type.</typeparam>
    /// <param name="stages">The stage list to group.</param>
    /// <param name="groups">The group list to fill.</param>
    /// <param name="resolver">The per-feature lookup resolver for the table.</param>
    private void BuildStageGroups<TLookup>(
        List<ShapingStage> stages,
        List<ShapePlanStageGroup<TLookup>> groups,
        ShapePlanLookupResolver<TLookup> resolver)
    {
        // The stage list is a flat sequence of (feature, optional pre action,
        // optional post action) entries. Application order requires that every
        // lookup registered by stages before a pause action has applied before
        // that action runs, so the sequence partitions into groups: a stage with
        // a pre action starts a new group, and a stage with a post action ends
        // its group. Within one group all features apply together, ordered by
        // lookup index rather than by stage, because the specification orders
        // lookups within a single application pass by their position in the
        // font's lookup list.
        int stageIndex = 0;
        while (stageIndex < stages.Count)
        {
            // Walk forward from the group's first stage until something closes
            // it: the current stage carries a post action, the list ends, or the
            // next stage carries a pre action and therefore opens the next group.
            int groupEnd = stageIndex;
            while (true)
            {
                groupEnd++;
                if (stages[groupEnd - 1].HasPostAction || groupEnd >= stages.Count || stages[groupEnd].HasPreAction)
                {
                    break;
                }
            }

            // Resolve each stage feature in the group to its lookups and fold
            // them into one list ordered by lookup index, freezing each entry's
            // combined mask from the classification recorded at registration. A
            // zero mask means the feature is disabled for the plan or has no bit,
            // so its lookups are never collected. The scan below runs backwards
            // from the tail because resolved lookups arrive mostly ascending, so
            // the insertion point is almost always at or near the end.
            ShapePlanStageGroup<TLookup> group = new(stageIndex, groupEnd);
            List<(Tag Feature, ushort Index, TLookup LookupTable, uint Mask, bool AutoZwnj, bool AutoZwj, bool Random, bool PerSyllable)> merged = group.Lookups;
            for (int s = stageIndex; s < groupEnd; s++)
            {
                Tag featureTag = stages[s].FeatureTag;
                uint featureMask = this.Features.GetMask(featureTag);
                if (featureMask == 0)
                {
                    continue;
                }

                if (!resolver(in featureTag, out List<(Tag Feature, ushort Index, TLookup LookupTable)>? lookups) || lookups is null)
                {
                    continue;
                }

                ShapingFeatureFlags featureFlags = this.Features.GetFlags(featureTag);
                bool autoZwnj = (featureFlags & ShapingFeatureFlags.ManualZwnj) == 0;
                bool autoZwj = (featureFlags & ShapingFeatureFlags.ManualZwj) == 0;
                bool random = (featureFlags & ShapingFeatureFlags.Random) != 0;
                bool perSyllable = (featureFlags & ShapingFeatureFlags.PerSyllable) != 0;

                foreach ((Tag Feature, ushort Index, TLookup LookupTable) featureLookup in lookups)
                {
                    // Scan from the tail toward the head. Three outcomes: the
                    // lookup index is already present, so this feature's mask
                    // joins the entry and the joiner handling intersects; a
                    // smaller index is found, so the new entry inserts directly
                    // after it; or the head is reached, so the new entry inserts
                    // first.
                    int insertAt = merged.Count;
                    bool alreadyMerged = false;
                    while (insertAt > 0)
                    {
                        (Tag Feature, ushort Index, TLookup LookupTable, uint Mask, bool AutoZwnj, bool AutoZwj, bool Random, bool PerSyllable) prior = merged[insertAt - 1];
                        if (prior.Index == featureLookup.Index)
                        {
                            merged[insertAt - 1] = (prior.Feature, prior.Index, prior.LookupTable, prior.Mask | featureMask, prior.AutoZwnj && autoZwnj, prior.AutoZwj && autoZwj, prior.Random, prior.PerSyllable);
                            alreadyMerged = true;
                            break;
                        }

                        if (prior.Index < featureLookup.Index)
                        {
                            break;
                        }

                        insertAt--;
                    }

                    if (!alreadyMerged)
                    {
                        merged.Insert(insertAt, (featureLookup.Feature, featureLookup.Index, featureLookup.LookupTable, featureMask, autoZwnj, autoZwj, random, perSyllable));
                    }
                }
            }

            groups.Add(group);

            // The next group starts where this one ended; every stage belongs to
            // exactly one group.
            stageIndex = groupEnd;
        }
    }
}

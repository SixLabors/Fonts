// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.GSub;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The Glyph Substitution (GSUB) table provides data for substitution of glyphs for appropriate rendering of scripts,
/// such as cursively-connecting forms in Arabic script, or for advanced typographic effects, such as ligatures.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub"/>
/// </summary>
internal class GSubTable : Table
{
    /// <summary>
    /// Caches resolved feature lookups per stage feature, script, and language
    /// candidates. See <see cref="TryGetFeatureLookups"/> for the variable-font bypass
    /// and the read-only contract on cached lists.
    /// </summary>
    private readonly ConcurrentDictionary<FeatureLookupsKey, List<(Tag Feature, ushort Index, LookupTable LookupTable)>> featureLookupsCache = new();

    /// <summary>
    /// The OpenType table tag for the GSUB table.
    /// </summary>
    public const string TableName = "GSUB";

    /// <summary>
    /// The invalid but widely shipped language system record tag 'dflt'.
    /// </summary>
    private static readonly Tag DefaultLangSysTag = Tag.Parse("dflt");

    /// <summary>
    /// Initializes a new instance of the <see cref="GSubTable"/> class.
    /// </summary>
    /// <param name="scriptList">The script list table, or <see langword="null"/> if not present.</param>
    /// <param name="featureList">The feature list table.</param>
    /// <param name="lookupList">The lookup list table.</param>
    /// <param name="featureVariations">The feature variations table for variable fonts, or <see langword="null"/>.</param>
    public GSubTable(ScriptList? scriptList, FeatureListTable featureList, LookupListTable lookupList, FeatureVariationsTable? featureVariations = null)
    {
        this.ScriptList = scriptList;
        this.FeatureList = featureList;
        this.LookupList = lookupList;
        this.FeatureVariations = featureVariations;
    }

    /// <summary>
    /// Gets the script list table, or <see langword="null"/> if not present.
    /// </summary>
    public ScriptList? ScriptList { get; }

    /// <summary>
    /// Gets the feature list table.
    /// </summary>
    public FeatureListTable FeatureList { get; }

    /// <summary>
    /// Gets the lookup list table containing all substitution lookups.
    /// </summary>
    public LookupListTable LookupList { get; }

    /// <summary>
    /// Gets the feature variations table for variable fonts, or <see langword="null"/> if not present.
    /// </summary>
    public FeatureVariationsTable? FeatureVariations { get; }

    /// <summary>
    /// Loads the <see cref="GSubTable"/> from the font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="GSubTable"/>, or <see langword="null"/> if not present.</returns>
    public static GSubTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="GSubTable"/> from a big endian binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="GSubTable"/>.</returns>
    public static GSubTable Load(BigEndianBinaryReader reader)
    {
        // GSUB Header, Version 1.0
        // +----------+-------------------+-----------------------------------------------------------+
        // | Type     | Name              | Description                                               |
        // +==========+===================+===========================================================+
        // | uint16   | majorVersion      | Major version of the GSUB table, = 1                      |
        // +----------+-------------------+-----------------------------------------------------------+
        // | uint16   | minorVersion      | Minor version of the GSUB table, = 0                      |
        // +----------+-------------------+-----------------------------------------------------------+
        // | Offset16 | scriptListOffset  | Offset to ScriptList table, from beginning of GSUB table  |
        // +----------+-------------------+-----------------------------------------------------------+
        // | Offset16 | featureListOffset | Offset to FeatureList table, from beginning of GSUB table |
        // +----------+-------------------+-----------------------------------------------------------+
        // | Offset16 | lookupListOffset  | Offset to LookupList table, from beginning of GSUB table  |
        // +----------+-------------------+-----------------------------------------------------------+

        // GSUB Header, Version 1.1
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | Type     | Name                    | Description                                                                   |
        // +==========+=========================+===============================================================================+
        // | uint16   | majorVersion            | Major version of the GSUB table, = 1                                          |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | uint16   | minorVersion            | Minor version of the GSUB table, = 1                                          |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | Offset16 | scriptListOffset        | Offset to ScriptList table, from beginning of GSUB table                      |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | Offset16 | featureListOffset       | Offset to FeatureList table, from beginning of GSUB table                     |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | Offset16 | lookupListOffset        | Offset to LookupList table, from beginning of GSUB table                      |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        // | Offset32 | featureVariationsOffset | Offset to FeatureVariations table, from beginning of GSUB table (may be NULL) |
        // +----------+-------------------------+-------------------------------------------------------------------------------+
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();

        ushort scriptListOffset = reader.ReadOffset16();
        ushort featureListOffset = reader.ReadOffset16();
        ushort lookupListOffset = reader.ReadOffset16();
        uint featureVariationsOffset = (minorVersion == 1) ? reader.ReadOffset32() : 0;

        // TODO: Optimization. Allow only reading the scriptList.
        ScriptList? scriptList = ScriptList.Load(reader, scriptListOffset);

        FeatureListTable featureList = FeatureListTable.Load(reader, featureListOffset);

        LookupListTable lookupList = LookupListTable.Load(reader, lookupListOffset);

        FeatureVariationsTable? featureVariations = featureVariationsOffset != 0
            ? FeatureVariationsTable.Load(reader, featureVariationsOffset, featureList)
            : null;

        return new GSubTable(scriptList, featureList, lookupList, featureVariations);
    }

    /// <summary>
    /// Applies glyph substitution to the buffer using GSUB lookup rules.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    public void ApplySubstitution(FontMetrics fontMetrics, ShapingBuffer buffer)
    {
        // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
        int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(buffer.Count);
        int maxOperationsCount = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(buffer.Count);
        int currentOperations = 0;

        for (int i = 0; i < buffer.Count; i++)
        {
            // Choose a shaper based on the script.
            // This determines which features to apply to which glyphs.
            int index = i;
            ScriptItemizer.ShapingRun run = ScriptItemizer.ReadRun(buffer, ref i, maxCount, out int count);

            Tag unicodeScriptTag = this.GetUnicodeScriptTag(run.Script);
            ShapePlan shapePlan = buffer.GetOrCreatePlan(run.Script, unicodeScriptTag, fontMetrics, run.Culture, run.FeatureTags);

            BaseShaper shaper = shapePlan.Shaper;

            // Plan substitution features for each glyph.
            // Shapers can adjust the count during initialization and feature processing so we must capture
            // the current count to allow resetting indexes and processing counts.
            int collectionCount = buffer.Count;
            shaper.Plan(fontMetrics, buffer, index, count);
            int delta = buffer.Count - collectionCount;
            i += delta;
            count += delta;

            // Stages are applied in pause-delimited groups: a stage action is a
            // synchronization point, and between two actions every registered
            // feature's lookups apply together in lookup-list order, the order the
            // specification defines for lookups within a single application pass. A
            // lookup registered by several of the group's features applies once with
            // their glyph masks combined. Group boundaries, merged lookup lists, and
            // entry masks are all prebuilt on the plan.
            List<ShapePlanStageGroup<LookupTable>> groups = shapePlan.GetOrBuildGSubStageGroups();
            List<ShapingStage> stages = shapePlan.Stages;
            SkippingGlyphIterator iterator = new(fontMetrics, buffer, index, default, 0);
            for (int g = 0; g < groups.Count; g++)
            {
                ShapePlanStageGroup<LookupTable> group = groups[g];

                collectionCount = buffer.Count;
                stages[group.Start].PreProcessFeature(shapePlan, buffer, index, count);

                // Account for substitutions changing the length of the buffer.
                delta = buffer.Count - collectionCount;
                count += delta;
                i += delta;

                this.ApplyMergedLookups(
                    fontMetrics,
                    buffer,
                    ref iterator,
                    group.Lookups,
                    index,
                    ref count,
                    ref i,
                    ref collectionCount,
                    maxCount,
                    maxOperationsCount,
                    ref currentOperations);

                collectionCount = buffer.Count;
                stages[group.End - 1].PostProcessFeature(shapePlan, buffer, index, count);

                // Account for substitutions changing the length of the buffer.
                delta = buffer.Count - collectionCount;
                count += delta;
                i += delta;
            }

            // Record the segment with its post-substitution range so the in-place
            // positioning pass can reuse the plan; one plan then drives both tables.
            // Mark after GSUB because substitutions can change the number and order
            // of glyph records to which layout will later apply tracking.
            ScriptItemizer.MarkCursiveTrackingRun(buffer, index, count, run.Script);
            buffer.SegmentPlans.Add((index, count, run.Script, shapePlan));
        }
    }

    /// <summary>
    /// Applies a stage group's merged lookups to the glyph substitution buffer in
    /// lookup-index order. Each entry's mask combines every group feature that
    /// registered the lookup, so the per-glyph gate stays a single bitwise AND.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="merged">The group's lookups, sorted by lookup index.</param>
    /// <param name="index">The starting index in the buffer.</param>
    /// <param name="count">The number of glyphs to process (updated by substitutions).</param>
    /// <param name="i">The outer loop index (updated by substitutions).</param>
    /// <param name="collectionCount">The tracked buffer count (updated by substitutions).</param>
    /// <param name="maxCount">The maximum allowable buffer count.</param>
    /// <param name="maxOperationsCount">The maximum allowable operations count.</param>
    /// <param name="currentOperations">The current operations counter.</param>
    private void ApplyMergedLookups(
        FontMetrics fontMetrics,
        ShapingBuffer buffer,
        ref SkippingGlyphIterator iterator,
        List<(Tag Feature, ushort Index, LookupTable LookupTable, uint Mask, bool AutoZwnj, bool AutoZwj, bool Random, bool PerSyllable)> merged,
        int index,
        ref int count,
        ref int i,
        ref int collectionCount,
        int maxCount,
        int maxOperationsCount,
        ref int currentOperations)
    {
        for (int m = 0; m < merged.Count; m++)
        {
            (Tag feature, ushort _, LookupTable featureLookupTable, uint featureMask, bool autoZwnj, bool autoZwj, bool random, bool perSyllable) = merged[m];

            // Skip the whole lookup when its coverage cannot intersect any glyph id
            // the buffer has ever contained; most fonts carry many lookups for
            // glyphs a given text never produces.
            // A lookup whose mask no record carries cannot match anything, so
            // the whole pass is skipped rather than walked. Features that are
            // registered for every plan but enabled only by particular text,
            // such as the fraction trio, cost nothing elsewhere.
            if ((featureMask & buffer.EnabledFeatureMaskUnion) == 0
                || !featureLookupTable.Digest.MightIntersect(buffer.GlyphDigest))
            {
                continue;
            }

            buffer.SetLookupMatchState(featureMask, autoZwnj, autoZwj, random, perSyllable);
            iterator.Reset(index, featureLookupTable.LookupFlags, featureLookupTable.MarkFilteringSet);

            if (featureLookupTable.IsReverse)
            {
                // Each replacement may create the context needed by a glyph to its
                // left, so reverse lookups walk the segment end-to-start in place.
                int reverseSegmentEnd = index + count;
                for (int position = reverseSegmentEnd - 1; position >= index; position--)
                {
                    if (buffer.Count >= maxCount || currentOperations++ >= maxOperationsCount)
                    {
                        collectionCount = buffer.Count;
                        return;
                    }

                    ref GlyphShapingData glyphData = ref buffer[position];
                    if ((glyphData.FeatureMask & featureMask) == 0
                        || !featureLookupTable.Digest.MightContain(glyphData.GlyphId)
                        || iterator.IsIgnored(position))
                    {
                        continue;
                    }

                    featureLookupTable.TrySubstitution(fontMetrics, this, buffer, feature, featureMask, position, reverseSegmentEnd - position);
                }

                collectionCount = buffer.Count;
                continue;
            }

            // One output pass per lookup: the cursor consumes the input side and
            // every record streams to the output side exactly once, so a length
            // change costs one streaming pass instead of one shift per mutation.
            // The pass begins at the segment, adopting everything before it, and
            // a pass that changes nothing closes without touching the tail.
            // Input-side indices are stable for the whole pass, which the
            // matchers rely on.
            buffer.BeginOutputPass(index);

            // The segment's end is held as the number of records that follow it,
            // which nothing the pass does can disturb: replacements, insertions,
            // and the room a rewind opens all land before the tail. An absolute
            // index would have to be corrected after each of them.
            int totalBefore = buffer.Count;
            int segmentEnd = index + count;
            while (buffer.ReadIndex < segmentEnd && buffer.ReadIndex < buffer.Count)
            {
                // The digest cheaply rejects glyphs no subtable of this lookup can
                // affect; a maybe falls through to the exact coverage test inside.
                // Masked and ignored records still stream to the output unchanged,
                // preserving every record. Consecutive rejections are adopted as
                // one range so the output pass moves its cursors once.
                int position = buffer.ReadIndex;
                bool operationsLimitReached = false;
                while (position < segmentEnd && position < buffer.Count)
                {
                    if (buffer.Count >= maxCount || currentOperations++ >= maxOperationsCount)
                    {
                        operationsLimitReached = true;
                        break;
                    }

                    ref GlyphShapingData candidate = ref buffer[position];
                    if ((candidate.FeatureMask & featureMask) != 0
                        && featureLookupTable.Digest.MightContain(candidate.GlyphId)
                        && !iterator.IsIgnored(position))
                    {
                        break;
                    }

                    position++;
                }

                buffer.CopyGlyphs(position - buffer.ReadIndex);

                if (operationsLimitReached)
                {
                    // The pass must always close: stream the remainder and
                    // reconcile the segment bookkeeping before bailing out.
                    buffer.EndOutputPass();
                    count += buffer.Count - totalBefore;
                    i += buffer.Count - totalBefore;
                    collectionCount = buffer.Count;
                    return;
                }

                if (buffer.ReadIndex >= segmentEnd || buffer.ReadIndex >= buffer.Count)
                {
                    break;
                }

                position = buffer.ReadIndex;
                int lengthBefore = buffer.Count;
                featureLookupTable.TrySubstitution(fontMetrics, this, buffer, feature, featureMask, position, segmentEnd - position);

                // Anything that lengthened the input side moved the records
                // after the segment along with it: an in-place mutation, or the
                // room a rewind opened. Both shift the segment's end by the
                // same amount, so the bound stays a plain comparison.
                segmentEnd += buffer.Count - lengthBefore;

                if (buffer.ReadIndex == position)
                {
                    buffer.CopyGlyph();
                }
            }

            buffer.EndOutputPass();

            // The pass closed, so the buffer holds the records it produced: the
            // whole segment's change surfaces once, here.
            count += buffer.Count - totalBefore;
            i += buffer.Count - totalBefore;
            collectionCount = buffer.Count;
        }
    }

    /// <summary>
    /// Tries to get the feature lookups for the given stage feature, script, and language.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="stageFeature">The feature tag for the current shaping stage.</param>
    /// <param name="script">The script class.</param>
    /// <param name="languageTags">
    /// The candidate OpenType language system tags, most specific first. An empty array
    /// selects the default language system.
    /// </param>
    /// <param name="value">When this method returns, contains the list of feature lookups if found.</param>
    /// <returns><see langword="true"/> if lookups were found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetFeatureLookups(
        FontMetrics fontMetrics,
        in Tag stageFeature,
        ScriptClass script,
        Tag[] languageTags,
        [NotNullWhen(true)] out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? value)
    {
        if (this.ScriptList is null)
        {
            value = null;
            return false;
        }

        // Feature variations resolve against the font's live variation coordinates, so
        // caching would mix results across differently configured variable fonts.
        if (this.FeatureVariations is not null)
        {
            value = this.ResolveFeatureLookups(fontMetrics, stageFeature, script, languageTags);
            return value.Count > 0;
        }

        // Resolution depends only on this table's data for a given feature, script,
        // and language candidates, so results, including empty ones, are cached for
        // the table's lifetime. The cached list is shared: consumers must not mutate
        // it. A concurrent first-resolution race only duplicates deterministic work.
        FeatureLookupsKey key = new(stageFeature, script, languageTags);
        if (!this.featureLookupsCache.TryGetValue(key, out value))
        {
            value = this.ResolveFeatureLookups(fontMetrics, stageFeature, script, languageTags);
            this.featureLookupsCache.TryAdd(key, value);
        }

        return value.Count > 0;
    }

    /// <summary>
    /// Resolves the feature lookups for the given stage feature, script, and language
    /// through the selection ladder documented inline.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="stageFeature">The feature tag for the current shaping stage.</param>
    /// <param name="script">The script class.</param>
    /// <param name="languageTags">The candidate OpenType language system tags, most specific first.</param>
    /// <returns>The resolved lookups; empty when the feature yields none.</returns>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)> ResolveFeatureLookups(
        FontMetrics fontMetrics,
        Tag stageFeature,
        ScriptClass script,
        Tag[] languageTags)
    {
        // Resolve feature substitutions from FeatureVariations (variable fonts).
        FeatureTableSubstitutionRecord[]? substitutions = this.FeatureVariations
            ?.FindMatchingSubstitutions(fontMetrics.GetNormalizedCoordinates());

        // Step 1: script selection. Map the Unicode script class onto the font's
        // script table, falling back to the font's first script when the font does not
        // declare the script.
        // The caching entry point rejects a null script list before dispatching here.
        ScriptListTable scriptListTable = this.ScriptList!.Default();
        Tag[] tags = UnicodeScriptTagMap.Instance[script];
        for (int i = 0; i < tags.Length; i++)
        {
            if (this.ScriptList.TryGetValue(tags[i].Value, out ScriptListTable? table))
            {
                scriptListTable = table;
                break;
            }
        }

        // Step 2: language selection. Walk the culture's candidate tags in priority
        // order, most specific first, scanning the script's named language systems for
        // a tag match; the first candidate the font declares wins, so a zh-HK run
        // selects ZHH before ZHT. A match commits even when the selected language
        // system lacks this stage feature: per the specification a LangSys table is the
        // complete feature set for its language, so falling through to the default
        // below would merge two languages' features, exactly the output language
        // systems exist to prevent. An empty candidate array skips this step entirely.
        LangSysTable[] langSysTables = scriptListTable.LangSysTables;
        for (int i = 0; i < languageTags.Length; i++)
        {
            uint language = languageTags[i].Value;
            for (int j = 0; j < langSysTables.Length; j++)
            {
                if (langSysTables[j].LangSysTag == language)
                {
                    return this.GetFeatureLookups(stageFeature, substitutions, langSysTables[j]);
                }
            }
        }

        // Step 3: no culture, or no candidate the font declares. A language system
        // record explicitly tagged dflt is preferred over the true default: the tag is
        // invalid per the specification, but fonts built from old documentation typos
        // carry one, and the reference engines honor it.
        LangSysTable[] langSysRecords = scriptListTable.LangSysTables;
        for (int i = 0; i < langSysRecords.Length; i++)
        {
            if (langSysRecords[i].LangSysTag == DefaultLangSysTag.Value)
            {
                return this.GetFeatureLookups(stageFeature, substitutions, langSysRecords[i]);
            }
        }

        LangSysTable? defaultLangSysTable = scriptListTable.DefaultLangSysTable;
        if (defaultLangSysTable != null)
        {
            return this.GetFeatureLookups(stageFeature, substitutions, defaultLangSysTable);
        }

        // Step 4: no default language system either. Nothing applies: the font scoped
        // every feature to specific languages, and the reference engines agree that no
        // language system means no lookups. Features such as SimSun's vertical
        // alternates, which live only under its Chinese language systems, are reached by
        // setting TextOptions.Culture to a Chinese culture.
        return [];
    }

    /// <summary>
    /// Gets the OpenType script tag for the given script class, checking against the font's ScriptList.
    /// </summary>
    /// <param name="script">The script class.</param>
    /// <returns>The matching script tag, or default if not found.</returns>
    private Tag GetUnicodeScriptTag(ScriptClass script)
    {
        if (this.ScriptList is null)
        {
            return default;
        }

        Tag[] tags = UnicodeScriptTagMap.Instance[script];
        for (int i = 0; i < tags.Length; i++)
        {
            if (this.ScriptList.TryGetValue(tags[i].Value, out ScriptListTable? _))
            {
                return tags[i];
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the feature lookups for the given stage feature from the specified language system tables.
    /// </summary>
    /// <param name="stageFeature">The feature tag for the current shaping stage.</param>
    /// <param name="substitutions">Optional feature table substitutions from FeatureVariations.</param>
    /// <param name="langSysTables">The language system tables to search.</param>
    /// <returns>A sorted list of feature lookups.</returns>
    private List<(Tag Feature, ushort Index, LookupTable LookupTable)> GetFeatureLookups(
        in Tag stageFeature,
        FeatureTableSubstitutionRecord[]? substitutions,
        params LangSysTable[] langSysTables)
    {
        List<(Tag Feature, ushort Index, LookupTable LookupTable)> lookups = [];
        for (int i = 0; i < langSysTables.Length; i++)
        {
            ushort[] featureIndices = langSysTables[i].FeatureIndices;
            for (int j = 0; j < featureIndices.Length; j++)
            {
                ushort featureIndex = featureIndices[j];
                FeatureTable featureTable = ResolveFeatureTable(this.FeatureList, featureIndex, substitutions);
                Tag feature = featureTable.FeatureTag;

                if (stageFeature != feature)
                {
                    continue;
                }

                ushort[] lookupListIndices = featureTable.LookupListIndices;
                for (int k = 0; k < lookupListIndices.Length; k++)
                {
                    ushort lookupIndex = lookupListIndices[k];
                    LookupTable lookupTable = this.LookupList.LookupTables[lookupIndex];
                    lookups.Add((feature, lookupIndex, lookupTable));
                }
            }
        }

        lookups.Sort((x, y) => x.Index - y.Index);
        return lookups;
    }

    /// <summary>
    /// Resolves the feature table for the given index, checking for substitutions from FeatureVariations first.
    /// </summary>
    /// <param name="featureList">The feature list table.</param>
    /// <param name="featureIndex">The feature index.</param>
    /// <param name="substitutions">Optional feature table substitutions from FeatureVariations.</param>
    /// <returns>The resolved feature table.</returns>
    private static FeatureTable ResolveFeatureTable(
        FeatureListTable featureList,
        ushort featureIndex,
        FeatureTableSubstitutionRecord[]? substitutions)
    {
        if (substitutions is not null)
        {
            for (int i = 0; i < substitutions.Length; i++)
            {
                if (substitutions[i].FeatureIndex == featureIndex)
                {
                    return substitutions[i].AlternateFeatureTable;
                }
            }
        }

        return featureList.FeatureTables[featureIndex];
    }
}

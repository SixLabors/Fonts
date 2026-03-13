// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

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
    /// The OpenType table tag for the GSUB table.
    /// </summary>
    internal const string TableName = "GSUB";

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
    internal static GSubTable Load(BigEndianBinaryReader reader)
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
    /// Applies glyph substitution to the collection using GSUB lookup rules.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph substitution collection.</param>
    public void ApplySubstitution(FontMetrics fontMetrics, GlyphSubstitutionCollection collection)
    {
        // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
        int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
        int maxOperationsCount = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(collection.Count);
        int currentOperations = 0;

        for (int i = 0; i < collection.Count; i++)
        {
            // Choose a shaper based on the script.
            // This determines which features to apply to which glyphs.
            ScriptClass current = this.GetScriptClass(CodePoint.GetScriptClass(collection[i].CodePoint));

            int index = i;
            int count = 1;
            while (i < collection.Count - 1)
            {
                // We want to assign the same feature lookups to individual sections of the text rather
                // than the text as a whole to ensure that different language shapers do not interfere
                // with each other when the text contains multiple languages.
                ScriptClass next = this.GetScriptClass(CodePoint.GetScriptClass(collection[i + 1].CodePoint));
                if (next != current &&
                    current is not ScriptClass.Common and not ScriptClass.Unknown and not ScriptClass.Inherited &&
                    next is not ScriptClass.Common and not ScriptClass.Unknown and not ScriptClass.Inherited)
                {
                    break;
                }

                if (current is ScriptClass.Common or ScriptClass.Unknown or ScriptClass.Inherited)
                {
                    current = next;
                }

                i++;
                count++;

                if (i >= maxCount)
                {
                    break;
                }
            }

            Tag unicodeScriptTag = this.GetUnicodeScriptTag(current);
            BaseShaper shaper = ShaperFactory.Create(current, unicodeScriptTag, fontMetrics, collection.TextOptions);

            // Plan substitution features for each glyph.
            // Shapers can adjust the count during initialization and feature processing so we must capture
            // the current count to allow resetting indexes and processing counts.
            int collectionCount = collection.Count;
            shaper.Plan(collection, index, count);
            int delta = collection.Count - collectionCount;
            i += delta;
            count += delta;

            IEnumerable<ShapingStage> stages = shaper.GetShapingStages();
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default, 0);
            foreach (ShapingStage stage in stages)
            {
                collectionCount = collection.Count;
                stage.PreProcessFeature(collection, index, count);

                // Account for substitutions changing the length of the collection.
                delta = collection.Count - collectionCount;
                count += delta;
                i += delta;

                Tag featureTag = stage.FeatureTag;

                this.ApplyFeature(
                    fontMetrics,
                    collection,
                    ref iterator,
                    in featureTag,
                    current,
                    index,
                    ref count,
                    ref i,
                    ref collectionCount,
                    maxCount,
                    maxOperationsCount,
                    ref currentOperations);

                collectionCount = collection.Count;
                stage.PostProcessFeature(collection, index, count);

                // Account for substitutions changing the length of the collection.
                delta = collection.Count - collectionCount;
                count += delta;
                i += delta;
            }
        }
    }

    /// <summary>
    /// Applies a specific feature's lookups to the glyph substitution collection.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="featureTag">The feature tag to apply.</param>
    /// <param name="current">The current script class.</param>
    /// <param name="index">The starting index in the collection.</param>
    /// <param name="count">The number of glyphs to process (updated by substitutions).</param>
    /// <param name="i">The outer loop index (updated by substitutions).</param>
    /// <param name="collectionCount">The tracked collection count (updated by substitutions).</param>
    /// <param name="maxCount">The maximum allowable collection count.</param>
    /// <param name="maxOperationsCount">The maximum allowable operations count.</param>
    /// <param name="currentOperations">The current operations counter.</param>
    internal void ApplyFeature(
        FontMetrics fontMetrics,
        GlyphSubstitutionCollection collection,
        ref SkippingGlyphIterator iterator,
        in Tag featureTag,
        ScriptClass current,
        int index,
        ref int count,
        ref int i,
        ref int collectionCount,
        int maxCount,
        int maxOperationsCount,
        ref int currentOperations)
    {
        if (this.TryGetFeatureLookups(fontMetrics, in featureTag, current, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? lookups))
        {
            // Apply features in order.
            foreach ((Tag Feature, ushort Index, LookupTable LookupTable) featureLookup in lookups)
            {
                Tag feature = featureLookup.Feature;
                LookupTable featureLookupTable = featureLookup.LookupTable;
                iterator.Reset(index, featureLookupTable.LookupFlags, featureLookupTable.MarkFilteringSet);

                while (iterator.Index < index + count)
                {
                    if (collection.Count >= maxCount || currentOperations++ >= maxOperationsCount)
                    {
                        return;
                    }

                    if (!collection[iterator.Index].EnabledFeatureTags.Contains(feature))
                    {
                        iterator.Next();
                        continue;
                    }

                    collectionCount = collection.Count;
                    featureLookup.LookupTable.TrySubstitution(fontMetrics, this, collection, featureLookup.Feature, iterator.Index, count - (iterator.Index - index));
                    iterator.Next();

                    // Account for substitutions changing the length of the collection.
                    int delta = collection.Count - collectionCount;
                    count += delta;
                    i += delta;
                }
            }
        }
    }

    /// <summary>
    /// Tries to get the feature lookups for the given stage feature and script.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="stageFeature">The feature tag for the current shaping stage.</param>
    /// <param name="script">The script class.</param>
    /// <param name="value">When this method returns, contains the list of feature lookups if found.</param>
    /// <returns><see langword="true"/> if lookups were found; otherwise, <see langword="false"/>.</returns>
    internal bool TryGetFeatureLookups(
        FontMetrics fontMetrics,
        in Tag stageFeature,
        ScriptClass script,
        [NotNullWhen(true)] out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? value)
    {
        if (this.ScriptList is null)
        {
            value = null;
            return false;
        }

        // Resolve feature substitutions from FeatureVariations (variable fonts).
        FeatureTableSubstitutionRecord[]? substitutions = this.FeatureVariations
            ?.FindMatchingSubstitutions(fontMetrics.GetNormalizedCoordinates());

        ScriptListTable scriptListTable = this.ScriptList.Default();
        Tag[] tags = UnicodeScriptTagMap.Instance[script];
        for (int i = 0; i < tags.Length; i++)
        {
            if (this.ScriptList.TryGetValue(tags[i].Value, out ScriptListTable? table))
            {
                scriptListTable = table;
                break;
            }
        }

        LangSysTable? defaultLangSysTable = scriptListTable.DefaultLangSysTable;
        if (defaultLangSysTable != null)
        {
            value = this.GetFeatureLookups(stageFeature, substitutions, defaultLangSysTable);
            return value.Count > 0;
        }

        value = this.GetFeatureLookups(stageFeature, substitutions, scriptListTable.LangSysTables);
        return value.Count > 0;
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
                    lookups.Add(new(feature, lookupIndex, lookupTable));
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

    /// <summary>
    /// Maps a script class to an effective script class, checking whether the font supports it.
    /// Falls back to <see cref="ScriptClass.Default"/> if the script is not present in the font.
    /// </summary>
    /// <param name="current">The script class to check.</param>
    /// <returns>The effective script class.</returns>
    private ScriptClass GetScriptClass(ScriptClass current)
    {
        if (current is ScriptClass.Common or ScriptClass.Unknown or ScriptClass.Inherited)
        {
            return current;
        }

        if (this.ScriptList is null)
        {
            return ScriptClass.Default;
        }

        Tag[] tags = UnicodeScriptTagMap.Instance[current];

        for (int i = 0; i < tags.Length; i++)
        {
            if (this.ScriptList.TryGetValue(tags[i].Value, out ScriptListTable? _))
            {
                return current;
            }
        }

        // Script for `current` not present in the font: use default shaper.
        return ScriptClass.Default;
    }
}

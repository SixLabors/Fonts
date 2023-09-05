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
    internal const string TableName = "GSUB";

    public GSubTable(ScriptList? scriptList, FeatureListTable featureList, LookupListTable lookupList)
    {
        this.ScriptList = scriptList;
        this.FeatureList = featureList;
        this.LookupList = lookupList;
    }

    public ScriptList? ScriptList { get; }

    public FeatureListTable FeatureList { get; }

    public LookupListTable LookupList { get; }

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
        var scriptList = ScriptList.Load(reader, scriptListOffset);

        var featureList = FeatureListTable.Load(reader, featureListOffset);

        var lookupList = LookupListTable.Load(reader, lookupListOffset);

        // TODO: Feature Variations.
        return new GSubTable(scriptList, featureList, lookupList);
    }

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
            ScriptClass current = CodePoint.GetScriptClass(collection[i].CodePoint);

            int index = i;
            int count = 1;
            while (i < collection.Count - 1)
            {
                // We want to assign the same feature lookups to individual sections of the text rather
                // than the text as a whole to ensure that different language shapers do not interfere
                // with each other when the text contains multiple languages.
                ScriptClass next = CodePoint.GetScriptClass(collection[i + 1].CodePoint);
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
            BaseShaper shaper = ShaperFactory.Create(current, unicodeScriptTag, collection.TextOptions);

            // Plan substitution features for each glyph.
            // Shapers can adjust the count during initialization and feature processing so we must capture
            // the current count to allow resetting indexes and processing counts.
            int collectionCount = collection.Count;
            shaper.Plan(collection, index, count);
            int delta = collection.Count - collectionCount;
            i += delta;
            count += delta;

            IEnumerable<ShapingStage> stages = shaper.GetShapingStages();
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
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
        if (this.TryGetFeatureLookups(in featureTag, current, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? lookups))
        {
            // Apply features in order.
            foreach ((Tag Feature, ushort Index, LookupTable LookupTable) featureLookup in lookups)
            {
                Tag feature = featureLookup.Feature;
                iterator.Reset(index, featureLookup.LookupTable.LookupFlags);

                while (iterator.Index < index + count)
                {
                    if (collection.Count >= maxCount || currentOperations++ >= maxOperationsCount)
                    {
                        return;
                    }

                    List<TagEntry> glyphFeatures = collection[iterator.Index].Features;
                    if (!HasFeature(glyphFeatures, in feature))
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

    internal bool TryGetFeatureLookups(
        in Tag stageFeature,
        ScriptClass script,
        [NotNullWhen(true)] out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? value)
    {
        if (this.ScriptList is null)
        {
            value = null;
            return false;
        }

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
            value = this.GetFeatureLookups(stageFeature, defaultLangSysTable);
            return value.Count > 0;
        }

        value = this.GetFeatureLookups(stageFeature, scriptListTable.LangSysTables);
        return value.Count > 0;
    }

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

    private List<(Tag Feature, ushort Index, LookupTable LookupTable)> GetFeatureLookups(in Tag stageFeature, params LangSysTable[] langSysTables)
    {
        List<(Tag Feature, ushort Index, LookupTable LookupTable)> lookups = new();
        for (int i = 0; i < langSysTables.Length; i++)
        {
            ushort[] featureIndices = langSysTables[i].FeatureIndices;
            for (int j = 0; j < featureIndices.Length; j++)
            {
                FeatureTable featureTable = this.FeatureList.FeatureTables[featureIndices[j]];
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

    private static bool HasFeature(List<TagEntry> glyphFeatures, in Tag feature)
    {
        for (int i = 0; i < glyphFeatures.Count; i++)
        {
            TagEntry entry = glyphFeatures[i];
            if (entry.Tag == feature && entry.Enabled)
            {
                return true;
            }
        }

        return false;
    }
}

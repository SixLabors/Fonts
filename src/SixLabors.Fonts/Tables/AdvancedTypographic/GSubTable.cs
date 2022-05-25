// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.GSub;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    /// <summary>
    /// The Glyph Substitution (GSUB) table provides data for substitution of glyphs for appropriate rendering of scripts,
    /// such as cursively-connecting forms in Arabic script, or for advanced typographic effects, such as ligatures.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub"/>
    /// </summary>
    [TableName(TableName)]
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
            for (ushort i = 0; i < collection.Count; i++)
            {
                // Choose a shaper based on the script.
                // This determines which features to apply to which glyphs.
                ScriptClass current = CodePoint.GetScriptClass(collection.GetGlyphShapingData(i).CodePoint);
                BaseShaper shaper = ShaperFactory.Create(current, collection.TextOptions);

                ushort index = i;
                ushort count = 1;
                while (i < collection.Count - 1)
                {
                    // We want to assign the same feature lookups to individual sections of the text rather
                    // than the text as a whole to ensure that different language shapers do not interfere
                    // with each other when the text contains multiple languages.
                    GlyphShapingData nextData = collection.GetGlyphShapingData(i + 1);
                    ScriptClass next = CodePoint.GetScriptClass(nextData.CodePoint);
                    if (next is not ScriptClass.Common and not ScriptClass.Unknown and not ScriptClass.Inherited && next != current)
                    {
                        break;
                    }

                    i++;
                    count++;
                }

                // Assign Substitution features to each glyph.
                shaper.AssignFeatures(collection, index, count);

                // Shapers can adjust the count.
                count = (ushort)Math.Min(count, collection.Count - index);

                IEnumerable<Tag> stageFeatures = shaper.GetShapingStageFeatures();

                int currentCount = collection.Count;
                SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
                foreach (Tag stageFeature in stageFeatures)
                {
                    if (!this.TryGetFeatureLookups(in stageFeature, current, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? lookups))
                    {
                        continue;
                    }

                    // Apply features in order.
                    foreach ((Tag Feature, ushort Index, LookupTable LookupTable) featureLookup in lookups)
                    {
                        Tag feature = featureLookup.Feature;
                        iterator.Reset(index, featureLookup.LookupTable.LookupFlags);

                        while (iterator.Index < index + count)
                        {
                            List<TagEntry> glyphFeatures = collection.GetGlyphShapingData(iterator.Index).Features;
                            if (!HasFeature(glyphFeatures, in feature))
                            {
                                iterator.Next();
                                continue;
                            }

                            featureLookup.LookupTable.TrySubstitution(fontMetrics, this, collection, featureLookup.Feature, iterator.Index, count - (iterator.Index - index));
                            iterator.Next();

                            // Account for substitutions changing the length of the collection.
                            if (collection.Count != currentCount)
                            {
                                count = (ushort)(count - (currentCount - collection.Count));
                                currentCount = collection.Count;
                            }
                        }
                    }
                }
            }
        }

        private bool TryGetFeatureLookups(
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
}

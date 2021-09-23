// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SixLabors.Fonts.Tables.AdvancedTypographic.Gsub;
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

        public GSubTable(ScriptList scriptList, FeatureListTable featureList, LookupListTable lookupList)
        {
            this.ScriptList = scriptList;
            this.FeatureList = featureList;
            this.LookupList = lookupList;
        }

        public ScriptList ScriptList { get; }

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

        public void ApplySubstitution(GlyphSubstitutionCollection collection, ushort index, int count)
        {
            if (!collection.TryGetShapingFeatures(index, out IReadOnlySet<Tag>? featureTags))
            {
                return;
            }

            collection.GetGlyphData(index, out CodePoint codePoint, out _, out _, out _);

            ScriptListTable scriptListTable = this.ScriptList.Default();
            Script script = CodePoint.GetScript(codePoint);
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
                this.ApplyFeatureSubstitution(collection, index, count, featureTags, defaultLangSysTable);
                return;
            }

            this.ApplyFeatureSubstitution(collection, index, count, featureTags, scriptListTable.LangSysTables);
        }

        private void ApplyFeatureSubstitution(
            GlyphSubstitutionCollection collection,
            ushort index,
            int count,
            IReadOnlySet<Tag> featureTags,
            params LangSysTable[] langSysTables)
        {
            for (int i = 0; i < langSysTables.Length; i++)
            {
                ushort[] featureIndices = langSysTables[i].FeatureIndices;
                for (int j = 0; j < featureIndices.Length; j++)
                {
                    FeatureTable featureTable = this.FeatureList.FeatureTables[featureIndices[j]];
                    Tag tag = featureTable.FeatureTag;

                    // Check tag against all features, which should be applied to the given glyph.
                    if (!featureTags.Contains(tag))
                    {
                        continue;
                    }

                    ushort[] lookupListIndices = featureTable.LookupListIndices;
                    for (int k = 0; k < lookupListIndices.Length; k++)
                    {
                        // TODO: Consider caching the relevant langtables per script.
                        // There's a lot of repetitive checks here.
                        LookupTable lookupTable = this.LookupList.LookupTables[lookupListIndices[k]];
                        lookupTable.TrySubstitution(this, collection, index, count);
                    }
                }
            }
        }
    }
}

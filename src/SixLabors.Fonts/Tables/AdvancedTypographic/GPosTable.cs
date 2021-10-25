// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    /// <summary>
    /// The Glyph Positioning table (GPOS) provides precise control over glyph placement for
    /// sophisticated text layout and rendering in each script and language system that a font supports.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos"/>
    /// </summary>
    [TableName(TableName)]
    internal class GPosTable : Table
    {
        internal const string TableName = "GPOS";

        public GPosTable(ScriptList scriptList, FeatureListTable featureList, LookupListTable lookupList)
        {
            this.ScriptList = scriptList;
            this.FeatureList = featureList;
            this.LookupList = lookupList;
        }

        public ScriptList ScriptList { get; }

        public FeatureListTable FeatureList { get; }

        public LookupListTable LookupList { get; }

        public static GPosTable? Load(FontReader fontReader)
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

        internal static GPosTable Load(BigEndianBinaryReader reader)
        {
            // GPOS Header, Version 1.0
            // +----------+-------------------+-----------------------------------------------------------+
            // | Type     | Name              | Description                                               |
            // +==========+===================+===========================================================+
            // | uint16   | majorVersion      | Major version of the GPOS table, = 1                      |
            // +----------+-------------------+-----------------------------------------------------------+
            // | uint16   | minorVersion      | Minor version of the GPOS table, = 0                      |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | scriptListOffset  | Offset to ScriptList table, from beginning of GPOS table  |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | featureListOffset | Offset to FeatureList table, from beginning of GPOS table |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | lookupListOffset  | Offset to LookupList table, from beginning of GPOS table  |
            // +----------+-------------------+-----------------------------------------------------------+

            // GPOS Header, Version 1.1
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | Type     | Name                    | Description                                                                   |
            // +==========+=========================+===============================================================================+
            // | uint16   | majorVersion            | Major version of the GPOS table, = 1                                          |
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | uint16   | minorVersion            | Minor version of the GPOS table, = 1                                          |
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | Offset16 | scriptListOffset        | Offset to ScriptList table, from beginning of GPOS table                      |
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | Offset16 | featureListOffset       | Offset to FeatureList table, from beginning of GPOS table                     |
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | Offset16 | lookupListOffset        | Offset to LookupList table, from beginning of GPOS table                      |
            // +----------+-------------------------+-------------------------------------------------------------------------------+
            // | Offset32 | featureVariationsOffset | Offset to FeatureVariations table, from beginning of GPOS table (may be NULL) |
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
            return new GPosTable(scriptList, featureList, lookupList);
        }

        public bool TryUpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection)
        {
            bool updated = false;
            for (ushort i = 0; i < collection.Count; i++)
            {
                ScriptClass current = CodePoint.GetScriptClass(collection.GetGlyphShapingData(i).CodePoint);
                BaseShaper shaper = ShaperFactory.Create(current);

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

                if (shaper.MarkZeroingMode == MarkZeroingMode.PreGPos)
                {
                    ZeroMarkAdvances(fontMetrics, collection, index, count);
                }

                // Assign Substitution features to each glyph.
                shaper.AssignFeatures(collection, index, count);
                IEnumerable<Tag> stageFeatures = shaper.GetShapingStageFeatures();
                SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
                foreach (Tag stageFeature in stageFeatures)
                {
                    List<(Tag Feature, ushort Index, LookupTable LookupTable)> lookups = this.GetFeatureLookups(in stageFeature, current);
                    if (lookups.Count == 0)
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

                            updated |= featureLookup.LookupTable.TryUpdatePosition(fontMetrics, this, collection, featureLookup.Feature, iterator.Index, count - (iterator.Index - index));
                            iterator.Next();
                        }
                    }
                }

                if (shaper.MarkZeroingMode == MarkZeroingMode.PostGpos)
                {
                    ZeroMarkAdvances(fontMetrics, collection, index, count);
                }

                FixMarkAttachment(fontMetrics, collection, index, count);
            }

            return updated;
        }

        private List<(Tag Feature, ushort Index, LookupTable LookupTable)> GetFeatureLookups(in Tag stageFeature, ScriptClass script)
        {
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
                return this.GetFeatureLookups(stageFeature, defaultLangSysTable);
            }

            return this.GetFeatureLookups(stageFeature, scriptListTable.LangSysTables);
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

        private static void FixMarkAttachment(FontMetrics fontMetrics, GlyphPositioningCollection collection, ushort index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                GlyphShapingData data = collection.GetGlyphShapingData(currentIndex);
                if (data.MarkAttachment != -1)
                {
                    int j = data.MarkAttachment;
                    GlyphShapingData markData = collection.GetGlyphShapingData(j);
                    Vector2 curOffset = collection.GetOffset(fontMetrics, (ushort)currentIndex, data.GlyphIds[0]);
                    Vector2 xy = Vector2.Zero;// collection.GetOffset(fontMetrics, (ushort)j, markData.GlyphIds[0]);

                    if (data.Direction == TextDirection.LeftToRight)
                    {
                        for (int k = j; k < i; k++)
                        {
                            markData = collection.GetGlyphShapingData(k);
                            Vector2 advance = collection.GetAdvance(fontMetrics, (ushort)k, markData.GlyphIds[0]);
                            xy -= advance;
                        }
                    }
                    else
                    {
                        for (int k = j + 1; k < i + 1; k++)
                        {
                            markData = collection.GetGlyphShapingData(k);
                            Vector2 advance = collection.GetAdvance(fontMetrics, (ushort)k, markData.GlyphIds[0]);
                            xy += advance;
                        }
                    }

                    collection.Offset(fontMetrics, (ushort)currentIndex, data.GlyphIds[0], (short)xy.X, (short)xy.Y);
                }
            }
        }

        private static void ZeroMarkAdvances(FontMetrics fontMetrics, GlyphPositioningCollection collection, ushort index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                GlyphShapingData data = collection.GetGlyphShapingData(currentIndex);
                ushort glyphId = data.GlyphIds[0];
                if (AdvancedTypographicUtils.IsMarkGlyph(fontMetrics, data.GlyphIds[0], data))
                {
                    collection.SetAdvance(fontMetrics, (ushort)currentIndex, glyphId, 0, 0);
                }
            }
        }
    }
}

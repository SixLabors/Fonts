// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal class GPosTable : Table
    {
        private static readonly Tag KernTag = Tag.Parse("kern");

        private static readonly Tag VKernTag = Tag.Parse("vkrn");

        internal const string TableName = "GPOS";

        public GPosTable(ScriptList? scriptList, FeatureListTable featureList, LookupListTable lookupList)
        {
            this.ScriptList = scriptList;
            this.FeatureList = featureList;
            this.LookupList = lookupList;
        }

        public ScriptList? ScriptList { get; }

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

        public bool TryUpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection, out bool kerned)
        {
            // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
            int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
            int maxOperationsCount = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(collection.Count);
            int currentOperations = 0;
            bool maxOperationsReached = false;

            kerned = false;
            bool updated = false;
            for (int i = 0; i < collection.Count; i++)
            {
                if (!collection.ShouldProcess(fontMetrics, i))
                {
                    continue;
                }

                ScriptClass current = CodePoint.GetScriptClass(collection[i].CodePoint);

                int index = i;
                int count = 1;
                while (i < collection.Count - 1)
                {
                    // We want to assign the same feature lookups to individual sections of the text rather
                    // than the text as a whole to ensure that different language shapers do not interfere
                    // with each other when the text contains multiple languages.
                    GlyphShapingData nextData = collection[i + 1];
                    ScriptClass next = CodePoint.GetScriptClass(nextData.CodePoint);
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

                if (shaper.MarkZeroingMode == MarkZeroingMode.PreGPos)
                {
                    ZeroMarkAdvances(fontMetrics, collection, index, count);
                }

                // Plan positioning features for each glyph.
                shaper.Plan(collection, index, count);
                IEnumerable<ShapingStage> shapingStages = shaper.GetShapingStages();
                SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
                foreach (ShapingStage stage in shapingStages)
                {
                    stage.PreProcessFeature(collection, index, count);

                    Tag featureTag = stage.FeatureTag;
                    if (this.TryGetFeatureLookups(in featureTag, current, out List<(Tag Feature, ushort Index, LookupTable LookupTable)>? lookups))
                    {
                        // Apply features in order.
                        foreach ((Tag Feature, ushort Index, LookupTable LookupTable) featureLookup in lookups)
                        {
                            Tag feature = featureLookup.Feature;
                            iterator.Reset(index, featureLookup.LookupTable.LookupFlags);

                            while (iterator.Index < index + count)
                            {
                                if (currentOperations++ >= maxOperationsCount)
                                {
                                    maxOperationsReached = true;
                                    goto EndLookups;
                                }

                                List<TagEntry> glyphFeatures = collection[iterator.Index].Features;
                                if (!HasFeature(glyphFeatures, in feature))
                                {
                                    iterator.Next();
                                    continue;
                                }

                                bool success = featureLookup.LookupTable.TryUpdatePosition(fontMetrics, this, collection, featureLookup.Feature, iterator.Index, count - (iterator.Index - index));
                                kerned |= success && (feature == KernTag || feature == VKernTag);
                                updated |= success;
                                iterator.Next();
                            }
                        }
                    }

                    stage.PostProcessFeature(collection, index, count);
                }

                EndLookups:
                if (shaper.MarkZeroingMode == MarkZeroingMode.PostGpos)
                {
                    ZeroMarkAdvances(fontMetrics, collection, index, count);
                }

                FixCursiveAttachment(collection, index, count);
                FixMarkAttachment(collection, index, count);
                UpdatePositions(fontMetrics, collection, index, count);

                if (i >= maxCount || maxOperationsReached)
                {
                    return updated;
                }
            }

            return updated;
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

        private static void FixCursiveAttachment(GlyphPositioningCollection collection, int index, int count)
        {
            LayoutMode layoutMode = collection.TextOptions.LayoutMode;
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                GlyphShapingData data = collection[currentIndex];
                if (data.CursiveAttachment != -1)
                {
                    int j = data.CursiveAttachment + currentIndex;
                    if (j > count)
                    {
                        return;
                    }

                    GlyphShapingData cursiveData = collection[j];
                    if (!AdvancedTypographicUtils.IsVerticalGlyph(data.CodePoint, layoutMode))
                    {
                        data.Bounds.Y += cursiveData.Bounds.Y;
                    }
                    else
                    {
                        data.Bounds.X += cursiveData.Bounds.X;
                    }
                }
            }
        }

        private static void FixMarkAttachment(GlyphPositioningCollection collection, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                GlyphShapingData data = collection[currentIndex];
                if (data.MarkAttachment != -1)
                {
                    int j = data.MarkAttachment;
                    GlyphShapingData markData = collection[j];
                    data.Bounds.X += markData.Bounds.X;
                    data.Bounds.Y += markData.Bounds.Y;

                    if (data.Direction == TextDirection.LeftToRight)
                    {
                        for (int k = j; k < currentIndex; k++)
                        {
                            markData = collection[k];
                            data.Bounds.X -= markData.Bounds.Width;
                            data.Bounds.Y -= markData.Bounds.Height;
                        }
                    }
                    else
                    {
                        for (int k = j + 1; k < currentIndex + 1; k++)
                        {
                            markData = collection[k];
                            data.Bounds.X += markData.Bounds.Width;
                            data.Bounds.Y += markData.Bounds.Height;
                        }
                    }
                }
            }
        }

        private static void ZeroMarkAdvances(FontMetrics fontMetrics, GlyphPositioningCollection collection, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                GlyphShapingData data = collection[currentIndex];
                if (AdvancedTypographicUtils.IsMarkGlyph(fontMetrics, data.GlyphId, data))
                {
                    data.Bounds.Width = 0;
                    data.Bounds.Height = 0;
                }
            }
        }

        private static void UpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int currentIndex = i + index;
                collection.UpdatePosition(fontMetrics, currentIndex);
            }
        }
    }
}

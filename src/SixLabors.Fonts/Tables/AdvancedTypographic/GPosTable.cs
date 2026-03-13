// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The Glyph Positioning table (GPOS) provides precise control over glyph placement for
/// sophisticated text layout and rendering in each script and language system that a font supports.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos"/>
/// </summary>
internal class GPosTable : Table
{
    /// <summary>
    /// The tag for the horizontal kerning feature ('kern').
    /// </summary>
    private static readonly Tag KernTag = Tag.Parse("kern");

    /// <summary>
    /// The tag for the vertical kerning feature ('vkrn').
    /// </summary>
    private static readonly Tag VKernTag = Tag.Parse("vkrn");

    /// <summary>
    /// The OpenType table tag for the GPOS table.
    /// </summary>
    internal const string TableName = "GPOS";

    /// <summary>
    /// Initializes a new instance of the <see cref="GPosTable"/> class.
    /// </summary>
    /// <param name="scriptList">The script list table, or <see langword="null"/> if not present.</param>
    /// <param name="featureList">The feature list table.</param>
    /// <param name="lookupList">The lookup list table.</param>
    /// <param name="featureVariations">The feature variations table for variable fonts, or <see langword="null"/>.</param>
    public GPosTable(ScriptList? scriptList, FeatureListTable featureList, LookupListTable lookupList, FeatureVariationsTable? featureVariations = null)
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
    /// Gets the lookup list table containing all positioning lookups.
    /// </summary>
    public LookupListTable LookupList { get; }

    /// <summary>
    /// Gets the feature variations table for variable fonts, or <see langword="null"/> if not present.
    /// </summary>
    public FeatureVariationsTable? FeatureVariations { get; }

    /// <summary>
    /// Loads the <see cref="GPosTable"/> from the font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="GPosTable"/>, or <see langword="null"/> if not present.</returns>
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

    /// <summary>
    /// Loads the <see cref="GPosTable"/> from a big endian binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="GPosTable"/>.</returns>
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
        ScriptList? scriptList = ScriptList.Load(reader, scriptListOffset);

        FeatureListTable featureList = FeatureListTable.Load(reader, featureListOffset);

        LookupListTable lookupList = LookupListTable.Load(reader, lookupListOffset);

        FeatureVariationsTable? featureVariations = featureVariationsOffset != 0
            ? FeatureVariationsTable.Load(reader, featureVariationsOffset, featureList)
            : null;

        return new GPosTable(scriptList, featureList, lookupList, featureVariations);
    }

    /// <summary>
    /// Tries to update the positions of glyphs in the collection using GPOS lookup rules.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="kerned">When this method returns, indicates whether kerning was applied.</param>
    /// <returns><see langword="true"/> if any positioning was updated; otherwise, <see langword="false"/>.</returns>
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

            ScriptClass current = this.GetScriptClass(CodePoint.GetScriptClass(collection[i].CodePoint));

            int index = i;
            int count = 1;
            while (i < collection.Count - 1)
            {
                // We want to assign the same feature lookups to individual sections of the text rather
                // than the text as a whole to ensure that different language shapers do not interfere
                // with each other when the text contains multiple languages.
                int ni = i + 1;
                GlyphShapingData nextData = collection[ni];
                if (!collection.ShouldProcess(fontMetrics, ni))
                {
                    break;
                }

                ScriptClass next = this.GetScriptClass(CodePoint.GetScriptClass(nextData.CodePoint));
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

            if (shaper.MarkZeroingMode == MarkZeroingMode.PreGPos)
            {
                ZeroMarkAdvances(fontMetrics, collection, index, count);
            }

            // Plan positioning features for each glyph.
            shaper.Plan(collection, index, count);
            IEnumerable<ShapingStage> shapingStages = shaper.GetShapingStages();
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default, 0);
            foreach (ShapingStage stage in shapingStages)
            {
                stage.PreProcessFeature(collection, index, count);

                Tag featureTag = stage.FeatureTag;
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
                            if (currentOperations++ >= maxOperationsCount)
                            {
                                maxOperationsReached = true;
                                goto EndLookups;
                            }

                            if (!collection[iterator.Index].EnabledFeatureTags.Contains(feature))
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

    /// <summary>
    /// Tries to get the feature lookups for the given stage feature and script.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="stageFeature">The feature tag for the current shaping stage.</param>
    /// <param name="script">The script class.</param>
    /// <param name="value">When this method returns, contains the list of feature lookups if found.</param>
    /// <returns><see langword="true"/> if lookups were found; otherwise, <see langword="false"/>.</returns>
    private bool TryGetFeatureLookups(
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

    /// <summary>
    /// Fixes cursive attachment positioning by propagating Y (or X for vertical) offsets.
    /// </summary>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of glyphs to process.</param>
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

    /// <summary>
    /// Fixes mark attachment positioning by propagating offsets from base glyphs.
    /// </summary>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of glyphs to process.</param>
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

    /// <summary>
    /// Zeros the advance widths and heights for mark glyphs within the specified range.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of glyphs to process.</param>
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

    /// <summary>
    /// Updates glyph positions in the collection for the specified range.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of glyphs to process.</param>
    private static void UpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection, int index, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int currentIndex = i + index;
            collection.UpdatePosition(fontMetrics, currentIndex);
        }
    }
}

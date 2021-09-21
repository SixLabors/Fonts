// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
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

        public bool TryUpdatePositions(
            IFontMetrics fontMetrics,
            GlyphPositioningCollection collection,
            ushort index,
            int count)
        {
            collection.GetCodePointAndGlyphIds(index, out CodePoint codePoint, out int _, out ReadOnlySpan<int> _);

            ScriptListTable scriptListTable = this.ScriptList.Default();
            Tag[] tags = UnicodeScriptTagMap.Instance[CodePoint.GetScript(codePoint)];
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
                return this.TryUpdateFeaturePositions(fontMetrics, collection, index, count, defaultLangSysTable);
            }

            return this.TryUpdateFeaturePositions(fontMetrics, collection, index, count, scriptListTable.LangSysTables);
        }

        private bool TryUpdateFeaturePositions(
            IFontMetrics fontMetrics,
            GlyphPositioningCollection collection,
            ushort index,
            int count,
            params LangSysTable[] langSysTables)
        {
            bool updated = false;
            for (int i = 0; i < langSysTables.Length; i++)
            {
                ushort[] featureIndices = langSysTables[i].FeatureIndices;
                for (int j = 0; j < featureIndices.Length; j++)
                {
                    // TODO: Should we be applying all features?
                    ushort[] lookupListIndices = this.FeatureList.FeatureTables[featureIndices[j]].LookupListIndices;
                    for (int k = 0; k < lookupListIndices.Length; k++)
                    {
                        // TODO: Consider caching the relevant langtables per script.
                        // There's a lot of repetitive checks here.
                        LookupTable lookupTable = this.LookupList.LookupTables[lookupListIndices[k]];
                        updated |= lookupTable.TryUpdatePosition(fontMetrics, this, collection, index, count);
                    }
                }
            }

            return updated;
        }
    }
}

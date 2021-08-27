// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Tables.General.Gsub;

namespace SixLabors.Fonts.Tables.General
{
    /// <summary>
    /// The Glyph Substitution (GSUB) table provides data for substition of glyphs for appropriate rendering of scripts,
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
            long position = reader.BaseStream.Position;
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();

            ushort scriptListOffset = reader.ReadOffset16();
            ushort featureListOffset = reader.ReadOffset16();
            ushort lookupListOffset = reader.ReadOffset16();
            uint featureVariationsOffset = (minorVersion == 1) ? reader.ReadOffset32() : 0;

            // TODO: Optimization. Allow only reading the scriptList.
            var scriptList = ScriptList.Load(reader, position + scriptListOffset);

            var featureList = FeatureListTable.Load(reader, position + featureListOffset);

            var lookupList = LookupListTable.Load(reader, position + lookupListOffset);

            // TODO: Feature Variations.
            return new GSubTable(scriptList, featureList, lookupList);
        }

        private static LookupSubTable LoadLookupSubTable(ushort lookupType, BigEndianBinaryReader reader, long offset)
            => lookupType switch
            {
                1 => SingleSubstitutionSubTable.Load(reader, offset),
                7 => ExtensionSubstitutionSubTable.Load(reader, offset, LoadLookupSubTable),
                _ => new NotImplementedSubTable(),
            };
    }
}

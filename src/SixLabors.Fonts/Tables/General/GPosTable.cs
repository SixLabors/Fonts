// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables.General
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

        public GPosTable(ScriptList scriptList, FeatureList featureList)
        {
            this.ScriptList = scriptList;
            this.FeatureList = featureList;
        }

        public ScriptList ScriptList { get; }

        public FeatureList FeatureList { get; }

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
            long position = reader.BaseStream.Position;
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();

            ushort scriptListOffset = reader.ReadOffset16();
            ushort featureListOffset = reader.ReadOffset16();
            ushort lookupListOffset = reader.ReadOffset16();
            uint featureVariationsOffset = (minorVersion == 1) ? reader.ReadOffset32() : 0;

            // TODO: Optimization. ALlow only reading the scriptList.
            var scriptList = ScriptList.Load(reader, position + scriptListOffset);

            var featureList = FeatureList.Load(reader, position + featureListOffset);
            throw new NotImplementedException();
        }
    }
}

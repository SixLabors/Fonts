// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// OpenType Layout fonts may contain one or more groups of glyphs used to render various scripts,
    /// which are enumerated in a ScriptList table. Both the GSUB and GPOS tables define
    /// Script List tables (ScriptList):
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#slTbl_sRec"/>
    /// </summary>
    internal sealed class ScriptList : Dictionary<uint, ScriptListTable>
    {
        private ScriptList()
        {
        }

        public static ScriptList Load(BigEndianBinaryReader reader, long offset)
        {
            // ScriptListTable
            // +--------------+----------------------------+-------------------------------------------------------------+
            // | Type         | Name                       | Description                                                 |
            // +==============+============================+=============================================================+
            // | uint16       | scriptCount                | Number of ScriptRecords                                     |
            // +--------------+----------------------------+-------------------------------------------------------------+
            // | ScriptRecord | scriptRecords[scriptCount] | Array of ScriptRecords, listed alphabetically by script tag |
            // +--------------+----------------------------+-------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort scriptCount = reader.ReadUInt16();
            var scriptList = new ScriptList();

            // Read records (tags and table offsets)
            uint[] scriptTags = new uint[scriptCount];
            ushort[] scriptOffsets = new ushort[scriptCount];

            for (int i = 0; i < scriptTags.Length; i++)
            {
                scriptTags[i] = reader.ReadUInt32();
                scriptOffsets[i] = reader.ReadUInt16();
            }

            // Read each table and add it to the dictionary
            for (int i = 0; i < scriptCount; ++i)
            {
                uint scriptTag = scriptTags[i];
                var scriptTable = ScriptListTable.Load(scriptTag, reader, offset + scriptOffsets[i]);
                scriptList.Add(scriptTag, scriptTable);
            }

            return scriptList;
        }
    }

    internal sealed class ScriptListTable
    {
        private readonly LangSysTable? defaultLang;
        private readonly LangSysTable[] langSysTables;

        private ScriptListTable(LangSysTable[] langSysTables, LangSysTable? defaultLang, uint scriptTag)
        {
            this.langSysTables = langSysTables;
            this.defaultLang = defaultLang;
            this.ScriptTag = scriptTag;
        }

        public uint ScriptTag { get; }

        public static ScriptListTable Load(uint scriptTag, BigEndianBinaryReader reader, long offset)
        {
            // ScriptListTable
            // +---------------+------------------------------+-------------------------------------------------------------------------------+
            // | Type          | Name                         | Description                                                                   |
            // +===============+==============================+===============================================================================+
            // | Offset16      | defaultLangSysOffset         | Offset to default LangSys table, from beginning of Script table — may be NULL |
            // +---------------+------------------------------+-------------------------------------------------------------------------------+
            // | uint16        | langSysCount                 | Number of LangSysRecords for this script — excluding the default LangSys      |
            // +---------------+------------------------------+-------------------------------------------------------------------------------+
            // | LangSysRecord | langSysRecords[langSysCount] | Array of LangSysRecords, listed alphabetically by LangSys tag                 |
            // +---------------+------------------------------+-------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort defaultLangSysOffset = reader.ReadOffset16();
            ushort langSysCount = reader.ReadUInt16();

            var langSysTables = new LangSysTable[langSysCount];
            for (int i = 0; i < langSysTables.Length; i++)
            {
                // LangSysRecord
                // +----------+---------------+---------------------------------------------------------+
                // | Type     | Name          | Description                                             |
                // +==========+===============+=========================================================+
                // | Tag      | langSysTag    | 4-byte LangSysTag identifier                            |
                // +----------+---------------+---------------------------------------------------------+
                // | Offset16 | langSysOffset | Offset to LangSys table, from beginning of Script table |
                // +----------+---------------+---------------------------------------------------------+
                uint langSysTag = reader.ReadUInt32();
                ushort langSysOffset = reader.ReadOffset16();
                langSysTables[i] = new LangSysTable(langSysTag, langSysOffset);
            }

            // Load the default table.
            LangSysTable? defaultLangSysTable = null;
            if (defaultLangSysOffset > 0)
            {
                defaultLangSysTable = new LangSysTable(0, defaultLangSysOffset);
                defaultLangSysTable.LoadFeatures(reader, offset + defaultLangSysOffset);
            }

            // Load the other table features.
            // We do this last to avoid excessive seeking.
            for (int i = 0; i < langSysTables.Length; i++)
            {
                LangSysTable langSysTable = langSysTables[i];
                langSysTable.LoadFeatures(reader, offset + langSysTable.Offset);
            }

            return new ScriptListTable(langSysTables, defaultLangSysTable, scriptTag);
        }
    }

    internal class LangSysTable
    {
        public LangSysTable(uint tag, ushort offset)
        {
            this.Tag = tag;
            this.Offset = offset;
        }

        public uint Tag { get; }

        public ushort Offset { get; }

        public ushort RequiredFeatureIndex { get; private set; }

        public ushort[] FeatureIndices { get; private set; } = Array.Empty<ushort>();

        public void LoadFeatures(BigEndianBinaryReader reader, long offset)
        {
            // +----------+-----------------------------------+-----------------------------------------------------------------------------------------+
            // | Type     | Name                              | Description                                                                             |
            // +==========+===================================+=========================================================================================+
            // | Offset16 | lookupOrderOffset                 | = NULL(reserved for an offset to a reordering table)                                   |
            // +----------+-----------------------------------+-----------------------------------------------------------------------------------------+
            // | uint16   | requiredFeatureIndex              | Index of a feature required for this language system; if no required features = 0xFFFF  |
            // +----------+-----------------------------------+-----------------------------------------------------------------------------------------+
            // | uint16   | featureIndexCount                 | Number of feature index values for this language system — excludes the required feature |
            // +----------+-----------------------------------+-----------------------------------------------------------------------------------------+
            // | uint16   | featureIndices[featureIndexCount] | Array of indices into the FeatureList, in arbitrary order                               |
            // +----------+-----------------------------------+-----------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);
            ushort lookupOrderOffset = reader.ReadOffset16();
            this.RequiredFeatureIndex = reader.ReadUInt16();
            ushort featureIndexCount = reader.ReadUInt16();
            this.FeatureIndices = reader.ReadUInt16Array(featureIndexCount);
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// OpenType Layout fonts may contain one or more groups of glyphs used to render various scripts,
/// which are enumerated in a ScriptList table. Both the GSUB and GPOS tables define
/// Script List tables (ScriptList):
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#slTbl_sRec"/>
/// </summary>
internal sealed class ScriptList : Dictionary<Tag, ScriptListTable>
{
    private readonly Tag scriptTag;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptList"/> class.
    /// </summary>
    /// <param name="scriptTag">The tag of the first (default) script in the list.</param>
    private ScriptList(Tag scriptTag) => this.scriptTag = scriptTag;

    /// <summary>
    /// Loads the <see cref="ScriptList"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the GPOS or GSUB table to the ScriptList table.</param>
    /// <returns>The <see cref="ScriptList"/>, or <see langword="null"/> if the script count is zero.</returns>
    public static ScriptList? Load(BigEndianBinaryReader reader, long offset)
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

        // Read records (tags and table offsets)
        var scriptTags = new Tag[scriptCount];
        ushort[] scriptOffsets = new ushort[scriptCount];

        for (int i = 0; i < scriptTags.Length; i++)
        {
            scriptTags[i] = reader.ReadUInt32();
            scriptOffsets[i] = reader.ReadUInt16();
        }

        // Read each table and add it to the dictionary
        ScriptList? scriptList = null;
        for (int i = 0; i < scriptCount; ++i)
        {
            Tag scriptTag = scriptTags[i];
            if (i == 0)
            {
                scriptList = new ScriptList(scriptTag);
            }

            var scriptTable = ScriptListTable.Load(scriptTag, reader, offset + scriptOffsets[i]);
            scriptList!.Add(scriptTag, scriptTable);
        }

        return scriptList;
    }

    /// <summary>
    /// Gets the default script table (the first script in the list).
    /// Dictionaries are unordered, so this uses the stored first script tag.
    /// </summary>
    /// <returns>The default <see cref="ScriptListTable"/>.</returns>
    public ScriptListTable Default() => this[this.scriptTag];
}

/// <summary>
/// A Script table identifies the language systems supported by a script and contains a default
/// language system table and an array of language system tables.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record"/>
/// </summary>
internal sealed class ScriptListTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptListTable"/> class.
    /// </summary>
    /// <param name="langSysTables">The array of language system tables.</param>
    /// <param name="defaultLang">The default language system table, or <see langword="null"/> if none.</param>
    /// <param name="scriptTag">The 4-byte script identification tag.</param>
    private ScriptListTable(LangSysTable[] langSysTables, LangSysTable? defaultLang, Tag scriptTag)
    {
        this.LangSysTables = langSysTables;
        this.DefaultLangSysTable = defaultLang;
        this.ScriptTag = scriptTag;
    }

    /// <summary>
    /// Gets the 4-byte script identification tag.
    /// </summary>
    public Tag ScriptTag { get; }

    /// <summary>
    /// Gets the default language system table, or <see langword="null"/> if none is defined.
    /// </summary>
    public LangSysTable? DefaultLangSysTable { get; }

    /// <summary>
    /// Gets the array of language system tables for this script.
    /// </summary>
    public LangSysTable[] LangSysTables { get; }

    /// <summary>
    /// Loads the <see cref="ScriptListTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="scriptTag">The 4-byte script identification tag.</param>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the Script table.</param>
    /// <returns>The <see cref="ScriptListTable"/>.</returns>
    public static ScriptListTable Load(Tag scriptTag, BigEndianBinaryReader reader, long offset)
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

        var langSysRecords = new LangSysRecord[langSysCount];
        for (int i = 0; i < langSysRecords.Length; i++)
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
            langSysRecords[i] = new LangSysRecord(langSysTag, langSysOffset);
        }

        // Load the default table.
        LangSysTable? defaultLangSysTable = null;
        if (defaultLangSysOffset > 0)
        {
            defaultLangSysTable = LangSysTable.Load(0, reader, offset + defaultLangSysOffset);
        }

        // Load the other table features.
        // We do this last to avoid excessive seeking.
        var langSysTables = new LangSysTable[langSysCount];
        for (int i = 0; i < langSysTables.Length; i++)
        {
            LangSysRecord langSysRecord = langSysRecords[i];
            langSysTables[i] = LangSysTable.Load(langSysRecord.LangSysTag, reader, offset + langSysRecord.LangSysOffset);
        }

        return new ScriptListTable(langSysTables, defaultLangSysTable, scriptTag);
    }

    /// <summary>
    /// A LangSysRecord contains a language system tag and its offset to the LangSys table.
    /// </summary>
    private readonly struct LangSysRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LangSysRecord"/> struct.
        /// </summary>
        /// <param name="langSysTag">The 4-byte language system tag identifier.</param>
        /// <param name="langSysOffset">The offset to the LangSys table from the beginning of the Script table.</param>
        public LangSysRecord(uint langSysTag, ushort langSysOffset)
        {
            this.LangSysTag = langSysTag;
            this.LangSysOffset = langSysOffset;
        }

        /// <summary>
        /// Gets the 4-byte language system tag identifier.
        /// </summary>
        public uint LangSysTag { get; }

        /// <summary>
        /// Gets the offset to the LangSys table from the beginning of the Script table.
        /// </summary>
        public ushort LangSysOffset { get; }
    }
}

/// <summary>
/// The Language System table (LangSys) identifies language-system features for a script.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table"/>
/// </summary>
internal sealed class LangSysTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LangSysTable"/> class.
    /// </summary>
    /// <param name="langSysTag">The 4-byte language system tag identifier.</param>
    /// <param name="requiredFeatureIndex">The index of a required feature; 0xFFFF if none.</param>
    /// <param name="featureIndices">The array of indices into the FeatureList.</param>
    private LangSysTable(uint langSysTag, ushort requiredFeatureIndex, ushort[] featureIndices)
    {
        this.LangSysTag = langSysTag;
        this.RequiredFeatureIndex = requiredFeatureIndex;
        this.FeatureIndices = featureIndices;
    }

    /// <summary>
    /// Gets the 4-byte language system tag identifier.
    /// </summary>
    public uint LangSysTag { get; }

    /// <summary>
    /// Gets the index of a feature required for this language system; 0xFFFF if no required features.
    /// </summary>
    public ushort RequiredFeatureIndex { get; }

    /// <summary>
    /// Gets the array of indices into the FeatureList, in arbitrary order.
    /// </summary>
    public ushort[] FeatureIndices { get; } = Array.Empty<ushort>();

    /// <summary>
    /// Loads the <see cref="LangSysTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="langSysTag">The 4-byte language system tag identifier.</param>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the LangSys table.</param>
    /// <returns>The <see cref="LangSysTable"/>.</returns>
    public static LangSysTable Load(uint langSysTag, BigEndianBinaryReader reader, long offset)
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
        ushort requiredFeatureIndex = reader.ReadUInt16();
        ushort featureIndexCount = reader.ReadUInt16();

        ushort[] featureIndices = reader.ReadUInt16Array(featureIndexCount);
        return new LangSysTable(langSysTag, requiredFeatureIndex, featureIndices);
    }
}

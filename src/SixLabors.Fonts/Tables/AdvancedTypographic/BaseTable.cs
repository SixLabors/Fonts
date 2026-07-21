// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The Baseline table (BASE) provides information used to align glyphs of different scripts
/// and sizes in a line of text, whether the glyphs are in the same font or in different fonts.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/base"/>
/// </summary>
internal sealed class BaseTable : Table
{
    /// <summary>
    /// The OpenType table tag for the BASE table.
    /// </summary>
    internal const string TableName = "BASE";

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTable"/> class.
    /// </summary>
    /// <param name="horizontalAxis">The horizontal axis table, or <see langword="null"/> if not present.</param>
    /// <param name="verticalAxis">The vertical axis table, or <see langword="null"/> if not present.</param>
    public BaseTable(BaseAxisTable? horizontalAxis, BaseAxisTable? verticalAxis)
    {
        this.HorizontalAxis = horizontalAxis;
        this.VerticalAxis = verticalAxis;
    }

    /// <summary>
    /// Gets the axis table holding baseline data for horizontal text layout, where baseline
    /// coordinates are Y values, or <see langword="null"/> if the font provides none.
    /// </summary>
    public BaseAxisTable? HorizontalAxis { get; }

    /// <summary>
    /// Gets the axis table holding baseline data for vertical text layout, where baseline
    /// coordinates are X values, or <see langword="null"/> if the font provides none.
    /// </summary>
    public BaseAxisTable? VerticalAxis { get; }

    /// <summary>
    /// Tries to get the coordinate of the named baseline for the given layout direction from
    /// the default script record of the matching axis.
    /// </summary>
    /// <param name="baselineTag">The baseline identification tag, for example 'hang' or 'ideo'.</param>
    /// <param name="isVerticalLayout">
    /// Whether to read the vertical axis, whose coordinates are X values, rather than the
    /// horizontal axis, whose coordinates are Y values.
    /// </param>
    /// <param name="coordinate">
    /// The baseline coordinate in design units, measured from the zero position on the
    /// relevant axis.
    /// </param>
    /// <returns><see langword="true"/> when the axis defines the named baseline; otherwise <see langword="false"/>.</returns>
    public bool TryGetBaselineCoordinate(Tag baselineTag, bool isVerticalLayout, out short coordinate)
    {
        BaseAxisTable? axis = isVerticalLayout
            ? this.VerticalAxis
            : this.HorizontalAxis;

        if (axis is null)
        {
            coordinate = 0;
            return false;
        }

        return axis.TryGetBaselineCoordinate(baselineTag, out coordinate);
    }

    /// <summary>
    /// Loads the <see cref="BaseTable"/> from the font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="BaseTable"/>, or <see langword="null"/> if not present.</returns>
    public static BaseTable? Load(FontReader fontReader)
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
    /// Loads the <see cref="BaseTable"/> from a big endian binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="BaseTable"/>.</returns>
    internal static BaseTable Load(BigEndianBinaryReader reader)
    {
        // BASE Header, Version 1.0
        // +----------+-----------------+--------------------------------------------------------------------------+
        // | Type     | Name            | Description                                                              |
        // +==========+=================+==========================================================================+
        // | uint16   | majorVersion    | Major version of the BASE table, = 1                                     |
        // +----------+-----------------+--------------------------------------------------------------------------+
        // | uint16   | minorVersion    | Minor version of the BASE table, = 0                                     |
        // +----------+-----------------+--------------------------------------------------------------------------+
        // | Offset16 | horizAxisOffset | Offset to horizontal Axis table, from beginning of BASE table (may be NULL) |
        // +----------+-----------------+--------------------------------------------------------------------------+
        // | Offset16 | vertAxisOffset  | Offset to vertical Axis table, from beginning of BASE table (may be NULL)   |
        // +----------+-----------------+--------------------------------------------------------------------------+

        // BASE Header, Version 1.1
        // +----------+--------------------+-------------------------------------------------------------------------------+
        // | Type     | Name               | Description                                                                   |
        // +==========+====================+===============================================================================+
        // | uint16   | majorVersion       | Major version of the BASE table, = 1                                          |
        // +----------+--------------------+-------------------------------------------------------------------------------+
        // | uint16   | minorVersion       | Minor version of the BASE table, = 1                                          |
        // +----------+--------------------+-------------------------------------------------------------------------------+
        // | Offset16 | horizAxisOffset    | Offset to horizontal Axis table, from beginning of BASE table (may be NULL)   |
        // +----------+--------------------+-------------------------------------------------------------------------------+
        // | Offset16 | vertAxisOffset     | Offset to vertical Axis table, from beginning of BASE table (may be NULL)     |
        // +----------+--------------------+-------------------------------------------------------------------------------+
        // | Offset32 | itemVarStoreOffset | Offset to ItemVariationStore table, from beginning of BASE table (may be NULL) |
        // +----------+--------------------+-------------------------------------------------------------------------------+
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();

        ushort horizAxisOffset = reader.ReadOffset16();
        ushort vertAxisOffset = reader.ReadOffset16();

        BaseAxisTable? horizontalAxis = horizAxisOffset != 0
            ? BaseAxisTable.Load(reader, horizAxisOffset)
            : null;

        BaseAxisTable? verticalAxis = vertAxisOffset != 0
            ? BaseAxisTable.Load(reader, vertAxisOffset)
            : null;

        return new BaseTable(horizontalAxis, verticalAxis);
    }
}

/// <summary>
/// An Axis table of the BASE table stores all baseline information for one text layout
/// direction: baseline identification tags and the per-script baseline coordinates.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/base#axis-tables-horizaxis-and-vertaxis"/>
/// </summary>
internal sealed class BaseAxisTable
{
    /// <summary>
    /// The 'DFLT' script identification tag, preferred when selecting the script record that
    /// supplies baseline coordinates for the whole font.
    /// </summary>
    private static readonly Tag DefaultScriptTag = Tag.Parse("DFLT");

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAxisTable"/> class.
    /// </summary>
    /// <param name="baselineTags">The baseline identification tags in this text direction.</param>
    /// <param name="scripts">The per-script baseline entries in this text direction.</param>
    public BaseAxisTable(Tag[] baselineTags, BaseScriptEntry[] scripts)
    {
        this.BaselineTags = baselineTags;
        this.Scripts = scripts;
    }

    /// <summary>
    /// Gets the baseline identification tags in this text direction, in alphabetical order.
    /// Baseline coordinates in each script's values are stored in matching order.
    /// </summary>
    public Tag[] BaselineTags { get; }

    /// <summary>
    /// Gets the per-script baseline entries in this text direction.
    /// </summary>
    public BaseScriptEntry[] Scripts { get; }

    /// <summary>
    /// Tries to get the coordinate of the named baseline from the 'DFLT' script entry, falling
    /// back to the first script entry that carries baseline values.
    /// </summary>
    /// <param name="baselineTag">The baseline identification tag, for example 'hang' or 'ideo'.</param>
    /// <param name="coordinate">
    /// The baseline coordinate in design units, measured from the zero position on the
    /// relevant axis.
    /// </param>
    /// <returns><see langword="true"/> when the axis defines the named baseline; otherwise <see langword="false"/>.</returns>
    public bool TryGetBaselineCoordinate(Tag baselineTag, out short coordinate)
    {
        coordinate = 0;

        int index = -1;
        for (int i = 0; i < this.BaselineTags.Length; i++)
        {
            if (this.BaselineTags[i] == baselineTag)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        BaseValuesTable? values = null;
        for (int i = 0; i < this.Scripts.Length; i++)
        {
            BaseScriptEntry entry = this.Scripts[i];
            if (entry.Values is null)
            {
                continue;
            }

            if (entry.ScriptTag == DefaultScriptTag)
            {
                values = entry.Values;
                break;
            }

            values ??= entry.Values;
        }

        if (values is null || index >= values.Coordinates.Length)
        {
            return false;
        }

        coordinate = values.Coordinates[index];
        return true;
    }

    /// <summary>
    /// Loads the <see cref="BaseAxisTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the BASE table to the Axis table.</param>
    /// <returns>The <see cref="BaseAxisTable"/>.</returns>
    public static BaseAxisTable Load(BigEndianBinaryReader reader, long offset)
    {
        // Axis Table
        // +----------+----------------------+------------------------------------------------------------------------+
        // | Type     | Name                 | Description                                                            |
        // +==========+======================+========================================================================+
        // | Offset16 | baseTagListOffset    | Offset to BaseTagList table, from beginning of Axis table (may be NULL) |
        // +----------+----------------------+------------------------------------------------------------------------+
        // | Offset16 | baseScriptListOffset | Offset to BaseScriptList table, from beginning of Axis table           |
        // +----------+----------------------+------------------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort baseTagListOffset = reader.ReadOffset16();
        ushort baseScriptListOffset = reader.ReadOffset16();

        // BaseTagList Table
        // +--------+----------------------------+--------------------------------------------------------------------+
        // | Type   | Name                       | Description                                                        |
        // +========+============================+====================================================================+
        // | uint16 | baseTagCount               | Number of baseline identification tags in this text direction      |
        // +--------+----------------------------+--------------------------------------------------------------------+
        // | Tag    | baselineTags[baseTagCount] | Array of 4-byte baseline identification tags, in alphabetical order |
        // +--------+----------------------------+--------------------------------------------------------------------+
        Tag[] baselineTags = Array.Empty<Tag>();
        if (baseTagListOffset != 0)
        {
            reader.Seek(offset + baseTagListOffset, SeekOrigin.Begin);

            ushort baseTagCount = reader.ReadUInt16();
            baselineTags = new Tag[baseTagCount];
            for (int i = 0; i < baselineTags.Length; i++)
            {
                baselineTags[i] = reader.ReadUInt32();
            }
        }

        // BaseScriptList Table
        // +------------------+-------------------------------------+-------------------------------------------------+
        // | Type             | Name                                | Description                                     |
        // +==================+=====================================+=================================================+
        // | uint16           | baseScriptCount                     | Number of BaseScriptRecords defined             |
        // +------------------+-------------------------------------+-------------------------------------------------+
        // | BaseScriptRecord | baseScriptRecords[baseScriptCount]  | Array of BaseScriptRecords, in alphabetical     |
        // |                  |                                     | order by baseScriptTag                          |
        // +------------------+-------------------------------------+-------------------------------------------------+

        // BaseScriptRecord
        // +----------+------------------+-----------------------------------------------------------------------+
        // | Type     | Name             | Description                                                           |
        // +==========+==================+=======================================================================+
        // | Tag      | baseScriptTag    | 4-byte script identification tag                                      |
        // +----------+------------------+-----------------------------------------------------------------------+
        // | Offset16 | baseScriptOffset | Offset to BaseScript table, from beginning of BaseScriptList          |
        // +----------+------------------+-----------------------------------------------------------------------+
        long scriptListStart = offset + baseScriptListOffset;
        reader.Seek(scriptListStart, SeekOrigin.Begin);

        ushort baseScriptCount = reader.ReadUInt16();
        var scriptTags = new Tag[baseScriptCount];
        ushort[] scriptOffsets = new ushort[baseScriptCount];
        for (int i = 0; i < scriptTags.Length; i++)
        {
            scriptTags[i] = reader.ReadUInt32();
            scriptOffsets[i] = reader.ReadOffset16();
        }

        var scripts = new BaseScriptEntry[baseScriptCount];
        for (int i = 0; i < scripts.Length; i++)
        {
            scripts[i] = BaseScriptEntry.Load(scriptTags[i], reader, scriptListStart + scriptOffsets[i]);
        }

        return new BaseAxisTable(baselineTags, scripts);
    }
}

/// <summary>
/// A BaseScript entry pairs a script identification tag with the baseline values the BASE
/// table defines for that script in one text direction.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/base#basescript-table"/>
/// </summary>
internal sealed class BaseScriptEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseScriptEntry"/> class.
    /// </summary>
    /// <param name="scriptTag">The script identification tag.</param>
    /// <param name="values">The baseline values for the script, or <see langword="null"/> if none are defined.</param>
    public BaseScriptEntry(Tag scriptTag, BaseValuesTable? values)
    {
        this.ScriptTag = scriptTag;
        this.Values = values;
    }

    /// <summary>
    /// Gets the script identification tag.
    /// </summary>
    public Tag ScriptTag { get; }

    /// <summary>
    /// Gets the baseline values for the script, or <see langword="null"/> if none are defined.
    /// </summary>
    public BaseValuesTable? Values { get; }

    /// <summary>
    /// Loads the <see cref="BaseScriptEntry"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="scriptTag">The script identification tag from the owning record.</param>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the BASE table to the BaseScript table.</param>
    /// <returns>The <see cref="BaseScriptEntry"/>.</returns>
    public static BaseScriptEntry Load(Tag scriptTag, BigEndianBinaryReader reader, long offset)
    {
        // BaseScript Table
        // +----------------+-------------------------------------+---------------------------------------------------+
        // | Type           | Name                                | Description                                       |
        // +================+=====================================+===================================================+
        // | Offset16       | baseValuesOffset                    | Offset to BaseValues table, from beginning of     |
        // |                |                                     | BaseScript table (may be NULL)                    |
        // +----------------+-------------------------------------+---------------------------------------------------+
        // | Offset16       | defaultMinMaxOffset                 | Offset to MinMax table, from beginning of         |
        // |                |                                     | BaseScript table (may be NULL)                    |
        // +----------------+-------------------------------------+---------------------------------------------------+
        // | uint16         | baseLangSysCount                    | Number of BaseLangSys records defined             |
        // +----------------+-------------------------------------+---------------------------------------------------+
        // | BaseLangSys    | baseLangSysRecords[baseLangSysCount] | Array of BaseLangSys records, in alphabetical    |
        // |                |                                     | order by BaseLangSysTag                           |
        // +----------------+-------------------------------------+---------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort baseValuesOffset = reader.ReadOffset16();

        BaseValuesTable? values = baseValuesOffset != 0
            ? BaseValuesTable.Load(reader, offset + baseValuesOffset)
            : null;

        return new BaseScriptEntry(scriptTag, values);
    }
}

/// <summary>
/// A BaseValues table lists the coordinate positions of all baselines named in the
/// corresponding BaseTagList for one script and identifies the script's default baseline.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/base#basevalues-table"/>
/// </summary>
internal sealed class BaseValuesTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseValuesTable"/> class.
    /// </summary>
    /// <param name="defaultBaselineIndex">The index of the script's default baseline in the axis baseline tags.</param>
    /// <param name="coordinates">The baseline coordinates in design units, in baseline tag order.</param>
    public BaseValuesTable(ushort defaultBaselineIndex, short[] coordinates)
    {
        this.DefaultBaselineIndex = defaultBaselineIndex;
        this.Coordinates = coordinates;
    }

    /// <summary>
    /// Gets the index of the script's default baseline in the axis baseline tags.
    /// </summary>
    public ushort DefaultBaselineIndex { get; }

    /// <summary>
    /// Gets the baseline coordinates in design units, ordered to match the axis baseline tags.
    /// </summary>
    public short[] Coordinates { get; }

    /// <summary>
    /// Loads the <see cref="BaseValuesTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the BASE table to the BaseValues table.</param>
    /// <returns>The <see cref="BaseValuesTable"/>.</returns>
    public static BaseValuesTable Load(BigEndianBinaryReader reader, long offset)
    {
        // BaseValues Table
        // +----------+-----------------------------------+-----------------------------------------------------------+
        // | Type     | Name                              | Description                                               |
        // +==========+===================================+===========================================================+
        // | uint16   | defaultBaselineIndex              | Index of default baseline for this script, equals index   |
        // |          |                                   | of baseline tag in baselineTags array of the BaseTagList  |
        // +----------+-----------------------------------+-----------------------------------------------------------+
        // | uint16   | baseCoordCount                    | Number of BaseCoord tables defined, should equal          |
        // |          |                                   | baseTagCount in the BaseTagList                           |
        // +----------+-----------------------------------+-----------------------------------------------------------+
        // | Offset16 | baseCoordOffsets[baseCoordCount]  | Array of offsets to BaseCoord tables, from beginning of   |
        // |          |                                   | BaseValues table, order matches baselineTags array        |
        // +----------+-----------------------------------+-----------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort defaultBaselineIndex = reader.ReadUInt16();
        ushort baseCoordCount = reader.ReadUInt16();
        ushort[] coordOffsets = new ushort[baseCoordCount];
        for (int i = 0; i < coordOffsets.Length; i++)
        {
            coordOffsets[i] = reader.ReadOffset16();
        }

        // BaseCoord Tables
        // All three formats begin with the format identifier followed by the coordinate in
        // design units. Format 2 appends a reference glyph and contour point index used for
        // hinting adjustments; format 3 appends an offset to a Device or VariationIndex
        // table. The design unit coordinate is authoritative in every format.
        // +--------+------------+-------------------------------------+
        // | Type   | Name       | Description                         |
        // +========+============+=====================================+
        // | uint16 | format     | Format identifier, = 1, 2 or 3      |
        // +--------+------------+-------------------------------------+
        // | int16  | coordinate | X or Y value, in design units       |
        // +--------+------------+-------------------------------------+
        short[] coordinates = new short[baseCoordCount];
        for (int i = 0; i < coordinates.Length; i++)
        {
            reader.Seek(offset + coordOffsets[i], SeekOrigin.Begin);
            ushort format = reader.ReadUInt16();
            coordinates[i] = reader.ReadInt16();
        }

        return new BaseValuesTable(defaultBaselineIndex, coordinates);
    }
}

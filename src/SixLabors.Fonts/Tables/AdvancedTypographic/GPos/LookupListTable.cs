// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// The headers of the GSUB and GPOS tables contain offsets to Lookup List tables (LookupList) for
/// glyph substitution (GSUB table) and glyph positioning (GPOS table). The LookupList table contains
/// an array of offsets to Lookup tables (lookupOffsets).
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table"/>
/// </summary>
internal sealed class LookupListTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupListTable"/> class.
    /// </summary>
    /// <param name="lookupCount">The number of lookups in this table.</param>
    /// <param name="lookupTables">The array of lookup tables.</param>
    private LookupListTable(ushort lookupCount, LookupTable[] lookupTables)
    {
        this.LookupCount = lookupCount;
        this.LookupTables = lookupTables;
    }

    /// <summary>
    /// Gets the number of lookups in this table.
    /// </summary>
    public ushort LookupCount { get; }

    /// <summary>
    /// Gets the array of lookup tables.
    /// </summary>
    public LookupTable[] LookupTables { get; }

    /// <summary>
    /// Loads the <see cref="LookupListTable"/> from the specified reader at the given offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the lookup list table.</param>
    /// <returns>The loaded <see cref="LookupListTable"/>.</returns>
    public static LookupListTable Load(BigEndianBinaryReader reader, long offset)
    {
        // +----------+----------------------------+---------------------------------------------------------------+
        // | Type     | Name                       | Description                                                   |
        // +==========+============================+===============================================================+
        // | uint16   | lookupCount                | Number of lookups in this table                               |
        // +----------+----------------------------+---------------------------------------------------------------+
        // | Offset16 | lookupOffsets[lookupCount] | Array of offsets to Lookup tables, from beginning             |
        // |          |                            | of LookupList — zero based (first lookup is Lookup index = 0) |
        // +----------+----------------------------+---------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort lookupCount = reader.ReadUInt16();
        using Buffer<ushort> lookupOffsetsBuffer = new(lookupCount);
        Span<ushort> lookupOffsets = lookupOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(lookupOffsets);

        LookupTable[] lookupTables = new LookupTable[lookupCount];

        for (int i = 0; i < lookupTables.Length; i++)
        {
            lookupTables[i] = LookupTable.Load(reader, offset + lookupOffsets[i]);
        }

        return new LookupListTable(lookupCount, lookupTables);
    }
}

/// <summary>
/// A Lookup table (Lookup) defines the specific conditions, type, and results of a substitution
/// or positioning action that is used to implement a feature. For example, a substitution
/// operation requires a list of target glyph indices to be replaced, a list of replacement glyph
/// indices, and a description of the type of substitution action.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"/>
/// </summary>
internal sealed class LookupTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupTable"/> class.
    /// </summary>
    /// <param name="lookupType">The lookup type, identifying the kind of positioning operation.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <param name="lookupSubTables">The array of lookup subtables.</param>
    private LookupTable(
        ushort lookupType,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        LookupSubTable[] lookupSubTables)
    {
        this.LookupType = lookupType;
        this.LookupFlags = lookupFlags;
        this.MarkFilteringSet = markFilteringSet;
        this.LookupSubTables = lookupSubTables;
    }

    /// <summary>
    /// Gets the lookup type that identifies the kind of positioning operation.
    /// </summary>
    public ushort LookupType { get; }

    /// <summary>
    /// Gets the lookup qualifiers.
    /// </summary>
    public LookupFlags LookupFlags { get; }

    /// <summary>
    /// Gets the index into the GDEF mark glyph sets structure.
    /// </summary>
    public ushort MarkFilteringSet { get; }

    /// <summary>
    /// Gets the array of lookup subtables.
    /// </summary>
    public LookupSubTable[] LookupSubTables { get; }

    /// <summary>
    /// Loads the <see cref="LookupTable"/> from the specified reader at the given offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the lookup table.</param>
    /// <returns>The loaded <see cref="LookupTable"/>.</returns>
    public static LookupTable Load(BigEndianBinaryReader reader, long offset)
    {
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | Type     | Name                           | Description                                                 |
        // +==========+================================+=============================================================+
        // | uint16   | lookupType                     | Different enumerations for GSUB and GPOS.                   |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | lookupFlag                     | Lookup qualifiers .                                         |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | subTableCount                  | Number of subtables for this lookup.                        |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | Offset16 | subtableOffsets[subTableCount] | Array of offsets to lookup subtables, from beginning of     |
        // |          |                                | Lookup table.                                               |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | markFilteringSet               | Index (base 0) into GDEF mark glyph sets structure.         |
        // |          |                                | This field is only present if the USE_MARK_FILTERING_SET    |
        // |          |                                | lookup flag is set.                                         |
        // +----------+--------------------------------+-------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort lookupType = reader.ReadUInt16();
        LookupFlags lookupFlags = reader.ReadUInt16<LookupFlags>();
        ushort subTableCount = reader.ReadUInt16();

        using Buffer<ushort> subTableOffsetsBuffer = new(subTableCount);
        Span<ushort> subTableOffsets = subTableOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(subTableOffsets);

        // The fifth bit indicates the presence of a MarkFilteringSet field in the Lookup table.
        ushort markFilteringSet = ((lookupFlags & LookupFlags.UseMarkFilteringSet) != 0)
            ? reader.ReadUInt16()
            : (ushort)0;

        LookupSubTable[] lookupSubTables = new LookupSubTable[subTableCount];

        for (int i = 0; i < lookupSubTables.Length; i++)
        {
            lookupSubTables[i] = LoadLookupSubTable(lookupType, lookupFlags, markFilteringSet, reader, offset + subTableOffsets[i]);
        }

        return new LookupTable(lookupType, lookupFlags, markFilteringSet, lookupSubTables);
    }

    /// <summary>
    /// Loads the appropriate lookup subtable based on the lookup type.
    /// </summary>
    /// <param name="lookupType">The lookup type identifier.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    private static LookupSubTable LoadLookupSubTable(ushort lookupType, LookupFlags lookupFlags, ushort markFilteringSet, BigEndianBinaryReader reader, long offset)
        => lookupType switch
        {
            1 => LookupType1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            3 => LookupType3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            4 => LookupType4SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            5 => LookupType5SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            6 => LookupType6SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            7 => LookupType7SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            8 => LookupType8SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            9 => LookupType9SubTable.Load(reader, offset, lookupFlags, markFilteringSet, LoadLookupSubTable),
            _ => new NotImplementedSubTable()
        };

    /// <summary>
    /// Attempts to update the position of glyphs in the collection at the specified index.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GPOS table.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="feature">The feature tag.</param>
    /// <param name="index">The zero-based index of the glyph to position.</param>
    /// <param name="count">The number of glyphs remaining in the sequence.</param>
    /// <returns><see langword="true"/> if the position was updated; otherwise, <see langword="false"/>.</returns>
    public bool TryUpdatePosition(
        FontMetrics fontMetrics,
        GPosTable table,
        GlyphPositioningCollection collection,
        Tag feature,
        int index,
        int count)
    {
        foreach (LookupSubTable subTable in this.LookupSubTables)
        {
            // A lookup is finished for a glyph after the client locates the target
            // glyph or glyph context and performs a positioning action, if specified.
            if (subTable.TryUpdatePosition(fontMetrics, table, collection, feature, index, count))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Base class for all GPOS lookup subtables. Each subtable implements a specific type of glyph positioning operation.
/// </summary>
internal abstract class LookupSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupSubTable"/> class.
    /// </summary>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    protected LookupSubTable(LookupFlags lookupFlags, ushort markFilteringSet)
    {
        this.LookupFlags = lookupFlags;
        this.MarkFilteringSet = markFilteringSet;
    }

    /// <summary>
    /// Gets the lookup qualifiers.
    /// </summary>
    public LookupFlags LookupFlags { get; }

    /// <summary>
    /// Gets the mark filtering set index.
    /// </summary>
    public ushort MarkFilteringSet { get; }

    /// <summary>
    /// Attempts to update the position of glyphs in the collection at the specified index.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GPOS table.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="feature">The feature tag.</param>
    /// <param name="index">The zero-based index of the glyph to position.</param>
    /// <param name="count">The number of glyphs remaining in the sequence.</param>
    /// <returns><see langword="true"/> if the position was updated; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryUpdatePosition(
        FontMetrics fontMetrics,
        GPosTable table,
        GlyphPositioningCollection collection,
        Tag feature,
        int index,
        int count);
}

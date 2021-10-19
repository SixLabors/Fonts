// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// The headers of the GSUB and GPOS tables contain offsets to Lookup List tables (LookupList) for
    /// glyph substitution (GSUB table) and glyph positioning (GPOS table). The LookupList table contains
    /// an array of offsets to Lookup tables (lookupOffsets).
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table"/>
    /// </summary>
    internal sealed class LookupListTable
    {
        private LookupListTable(ushort lookupCount, LookupTable[] lookupTables)
        {
            this.LookupCount = lookupCount;
            this.LookupTables = lookupTables;
        }

        public ushort LookupCount { get; }

        public LookupTable[] LookupTables { get; }

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
            ushort[] lookupOffsets = reader.ReadUInt16Array(lookupCount);

            var lookupTables = new LookupTable[lookupCount];

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

        public ushort LookupType { get; }

        public LookupFlags LookupFlags { get; }

        public ushort MarkFilteringSet { get; }

        public LookupSubTable[] LookupSubTables { get; }

        public static LookupTable Load(BigEndianBinaryReader reader, long offset)
        {
            // +----------+--------------------------------+-------------------------------------------------------------+
            // | Type     | Name                           | Description                                                 |
            // +==========+================================+=============================================================+
            // | uint16   | lookupType                     | Different enumerations for GSUB and GPOS                    |
            // +----------+--------------------------------+-------------------------------------------------------------+
            // | uint16   | lookupFlag                     | Lookup qualifiers                                           |
            // +----------+--------------------------------+-------------------------------------------------------------+
            // | uint16   | subTableCount                  | Number of subtables for this lookup                         |
            // +----------+--------------------------------+-------------------------------------------------------------+
            // | Offset16 | subtableOffsets[subTableCount] | Array of offsets to lookup subtables, from beginning of     |
            // |          |                                | Lookup table                                                |
            // +----------+--------------------------------+-------------------------------------------------------------+
            // | uint16   | markFilteringSet               | Index (base 0) into GDEF mark glyph sets structure.         |
            // |          |                                | This field is only present if the USE\_MARK\_FILTERING\_SET |
            // |          |                                | lookup flag is set.                                         |
            // +----------+--------------------------------+-------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort lookupType = reader.ReadUInt16();
            LookupFlags lookupFlags = reader.ReadUInt16<LookupFlags>();
            ushort subTableCount = reader.ReadUInt16();

            ushort[] subTableOffsets = reader.ReadUInt16Array(subTableCount);

            // The fifth bit indicates the presence of a MarkFilteringSet field in the Lookup table.
            ushort markFilteringSet = ((lookupFlags & LookupFlags.UseMarkFilteringSet) != 0)
                ? reader.ReadUInt16()
                : (ushort)0;

            var lookupSubTables = new LookupSubTable[subTableCount];

            for (int i = 0; i < lookupSubTables.Length; i++)
            {
                lookupSubTables[i] = LoadLookupSubTable(lookupType, lookupFlags, reader, offset + subTableOffsets[i]);
            }

            return new LookupTable(lookupType, lookupFlags, markFilteringSet, lookupSubTables);
        }

        public bool TrySubstitution(
            FontMetrics fontMetrics,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            ushort index,
            int count)
        {
            foreach (LookupSubTable subTable in this.LookupSubTables)
            {
                if (subTable.TrySubstitution(fontMetrics, table, collection, feature, index, count))
                {
                    // A lookup is finished for a glyph after the client locates the target
                    // glyph or glyph context and performs a substitution, if specified.
                    return true;
                }
            }

            return false;
        }

        private static LookupSubTable LoadLookupSubTable(ushort lookupType, LookupFlags lookupFlags, BigEndianBinaryReader reader, long offset)
            => lookupType switch
            {
                1 => LookupType1SubTable.Load(reader, offset, lookupFlags),
                2 => LookupType2SubTable.Load(reader, offset, lookupFlags),
                3 => LookupType3SubTable.Load(reader, offset, lookupFlags),
                4 => LookupType4SubTable.Load(reader, offset, lookupFlags),
                5 => LookupType5SubTable.Load(reader, offset, lookupFlags),
                6 => LookupType6SubTable.Load(reader, offset, lookupFlags),
                7 => LookupType7SubTable.Load(reader, offset, lookupFlags, LoadLookupSubTable),
                8 => LookupType8SubTable.Load(reader, offset, lookupFlags),
                _ => throw new InvalidFontFileException($"Invalid value for 'lookupType' {lookupType}. Should be between '1' and '8' inclusive.")
            };
    }

    internal abstract class LookupSubTable
    {
        public LookupFlags LookupFlags { get; protected set; }

        public abstract bool TrySubstitution(
            FontMetrics fontMetrics,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            ushort index,
            int count);
    }
}

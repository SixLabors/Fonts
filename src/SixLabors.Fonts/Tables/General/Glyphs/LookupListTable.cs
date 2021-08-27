// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// The headers of the GSUB and GPOS tables contain offsets to Lookup List tables (LookupList) for
    /// glyph substitution (GSUB table) and glyph positioning (GPOS table). The LookupList table contains
    /// an array of offsets to Lookup tables (lookupOffsets).
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table"/>
    /// </summary>
    internal class LookupListTable
    {
        private LookupListTable(ushort lookupCount, LookupTable[] lookupTables)
        {
            this.LookupCount = lookupCount;
            this.LookupTables = lookupTables;
        }

        public ushort LookupCount { get; }

        public LookupTable[] LookupTables { get; }

        public static LookupListTable Load(
            BigEndianBinaryReader reader,
            long offset,
            Func<ushort, BigEndianBinaryReader, long, LookupSubTable> subTableLoader)
        {
            // +----------+----------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | Type     | Name                       | Description                                                                                                     |
            // +==========+============================+=================================================================================================================+
            // | uint16   | lookupCount                | Number of lookups in this table                                                                                 |
            // +----------+----------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | Offset16 | lookupOffsets[lookupCount] | Array of offsets to Lookup tables, from beginning of LookupList — zero based (first lookup is Lookup index = 0) |
            // +----------+----------------------------+-----------------------------------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort lookupCount = reader.ReadUInt16();
            ushort[] lookupOffsets = reader.ReadUInt16Array(lookupCount);

            var lookupTables = new LookupTable[lookupCount];

            for (int i = 0; i < lookupTables.Length; i++)
            {
                lookupTables[i] = LookupTable.Load(reader, offset + lookupOffsets[i], subTableLoader);
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
    internal class LookupTable
    {
        private LookupTable(
            ushort lookupType,
            ushort lookupFlags,
            ushort markFilteringSet,
            LookupSubTable[] lookupSubTables)
        {
            this.LookupType = lookupType;
            this.LookupFlags = lookupFlags;
            this.MarkFilteringSet = markFilteringSet;
            this.LookupSubTables = lookupSubTables;
        }

        public ushort LookupType { get; }

        public ushort LookupFlags { get; }

        public ushort MarkFilteringSet { get; }

        public LookupSubTable[] LookupSubTables { get; }

        public static LookupTable Load(
            BigEndianBinaryReader reader,
            long offset,
            Func<ushort, BigEndianBinaryReader, long, LookupSubTable> subTableLoader)
        {
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            // | Type     | Name                           | Description                                                                                                                         |
            // +==========+================================+=====================================================================================================================================+
            // | uint16   | lookupType                     | Different enumerations for GSUB and GPOS                                                                                            |
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            // | uint16   | lookupFlag                     | Lookup qualifiers                                                                                                                   |
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            // | uint16   | subTableCount                  | Number of subtables for this lookup                                                                                                 |
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            // | Offset16 | subtableOffsets[subTableCount] | Array of offsets to lookup subtables, from beginning of Lookup table                                                                |
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            // | uint16   | markFilteringSet               | Index (base 0) into GDEF mark glyph sets structure. This field is only present if the USE\_MARK\_FILTERING\_SET lookup flag is set. |
            // +----------+--------------------------------+-------------------------------------------------------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort lookupType = reader.ReadUInt16();
            ushort lookupFlags = reader.ReadUInt16();
            ushort subTableCount = reader.ReadUInt16();

            ushort[] subTableOffsets = reader.ReadUInt16Array(subTableCount);

            // The fifth bit indicates the presence of a MarkFilteringSet field in the Lookup table.
            ushort markFilteringSet = ((lookupFlags & 0x0010) != 0)
                ? reader.ReadUInt16()
                : (ushort)0;

            var lookupSubTables = new LookupSubTable[subTableCount];

            for (int i = 0; i < lookupSubTables.Length; i++)
            {
                lookupSubTables[i] = subTableLoader.Invoke(lookupType, reader, offset + subTableOffsets[i]);
            }

            return new LookupTable(lookupType, lookupFlags, markFilteringSet, lookupSubTables);
        }
    }

    internal abstract class LookupSubTable
    {
        // TODO: Flesh me out.
    }
}

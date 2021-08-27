// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables.General.Gsub
{
    /// <summary>
    /// Single substitution (SingleSubst) subtables tell a client to replace a single glyph with another glyph.
    /// The subtables can be either of two formats. Both formats require two distinct sets of glyph indices:
    /// one that defines input glyphs (specified in the Coverage table), and one that defines the output glyphs.
    /// Format 1 requires less space than Format 2, but it is less flexible.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-1-single-substitution-subtable"/>
    /// </summary>
    internal class SingleSubstitutionSubTable
    {
        private SingleSubstitutionSubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => SingleSubstitutionFormat1SubTable.Load(reader, offset),
                2 => SingleSubstitutionFormat2SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'substFormat' {substFormat}. Should be '1' or '2'."),
            };
        }
    }

    internal sealed class SingleSubstitutionFormat1SubTable : LookupSubTable
    {
        private SingleSubstitutionFormat1SubTable()
        {
        }

        public static SingleSubstitutionFormat1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SingleSubstFormat1
            // +----------+----------------+----------------------------------------------------------+
            // | Type     | Name           | Description                                              |
            // +==========+================+==========================================================+
            // | uint16   | substFormat    | Format identifier: format = 1                            |
            // +----------+----------------+----------------------------------------------------------+
            // | Offset16 | coverageOffset | Offset to Coverage table, from beginning of substitution |
            // |          |                | subtable                                                 |
            // +----------+----------------+----------------------------------------------------------+
            // | int16    | deltaGlyphID   | Add to original glyph ID to get substitute glyph ID      |
            // +----------+----------------+----------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort deltaGlyphID = reader.ReadUInt16();

            // TODO: Create Coverage Table type
            throw new NotImplementedException();
        }
    }

    internal sealed class SingleSubstitutionFormat2SubTable : LookupSubTable
    {
        private SingleSubstitutionFormat2SubTable()
        {
        }

        public static SingleSubstitutionFormat1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SingleSubstFormat2
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | Type     | Name                           | Description                                               |
            // +==========+================================+===========================================================+
            // | uint16   | substFormat                    | Format identifier: format = 2                             |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | Offset16 | coverageOffset                 | Offset to Coverage table, from beginning of substitution  |
            // |          |                                | subtable                                                  |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | uint16   | glyphCount                     | Number of glyph IDs in the substituteGlyphIDs array       |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | uint16   | substituteGlyphIDs[glyphCount] | Array of substitute glyph IDs — ordered by Coverage index |
            // +----------+--------------------------------+-----------------------------------------------------------+
            throw new NotImplementedException();
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tables.General
{
    internal sealed class LigatureCaretList
    {
        public LigatureGlyph[]? LigatureGlyphs { get; internal set; }

        public CoverageTable? CoverageTable { get; internal set; }

        public static LigatureCaretList Load(BigEndianBinaryReader reader, long offset)
        {
            // Ligature Caret list
            // Type      | Name                           | Description
            // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
            // Offset16  | coverageOffset                 | Offset to Coverage table - from beginning of LigCaretList table.
            // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16    | ligGlyphCount                  | Number of ligature glyphs.
            // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
            // Offset16  | ligGlyphOffsets[ligGlyphCount] | Array of offsets to LigGlyph tables, from beginning of LigCaretList table —in Coverage Index order.
            // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
            reader.Seek(offset, SeekOrigin.Begin);

            ushort coverageOffset = reader.ReadUInt16();
            ushort ligGlyphCount = reader.ReadUInt16();
            ushort[] ligGlyphOffsets = reader.ReadUInt16Array(ligGlyphCount);

            var ligatureCaretList = new LigatureCaretList()
            {
                CoverageTable = CoverageTable.Load(reader, offset + coverageOffset)
            };

            ligatureCaretList.LigatureGlyphs = new LigatureGlyph[ligGlyphCount];
            for (int i = 0; i < ligGlyphCount; ++i)
            {
                ligatureCaretList.LigatureGlyphs[i] = LigatureGlyph.Load(reader, ligGlyphOffsets[i]);
            }

            return ligatureCaretList;
        }
    }
}

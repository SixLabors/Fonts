// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
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

            using Buffer<ushort> ligGlyphOffsetsBuffer = new(ligGlyphCount);
            Span<ushort> ligGlyphOffsets = ligGlyphOffsetsBuffer.GetSpan();
            reader.ReadUInt16Array(ligGlyphOffsets);

            var ligatureCaretList = new LigatureCaretList()
            {
                CoverageTable = CoverageTable.Load(reader, offset + coverageOffset)
            };

            ligatureCaretList.LigatureGlyphs = new LigatureGlyph[ligGlyphCount];
            for (int i = 0; i < ligatureCaretList.LigatureGlyphs.Length; i++)
            {
                ligatureCaretList.LigatureGlyphs[i] = LigatureGlyph.Load(reader, ligGlyphOffsets[i]);
            }

            return ligatureCaretList;
        }
    }
}

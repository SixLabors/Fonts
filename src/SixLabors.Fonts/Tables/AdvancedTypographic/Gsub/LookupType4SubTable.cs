// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// A Ligature Substitution (LigatureSubst) subtable identifies ligature substitutions where a single glyph replaces multiple glyphs.
    /// One LigatureSubst subtable can specify any number of ligature substitutions.
    /// The subtable has one format: LigatureSubstFormat1.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-4-ligature-substitution-subtable"/>
    /// </summary>
    internal static class LookupType4SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType4Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'substFormat' {substFormat}. Should be '1'."),
            };
        }
    }

    internal sealed class LookupType4Format1SubTable : LookupSubTable
    {
        private readonly LigatureSetTable[] ligatureSetTables;
        private readonly CoverageTable coverageTable;

        private LookupType4Format1SubTable(LigatureSetTable[] ligatureSetTables, CoverageTable coverageTable)
        {
            this.ligatureSetTables = ligatureSetTables;
            this.coverageTable = coverageTable;
        }

        public static LookupType4Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // Ligature Substitution Format 1
            // +----------+--------------------------------------+--------------------------------------------------------------------+
            // | Type     | Name                                 | Description                                                        |
            // +==========+======================================+====================================================================+
            // | uint16   | substFormat                          | Format identifier: format = 1                                      |
            // +----------+--------------------------------------+--------------------------------------------------------------------+
            // | Offset16 | coverageOffset                       | Offset to Coverage table, from beginning of substitution           |
            // |          |                                      | subtable                                                           |
            // +----------+--------------------------------------+--------------------------------------------------------------------+
            // | uint16   | ligatureSetCount                     | Number of LigatureSet tables                                       |
            // +----------+--------------------------------------+--------------------------------------------------------------------+
            // | Offset16 | ligatureSetOffsets[ligatureSetCount] | Array of offsets to LigatureSet tables. Offsets are from beginning |
            // |          |                                      | of substitution subtable, ordered by Coverage index                |
            // +----------+--------------------------------------+--------------------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort ligatureSetCount = reader.ReadUInt16();
            ushort[] ligatureSetOffsets = reader.ReadUInt16Array(ligatureSetCount);

            var ligatureSetTables = new LigatureSetTable[ligatureSetCount];
            for (int i = 0; i < ligatureSetTables.Length; i++)
            {
                // LigatureSet Table
                // +----------+--------------------------------+--------------------------------------------------------------------+
                // | Type     | Name                           | Description                                                        |
                // +==========+================================+====================================================================+
                // | uint16   | ligatureCount                  | Number of Ligature tables                                          |
                // +----------+--------------------------------+--------------------------------------------------------------------+
                // | Offset16 | ligatureOffsets[LigatureCount] | Array of offsets to Ligature tables. Offsets are from beginning of |
                // |          |                                | LigatureSet table, ordered by preference.                          |
                // +----------+--------------------------------+--------------------------------------------------------------------+
                long ligatureSetOffset = offset + ligatureSetOffsets[i];
                reader.Seek(ligatureSetOffset, SeekOrigin.Begin);
                ushort ligatureCount = reader.ReadUInt16();
                ushort[] ligatureOffsets = reader.ReadUInt16Array(ligatureCount);
                var ligatureTables = new LigatureTable[ligatureCount];

                // Ligature Table
                // +--------+---------------------------------------+------------------------------------------------------+
                // | Type   | Name                                  | Description                                          |
                // +========+=======================================+======================================================+
                // | uint16 | ligatureGlyph                         | glyph ID of ligature to substitute                   |
                // +--------+---------------------------------------+------------------------------------------------------+
                // | uint16 | componentCount                        | Number of components in the ligature                 |
                // +--------+---------------------------------------+------------------------------------------------------+
                // | uint16 | componentGlyphIDs[componentCount - 1] | Array of component glyph IDs — start with the second |
                // |        |                                       | component, ordered in writing direction              |
                // +--------+---------------------------------------+------------------------------------------------------+
                for (int j = 0; j < ligatureTables.Length; j++)
                {
                    reader.Seek(ligatureSetOffset + ligatureOffsets[j], SeekOrigin.Begin);
                    ushort ligatureGlyph = reader.ReadUInt16();
                    ushort componentCount = reader.ReadUInt16();
                    ushort[] componentGlyphIds = reader.ReadUInt16Array(componentCount - 1);
                    ligatureTables[j] = new LigatureTable(ligatureGlyph, componentGlyphIds);
                }

                ligatureSetTables[i] = new LigatureSetTable(ligatureTables);
            }

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType4Format1SubTable(ligatureSetTables, coverageTable);
        }

        public override bool TrySubstitution(
            IFontShaper shaper,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            ushort index,
            int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf(glyphId);
            if (offset <= -1)
            {
                return false;
            }

            LigatureSetTable ligatureSetTable = this.ligatureSetTables[offset];
            for (int i = 0; i < ligatureSetTable.Ligatures.Length; i++)
            {
                LigatureTable ligatureTable = ligatureSetTable.Ligatures[i];
                int remaining = count - 1;
                int compLength = ligatureTable.ComponentGlyphs.Length;
                if (compLength > remaining)
                {
                    continue;
                }

                bool allMatched = AdvancedTypographicUtils.MatchInputSequence(collection, feature, index, ligatureTable.ComponentGlyphs);
                if (allMatched)
                {
                    GlyphShapingData data = collection.GetGlyphShapingData(index);
                    shaper.TryGetGlyphClass(glyphId, out GlyphClassDef? glyphClass);
                    bool isMarkLigature = glyphClass == GlyphClassDef.MarkGlyph || CodePoint.IsMark(data.CodePoint);

                    for (int j = 0; j < ligatureTable.ComponentGlyphs.Length && isMarkLigature; j++)
                    {
                        // TODO: FontKit does the folowing
                        // isMarkLigature = this.glyphs[matched[i]].isMark;
                        // But isn't that just checking the same collection since the match should be the same?
                        shaper.TryGetGlyphClass(ligatureTable.ComponentGlyphs[i], out glyphClass);
                        isMarkLigature = glyphClass == GlyphClassDef.MarkGlyph;
                    }

                    int ligatureId = isMarkLigature ? 0 : collection.LigatureId++;
                    int lastLigatureId = data.LigatureId;

                    // TODO:
                    // Set ligatureId and ligatureComponent on glyphs that were skipped in the matched sequence.
                    // This allows GPOS to attach marks to the correct ligature components.
                    collection.Replace(index, compLength + 1, ligatureTable.GlyphId, ligatureId);
                    return true;
                }
            }

            return false;
        }

        public readonly struct LigatureSetTable
        {
            public LigatureSetTable(LigatureTable[] ligatures)
                => this.Ligatures = ligatures;

            public LigatureTable[] Ligatures { get; }
        }

        public readonly struct LigatureTable
        {
            public LigatureTable(ushort glyphId, ushort[] componentGlyphs)
            {
                this.GlyphId = glyphId;
                this.ComponentGlyphs = componentGlyphs;
            }

            public ushort GlyphId { get; }

            public ushort[] ComponentGlyphs { get; }
        }
    }
}

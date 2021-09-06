// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// A Contextual Substitution subtable describes glyph substitutions in context that replace one
    /// or more glyphs within a certain pattern of glyphs.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable"/>
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/otspec150/gsub#CS"/>
    /// </summary>
    internal sealed class LookupType5SubTable
    {
        private LookupType5SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType5Format1SubTable.Load(reader, offset),
                2 => LookupType5Format2SubTable.Load(reader, offset),
                3 => LookupType5Format3SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'substFormat' {substFormat}. Should be '1', '2', or '3'."),
            };
        }
    }

    internal sealed class LookupType5Format1SubTable : LookupSubTable
    {
        private readonly SequenceRuleSetTable[] seqRuleSetTables;
        private readonly CoverageTable coverageTable;

        private LookupType5Format1SubTable(SequenceRuleSetTable[] seqbRuleSetTables, CoverageTable coverageTable)
        {
            this.seqRuleSetTables = seqbRuleSetTables;
            this.coverageTable = coverageTable;
        }

        public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SequenceContextFormat1
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Type     | Name                               | Description                                                   |
            // +==========+====================================+===============================================================+
            // | uint16   | format                             | Format identifier: format = 1                                 |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Offset16 | coverageOffset                     | Offset to Coverage table, from beginning of                   |
            // |          |                                    | SequenceContextFormat1 table                                  |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | uint16   | seqRuleSetCount                    | Number of SequenceRuleSet tables                              |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Offset16 | seqRuleSetOffsets[seqRuleSetCount] | Array of offsets to SequenceRuleSet tables, from beginning of |
            // |          |                                    | SequenceContextFormat1 table (offsets may be NULL)            |
            // +----------+------------------------------------+---------------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort seqRuleSetCount = reader.ReadUInt16();
            ushort[] seqRuleSetOffsets = reader.ReadUInt16Array(seqRuleSetCount);
            var seqRuleSets = new SequenceRuleSetTable[seqRuleSetCount];

            for (int i = 0; i < seqRuleSets.Length; i++)
            {
                seqRuleSets[i] = SequenceRuleSetTable.Load(reader, offset + seqRuleSetOffsets[i]);
            }

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
            return new LookupType5Format1SubTable(seqRuleSets, coverageTable);
        }

        public override bool TrySubstition(GSubTable gSubTable, IGlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            if (this.coverageTable.CoverageIndexOf((ushort)glyphId) > -1)
            {
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#example-7-contextual-substitution-format-1
                return false;
            }

            return false;
        }

        public sealed class SequenceRuleSetTable
        {
            private SequenceRuleSetTable(SequenceRuleTable[] subRules)
                => this.SubRules = subRules;

            public SequenceRuleTable[] SubRules { get; }

            public static SequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
            {
                // SequenceRuleSet
                // +----------+------------------------------+----------------------------------------------------------------+
                // | Type     | Name                         | Description                                                    |
                // +==========+==============================+================================================================+
                // | uint16   | seqRuleCount                 | Number of SequenceRule tables                                  |
                // +----------+------------------------------+----------------------------------------------------------------+
                // | Offset16 | seqRuleOffsets[posRuleCount] | Array of offsets to SequenceRule tables, from beginning of the |
                // |          |                              | SequenceRuleSet table                                          |
                // +----------+------------------------------+----------------------------------------------------------------+
                reader.Seek(offset, SeekOrigin.Begin);
                ushort seqRuleCount = reader.ReadUInt16();
                ushort[] seqRuleOffsets = reader.ReadUInt16Array(seqRuleCount);

                var subRules = new SequenceRuleTable[seqRuleCount];
                for (int i = 0; i < subRules.Length; i++)
                {
                    subRules[i] = SequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
                }

                return new SequenceRuleSetTable(subRules);
            }

            public sealed class SequenceRuleTable
            {
                private SequenceRuleTable(ushort[] inputSequence, SequenceLookupRecord[] seqLookupRecords)
                {
                    this.InputSequence = inputSequence;
                    this.SequenceLookupRecords = seqLookupRecords;
                }

                public ushort[] InputSequence { get; }

                public SequenceLookupRecord[] SequenceLookupRecords { get; }

                public static SequenceRuleTable Load(BigEndianBinaryReader reader, long offset)
                {
                    // +----------------------+----------------------------------+---------------------------------------------------------+
                    // | Type                 | Name                             | Description                                             |
                    // +======================+==================================+=========================================================+
                    // | uint16               | glyphCount                       | Number of glyphs in the input glyph sequence            |
                    // +----------------------+----------------------------------+---------------------------------------------------------+
                    // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords                         |
                    // +----------------------+----------------------------------+---------------------------------------------------------+
                    // | uint16               | inputSequence[glyphCount - 1]    | Array of input glyph IDs—starting with the second glyph |
                    // +----------------------+----------------------------------+---------------------------------------------------------+
                    // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of Sequence lookup records                        |
                    // +----------------------+----------------------------------+---------------------------------------------------------+
                    reader.Seek(offset, SeekOrigin.Begin);
                    ushort glyphCount = reader.ReadUInt16();
                    ushort seqLookupCount = reader.ReadUInt16();
                    ushort[] inputSequence = reader.ReadUInt16Array(glyphCount - 1);
                    SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

                    return new SequenceRuleTable(inputSequence, seqLookupRecords);
                }
            }
        }
    }

    internal sealed class LookupType5Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ushort[] substituteGlyphs;

        private LookupType5Format2SubTable(ushort[] substituteGlyphs, CoverageTable coverageTable)
        {
            this.substituteGlyphs = substituteGlyphs;
            this.coverageTable = coverageTable;
        }

        public static LookupType5Format2SubTable Load(BigEndianBinaryReader reader, long offset)
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
            ushort coverageOffset = reader.ReadOffset16();
            ushort glyphCount = reader.ReadUInt16();
            ushort[] substituteGlyphIds = reader.ReadUInt16Array(glyphCount);
            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType5Format2SubTable(substituteGlyphIds, coverageTable);
        }

        public override bool TrySubstition(GSubTable gSubTable, IGlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf((ushort)glyphId);

            if (offset > -1)
            {
                collection.Replace(index, this.substituteGlyphs[offset]);
                return true;
            }

            return false;
        }
    }

    internal sealed class LookupType5Format3SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ushort[] substituteGlyphs;

        private LookupType5Format3SubTable(ushort[] substituteGlyphs, CoverageTable coverageTable)
        {
            this.substituteGlyphs = substituteGlyphs;
            this.coverageTable = coverageTable;
        }

        public static LookupType5Format3SubTable Load(BigEndianBinaryReader reader, long offset)
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
            ushort coverageOffset = reader.ReadOffset16();
            ushort glyphCount = reader.ReadUInt16();
            ushort[] substituteGlyphIds = reader.ReadUInt16Array(glyphCount);
            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType5Format3SubTable(substituteGlyphIds, coverageTable);
        }

        public override bool TrySubstition(GSubTable gSubTable, IGlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf((ushort)glyphId);

            if (offset > -1)
            {
                collection.Replace(index, this.substituteGlyphs[offset]);
                return true;
            }

            return false;
        }
    }
}

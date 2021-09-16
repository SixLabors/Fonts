// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// A Contextual Substitution subtable describes glyph substitutions in context that replace one
    /// or more glyphs within a certain pattern of glyphs.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable"/>
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

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf((ushort)glyphId);
            if (offset > -1)
            {
                // TODO: Check this.
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#example-7-contextual-substitution-format-1
                SequenceRuleSetTable ruleSetTable = this.seqRuleSetTables[offset];
                foreach (SequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
                {
                    int remaining = count - 1;
                    int seqLength = ruleTable.InputSequence.Length;
                    if (seqLength > remaining)
                    {
                        continue;
                    }

                    bool allMatched = GSubUtils.MatchInputSequence(collection, index, ruleTable.InputSequence);
                    if (!allMatched)
                    {
                        continue;
                    }

                    // It's a match. Perform substitutions and return true if anything changed.
                    bool hasChanged = false;
                    foreach (SequenceLookupRecord lookupRecord in ruleTable.SequenceLookupRecords)
                    {
                        ushort sequenceIndex = lookupRecord.SequenceIndex;
                        ushort lookupIndex = lookupRecord.LookupListIndex;

                        LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                        if (lookup.TrySubstitution(table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                        {
                            hasChanged = true;
                        }
                    }

                    return hasChanged;
                }
            }

            return false;
        }
    }

    internal sealed class LookupType5Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ClassDefinitionTable classDefinitionTable;
        private readonly ClassSequenceRuleSetTable[] sequenceRuleSetTables;

        private LookupType5Format2SubTable(
            ClassSequenceRuleSetTable[] sequenceRuleSetTables,
            ClassDefinitionTable classDefinitionTable,
            CoverageTable coverageTable)
        {
            this.sequenceRuleSetTables = sequenceRuleSetTables;
            this.classDefinitionTable = classDefinitionTable;
            this.coverageTable = coverageTable;
        }

        public static LookupType5Format2SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SequenceContextFormat2
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            // | Type     | Name                                         | Description                                                        |
            // +==========+==============================================+====================================================================+
            // | uint16   | format                                       | Format identifier: format = 2                                      |
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            // | Offset16 | coverageOffset                               | Offset to Coverage table, from beginning of                        |
            // |          |                                              | SequenceContextFormat2 table                                       |
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            // | Offset16 | classDefOffset                               | Offset to ClassDef table, from beginning of                        |
            // |          |                                              | SequenceContextFormat2 table                                       |
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            // | uint16   | classSeqRuleSetCount                         | Number of ClassSequenceRuleSet tables                              |
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            // | Offset16 | classSeqRuleSetOffsets[classSeqRuleSetCount] | Array of offsets to ClassSequenceRuleSet tables, from beginning of |
            // |          |                                              | SequenceContextFormat2 table (may be NULL)                         |
            // +----------+----------------------------------------------+--------------------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort classDefOffset = reader.ReadOffset16();
            ushort classSeqRuleSetCount = reader.ReadUInt16();
            ushort[] classSeqRuleSetOffsets = reader.ReadUInt16Array(classSeqRuleSetCount);

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
            var classDefTable = ClassDefinitionTable.Load(reader, offset + classDefOffset);

            var classSeqRuleSets = new ClassSequenceRuleSetTable[classSeqRuleSetCount];
            for (int i = 0; i < classSeqRuleSets.Length; i++)
            {
                classSeqRuleSets[i] = ClassSequenceRuleSetTable.Load(reader, offset + classSeqRuleSetOffsets[i]);
            }

            return new LookupType5Format2SubTable(classSeqRuleSets, classDefTable, coverageTable);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            if (this.coverageTable.CoverageIndexOf((ushort)glyphId) > -1)
            {
                // TODO: Check this.
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#52-context-substitution-format-2-class-based-glyph-contexts
                int offset = this.classDefinitionTable.ClassIndexOf((ushort)glyphId);
                if (offset < 0)
                {
                    return false;
                }

                ClassSequenceRuleSetTable ruleSetTable = this.sequenceRuleSetTables[offset];
                foreach (ClassSequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
                {
                    int remaining = count - 1;
                    int seqLength = ruleTable.InputSequence.Length;
                    if (seqLength > remaining)
                    {
                        continue;
                    }

                    bool allMatched = GSubUtils.MatchInputSequence(collection, index, ruleTable.InputSequence);
                    if (!allMatched)
                    {
                        continue;
                    }

                    // It's a match. Perform substitutions and return true if anything changed.
                    bool hasChanged = false;
                    foreach (SequenceLookupRecord lookupRecord in ruleTable.SequenceLookupRecords)
                    {
                        ushort sequenceIndex = lookupRecord.SequenceIndex;
                        ushort lookupIndex = lookupRecord.LookupListIndex;

                        LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                        if (lookup.TrySubstitution(table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                        {
                            hasChanged = true;
                        }
                    }

                    return hasChanged;
                }
            }

            return false;
        }
    }

    internal sealed class LookupType5Format3SubTable : LookupSubTable
    {
        private readonly CoverageTable[] coverageTables;
        private readonly SequenceLookupRecord[] sequenceLookupRecords;

        private LookupType5Format3SubTable(SequenceLookupRecord[] sequenceLookupRecords, CoverageTable[] coverageTables)
        {
            this.sequenceLookupRecords = sequenceLookupRecords;
            this.coverageTables = coverageTables;
        }

        public static LookupType5Format3SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SequenceContextFormat3
            // +----------------------+----------------------------------+-------------------------------------------+
            // | Type                 | Name                             | Description                               |
            // +======================+==================================+===========================================+
            // | uint16               | format                           | Format identifier: format = 3             |
            // +----------------------+----------------------------------+-------------------------------------------+
            // | uint16               | glyphCount                       | Number of glyphs in the input sequence    |
            // +----------------------+----------------------------------+-------------------------------------------+
            // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords           |
            // +----------------------+----------------------------------+-------------------------------------------+
            // | Offset16             | coverageOffsets[glyphCount]      | Array of offsets to Coverage tables, from |
            // |                      |                                  | beginning of SequenceContextFormat3       |
            // |                      |                                  | subtable                                  |
            // +----------------------+----------------------------------+-------------------------------------------+
            // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of SequenceLookupRecords            |
            // +----------------------+----------------------------------+-------------------------------------------+
            ushort glyphCount = reader.ReadUInt16();
            ushort seqLookupCount = reader.ReadUInt16();
            ushort[] coverageOffsets = reader.ReadUInt16Array(glyphCount);
            SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

            var coverageTables = new CoverageTable[glyphCount];
            for (int i = 0; i < coverageTables.Length; i++)
            {
                coverageTables[i] = CoverageTable.Load(reader, offset + coverageOffsets[i]);
            }

            return new LookupType5Format3SubTable(seqLookupRecords, coverageTables);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            // TODO: Check this
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#53-context-substitution-format-3-coverage-based-glyph-contexts
            foreach (CoverageTable coverageTable in this.coverageTables)
            {
                int offset = coverageTable.CoverageIndexOf((ushort)glyphId);
                if (offset <= -1)
                {
                    continue;
                }

                // It's a match. Perform substitutions and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in this.sequenceLookupRecords)
                {
                    ushort sequenceIndex = lookupRecord.SequenceIndex;
                    ushort lookupIndex = lookupRecord.LookupListIndex;

                    LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                    if (lookup.TrySubstitution(table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                    {
                        hasChanged = true;
                    }
                }

                return hasChanged;
            }

            return false;
        }
    }
}

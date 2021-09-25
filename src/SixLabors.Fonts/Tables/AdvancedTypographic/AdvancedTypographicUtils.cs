// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal static class AdvancedTypographicUtils
    {
        internal static bool MatchInputSequence(IGlyphShapingCollection collection, Tag feature, ushort index, ushort[] inputSequence)
        {
            int startIdx = index + 1;
            int i = 0;
            while (i < inputSequence.Length)
            {
                int collectionIdx = startIdx + i;
                if (collectionIdx == collection.Count)
                {
                    return false;
                }

                GlyphShapingData data = collection.GetGlyphShapingData(collectionIdx);
                if (!data.Features.Contains(feature))
                {
                    return false;
                }

                if (data.GlyphIds[0] != inputSequence[i])
                {
                    return false;
                }

                i++;
            }

            return i == inputSequence.Length;
        }

        internal static bool MatchSequence(IGlyphShapingCollection collection, int glyphSequenceIndex, ushort[] sequenceToMatch)
        {
            int sequenceToMatchIdx = 0;
            int glyphCollectionIdx = glyphSequenceIndex + 1;

            while (sequenceToMatchIdx < sequenceToMatch.Length)
            {
                if (glyphCollectionIdx >= collection.Count)
                {
                    return false;
                }

                ReadOnlySpan<ushort> glyphIds = collection[glyphCollectionIdx++];
                ushort glyphId = glyphIds[0];
                if (glyphId != sequenceToMatch[sequenceToMatchIdx++])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool MatchClassSequence(
            IGlyphShapingCollection collection,
            int glyphSequenceIndex,
            ushort[] sequenceToMatch,
            ClassDefinitionTable classDefinitionTable)
        {
            int sequenceToMatchIdx = 0;
            int glyphCollectionIdx = glyphSequenceIndex + 1;

            while (sequenceToMatchIdx < sequenceToMatch.Length)
            {
                if (glyphCollectionIdx >= collection.Count)
                {
                    return false;
                }

                ReadOnlySpan<ushort> glyphIds = collection[glyphCollectionIdx++];
                if (!MatchClass(sequenceToMatchIdx++, sequenceToMatch, classDefinitionTable, glyphIds))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool ApplyChainedSequenceRule(IGlyphShapingCollection collection, Tag feature, ushort index, ChainedSequenceRuleTable rule)
        {
            if (rule.BacktrackSequence.Length > 0
                && !MatchSequence(collection, -rule.BacktrackSequence.Length, rule.BacktrackSequence))
            {
                return false;
            }

            if (rule.LookaheadSequence.Length > 0
                && !MatchSequence(collection, 1 + rule.InputSequence.Length, rule.LookaheadSequence))
            {
                return false;
            }

            if (rule.InputSequence.Length > 0
                && !MatchInputSequence(collection, feature, index, rule.InputSequence))
            {
                return false;
            }

            return true;
        }

        internal static bool ApplyChainedClassSequenceRule(
            IGlyphShapingCollection collection,
            ushort index,
            ChainedClassSequenceRuleTable rule,
            ClassDefinitionTable inputClassDefinitionTable,
            ClassDefinitionTable backtrackClassDefinitionTable,
            ClassDefinitionTable lookaheadClassDefinitionTable)
        {
            if (rule.BacktrackSequence.Length > 0
                && !MatchClassSequence(collection, -rule.BacktrackSequence.Length, rule.BacktrackSequence, backtrackClassDefinitionTable))
            {
                return false;
            }

            if (rule.InputSequence.Length > 0 &&
                !MatchClassSequence(collection, index, rule.InputSequence, inputClassDefinitionTable))
            {
                return false;
            }

            if (rule.LookaheadSequence.Length > 0
                && !MatchClassSequence(collection, 1 + rule.InputSequence.Length, rule.LookaheadSequence, lookaheadClassDefinitionTable))
            {
                return false;
            }

            return true;
        }

        internal static bool CheckAllCoverages(IGlyphShapingCollection collection, ushort index, int count, CoverageTable[] input, CoverageTable[] backtrack, CoverageTable[] lookahead)
        {
            int inputLength = input.Length;

            // Check that there are enough context glyphs.
            if (index < backtrack.Length
                || inputLength + lookahead.Length > count)
            {
                return false;
            }

            // Check all coverages: if any of them does not match, abort update.
            if (!CheckCoverage(collection, input, index))
            {
                return false;
            }

            if (!CheckBacktrackCoverage(collection, backtrack, index - 1))
            {
                return false;
            }

            if (!CheckCoverage(collection, lookahead, index + inputLength))
            {
                return false;
            }

            return true;
        }

        private static bool CheckCoverage(IGlyphShapingCollection collection, CoverageTable[] coverageTable, int offset)
        {
            for (int i = 0; i < coverageTable.Length; ++i)
            {
                ushort id = collection[offset + i][0];
                if (id == 0 || coverageTable[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CheckBacktrackCoverage(IGlyphShapingCollection collection, CoverageTable[] coverageTable, int offset)
        {
            for (int i = 0; i < coverageTable.Length; ++i)
            {
                ushort id = collection[offset - i][0];
                if (id == 0 || coverageTable[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchClass(int idx, ushort[] sequence, ClassDefinitionTable classDefinitionTable, ReadOnlySpan<ushort> glyphIds)
        {
            ushort glyphId = glyphIds[0];
            int glyphIdClass = classDefinitionTable.ClassIndexOf(glyphId);
            ushort sequenceEntryClassId = sequence[idx];
            return glyphIdClass == sequenceEntryClassId;
        }
    }
}

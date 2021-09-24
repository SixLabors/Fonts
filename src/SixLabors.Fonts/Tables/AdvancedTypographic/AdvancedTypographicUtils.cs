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

        internal static bool CheckCoverage(IGlyphShapingCollection collection, CoverageTable[] coverageTable, int offset)
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

        internal static bool CheckBacktrackCoverage(IGlyphShapingCollection collection, CoverageTable[] coverageTable, int offset)
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

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal static class AdvancedTypographicUtils
    {
        internal static bool MatchInputSequence(IGlyphShapingCollection collection, ushort index, ushort[] inputSequence)
        {
            bool allMatched = true;
            int startIdx = index + 1;
            for (int i = 0; i < inputSequence.Length; i++)
            {
                int collectionIdx = startIdx + i;
                if (collectionIdx < collection.Count && collection[collectionIdx][0] != inputSequence[i])
                {
                    allMatched = false;
                    break;
                }
            }

            return allMatched;
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

        private static bool MatchClass(int idx, ushort[] sequence, ClassDefinitionTable classDefinitionTable, ReadOnlySpan<ushort> glyphIds)
        {
            ushort glyphId = glyphIds[0];
            int glyphIdClass = classDefinitionTable.ClassIndexOf(glyphId);
            ushort sequenceEntryClassId = sequence[idx];
            return glyphIdClass == sequenceEntryClassId;
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal static class AdvancedTypographicUtils
    {
        internal static bool MatchInputSequence(IGlyphCollection collection, ushort index, ushort[] inputSequence)
        {
            bool allMatched = true;
            int startIdx = index + 1;
            for (int i = 0; i < inputSequence.Length; i++)
            {
                int collectionIdx = startIdx + i;
                if (collectionIdx < collection.Count && collection.GetGlyphIds(collectionIdx)[0] != inputSequence[i])
                {
                    allMatched = false;
                    break;
                }
            }

            return allMatched;
        }

        internal static bool MatchSequence(IGlyphCollection collection, int glyphIndex, int sequenceIndex, ushort[] sequence)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < sequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos++, out _, out _, out _, out ReadOnlySpan<int> glyphIds);
                int glyphId = glyphIds[0];
                if (glyphId != sequence[idx++])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool MatchClassSequence(
            IGlyphCollection collection,
            int glyphIndex,
            int sequenceIndex,
            ushort[] sequence,
            ClassDefinitionTable classDefinitionTable)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < sequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos++, out _, out _, out _, out ReadOnlySpan<int> glyphIds);
                if (!MatchClass(idx++, sequence, classDefinitionTable, glyphIds))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchClass(int idx, ushort[] sequence, ClassDefinitionTable classDefinitionTable, ReadOnlySpan<int> glyphIds)
        {
            int glyphId = glyphIds[0];
            int glyphIdClass = classDefinitionTable.ClassIndexOf((ushort)glyphId);
            ushort sequenceEntry = sequence[idx];
            int sequenceEntryClassId = classDefinitionTable.ClassIndexOf(sequenceEntry);
            if (glyphIdClass != sequenceEntryClassId)
            {
                return false;
            }

            return true;
        }
    }
}
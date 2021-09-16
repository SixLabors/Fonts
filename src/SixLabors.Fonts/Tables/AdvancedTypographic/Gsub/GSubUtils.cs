// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    internal class GSubUtils
    {
        internal static bool MatchInputSequence(GlyphSubstitutionCollection collection, ushort index, ushort[] inputSequence)
        {
            bool allMatched = true;
            int startIdx = index + 1;
            for (int i = 0; i < inputSequence.Length; i++)
            {
                if (collection[startIdx + i][0] != inputSequence[i])
                {
                    allMatched = false;
                    break;
                }
            }

            return allMatched;
        }

        internal static bool MatchBacktrackClassIdSequence(
            GlyphSubstitutionCollection collection,
            int glyphIndex,
            int sequenceIndex,
            ushort[] backtrackSequence,
            ClassDefinitionTable backtrackClassDefinitionTable)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < backtrackSequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos, out _, out _, out System.Collections.Generic.IEnumerable<int>? glyphIds);
                int glyphId = glyphIds.First();
                int glyphIdClass = backtrackClassDefinitionTable.ClassIndexOf((ushort)glyphId);
                ushort backtrackEntry = backtrackSequence[idx];
                int backTrackClassId = backtrackClassDefinitionTable.ClassIndexOf(backtrackEntry);
                if (glyphIdClass != backTrackClassId)
                {
                    return false;
                }

                pos++;
                idx++;
            }

            return true;
        }

        internal static bool MatchLookAheadClassIdSequence(
            GlyphSubstitutionCollection collection,
            int glyphIndex,
            int sequenceIndex,
            ushort[] lookAheadSequence,
            ClassDefinitionTable lookAheadClassDefinitionTable)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < lookAheadSequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos, out _, out _, out System.Collections.Generic.IEnumerable<int>? glyphIds);
                int glyphId = glyphIds.First();
                int glyphIdClass = lookAheadClassDefinitionTable.ClassIndexOf((ushort)glyphId);
                ushort lookAheadEntry = lookAheadSequence[idx];
                int lookAheadClassId = lookAheadClassDefinitionTable.ClassIndexOf(lookAheadEntry);
                if (glyphIdClass != lookAheadClassId)
                {
                    return false;
                }

                pos++;
                idx++;
            }

            return true;
        }
    }
}

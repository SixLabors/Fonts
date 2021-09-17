// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;

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

        internal static bool MatchSequence(
            GlyphSubstitutionCollection collection,
            int glyphIndex,
            int sequenceIndex,
            ushort[] sequence)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < sequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos, out _, out _, out System.Collections.Generic.IEnumerable<int>? glyphIds);
                int glyphId = glyphIds.First();
                if (glyphId != sequence[idx])
                {
                    return false;
                }

                pos++;
                idx++;
            }

            return true;
        }

        internal static bool MatchClassSequence(
            GlyphSubstitutionCollection collection,
            int glyphIndex,
            int sequenceIndex,
            ushort[] sequence,
            ClassDefinitionTable classDefinitionTable)
        {
            int pos = glyphIndex - sequenceIndex;
            int idx = 0;

            while (idx < sequence.Length)
            {
                collection.GetCodePointAndGlyphIds(pos, out _, out _, out System.Collections.Generic.IEnumerable<int>? glyphIds);
                int glyphId = glyphIds.First();
                int glyphIdClass = classDefinitionTable.ClassIndexOf((ushort)glyphId);
                ushort sequenceEntry = sequence[idx];
                int sequenceEntryClassId = classDefinitionTable.ClassIndexOf(sequenceEntry);
                if (glyphIdClass != sequenceEntryClassId)
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

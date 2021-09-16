// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    internal class GSubUtils
    {
        internal static bool MatchInputSequence(GlyphSubstitutionCollection collection, ushort index, ushort[] inputSequence)
        {
            bool allMatched = true;
            int temp = index + 1;
            for (int j = 0; j < inputSequence.Length; j++)
            {
                if (collection[temp + j][0] != inputSequence[j])
                {
                    allMatched = false;
                    break;
                }
            }

            return allMatched;
        }
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Grapheme Cluster Algorithm. UAX:29
    /// <see href="https://www.unicode.org/reports/tr29/tr29-37.html"/>
    /// </summary>
    internal static class GraphemeClusterAlgorithm
    {
        // https://www.unicode.org/Public/13.0.0/ucd/auxiliary/GraphemeBreakTest.html
        private static readonly byte[][] PairTable = new byte[][]
        {
            // .         Any   CR   LF   Control   Extend   Regional_Indicator   Prepend   SpacingMark   L   V   T   LV   LVT   ExtPict   ZWJ   SOT   EOT   ExtPictZwg
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // Any
            new byte[] { 1,    1,   1,   1,        1,       1,                   1,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // CR
            new byte[] { 1,    0,   1,   1,        1,       1,                   1,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // LF
            new byte[] { 1,    1,   1,   1,        1,       1,                   1,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // Control
            new byte[] { 0,    1,   1,   1,        0,       0,                   0,        0,            0,  0,  0,  0,   0,    0,        0,    1,    1,    0,     },    // Extend
            new byte[] { 1,    1,   1,   1,        1,       0,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // Regional_Indicator
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // Prepend
            new byte[] { 0,    1,   1,   1,        0,       0,                   0,        0,            0,  0,  0,  0,   0,    0,        0,    1,    1,    0,     },    // SpacingMark
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            0,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // L
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            0,  0,  1,  0,   1,    1,        1,    1,    1,    1,     },    // V
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  0,  0,  0,   0,    1,        1,    1,    1,    1,     },    // T
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            0,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // LV
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            0,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // LVT
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    0,     },    // ExtPict
            new byte[] { 0,    1,   1,   1,        0,       0,                   0,        0,            0,  0,  0,  0,   0,    0,        0,    1,    1,    0,     },    // ZWJ
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // SOT
            new byte[] { 1,    1,   1,   1,        1,       1,                   1,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // EOT
            new byte[] { 1,    1,   1,   1,        1,       1,                   0,        1,            1,  1,  1,  1,   1,    1,        1,    1,    1,    1,     },    // ExtPictZwg
        };

        /// <summary>
        /// Given a sequence of code points, return its grapheme cluster boundaries.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <returns>An enumerable of grapheme cluster boundaries.</returns>
        public static IEnumerable<Grapheme> GetGraphemes(string text)
        {
            int codepoints = CodePoint.GetCodePointCount(text);
            int charPosition = 0;
            int i = 0;

            while (i < codepoints)
            {
                if (IsBoundary(text, charPosition, out int charsConsumed))
                {
                    int start = charPosition;
                    while (!IsBoundary(text, start + charsConsumed, out int count))
                    {
                        charsConsumed += count;
                    }

                    ReadOnlyMemory<char> slice = text.AsMemory().Slice(start, charsConsumed);
                    yield return new Grapheme(CodePoint.ReadAt(text, start), start, slice);
                }

                i++;
                charPosition += charsConsumed;
            }
        }

        /// <summary>
        /// Given a sequence of code points, return its grapheme cluster boundaries.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <returns>An enumerable of grapheme cluster boundaries.</returns>
        public static IEnumerable<int> GetBoundaries(string text)
        {
            int codepoints = CodePoint.GetCodePointCount(text);
            int charPosition = 0;
            int i = 0;

            // Count past the last codepoint index to ensure SOT and EOT are counted.
            // See GraphemeBreakTests.txt.
            // ÷ 0020 ÷ 0020 ÷  #  ÷ [0.2] SPACE (Other) ÷ [999.0] SPACE (Other) ÷ [0.3]
            while (i <= codepoints)
            {
                if (IsBoundary(text, charPosition, out int charsConsumed))
                {
                    // TODO: Return Grapheme struct containing further info.
                    yield return i;
                }

                i++;
                charPosition += charsConsumed;
            }
        }

        /// <summary>
        /// Check if a position in a code point buffer is a grapheme cluster boundary.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <param name="position">The position to check.</param>
        /// <param name="charsConsumed">The count of chars consumed reading the text.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsBoundary(string text, int position, out int charsConsumed)
        {
            charsConsumed = 0;
            if (text.Length == 0)
            {
                return false;
            }

            ReadOnlySpan<char> chars = text.AsMemory().Span;
            int prevCharCount = 0;

            // Get the grapheme cluster class of the character on each side
            GraphemeClusterClass a = (position <= 0)
                ? GraphemeClusterClass.SOT
                : CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFrom(chars.Slice(0, position), out prevCharCount));

            GraphemeClusterClass b = (position < text.Length)
                ? CodePoint.GetGraphemeClusterClass(CodePoint.ReadAt(text, position, out charsConsumed))
                : GraphemeClusterClass.EOT;

            // Rule 11 - Special handling for ZWJ in extended pictograph
            if (a == GraphemeClusterClass.ZWJ)
            {
                ReadOnlySpan<char> slice = chars.Slice(0, position - prevCharCount);
                int i = slice.Length;
                while (slice.Length >= 0 && CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFrom(slice, out int o)) == GraphemeClusterClass.Extend)
                {
                    slice = slice.Slice(0, slice.Length - o);
                }

                if (i >= 0 && CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFrom(slice, out int _)) == GraphemeClusterClass.ExtPict)
                {
                    a = GraphemeClusterClass.ExtPictZwg;
                }
            }

            // Special handling for regional indicator
            // Rule 12 and 13
            if (a == GraphemeClusterClass.Regional_Indicator)
            {
                // Count how many
                int count = 0;
                ReadOnlySpan<char> slice = chars.Slice(0, position - prevCharCount);
                while (slice.Length > 0)
                {
                    if (CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFrom(slice, out int o)) != GraphemeClusterClass.Regional_Indicator)
                    {
                        break;
                    }

                    slice = slice.Slice(0, slice.Length - o);
                    count++;
                }

                // If odd, switch from RI to Any
                if ((count % 2) != 0)
                {
                    a = GraphemeClusterClass.Any;
                }
            }

            return PairTable[(int)b][(int)a] != 0;
        }
    }
}

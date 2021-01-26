// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Grapheme Cluster Algorithm. UAX:29
    /// <see href="https://www.unicode.org/reports/tr29/tr29-37.html"/>
    /// </summary>
    internal static class GraphemeClusterAlgorithm
    {
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
        /// <param name="codePoints">The code points.</param>
        /// <returns>An enumerable of grapheme cluster boundaries.</returns>
        public static IEnumerable<int> GetBoundaries(ArraySlice<int> codePoints)
        {
            for (int i = 0; i <= codePoints.Length; i++)
            {
                if (IsBoundary(codePoints, i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Check if a position in a code point buffer is a grapheme cluster boundary.
        /// </summary>
        /// <param name="codePoints">The code points.</param>
        /// <param name="position">The position to check.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsBoundary(ArraySlice<int> codePoints, int position)
        {
            if (codePoints.Length == 0)
            {
                return false;
            }

            // Get the grapheme cluster class of the character on each side
            GraphemeClusterClass a = position <= 0
                ? GraphemeClusterClass.SOT
                : UnicodeData.GetGraphemeClusterClass(codePoints[position - 1]);

            GraphemeClusterClass b = position < codePoints.Length
                ? UnicodeData.GetGraphemeClusterClass(codePoints[position])
                : GraphemeClusterClass.EOT;

            // Rule 11 - Special handling for ZWJ in extended pictograph
            if (a == GraphemeClusterClass.ZWJ)
            {
                var i = position - 2;
                while (i >= 0 && UnicodeData.GetGraphemeClusterClass(codePoints[i]) == GraphemeClusterClass.Extend)
                {
                    i--;
                }

                if (i >= 0 && UnicodeData.GetGraphemeClusterClass(codePoints[i]) == GraphemeClusterClass.ExtPict)
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
                for (int i = position - 1; i > 0; i--)
                {
                    if (UnicodeData.GetGraphemeClusterClass(codePoints[i - 1]) != GraphemeClusterClass.Regional_Indicator)
                    {
                        break;
                    }

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

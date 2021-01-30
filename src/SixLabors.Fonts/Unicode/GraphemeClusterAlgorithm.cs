// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Grapheme Cluster Algorithm. UAX:29
    /// <see href="https://www.unicode.org/reports/tr29/tr29-37.html"/>
    /// </summary>
    internal ref struct GraphemeClusterAlgorithm
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

        private readonly ReadOnlySpan<char> source;
        private int charPosition;
        private readonly int pointsLength;
        private int position;

        public GraphemeClusterAlgorithm(ReadOnlySpan<char> source)
            : this()
        {
            this.source = source;
            this.pointsLength = CodePoint.GetCodePointCount(source);
            this.charPosition = 0;
            this.position = 0;
        }

        /// <summary>
        /// Returns the grapheme cluster from the current source if one is found.
        /// </summary>
        /// <param name="grapheme">
        /// When this method returns, contains the value associate with the grapheme cluster;
        /// otherwise, the default value.
        /// This parameter is passed uninitialized.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryGetGrapheme(out Grapheme grapheme)
        {
            while (this.position < this.pointsLength)
            {
                if (this.IsBoundary(this.charPosition, out int charsConsumed))
                {
                    int start = this.charPosition;
                    while (!this.IsBoundary(start + charsConsumed, out int count))
                    {
                        charsConsumed += count;
                    }

                    ReadOnlySpan<char> slice = this.source.Slice(start, charsConsumed);

                    grapheme = new Grapheme(CodePoint.DecodeFromUtf16At(this.source, start), this.position, start, slice);
                    this.position++;
                    this.charPosition += charsConsumed;
                    return true;
                }

                this.position++;
                this.charPosition += charsConsumed;
            }

            grapheme = default;
            return false;
        }

        /// <summary>
        /// Returns the index of the grapheme cluster boundary from the current source if one is found.
        /// </summary>
        /// <param name="boundary">
        /// When this method returns, contains the value associate with the boundary;
        /// otherwise, -1.
        /// This parameter is passed uninitialized.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryGetBoundary(out int boundary)
        {
            // Count past the last codepoint index to ensure SOT and EOT are counted.
            // See GraphemeBreakTests.txt. ÷ = boundary.
            // ÷ 0020 ÷ 0020 ÷ // ÷ [0.2] SPACE (Other) ÷ [999.0] SPACE (Other) ÷ [0.3]
            while (this.position <= this.pointsLength)
            {
                if (this.IsBoundary(this.charPosition, out int charsConsumed))
                {
                    boundary = this.position;
                    this.position++;
                    this.charPosition += charsConsumed;
                    return true;
                }

                this.position++;
                this.charPosition += charsConsumed;
            }

            boundary = -1;
            return false;
        }

        private bool IsBoundary(int position, out int charsConsumed)
        {
            charsConsumed = 0;
            if (this.source.Length == 0)
            {
                return false;
            }

            int prevCharCount = 0;

            // Get the grapheme cluster class of the character on each side
            GraphemeClusterClass a = (position <= 0)
                ? GraphemeClusterClass.SOT
                : CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFromUtf16(this.source.Slice(0, position), out prevCharCount));

            GraphemeClusterClass b = (position < this.source.Length)
                ? CodePoint.GetGraphemeClusterClass(CodePoint.DecodeFromUtf16At(this.source, position, out charsConsumed))
                : GraphemeClusterClass.EOT;

            // Rule 11 - Special handling for ZWJ in extended pictograph
            if (a == GraphemeClusterClass.ZWJ)
            {
                ReadOnlySpan<char> slice = this.source.Slice(0, position - prevCharCount);
                int i = slice.Length;
                while (slice.Length >= 0 && CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFromUtf16(slice, out int o)) == GraphemeClusterClass.Extend)
                {
                    slice = slice.Slice(0, slice.Length - o);
                }

                if (i >= 0 && CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFromUtf16(slice, out int _)) == GraphemeClusterClass.ExtPict)
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
                ReadOnlySpan<char> slice = this.source.Slice(0, position - prevCharCount);
                while (slice.Length > 0)
                {
                    if (CodePoint.GetGraphemeClusterClass(CodePoint.DecodeLastFromUtf16(slice, out int o)) != GraphemeClusterClass.Regional_Indicator)
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

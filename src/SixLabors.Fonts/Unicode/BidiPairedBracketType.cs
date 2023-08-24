// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Provides enumeration for classifying characters into opening and closing paired brackets
    /// for the purposes of the Unicode Bidirectional Algorithm.
    /// <see href="https://www.unicode.org/Public/UCD/latest/ucd/BidiBrackets.txt"/>.
    /// </summary>
    public enum BidiPairedBracketType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Open.
        /// </summary>
        Open = 1,

        /// <summary>
        /// Close.
        /// </summary>
        Close = 2
    }
}

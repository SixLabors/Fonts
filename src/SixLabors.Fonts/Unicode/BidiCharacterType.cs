// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Provides enumeration for the Unicode Bidirectional character types.
    /// <see href="https://unicode.org/reports/tr9/#Table Bidirectional Character Types"/>.
    /// </summary>
    public enum BidiCharacterType
    {
        // Strong Types

        /// <summary>
        /// Left-to-right.
        /// </summary>
        LeftToRight = 0,

        /// <summary>
        /// Right-to-left.
        /// </summary>
        RightToLeft = 1,

        /// <summary>
        /// Arabic Letter.
        /// </summary>
        ArabicLetter = 2,

        // Weak Types

        /// <summary>
        /// European Number
        /// </summary>
        EuropeanNumber = 3,

        /// <summary>
        /// European Separator
        /// </summary>
        EuropeanSeparator = 4,

        /// <summary>
        /// European Terminator
        /// </summary>
        EuropeanTerminator = 5,

        /// <summary>
        /// Arabic Number.
        /// </summary>
        ArabicNumber = 6,

        /// <summary>
        /// Common Separator
        /// </summary>
        CommonSeparator = 7,

        /// <summary>
        /// Nonspacing Mark.
        /// </summary>
        NonspacingMark = 8,

        /// <summary>
        /// Boundary Neutral.
        /// </summary>
        BoundaryNeutral = 9,

        // Neutral Types

        /// <summary>
        /// Paragraph Separator
        /// </summary>
        ParagraphSeparator = 10,

        /// <summary>
        /// Segment Separator
        /// </summary>
        SegmentSeparator = 11,

        /// <summary>
        /// White Space.
        /// </summary>
        Whitespace = 12,

        /// <summary>
        /// Other Neutral.
        /// </summary>
        OtherNeutral = 13,

        // Explicit Formatting Types - Embed

        /// <summary>
        /// Left-to-right Embedding.
        /// </summary>
        LeftToRightEmbedding = 14,

        /// <summary>
        /// Left-to-right Override
        /// </summary>
        LeftToRightOverride = 15,

        /// <summary>
        /// Right-to-left Embedding
        /// </summary>
        RightToLeftEmbedding = 16,

        /// <summary>
        /// Right-to-left Override.
        /// </summary>
        RightToLeftOverride = 17,

        /// <summary>
        /// Pop Directional Format
        /// </summary>
        PopDirectionalFormat = 18,

        // Explicit Formatting Types - Isolate

        /// <summary>
        /// Left-to-right Isolate
        /// </summary>
        LeftToRightIsolate = 19,

        /// <summary>
        /// Right-to-left Isolate.
        /// </summary>
        RightToLeftIsolate = 20,

        /// <summary>
        /// First Strong Isolate
        /// </summary>
        FirstStrongIsolate = 21,

        /// <summary>
        /// Pop Directional Isolate.
        /// </summary>
        PopDirectionalIsolate = 22,
    }
}

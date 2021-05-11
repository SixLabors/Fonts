// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Provides enumeration for the Unicode Bidirectional character types.
    /// <see href="https://unicode.org/reports/tr9/#Table Bidirectional Character Types"/>.
    /// </summary>
    internal enum BidiCharacterType : byte
    {
        // Strong Types

        /// <summary>
        /// Left To Right.
        /// </summary>
        L = 0,

        /// <summary>
        /// Right To Left.
        /// </summary>
        R = 1,

        /// <summary>
        /// Arabic Letter.
        /// </summary>
        AL = 2,

        // Weak Types

        /// <summary>
        /// European Number
        /// </summary>
        EN = 3,

        /// <summary>
        /// European Separator
        /// </summary>
        ES = 4,

        /// <summary>
        /// European Terminator
        /// </summary>
        ET = 5,

        /// <summary>
        /// Arabic Number.
        /// </summary>
        AN = 6,

        /// <summary>
        /// Common Separator
        /// </summary>
        CS = 7,

        /// <summary>
        /// Nonspacing Mark.
        /// </summary>
        NSM = 8,

        /// <summary>
        /// Boundary Neutral.
        /// </summary>
        BN = 9,

        // Neutral Types

        /// <summary>
        /// Paragraph Separator
        /// </summary>
        B = 10,

        /// <summary>
        /// Segment Separator
        /// </summary>
        S = 11,

        /// <summary>
        /// White Space.
        /// </summary>
        WS = 12,

        /// <summary>
        /// Other Neutral.
        /// </summary>
        ON = 13,

        // Explicit Formatting Types - Embed

        /// <summary>
        /// Left To Right Embedding.
        /// </summary>
        LRE = 14,

        /// <summary>
        /// Left To Right Override
        /// </summary>
        LRO = 15,

        /// <summary>
        /// Right To Left Embedding
        /// </summary>
        RLE = 16,

        /// <summary>
        /// Right To Left Override.
        /// </summary>
        RLO = 17,

        /// <summary>
        /// Pop Directional Format
        /// </summary>
        PDF = 18,

        // Explicit Formatting Types - Isolate

        /// <summary>
        /// Left To Right Isolate
        /// </summary>
        LRI = 19,

        /// <summary>
        /// Right To Left Isolate.
        /// </summary>
        RLI = 20,

        /// <summary>
        /// First Strong Isolate
        /// </summary>
        FSI = 21,

        /// <summary>
        /// Pop Directional Isolate.
        /// </summary>
        PDI = 22,
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Provides enumeration for the Unicode Bidirectional character types.
    /// <see href="https://unicode.org/reports/tr9/#Table_Bidirectional_Character_Types"/>.
    /// </summary>
    internal enum BidiCharacterType : byte
    {
        // Strong types
        L = 0,
        R = 1,
        AL = 2,

        // Weak Types
        EN = 3,
        ES = 4,
        ET = 5,
        AN = 6,
        CS = 7,
        NSM = 8,
        BN = 9,

        // Neutral Types
        B = 10,
        S = 11,
        WS = 12,
        ON = 13,

        // Explicit Formatting Types - Embed
        LRE = 14,
        LRO = 15,
        RLE = 16,
        RLO = 17,
        PDF = 18,

        // Explicit Formatting Types - Isolate
        LRI = 19,
        RLI = 20,
        FSI = 21,
        PDI = 22,
    }
}

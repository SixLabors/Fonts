// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

// Redordering these enum properties will require the regeneration of the Grapheme.trie.
namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Unicode Grapheme Cluster classes.
    /// <see href="https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values"/>
    /// </summary>
    internal enum GraphemeClusterClass
    {
        Any = 0,
        CR = 1,
        LF = 2,
        Control = 3,
        Extend = 4,
        Regional_Indicator = 5,
        Prepend = 6,
        SpacingMark = 7,
        L = 8,
        V = 9,
        T = 10,
        LV = 11,
        LVT = 12,
        ExtPict = 13,
        ZWJ = 14,

        // Pseudo classes, not generated from character data but used by pair table
        SOT = 15,
        EOT = 16,
        ExtPictZwg = 17,
    }
}

// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode.Resources
{
    internal static partial class IndicShapingData
    {
        public const Categories ConsonantFlags = Categories.C | Categories.Ra | Categories.CM | Categories.V | Categories.Placeholder | Categories.Dotted_Circle;
        public const Categories JoinerFlags = Categories.ZWJ | Categories.ZWNJ;
        public const Categories HalantOrCoengFlags = Categories.H | Categories.Coeng;

        // Categories used in the OpenType spec:
        // https://www.microsoft.com/typography/otfntdev/devanot/shaping.aspx
        // https://learn.microsoft.com/en-us/typography/script-development/devanagari
        [Flags]
        public enum Categories
        {
            X = 1 << 0,
            C = 1 << 1,
            V = 1 << 2,
            N = 1 << 3,
            H = 1 << 4,
            ZWNJ = 1 << 5,
            ZWJ = 1 << 6,
            M = 1 << 7,
            SM = 1 << 8,
            VD = 1 << 9,
            A = 1 << 10,
            Placeholder = 1 << 11,
            Dotted_Circle = 1 << 12,
            RS = 1 << 13, // Register Shifter, used in Khmer OT spec.
            Coeng = 1 << 14, // Khmer-style Virama.
            Repha = 1 << 15, // Atomically-encoded logical or visual repha.
            Ra = 1 << 16,
            CM = 1 << 17, // Consonant-Medial.
            Symbol = 1 << 18 // Avagraha, etc that take marks (SM,A,VD).
        }

        // Visual positions in a syllable from left to right.
        [Flags]
        public enum Positions
        {
            Start = 1 << 0,
            Ra_To_Become_Reph = 1 << 1,
            Pre_M = 1 << 2,
            Pre_C = 1 << 3,
            Base_C = 1 << 4,
            After_Main = 1 << 5,
            Above_C = 1 << 6,
            Before_Sub = 1 << 7,
            Below_C = 1 << 8,
            After_Sub = 1 << 9,
            Before_Post = 1 << 10,
            Post_C = 1 << 11,
            After_Post = 1 << 12,
            Final_C = 1 << 13,
            SMVD = 1 << 14,
            End = 1 << 15
        }
    }
}

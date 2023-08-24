// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;

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

        public enum BasePosition
        {
            First,

            Last
        }

        public enum RephMode
        {
            /// <summary>
            /// Reph formed out of initial Ra,H sequence.
            /// </summary>
            Implicit,

            /// <summary>
            /// Reph formed out of initial Ra,H,ZWJ sequence.
            /// </summary>
            Explicit,

            /// <summary>
            /// Encoded Repha character, no reordering needed.
            /// </summary>
            Vis_Repha,

            /// <summary>
            /// Encoded Repha character, needs reordering.
            /// </summary>
            Log_Repha
        }

        public enum BlwfMode
        {
            /// <summary>
            /// Below-forms feature applied to pre-base and post-base.
            /// </summary>
            Pre_And_Post,

            /// <summary>
            /// Below-forms feature applied to post-base only.
            /// </summary>
            Post_Only
        }

        public static Dictionary<ScriptClass, ShapingConfiguration> IndicConfigurations { get; } = new()
        {
            {
                ScriptClass.Devanagari,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x094D,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.Before_Post,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Bengali,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x09CD,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Sub,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Gurmukhi,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0A4D,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.Before_Sub,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Gujarati,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0ACD,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.Before_Post,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Oriya,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0B4D,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Main,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Tamil,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0BCD,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Post,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Telugu,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0C4D,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Post,
                    RephMode = RephMode.Explicit,
                    BlwfMode = BlwfMode.Post_Only
                }
            },
            {
                ScriptClass.Kannada,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0CCD,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Post,
                    RephMode = RephMode.Implicit,
                    BlwfMode = BlwfMode.Post_Only
                }
            },
            {
                ScriptClass.Malayalam,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x0D4D,
                    BasePosition = BasePosition.Last,
                    RephPosition = Positions.After_Main,
                    RephMode = RephMode.Log_Repha,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            },
            {
                ScriptClass.Khmer,
                new()
                {
                    HasOldSpec = true,
                    Virama = 0x17D2,
                    BasePosition = BasePosition.First,
                    RephPosition = Positions.Ra_To_Become_Reph,
                    RephMode = RephMode.Vis_Repha,
                    BlwfMode = BlwfMode.Pre_And_Post
                }
            }
        };

        public static Dictionary<int, int[]> Decompositions { get; } = new()
        {
            // Khmer
            { 0x17BE, new int[] { 0x17C1, 0x17BE } },
            { 0x17BF, new int[] { 0x17C1, 0x17BF } },
            { 0x17C0, new int[] { 0x17C1, 0x17C0 } },
            { 0x17C4, new int[] { 0x17C1, 0x17C4 } },
            { 0x17C5, new int[] { 0x17C1, 0x17C5 } }
        };

        internal struct ShapingConfiguration
        {
            public static ShapingConfiguration Default = new()
            {
                HasOldSpec = false,
                Virama = 0,
                BasePosition = BasePosition.Last,
                RephPosition = Positions.Before_Post,
                RephMode = RephMode.Implicit,
                BlwfMode = BlwfMode.Pre_And_Post
            };

            public bool HasOldSpec;
            public int Virama;
            public BasePosition BasePosition;
            public Positions RephPosition;
            public RephMode RephMode;
            public BlwfMode BlwfMode;
        }
    }
}

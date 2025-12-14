// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SixLabors.Fonts.Unicode.Resources;

internal static partial class IndicShapingData
{
    /// <summary>
    /// Script shaping category values used for Indic, Khmer, and Myanmar text
    /// classification. These values correspond to the category codes used by
    /// HarfBuzz in its Indic-style shaping engines, including the extended
    /// categories required for Myanmar.
    ///
    /// The values serve as the input alphabet for the script syllable machines
    /// and determine script-specific parsing, reordering, and dotted circle insertion.
    ///
    /// Categories are sourced from the OpenType Script Development Specifications
    /// and HarfBuzz's generated Indic tables:
    ///
    /// Indic specification:
    /// https://learn.microsoft.com/en-us/typography/script-development/devanagari
    ///
    /// General Indic shaper category model and data:
    /// https://github.com/harfbuzz/harfbuzz/blob/main/src/hb-ot-shaper-indic.cc
    /// https://github.com/harfbuzz/harfbuzz/blob/main/src/hb-ot-shaper-indic-table.hh
    ///
    /// Khmer specification:
    /// https://learn.microsoft.com/en-us/typography/script-development/khmer
    ///
    /// Myanmar specification:
    /// https://learn.microsoft.com/en-us/typography/script-development/myanmar
    ///
    /// Myanmar machine exports:
    /// https://github.com/harfbuzz/harfbuzz/blob/main/src/hb-ot-shaper-myanmar-machine.rl
    ///
    /// Notes:
    /// * X is the default category and always has value 0.
    /// * Coeng intentionally shares the same category value as H, matching
    ///   HarfBuzz behavior for Khmer.
    /// * Some values are shared across scripts (for example VAbv, VBlw, VPre,
    ///   VPst) because the OpenType model for dependent vowels is the same.
    /// * Myanmar-specific medial and tone categories begin at 32 and above,
    ///   matching HarfBuzz's numeric category assignments.
    /// </summary>
    public enum Categories : int
    {
        // Core Indic-style categories (shared across scripts where applicable)
        X = 0,   // Uncategorized / default

        C = 1,   // Consonant
        V = 2,   // Dependent vowel
        N = 3,   // Nukta
        H = 4,   // Halant (virama)
        // Coeng = H,   // Khmer Coeng, mapped to H in HarfBuzz

        ZWNJ = 5,   // Zero width non-joiner
        ZWJ = 6,   // Zero width joiner
        M = 7,   // Generic matra / dependent vowel
        SM = 8,   // Syllable modifier / visarga / tone marks
        A = 9,   // Vowel sign A (and related)
        // VD = 9,   // Vowel-dependent sign (shares code with A)

        Placeholder = 10,  // Placeholder (NBSP, etc.)
        Dotted_Circle = 11,  // Explicit dotted circle

        RS = 12,  // Register shifter (Khmer)
        MPst = 13,  // Post-base matra
        Repha = 14,  // Repha form
        Ra = 15,  // Consonant Ra
        CM = 16,  // Consonant medial
        Symbol = 17,  // Symbol / Avagraha-like mark
        CS = 18,  // Consonant-with-stacker / special consonant

        SMPst = 57,  // Post-base spacing mark (shared Indic / Myanmar)

        // Shared positional vowel / matra categories (Indic / Khmer / Myanmar)
        VAbv = 20,  // Above-base vowel or matra
        VBlw = 21,  // Below-base vowel or matra
        VPre = 22,  // Pre-base vowel or matra
        VPst = 23,  // Post-base vowel or matra

        // Khmer-specific categories
        Robatic = 25,  // Khmer Robatic sign
        Xgroup = 26,  // Khmer X-group matra sequence
        Ygroup = 27,  // Khmer Y-group matra sequence
        Coeng = 28, // Remove once we no longer need it for Khmer

        // Myanmar-specific categories
        //IV = V,   // Independent vowel (shares code 2 with V in HarfBuzz)
        //DB = N,   // Dot-below (shares code 3 with N)
        //GB = Placeholder, // Generic base / placeholder (shares code 10)

        As = 32,  // Asat
        MH = 35,  // Medial Ha
        MR = 36,  // Medial Ra
        MW = 37,  // Medial Wa / Shan Wa
        MY = 38,  // Medial Ya / Mon Na / Mon Ma
        PT = 39,  // Pwo and related tone marks
        VS = 40,  // Variation selector
        ML = 41 // Medial Mon La
    }

    // Categories used in the Myanmar shaping engine.
    // Note:
    // The OpenType Myanmar spec defines categories D, D0, and P.
    // HarfBuzz collapses:
    //   D  => GB
    //   D0 => D => GB
    //   P  => GB
    // We follow the same normalization, so D, D0 and P do not appear
    // as distinct category flags.
    // Only the symbols that appear in the Myanmar grammar.
    // Values must match the Categories enum and the Ragel `export` codes.
    public enum MyanmarCategories : int
    {
        C = Categories.C,
        IV = Categories.V,
        DB = Categories.N,
        H = Categories.H,
        ZWNJ = Categories.ZWNJ,
        ZWJ = Categories.ZWJ,
        SM = Categories.SM,
        A = Categories.A,
        GB = Categories.Placeholder,
        Dotted_Circle = Categories.Dotted_Circle,
        Ra = Categories.Ra,
        CS = Categories.CS,
        SMPst = Categories.SMPst,

        VAbv = Categories.VAbv,
        VBlw = Categories.VBlw,
        VPre = Categories.VPre,
        VPst = Categories.VPst,

        As = Categories.As,
        MH = Categories.MH,
        MR = Categories.MR,
        MW = Categories.MW,
        MY = Categories.MY,
        PT = Categories.PT,
        VS = Categories.VS,
        ML = Categories.ML,
    }

    [Flags]
    public enum MyanmarSyllableType
    {
        Consonant_Syllable = 1 << 0,
        Broken_Cluster = 1 << 1,
        NonMyanmar_Cluster = 1 << 2
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

    public static uint ConsonantFlags { get; } =
        Flag(Categories.C) |
        Flag(Categories.Ra) |
        Flag(Categories.CM) |
        Flag(Categories.V) |
        Flag(Categories.Placeholder) |
        Flag(Categories.Dotted_Circle);

    // Note:
    // We treat Vowels and placeholders as if they were consonants.This is safe because Vowels
    // cannot happen in a consonant syllable.The plus side however is, we can call the
    // consonant syllable logic from the vowel syllable function and get it all right!
    // Keep in sync with the categories used in the Myanmar state machine generator.
    public static uint MyanmarConsonantFlags { get; } =
        Flag(MyanmarCategories.C) |
        Flag(MyanmarCategories.CS) |
        Flag(MyanmarCategories.Ra) |
        Flag(MyanmarCategories.IV) |
        Flag(MyanmarCategories.GB) |
        Flag(MyanmarCategories.Dotted_Circle);

    public static uint JoinerFlags { get; } =
        Flag(Categories.ZWJ) |
        Flag(Categories.ZWNJ);

    public static uint HalantOrCoengFlags { get; } =
        Flag(Categories.H) |
        Flag(Categories.Coeng);

    /// <summary>
    /// Provides a flag value for the given category. Only valid for categories &lt; 32.
    /// </summary>
    /// <param name="categories">The category for which to generate a bit flag. If null, the default category is used.</param>
    /// <returns>A 32-bit unsigned integer with a single bit set corresponding to the specified category value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Flag(Categories? categories)
        => FlagCoreChecked((int)(categories ?? default));

    /// <summary>
    /// Provides a flag value for the given category. Only valid for categories &lt; 32.
    /// </summary>
    /// <param name="categories">The category for which to generate a bit flag. If null, the default category is used.</param>
    /// <returns>A 32-bit unsigned integer with a single bit set corresponding to the specified category value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Flag(MyanmarCategories? categories)
        => FlagCoreChecked((int)(categories ?? default));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FlagCoreChecked(int value)
    {
#if DEBUG
        if ((uint)value >= 32u)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Flag() is only defined for enum values < 32.");
        }
#endif
        return 1u << value;
    }

    /// <summary>
    /// Returns a bit flag corresponding to the specified category, or zero if the category value is out of range.
    /// </summary>
    /// <param name="categories">The category for which to generate a bit flag. If null, the default category is used.</param>
    /// <returns>
    /// A 32-bit unsigned integer with a single bit set corresponding to the specified category value; returns 0 if the
    /// category value is not between 0 and 31, inclusive.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FlagUnsafe(Categories? categories)
        => FlagUnsafeCore((int)(categories ?? default));

    /// <summary>
    /// Returns a bit flag corresponding to the specified category, or zero if the category value is out of range.
    /// </summary>
    /// <param name="categories">The category for which to generate a bit flag. If null, the default category is used.</param>
    /// <returns>
    /// A 32-bit unsigned integer with a single bit set corresponding to the specified category value; returns 0 if the
    /// category value is not between 0 and 31, inclusive.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FlagUnsafe(MyanmarCategories? categories)
        => FlagUnsafeCore((int)(categories ?? default));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FlagUnsafeCore(int value) => value < 32 ? 1u << value : 0u;

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

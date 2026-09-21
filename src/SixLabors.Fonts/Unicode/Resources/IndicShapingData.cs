// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode.Resources;

internal static partial class IndicShapingData
{
    /// <summary>
    /// Script shaping category values used for Indic, Khmer, and Myanmar text
    /// classification.
    ///
    /// The values serve as the input alphabet for the script syllable machines
    /// and determine script-specific parsing, reordering, and dotted circle insertion.
    ///
    /// </summary>
    /// <remarks>
    /// The values are transcribed from HarfBuzz 14.2.1, <c>src/gen-indic-table.py</c>, symbols <c>category_map</c>, <c>category_overrides</c>, and <c>position_to_category</c>. The shaping categories and overrides are not derivable from the Unicode Character Database.
    /// </remarks>
    public enum Categories : int
    {
        // Core Indic-style categories (shared across scripts where applicable)
        X = 0,   // Uncategorized / default

        C = 1,   // Consonant
        V = 2,   // Dependent vowel
        N = 3,   // Nukta
        H = 4,   // Halant (virama)

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

        // Myanmar-specific categories
        // IV = V,   // Independent vowel (shares code 2 with V)
        // DB = N,   // Dot-below (shares code 3 with N)
        // GB = Placeholder, // Generic base / placeholder (shares code 10)
        As = 32,  // Asat
        MH = 35,  // Medial Ha
        MR = 36,  // Medial Ra
        MW = 37,  // Medial Wa / Shan Wa
        MY = 38,  // Medial Ya / Mon Na / Mon Ma
        PT = 39,  // Pwo and related tone marks
        VS = 40,  // Variation selector
        ML = 41 // Medial Mon La
    }

    /// <summary>
    /// The shaping categories consumed by the Khmer syllable machine.
    /// </summary>
    /// <remarks>
    /// The category set is transcribed from HarfBuzz 14.2.1, <c>src/hb-ot-shaper-khmer-machine.rl</c>, symbol <c>khmer_syllable_machine</c>. The values are not derivable from the Unicode Character Database.
    /// </remarks>
    public enum KhmerCategories : int
    {
        X = Categories.X,
        C = Categories.C,
        V = Categories.V,
        H = Categories.H,
        ZWNJ = Categories.ZWNJ,
        ZWJ = Categories.ZWJ,
        Placeholder = Categories.Placeholder,
        Dotted_Circle = Categories.Dotted_Circle,
        Ra = Categories.Ra,
        VAbv = Categories.VAbv,
        VBlw = Categories.VBlw,
        VPre = Categories.VPre,
        VPst = Categories.VPst,
        Robatic = Categories.Robatic,
        Xgroup = Categories.Xgroup,
        Ygroup = Categories.Ygroup,
    }

    // Categories used in the Myanmar shaping engine.
    // Note:
    // The OpenType Myanmar spec defines categories D, D0, and P.
    // The source table collapses:
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

    // Visual positions in a syllable from left to right. Ordinal values whose order
    // is the visual order; zero is reserved as the unassigned sentinel so a default
    // syllable record compares unequal to every real position.
    public enum Positions
    {
        Start = 1,
        Ra_To_Become_Reph = 2,
        Pre_M = 3,
        Pre_C = 4,
        Base_C = 5,
        After_Main = 6,
        Above_C = 7,
        Before_Sub = 8,
        Below_C = 9,
        After_Sub = 10,
        Before_Post = 11,
        Post_C = 12,
        After_Post = 13,
        Final_C = 14,
        SMVD = 15,
        End = 16
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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
            new ShapingConfiguration
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

    /// <summary>
    /// Gets the bit identifying a halant category.
    /// </summary>
    public static uint HalantFlags { get; } = Flag(Categories.H);

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

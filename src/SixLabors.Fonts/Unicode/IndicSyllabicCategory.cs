// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Indic_Syllabic_Category property values.
/// <see href="https://www.unicode.org/reports/tr44/#Indic_Syllabic_Category"/>
/// </summary>
/// <remarks>
/// These values describe subtypes relevant to Indic syllable, or aksara,
/// construction and segmentation.
/// </remarks>
public enum IndicSyllabicCategory
{
    /// <summary>
    /// Avagraha.
    /// </summary>
    Avagraha = 0,

    /// <summary>
    /// Bindu.
    /// </summary>
    Bindu = 1,

    /// <summary>
    /// Brahmi_Joining_Number.
    /// </summary>
    BrahmiJoiningNumber = 2,

    /// <summary>
    /// Cantillation_Mark.
    /// </summary>
    CantillationMark = 3,

    /// <summary>
    /// Consonant
    /// </summary>
    Consonant = 4,

    /// <summary>
    /// Consonant_Dead
    /// </summary>
    ConsonantDead = 5,

    /// <summary>
    /// Consonant_Final
    /// </summary>
    ConsonantFinal = 6,

    /// <summary>
    /// Consonant_Head_Letter
    /// </summary>
    ConsonantHeadLetter = 7,

    /// <summary>
    /// Consonant_Initial_Postfixed
    /// </summary>
    ConsonantInitialPostfixed = 8,

    /// <summary>
    /// Consonant_Killer
    /// </summary>
    ConsonantKiller = 9,

    /// <summary>
    /// Consonant_Medial
    /// </summary>
    ConsonantMedial = 10,

    /// <summary>
    /// Consonant_Placeholder
    /// </summary>
    ConsonantPlaceholder = 11,

    /// <summary>
    /// Consonant_Preceding_Repha
    /// </summary>
    ConsonantPrecedingRepha = 12,

    /// <summary>
    /// Consonant_Prefixed
    /// </summary>
    ConsonantPrefixed = 13,

    /// <summary>
    /// Consonant_Subjoined
    /// </summary>
    ConsonantSubjoined = 14,

    /// <summary>
    /// Consonant_Succeeding_Repha
    /// </summary>
    ConsonantSucceedingRepha = 15,

    /// <summary>
    /// Consonant_With_Stacker
    /// </summary>
    ConsonantWithStacker = 16,

    /// <summary>
    /// Gemination_Mark
    /// </summary>
    GeminationMark = 17,

    /// <summary>
    /// Invisible_Stacker
    /// </summary>
    InvisibleStacker = 18,

    /// <summary>
    /// Joiner
    /// </summary>
    Joiner = 19,

    /// <summary>
    /// Modifying_Letter
    /// </summary>
    ModifyingLetter = 20,

    /// <summary>
    /// Non_Joiner
    /// </summary>
    NonJoiner = 21,

    /// <summary>
    /// Nukta
    /// </summary>
    Nukta = 22,

    /// <summary>
    /// Number
    /// </summary>
    Number = 23,

    /// <summary>
    /// Number_Joiner
    /// </summary>
    NumberJoiner = 24,

    /// <summary>
    /// Pure_Killer
    /// </summary>
    PureKiller = 26,

    /// <summary>
    /// Register_Shifter
    /// </summary>
    RegisterShifter = 27,

    /// <summary>
    /// Reordering_Killer
    /// </summary>
    ReorderingKiller = 28,

    /// <summary>
    /// Syllable_Modifier
    /// </summary>
    SyllableModifier = 29,

    /// <summary>
    /// Tone_Letter
    /// </summary>
    ToneLetter = 30,

    /// <summary>
    /// Tone_Mark
    /// </summary>
    ToneMark = 31,

    /// <summary>
    /// Virama
    /// </summary>
    Virama = 32,

    /// <summary>
    /// Visarga
    /// </summary>
    Visarga = 33,

    /// <summary>
    /// Vowel
    /// </summary>
    Vowel = 34,

    /// <summary>
    /// Vowel_Dependent
    /// </summary>
    VowelDependent = 35,

    /// <summary>
    /// Vowel_Independent
    /// </summary>
    VowelIndependent = 36,

    /// <summary>
    /// Other.
    /// </summary>
    /// <remarks>
    /// This is the Unicode fallback for code points without an explicit
    /// Indic_Syllabic_Category.
    /// </remarks>
    Other = 0xFF
}

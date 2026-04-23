// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Indic_Positional_Category property values.
/// <see href="https://www.unicode.org/reports/tr44/#Indic_Positional_Category"/>
/// </summary>
/// <remarks>
/// These values describe the notional slot of dependent vowels, visible viramas, and related
/// signs around an Indic syllable core. The property supplements
/// <see cref="IndicSyllabicCategory"/> for shaping and segmentation, but it is not a
/// prescriptive font-design or final glyph-placement property. <see cref="NA"/> is
/// the default for code points whose syllabic role does not have a positional slot.
/// </remarks>
public enum IndicPositionalCategory
{
    /// <summary>
    /// Bottom.
    /// </summary>
    Bottom = 0,

    /// <summary>
    /// Bottom_And_Left.
    /// </summary>
    BottomAndLeft = 1,

    /// <summary>
    /// Bottom_And_Right.
    /// </summary>
    BottomAndRight = 2,

    /// <summary>
    /// Left.
    /// </summary>
    Left = 3,

    /// <summary>
    /// Left_And_Right.
    /// </summary>
    LeftAndRight = 4,

    /// <summary>
    /// Overstruck.
    /// </summary>
    Overstruck = 6,

    /// <summary>
    /// Right.
    /// </summary>
    Right = 7,

    /// <summary>
    /// Top.
    /// </summary>
    Top = 8,

    /// <summary>
    /// Top_And_Bottom.
    /// </summary>
    TopAndBottom = 9,

    /// <summary>
    /// Top_And_Bottom_And_Left.
    /// </summary>
    TopAndBottomAndLeft = 10,

    /// <summary>
    /// Top_And_Bottom_And_Right.
    /// </summary>
    TopAndBottomAndRight = 11,

    /// <summary>
    /// Top_And_Left.
    /// </summary>
    TopAndLeft = 12,

    /// <summary>
    /// Top_And_Left_And_Right.
    /// </summary>
    TopAndLeftAndRight = 13,

    /// <summary>
    /// Top_And_Right.
    /// </summary>
    TopAndRight = 14,

    /// <summary>
    /// Visual_Order_Left.
    /// </summary>
    VisualOrderLeft = 15,

    /// <summary>
    /// Not applicable.
    /// </summary>
    /// <remarks>
    /// This is the Unicode fallback for code points without an explicit
    /// Indic_Positional_Category.
    /// </remarks>
    NA = 0xFF,
}

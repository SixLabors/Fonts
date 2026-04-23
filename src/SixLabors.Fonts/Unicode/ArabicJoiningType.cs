// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Joining_Type property values used for Arabic-script cursive shaping.
/// <see href="https://www.unicode.org/reports/tr44/#Joining_Type"/>
/// </summary>
/// <remarks>
/// These values describe how a character participates in cursive joining with
/// neighboring characters.
/// </remarks>
public enum ArabicJoiningType
{
    /// <summary>
    /// Right_Joining (R): joins on the right side only.
    /// </summary>
    RightJoining,

    /// <summary>
    /// Left_Joining (L): joins on the left side only.
    /// </summary>
    LeftJoining,

    /// <summary>
    /// Dual_Joining (D): joins on both sides.
    /// </summary>
    DualJoining,

    /// <summary>
    /// Join_Causing (C): causes adjacent join-capable characters to join.
    /// </summary>
    JoinCausing,

    /// <summary>
    /// Non_Joining (U): does not participate in cursive joining.
    /// </summary>
    NonJoining,

    /// <summary>
    /// Transparent (T): ignored when determining the joining relationship of surrounding characters.
    /// </summary>
    Transparent
}

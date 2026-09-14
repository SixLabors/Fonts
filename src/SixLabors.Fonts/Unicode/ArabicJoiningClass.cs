// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Represents the Unicode joining properties of a given <see cref="CodePoint"/>.
/// <see href="https://www.unicode.org/reports/tr44/#Joining_Type"/>
/// <see href="https://www.unicode.org/reports/tr44/#Joining_Group"/>
/// </summary>
/// <remarks>
/// This combines the Unicode <c>Joining_Type</c> and <c>Joining_Group</c>
/// properties used by cursive shaping. Unlisted nonspacing marks, enclosing marks,
/// and format controls follow the Unicode default joining behavior.
/// </remarks>
public readonly struct ArabicJoiningClass
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArabicJoiningClass"/> struct.
    /// </summary>
    /// <param name="codePoint">The codepoint.</param>
    public ArabicJoiningClass(CodePoint codePoint)
    {
        UnicodeCategory category = CodePoint.GetGeneralCategory(codePoint);
        uint value = UnicodeData.GetJoiningClass((uint)codePoint.Value);
        this.JoiningType = GetJoiningType(value, category);
        this.JoiningGroup = (ArabicJoiningGroup)((value >> 16) & 0xFF);
    }

    /// <summary>
    /// Gets the Unicode joining type.
    /// </summary>
    public ArabicJoiningType JoiningType { get; }

    /// <summary>
    /// Gets the Unicode joining group.
    /// </summary>
    public ArabicJoiningGroup JoiningGroup { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ArabicJoiningType GetJoiningType(uint value, UnicodeCategory category)
    {
        ArabicJoiningType type = (ArabicJoiningType)(value & 0xFF);
        if (type != ArabicJoiningType.Unlisted)
        {
            return type;
        }

        // A character the joining data does not list is transparent when it is a
        // mark or a format character, and does not join otherwise.
        return category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format
            ? ArabicJoiningType.Transparent
            : ArabicJoiningType.NonJoining;
    }
}

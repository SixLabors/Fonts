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
        this.JoiningType = GetJoiningType(codePoint, value, category);
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
    private static ArabicJoiningType GetJoiningType(CodePoint codePoint, uint value, UnicodeCategory category)
    {
        var type = (ArabicJoiningType)(value & 0xFF);

        // All others not explicitly listed have joining type U
        if (type == ArabicJoiningType.NonJoining)
        {
            // 200C; ZERO WIDTH NON-JOINER; U; No_Joining_Group
            // 200D; ZERO WIDTH JOINER; C; No_Joining_Group
            // 202F; NARROW NO-BREAK SPACE; U; No_Joining_Group
            // 2066; LEFT-TO-RIGHT ISOLATE; U; No_Joining_Group
            // 2067; RIGHT-TO-LEFT ISOLATE; U; No_Joining_Group
            // 2068; FIRST STRONG ISOLATE; U; No_Joining_Group
            // 2069; POP DIRECTIONAL ISOLATE; U; No_Joining_Group
            if (codePoint.Value is 0x200C
                or 0x200D
                or 0x202F
                or 0x2066
                or 0x2067
                or 0x2068
                or 0x2069)
            {
                return type;
            }

            // Those that are not explicitly listed and that are of General Category Mn, Me, or Cf have joining type T.
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
            {
                type = ArabicJoiningType.Transparent;
            }
        }

        return type;
    }
}

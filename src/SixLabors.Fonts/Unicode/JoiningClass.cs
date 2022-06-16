// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents the Unicode Joining value of a given <see cref="CodePoint"/>.
    /// <see href="https://www.unicode.org/versions/Unicode13.0.0/ch09.pdf"/>
    /// </summary>
    public readonly struct JoiningClass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoiningClass"/> struct.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        public JoiningClass(CodePoint codePoint)
        {
            UnicodeCategory category = CodePoint.GetGeneralCategory(codePoint);
            uint value = UnicodeData.GetJoiningClass((uint)codePoint.Value);
            this.JoiningType = GetJoiningType(codePoint, value, category);
            this.JoiningGroup = (JoiningGroup)((value >> 16) & 0xFF);
        }

        /// <summary>
        /// Gets the Unicode joining type.
        /// </summary>
        public JoiningType JoiningType { get; }

        /// <summary>
        /// Gets the Unicode joining group.
        /// </summary>
        public JoiningGroup JoiningGroup { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JoiningType GetJoiningType(CodePoint codePoint, uint value, UnicodeCategory category)
        {
            var type = (JoiningType)(value & 0xFF);

            // All others not explicitly listed have joining type U
            if (type == JoiningType.NonJoining)
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
                    type = JoiningType.Transparent;
                }
            }

            return type;
        }
    }
}

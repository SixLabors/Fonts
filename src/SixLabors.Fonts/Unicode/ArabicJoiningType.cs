// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Unicode Arabic Joining Types
    /// <see href="https://www.unicode.org/versions/Unicode13.0.0/ch09.pdf"/> Table 9-3.
    /// </summary>
    public enum ArabicJoiningType
    {
        /// <summary>
        /// Right Joining (R)
        /// </summary>
        RightJoining,

        /// <summary>
        /// Left Joining (L)
        /// </summary>
        LeftJoining,

        /// <summary>
        /// Dual Joining (D)
        /// </summary>
        DualJoining,

        /// <summary>
        /// Join Causing (C)
        /// </summary>
        JoinCausing,

        /// <summary>
        /// Non Joining (U)
        /// </summary>
        NonJoining,

        /// <summary>
        /// Transparent (T)
        /// </summary>
        Transparent
    }
}

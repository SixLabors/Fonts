// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines modes to determine when line breaks should appear when words overflow
    /// their content box.
    /// </summary>
    public enum WordBreaking
    {
        /// <summary>
        /// Use the default line break rule.
        /// </summary>
        Standard,

        /// <summary>
        /// To prevent overflow, word breaks should be inserted between any two
        /// characters (excluding Chinese/Japanese/Korean text).
        /// </summary>
        BreakAll,

        /// <summary>
        /// Word breaks should not be used for Chinese/Japanese/Korean (CJK) text.
        /// Non-CJK text behavior is the same as for <see cref="Standard"/>
        /// </summary>
        KeepAll,

        /// <summary>
        /// Uses a combination of <see cref="Standard"/> and <see cref="BreakAll"/> rules in that order.
        /// </summary>
        BreakWord
    }
}

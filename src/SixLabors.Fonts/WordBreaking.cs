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
        /// No word breaking.
        /// </summary>
        None,

        /// <summary>
        /// The word is broken based upon grapheme boundaries.
        /// </summary>
        Auto
    }
}

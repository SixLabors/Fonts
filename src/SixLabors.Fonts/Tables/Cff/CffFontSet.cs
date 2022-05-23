// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    // TODO: Delete this type. We only ever have 1 font.

    /// <summary>
    /// A Compact Font Format (CFF) font program as described in The Compact Font Format specification (Adobe Technical Note #5176).
    /// A CFF font may contain multiple fonts and achieves compression by sharing details between fonts in the set.
    /// </summary>
    internal class CffFontSet
    {
        public string FontName { get; set; }

        /// <summary>
        /// Gets the individual fonts contained within this collection
        /// </summary>
        public List<Cff1Font> Fonts { get; } = new List<Cff1Font>();
    }
}

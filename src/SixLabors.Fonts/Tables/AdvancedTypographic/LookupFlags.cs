// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    /// <summary>
    /// LookupFlag bit enumeration, see: https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table
    /// </summary>
    [Flags]
    internal enum LookupFlags : ushort
    {
        /// <summary>
        /// This bit relates only to the correct processing of the cursive attachment lookup type (GPOS lookup type 3).
        /// When this bit is set, the last glyph in a given sequence to which the cursive attachment lookup is applied, will be positioned on the baseline.
        /// </summary>
        RightToLeft = 0x0001,

        /// <summary>
        /// If set, skips over base glyphs.
        /// </summary>
        IgnoreBaseGlypghs = 0x0002,

        /// <summary>
        /// If set, skips over ligatures.
        /// </summary>
        IgnoreLigatures = 0x0004,

        /// <summary>
        /// If set, skips over all combining marks.
        /// </summary>
        IgnoreMarks = 0x0008,

        /// <summary>
        /// If set, indicates that the lookup table structure is followed by a MarkFilteringSet field.
        /// The layout engine skips over all mark glyphs not in the mark filtering set indicated.
        /// </summary>
        UseMarkFilteringSet = 0x0010,

        /// <summary>
        /// If not zero, skips over all marks of attachment type different from specified.
        /// </summary>
        MarkAttachmentTypeMask = 0xFF00
    }
}

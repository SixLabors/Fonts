// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Provides enumeration determining when to zero mark advances.
/// </summary>
internal enum MarkZeroingMode
{
    /// <summary>
    /// Zero mark advances before GPOS processing.
    /// </summary>
    PreGPos,

    /// <summary>
    /// Zero mark advances after GPOS processing.
    /// </summary>
    PostGpos,

    /// <summary>
    /// Do not zero mark advances.
    /// </summary>
    None
}

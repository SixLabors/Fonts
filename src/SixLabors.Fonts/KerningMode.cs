// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Kerning is the contextual adjustment of inter-glyph spacing.
/// This property controls metric kerning, kerning that utilizes adjustment data contained in the font.
/// </summary>
public enum KerningMode
{
    /// <summary>
    /// Specifies that kerning is applied.
    /// </summary>
    Standard,

    /// <summary>
    /// Specifies that kerning is not applied.
    /// </summary>
    None,

    /// <summary>
    /// Specifies that kerning is applied at the discretion of the layout engine.
    /// </summary>
    Auto,
}

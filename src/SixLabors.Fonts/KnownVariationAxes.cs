// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

using SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Defines the registered design-variation axis tags for variable fonts.
/// These tags are used with <see cref="FontVariation"/> to control font design axes.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxisreg"/>
/// </summary>
public static class KnownVariationAxes
{
    /// <summary>
    /// Italic axis ('ital'). Controls the italic angle of the font.
    /// Value range: 0 (upright) to 1 (italic).
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxistag_ital"/>
    /// </summary>
    public static readonly Tag Italic = Tag.Parse("ital");

    /// <summary>
    /// Optical size axis ('opsz'). Adjusts the design for a specific text size in points.
    /// Typical range: 6 to 144. Larger values optimize for display use; smaller for body text.
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxistag_opsz"/>
    /// </summary>
    public static readonly Tag OpticalSize = Tag.Parse("opsz");

    /// <summary>
    /// Slant axis ('slnt'). Controls the slant angle of upright glyphs in degrees.
    /// Typical range: -90 to 90. Negative values slant to the right (the common direction).
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxistag_slnt"/>
    /// </summary>
    public static readonly Tag Slant = Tag.Parse("slnt");

    /// <summary>
    /// Width axis ('wdth'). Controls the relative width of the font as a percentage of normal.
    /// Typical range: 75 (condensed) to 125 (expanded). 100 represents the normal width.
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxistag_wdth"/>
    /// </summary>
    public static readonly Tag Width = Tag.Parse("wdth");

    /// <summary>
    /// Weight axis ('wght'). Controls the weight (boldness) of the font.
    /// Range: 1 to 1000. Common values: 100 (Thin), 300 (Light), 400 (Regular),
    /// 700 (Bold), 900 (Black).
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dvaraxistag_wght"/>
    /// </summary>
    public static readonly Tag Weight = Tag.Parse("wght");
}

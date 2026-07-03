// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents system font metrics associated with a platform family name.
/// </summary>
internal readonly struct SystemFontFamilyMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemFontFamilyMetrics"/> struct.
    /// </summary>
    /// <param name="familyName">The platform family name.</param>
    /// <param name="style">The platform font style.</param>
    /// <param name="metrics">The font metrics.</param>
    public SystemFontFamilyMetrics(string familyName, FontStyle style, FontMetrics metrics)
    {
        this.FamilyName = familyName;
        this.Style = style;
        this.Metrics = metrics;
    }

    /// <summary>
    /// Gets the platform family name.
    /// </summary>
    public string FamilyName { get; }

    /// <summary>
    /// Gets the platform font style.
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// Gets the font metrics.
    /// </summary>
    public FontMetrics Metrics { get; }
}

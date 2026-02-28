// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a collection of <see cref="FontMetrics"/>
/// </summary>
internal interface IFontMetricsCollection : IReadOnlyFontMetricsCollection
{
    /// <summary>
    /// Adds the font metrics and culture to the <see cref="IFontMetricsCollection"/>.
    /// </summary>
    /// <param name="metrics">The font metrics to add.</param>
    /// <param name="culture">The culture of the font metrics to add.</param>
    /// <returns>The new <see cref="FontFamily"/>.</returns>
    public FontFamily AddMetrics(FontMetrics metrics, CultureInfo culture);

    /// <summary>
    /// Adds the font metrics to the <see cref="IFontMetricsCollection"/>.
    /// </summary>
    /// <param name="metrics">The font metrics to add.</param>
    public void AddMetrics(FontMetrics metrics);
}

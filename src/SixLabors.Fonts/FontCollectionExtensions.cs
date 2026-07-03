// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Extension methods for <see cref="IFontCollection"/>.
/// </summary>
public static class FontCollectionExtensions
{
    /// <summary>
    /// Adds the fonts from the <see cref="SystemFonts"/> collection to this <see cref="FontCollection"/>.
    /// </summary>
    /// <param name="collection">The font collection.</param>
    /// <returns>The <see cref="FontCollection"/> containing the system fonts.</returns>
    public static FontCollection AddSystemFonts(this FontCollection collection)
    {
        foreach (SystemFontFamilyMetrics metric in ((SystemFontCollection)SystemFonts.Collection).GetAllFamilyMetrics())
        {
            ((IFontMetricsCollection)collection).AddMetrics(metric.Metrics, metric.FamilyName, metric.Style);
        }

        collection.AddSearchDirectories(SystemFonts.Collection.SearchDirectories);

        return collection;
    }

    /// <summary>
    /// Adds the fonts from the <see cref="SystemFonts"/> collection to this <see cref="FontCollection"/>.
    /// </summary>
    /// <param name="collection">The font collection.</param>
    /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of <see cref="FontMetrics"/> to add into the font collection.</param>
    /// <returns>The <see cref="FontCollection"/> containing the system fonts.</returns>
    public static FontCollection AddSystemFonts(this FontCollection collection, Predicate<FontMetrics> match)
    {
        bool isMatch = false;
        foreach (SystemFontFamilyMetrics metric in ((SystemFontCollection)SystemFonts.Collection).GetAllFamilyMetrics())
        {
            bool currentMatch = match(metric.Metrics);
            isMatch |= currentMatch;
            if (currentMatch)
            {
                ((IFontMetricsCollection)collection).AddMetrics(metric.Metrics, metric.FamilyName, metric.Style);
            }
        }

        if (isMatch)
        {
            collection.AddSearchDirectories(SystemFonts.Collection.SearchDirectories);
        }

        return collection;
    }
}

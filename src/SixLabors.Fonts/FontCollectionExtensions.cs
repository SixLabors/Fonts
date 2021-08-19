// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
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
            // This cast is safe because our underlying SystemFontCollection implements
            // both interfaces separately.
            foreach (IFontMetrics? metric in (IReadOnlyFontMetricsCollection)SystemFonts.Collection)
            {
                ((IFontMetricsCollection)collection).AddMetrics(metric);
            }

            return collection;
        }
    }
}

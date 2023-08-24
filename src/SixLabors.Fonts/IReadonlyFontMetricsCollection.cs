// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a readonly collection of font metrics.
    /// The interface uses compiler pattern matching to provide enumeration capabilities.
    /// </summary>
    internal interface IReadOnlyFontMetricsCollection
    {
        /// <summary>
        /// Gets the specified font metrics matching the given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when searching for a match.</param>
        /// <param name="style">The font style to use when searching for a match.</param>
        /// <param name="metrics">
        /// When this method returns, contains the metrics associated with the specified name,
        /// if the name is found; otherwise, the default value for the type of the family parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IReadOnlyFontMetricsCollection"/> contains font metrics
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        bool TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out FontMetrics? metrics);

        /// <summary>
        /// Gets the collection of available font metrics for a given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when searching for a match.</param>
        /// <returns>The <see cref="IEnumerable{FontMetrics}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        IEnumerable<FontMetrics> GetAllMetrics(string name, CultureInfo culture);

        /// <summary>
        /// Gets the collection of available font styles for a given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when searching for a match.</param>
        /// <returns>The <see cref="IEnumerable{FontStyle}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        IEnumerable<FontStyle> GetAllStyles(string name, CultureInfo culture);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        IEnumerator<FontMetrics> GetEnumerator();
    }
}

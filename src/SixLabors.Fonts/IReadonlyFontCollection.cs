// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a readonly collection of fonts.
    /// </summary>
    public interface IReadOnlyFontCollection
    {
        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> in this <see cref="IReadOnlyFontCollection"/>
        /// using the invariant culture.
        /// </summary>
        IEnumerable<FontFamily> Families { get; }

        /// <summary>
        /// <para>
        /// Gets the collection of directories that were searched for font families.
        /// </para>
        /// <para>
        /// The search directories are only applicable for the <see cref="SystemFontCollection"/> or if
        /// <see cref="FontCollectionExtensions.AddSystemFonts"/> was called on a font collection.
        /// For font collections that do not involve system fonts, the search directories is always empty.
        /// </para>
        /// </summary>
        public IEnumerable<string> SearchDirectories { get; }

        /// <summary>
        /// Gets the specified font family matching the invariant culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <returns>The first <see cref="FontFamily"/> matching the given name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        /// <exception cref="FontFamilyNotFoundException">The collection contains no matches.</exception>
        FontFamily Get(string name);

        /// <summary>
        /// Gets the specified font family matching the invariant culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="family">
        /// When this method returns, contains the family associated with the specified name,
        /// if the name is found; otherwise, the default value for the type of the family parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IReadOnlyFontCollection"/> contains a family
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        bool TryGet(string name, out FontFamily family);

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> in this <see cref="FontCollection"/>
        /// using the given culture.
        /// </summary>
        /// <param name="culture">The culture of the families to return.</param>
        /// <returns>The <see cref="IEnumerable{FontFamily}"/>.</returns>
        IEnumerable<FontFamily> GetByCulture(CultureInfo culture);

        /// <summary>
        /// Gets the specified font family matching the given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when searching for a match.</param>
        /// <returns>The first <see cref="FontFamily"/> matching the given name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        /// <exception cref="FontFamilyNotFoundException">The collection contains no matches.</exception>
        FontFamily Get(string name, CultureInfo culture);

        /// <summary>
        /// Gets the specified font family matching the given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when searching for a match.</param>
        /// <param name="family">
        /// When this method returns, contains the family associated with the specified name,
        /// if the name is found; otherwise, the default value for the type of the family parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IReadOnlyFontCollection"/> contains a family
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        bool TryGet(string name, CultureInfo culture, out FontFamily family);
    }
}

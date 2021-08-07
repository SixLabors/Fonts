// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readonly collection of fonts.
    /// </summary>
    public interface IReadOnlyFontCollection2
    {
        /// <summary>
        /// Gets the collection of <see cref="IFontFamily"/> in this <see cref="FontCollection"/>
        /// using the invariant culture.
        /// </summary>
        IEnumerable<IFontFamily> Families { get; }

        /// <summary>
        /// Gets the specified font family matching the invariant culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <returns>The first <see cref="IFontFamily"/> matching the given name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        /// <exception cref="FontFamilyNotFoundException">The collection contains no matches.</exception>
        IFontFamily Get(string name);

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
        /// <see langword="true"/> if the <see cref="IReadOnlyFontCollection2"/> contains a family
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        bool TryGet(string name, [NotNullWhen(true)] out IFontFamily? family);

#if SUPPORTS_CULTUREINFO_LCID
        /// <summary>
        /// Gets the collection of <see cref="IFontFamily"/> in this <see cref="FontCollection"/>
        /// using the given culture.
        /// </summary>
        /// <param name="culture">The culture of the families to return.</param>
        /// <returns>The <see cref="IEnumerable{IFontFamily}"/>.</returns>
        IEnumerable<IFontFamily> FamiliesByCulture(CultureInfo culture);

        /// <summary>
        /// Gets the specified font family matching the given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when search for a match.</param>
        /// <returns>The first <see cref="IFontFamily"/> matching the given name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        /// <exception cref="FontFamilyNotFoundException">The collection contains no matches.</exception>
        IFontFamily Get(string name, CultureInfo culture);

        /// <summary>
        /// Gets the specified font family matching the given culture and font family name.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The culture to use when search for a match.</param>
        /// <param name="family">
        /// When this method returns, contains the family associated with the specified name,
        /// if the name is found; otherwise, the default value for the type of the family parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IReadOnlyFontCollection2"/> contains a family
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
        bool TryGet(string name, CultureInfo culture, [NotNullWhen(true)] out IFontFamily? family);
#endif
    }
}

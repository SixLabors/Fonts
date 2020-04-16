// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readonly collection of fonts.
    /// </summary>
    public interface IReadOnlyFontCollection
    {
        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/> in the invariant culture.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        IEnumerable<FontFamily> Families { get; }

#if SUPPORTS_CULTUREINFO_LCID
        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <param name="culture">The culture to use while getting the family name from the installed set of fonts.</param>
        /// <returns>The set of fonts families using the fonts culture aware font name</returns>
        IEnumerable<FontFamily> FamiliesByCulture(CultureInfo culture);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="culture">The culture to use while getting the family name from the installed set of fonts.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        FontFamily Find(string fontFamily, CultureInfo culture);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="culture">The culture to use while getting the family name from the installed set of fonts.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        bool TryFind(string fontFamily, CultureInfo culture, [NotNullWhen(true)] out FontFamily? family);
#endif

        /// <summary>
        /// Finds the specified font family using the invariant culture font family name.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        FontFamily Find(string fontFamily);

        /// <summary>
        /// Finds the specified font family using the invariant culture font family name.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        bool TryFind(string fontFamily, [NotNullWhen(true)] out FontFamily? family);
    }
}

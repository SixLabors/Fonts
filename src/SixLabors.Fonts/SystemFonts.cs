// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public static class SystemFonts
    {
        private static Lazy<IReadOnlyFontCollection> lazySystemFonts = new Lazy<IReadOnlyFontCollection>(() => new SystemFontCollection());

        /// <summary>
        /// Gets the collection containing the globaly installled system fonts.
        /// </summary>
        /// <value>
        /// The system fonts.
        /// </value>
        public static IReadOnlyFontCollection Collection => lazySystemFonts.Value;

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/>s installed on current system.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public static IEnumerable<FontFamily> Families => Collection.Families;

        /// <summary>
        /// Finds the specified font family from the system font store.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The found family.</returns>
        /// <exception cref="Exceptions.FontFamilyNotFoundException">Thrown when the font family is not found.</exception>
        public static FontFamily Find(string fontFamily) => Collection.Find(fontFamily);

        /// <summary>
        /// Finds the specified font family from the system font store.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>True if a font of that family has been installed into the font collection.</returns>
        public static bool TryFind(string fontFamily, [NotNullWhen(true)] out FontFamily? family) => Collection.TryFind(fontFamily, out family);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family.
        /// </summary>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        /// <returns>Returns instance of the <see cref="Font"/> from the current collection.</returns>
        public static Font CreateFont(string fontFamily, float size, FontStyle style) => Collection.CreateFont(fontFamily, size, style);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
        /// </summary>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        /// <returns>Returns instance of the <see cref="Font"/> from the current collection.</returns>
        public static Font CreateFont(string fontFamily, float size) => Collection.CreateFont(fontFamily, size);

#if SUPPORTS_CULTUREINFO_LCID
        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/> in the current threads culture.
        /// </summary>
        /// <param name="culture">The culture to find the list of font familes for.</param>
        /// <returns>The set of fonts families using the fonts culture aware font name</returns>
        public static IEnumerable<FontFamily> FamiliesByCulture(CultureInfo culture)
            => Collection.FamiliesByCulture(culture);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="culture">The culture to find the font from of font family for.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        public static FontFamily Find(string fontFamily, CultureInfo culture)
            => Collection.Find(fontFamily, culture);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="culture">The culture to find the font from of font family for.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        public static bool TryFind(string fontFamily, CultureInfo culture, [NotNullWhen(true)] out FontFamily? family)
            => Collection.TryFind(fontFamily, culture, out family);
#endif
    }
}

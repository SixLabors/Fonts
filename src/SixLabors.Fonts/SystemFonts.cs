// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public static class SystemFonts
    {
        private static Lazy<IReadOnlyFontCollection> lazySystemFonts = new Lazy<IReadOnlyFontCollection>(() => LoadCollection(CultureInfo.InvariantCulture));

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
        /// <returns>The family if installed otherwise null</returns>
        public static FontFamily Find(string fontFamily) => Collection.Find(fontFamily);

        /// <summary>
        /// Finds the specified font family from the system font store.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        public static bool TryFind(string fontFamily, out FontFamily family) => Collection.TryFind(fontFamily, out family);

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

        /// <summary>
        /// Creates the collection of system fonts hosted the globably installed system fonts with metadata stored in a particular culture.
        /// </summary>
        /// <param name="culture">The culture to load the fonts metadata in.</param>
        /// <returns>the newly created collection containg the loaded system fonts with metadata in the desired culture (where availible).</returns>
        public static IReadOnlyFontCollection LoadCollection(CultureInfo culture) => new SystemFontCollection(culture);
    }
}

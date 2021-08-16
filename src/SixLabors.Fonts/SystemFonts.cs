// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
#if SUPPORTS_CULTUREINFO_LCID
using System.Globalization;
#endif

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public static class SystemFonts
    {
        private static readonly Lazy<SystemFontCollection> LazySystemFonts = new Lazy<SystemFontCollection>(() => new SystemFontCollection());

        /// <summary>
        /// Gets the collection containing the globally installed system fonts.
        /// </summary>
        public static IReadOnlyFontCollection Collection => LazySystemFonts.Value;

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/>s installed on current system.
        /// </summary>
        public static IEnumerable<FontFamily> Families => Collection.Families;

        /// <inheritdoc cref="IReadOnlyFontCollection.Get(string)"/>
        public static FontFamily Get(string name) => LazySystemFonts.Value.Get(name);

        /// <inheritdoc cref="IReadOnlyFontCollection.TryGet(string, out FontFamily)" />
        public static bool TryGet(string fontFamily, out FontFamily family)
            => Collection.TryGet(fontFamily, out family);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <returns>The new <see cref="Font"/>.</returns>
        public static Font CreateFont(string name, float size)
            => Collection.Get(name).CreateFont(size);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <param name="style">The font style.</param>
        /// <returns>The new <see cref="Font"/>.</returns>
        public static Font CreateFont(string name, float size, FontStyle style)
            => Collection.Get(name).CreateFont(size, style);

#if SUPPORTS_CULTUREINFO_LCID
        /// <inheritdoc cref="IReadOnlyFontCollection.GetByCulture(CultureInfo)"/>
        public static IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
            => Collection.GetByCulture(culture);

        /// <inheritdoc cref="IReadOnlyFontCollection.Get(string, CultureInfo)" />
        public static FontFamily Get(string fontFamily, CultureInfo culture)
            => Collection.Get(fontFamily, culture);

        /// <inheritdoc cref="IReadOnlyFontCollection.TryGet(string, CultureInfo, out FontFamily)" />
        public static bool TryGet(string fontFamily, CultureInfo culture, out FontFamily family)
            => Collection.TryGet(fontFamily, culture, out family);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The font culture.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <returns>The new <see cref="Font"/>.</returns>
        public static Font CreateFont(string name, CultureInfo culture, float size)
            => Collection.Get(name, culture).CreateFont(size);

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family.
        /// </summary>
        /// <param name="name">The font family name.</param>
        /// <param name="culture">The font culture.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <param name="style">The font style.</param>
        /// <returns>The new <see cref="Font"/>.</returns>
        public static Font CreateFont(string name, CultureInfo culture, float size, FontStyle style)
            => Collection.Get(name, culture).CreateFont(size, style);
#endif
    }
}

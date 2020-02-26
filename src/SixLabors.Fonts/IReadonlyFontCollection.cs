// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
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
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        IEnumerable<FontFamily> Families { get; }

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        FontFamily Find(string fontFamily);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        bool TryFind(string fontFamily, out FontFamily family);

        /// <summary>
        /// Finds the specified font family, also by looking into locale specific names.<br/>
        /// <b>Note</b>: On targets where <see cref="CultureInfo"/>.LCID is not supported,
        /// such as .NET Standard 1.0,  it's same as <see cref="Find(string)"/>
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="preferredCulture">Preferred culture, can be null.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        FontFamily Find(string fontFamily, CultureInfo? preferredCulture);

        /// <summary>
        /// Finds the specified font family, also by looking into locale specific names.
        /// <b>Note</b>: On targets where <see cref="CultureInfo"/>.LCID is not supported,
        /// such as .NET Standard 1.0,  it's same as <see cref="TryFind(string, out FontFamily)"/>
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="preferredCulture">Preferred culture, can be null.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        bool TryFind(string fontFamily, CultureInfo? preferredCulture, out FontFamily family);
    }
}

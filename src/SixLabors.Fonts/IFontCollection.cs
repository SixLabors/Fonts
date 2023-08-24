// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readable and writable collection of fonts.
    /// </summary>
    /// <seealso cref="IReadOnlyFontCollection" />
    public interface IFontCollection : IReadOnlyFontCollection
    {
        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="path">The filesystem path to the font file.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(string path);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="path">The filesystem path to the font file.</param>
        /// <param name="description">The description of the added font.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(string path, out FontDescription description);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(Stream stream);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="description">The description of the added font.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(Stream stream, out FontDescription description);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="path">The font collection path.</param>
        /// <returns>The new <see cref="IEnumerable{FontFamily}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(string path);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="path">The font collection path.</param>
        /// <param name="descriptions">The descriptions of the added fonts.</param>
        /// <returns>The new <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(string path, out IEnumerable<FontDescription> descriptions);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <returns>The new <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(Stream stream);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="descriptions">The descriptions of the added fonts.</param>
        /// <returns>The new <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(Stream stream, out IEnumerable<FontDescription> descriptions);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="path">The filesystem path to the font file.</param>
        /// <param name="culture">The culture of the font to add.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(string path, CultureInfo culture);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="path">The filesystem path to the font file.</param>
        /// <param name="culture">The culture of the font to add.</param>
        /// <param name="description">The description of the added font.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(string path, CultureInfo culture, out FontDescription description);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="culture">The culture of the font to add.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(Stream stream, CultureInfo culture);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="culture">The culture of the font to add.</param>
        /// <param name="description">The description of the added font.</param>
        /// <returns>The new <see cref="FontFamily"/>.</returns>
        FontFamily Add(Stream stream, CultureInfo culture, out FontDescription description);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="path">The font collection path.</param>
        /// <param name="culture">The culture of the fonts to add.</param>
        /// <returns>The new <see cref="IEnumerable{FontFamily}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(string path, CultureInfo culture);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="path">The font collection path.</param>
        /// <param name="culture">The culture of the fonts to add.</param>
        /// <param name="descriptions">The descriptions of the added fonts.</param>
        /// <returns>The new <see cref="IEnumerable{FontFamily}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(
            string path,
            CultureInfo culture,
            out IEnumerable<FontDescription> descriptions);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="culture">The culture of the fonts to add.</param>
        /// <returns>The new <see cref="IEnumerable{FontFamily}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(Stream stream, CultureInfo culture);

        /// <summary>
        /// Adds a true type font collection (.ttc).
        /// </summary>
        /// <param name="stream">The font stream.</param>
        /// <param name="culture">The culture of the fonts to add.</param>
        /// <param name="descriptions">The descriptions of the added fonts.</param>
        /// <returns>The new <see cref="IEnumerable{FontFamily}"/>.</returns>
        public IEnumerable<FontFamily> AddCollection(
            Stream stream,
            CultureInfo culture,
            out IEnumerable<FontDescription> descriptions);
    }
}

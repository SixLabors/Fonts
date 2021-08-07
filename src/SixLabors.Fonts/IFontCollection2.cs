// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readable and writable collection of fonts.
    /// </summary>
    /// <seealso cref="IReadOnlyFontCollection2" />
    public interface IFontCollection2 : IReadOnlyFontCollection2
    {
        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="path">The filesystem path to the font file.</param>
        /// <returns>The newly added <see cref="FileFontFamily"/>.</returns>
        FileFontFamily Add(string path);

        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>The newly added <see cref="FontFamily2"/>.</returns>
        FontFamily2 Add(Stream fontStream);
    }
}

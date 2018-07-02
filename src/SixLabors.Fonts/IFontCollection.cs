// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readable and writable collection of fonts.
    /// </summary>
    /// <seealso cref="SixLabors.Fonts.IReadOnlyFontCollection" />
    public interface IFontCollection : IReadOnlyFontCollection
    {
        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>the description of the font just loaded.</returns>
        FontFamily Install(Stream fontStream);
    }
}

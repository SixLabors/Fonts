// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a group of type faces having a similar basic design and certain
    /// variations in styles.
    /// </summary>
    public interface IFontFamily
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the culture this instance was extracted against.
        /// </summary>
        CultureInfo Culture { get; }

        /// <summary>
        /// Gets the collection of <see cref="FontStyle"/> that are currently available.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{FontStyle}"/>.</returns>
        IEnumerable<FontStyle> AvailableStyles();
    }

    /// <summary>
    /// Defines a group of type faces loaded from a given filesystem path having a similar basic design
    /// and certain variations in styles.
    /// </summary>
    public interface IFileFontFamily : IFontFamily
    {
        /// <summary>
        /// Gets the filesystem path to the font family source.
        /// </summary>
        string Path { get; }
    }
}

// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Base class for exceptions thrown by this library.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class FontFamilyNotFoundException : FontException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamilyNotFoundException"/> class.
        /// </summary>
        /// <param name="family">The name of the missing font family.</param>
        public FontFamilyNotFoundException(string family)
            : base($"{family} could not be found")
        {
            this.FontFamily = family;
        }

        /// <summary>
        /// Gets the name of the font familiy we failed to find.
        /// </summary>
        public string FontFamily { get; }
    }
}